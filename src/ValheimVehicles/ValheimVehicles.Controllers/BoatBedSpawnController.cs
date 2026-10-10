using System;
using UnityEngine;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.Components;
using ValheimVehicles.Helpers;
using ValheimVehicles.Shared.Constants;
using ValheimVehicles.SharedScripts;
using ZdoWatcher;
using Zolantris.Shared;

namespace ValheimVehicles.Controllers;

public static class BoatBedSpawnController
{
  public const string Key_BoatSpawnVehicleId = "VR_BoatSpawnVehicleId";
  public const string Key_BoatSpawnWorldUid = "VR_BoatSpawnWorldUid";
  public const string Key_BoatSpawnBedUid = "VR_BoatSpawnBedUid";
  public const string Key_BoatSpawnBedOffsetX = "VR_BoatSpawnBedOffsetX";
  public const string Key_BoatSpawnBedOffsetY = "VR_BoatSpawnBedOffsetY";
  public const string Key_BoatSpawnBedOffsetZ = "VR_BoatSpawnBedOffsetZ";

  private static float _lastBedPinUpdateTime;

  public static int GetBoundVehicleId()
  {
    if (Player.m_localPlayer == null) return 0;

    // 0. If current player profile does not have a custom spawn point in this world, they have no bed here!
    if (Game.instance != null && Game.instance.GetPlayerProfile() != null && !Game.instance.GetPlayerProfile().HaveCustomSpawnPoint())
    {
      return 0;
    }

    // 1. Check customData (persisted in character .fch file)
    if (Player.m_localPlayer.m_customData != null &&
        Player.m_localPlayer.m_customData.TryGetValue(Key_BoatSpawnVehicleId, out var valStr) &&
        int.TryParse(valStr, out var vId) && vId != 0)
    {
      // If bound to a specific world, ensure current world matches
      if (Player.m_localPlayer.m_customData.TryGetValue(Key_BoatSpawnWorldUid, out var wStr) &&
          long.TryParse(wStr, out var wUid) && wUid != 0L &&
          ZNet.instance != null && ZNet.instance.GetWorldUID() != 0L)
      {
        if (wUid != ZNet.instance.GetWorldUID())
        {
          return 0; // Bed was bound in a different world!
        }
      }

      return vId;
    }

    // 2. Check ZDO
    var zdo = Player.m_localPlayer.m_nview != null ? Player.m_localPlayer.m_nview.GetZDO() : null;
    if (zdo != null)
    {
      return zdo.GetInt(Key_BoatSpawnVehicleId, 0);
    }

    return 0;
  }

  public static ZDOID GetBoundBedUid()
  {
    if (Player.m_localPlayer == null) return ZDOID.None;

    if (Player.m_localPlayer.m_customData != null &&
        Player.m_localPlayer.m_customData.TryGetValue(Key_BoatSpawnBedUid, out var uidStr) &&
        !string.IsNullOrEmpty(uidStr))
    {
      var parts = uidStr.Split('_');
      if (parts.Length == 2 && long.TryParse(parts[0], out var u) && uint.TryParse(parts[1], out var id))
      {
        return new ZDOID(u, id);
      }
    }

    var playerZdo = Player.m_localPlayer.m_nview != null ? Player.m_localPlayer.m_nview.GetZDO() : null;
    if (playerZdo != null)
    {
      return playerZdo.GetZDOID(Key_BoatSpawnBedUid);
    }
    return ZDOID.None;
  }

  public static bool VehicleHasOnboardBed(int vehicleId)
  {
    if (vehicleId == 0) return false;

    // 1. If vehicle is loaded, check active pieces controller
    if (VehiclePiecesController.ActiveInstances.TryGetValue(vehicleId, out var vpc) && vpc != null)
    {
      if (vpc.m_bedPieces != null && vpc.m_bedPieces.Count > 0)
      {
        foreach (var bed in vpc.m_bedPieces)
        {
          if (bed != null) return true;
        }
      }
      return false; // Loaded vehicle has no bed pieces onboard
    }

    // 2. If vehicle is not loaded, verify if the bound bed UID exists and is parented to this vehicle
    var boundBedUid = GetBoundBedUid();
    if (boundBedUid != ZDOID.None && ZDOMan.instance != null)
    {
      var bedZdo = ZDOMan.instance.GetZDO(boundBedUid);
      if (bedZdo != null && bedZdo.IsValid())
      {
        var parentId = bedZdo.GetInt(VehicleZdoVars.MBParentId, 0);
        if (parentId == 0) parentId = VehiclePiecesController.GetParentID(bedZdo);
        if (parentId == vehicleId) return true;
      }
    }

    return false;
  }

  public static bool IsBoatSpawnActiveForVehicle(int vehicleId)
  {
    if (vehicleId == 0) return false;
    if (GetBoundVehicleId() != vehicleId) return false;
    return VehicleHasOnboardBed(vehicleId);
  }

  public static void SetBoatSpawn(Bed bed, int vehicleId)
  {
    if (Player.m_localPlayer == null || bed == null) return;

    var bedZdo = bed.m_nview != null ? bed.m_nview.GetZDO() : null;
    var bedUid = bedZdo != null ? bedZdo.m_uid : ZDOID.None;
    var localOffset = bedZdo != null ? bedZdo.GetVec3(VehicleZdoVars.MBPositionHash, Vector3.zero) : Vector3.zero;

    if (localOffset == Vector3.zero)
    {
      if (VehicleRecallController.GetVehicleLocation(vehicleId, out var vPos, out var vRot, out _, out _))
      {
        localOffset = Quaternion.Inverse(vRot) * (bed.GetSpawnPoint() - vPos);
      }
    }

    var worldUid = ZNet.instance != null ? ZNet.instance.GetWorldUID() : 0L;
    if (Player.m_localPlayer.m_customData != null)
    {
      Player.m_localPlayer.m_customData[Key_BoatSpawnVehicleId] = vehicleId.ToString();
      Player.m_localPlayer.m_customData[Key_BoatSpawnWorldUid] = worldUid.ToString();
      Player.m_localPlayer.m_customData[Key_BoatSpawnBedUid] = $"{bedUid.UserID}_{bedUid.ID}";
      Player.m_localPlayer.m_customData[Key_BoatSpawnBedOffsetX] = localOffset.x.ToString("F3");
      Player.m_localPlayer.m_customData[Key_BoatSpawnBedOffsetY] = localOffset.y.ToString("F3");
      Player.m_localPlayer.m_customData[Key_BoatSpawnBedOffsetZ] = localOffset.z.ToString("F3");
    }

    var playerZdo = Player.m_localPlayer.m_nview != null ? Player.m_localPlayer.m_nview.GetZDO() : null;
    if (playerZdo != null)
    {
      playerZdo.Set(Key_BoatSpawnVehicleId, vehicleId);
      playerZdo.Set(Key_BoatSpawnBedUid, bedUid);
      playerZdo.Set(VehicleZdoVars.MBPositionHash, localOffset);
    }

    // Set immediate custom spawn point in player profile
    if (Game.instance != null && Game.instance.GetPlayerProfile() != null)
    {
      Game.instance.GetPlayerProfile().SetCustomSpawnPoint(bed.GetSpawnPoint());
    }

    LoggerProvider.LogInfo($"[BoatBedSpawn] Player bound respawn to bed on vessel #{vehicleId} (Offset: {localOffset})");
  }

  public static void ClearBoatSpawn()
  {
    if (Player.m_localPlayer != null)
    {
      if (Player.m_localPlayer.m_customData != null)
      {
        Player.m_localPlayer.m_customData[Key_BoatSpawnVehicleId] = "0";
        Player.m_localPlayer.m_customData[Key_BoatSpawnWorldUid] = "0";
        Player.m_localPlayer.m_customData[Key_BoatSpawnBedUid] = "";
      }

      var playerZdo = Player.m_localPlayer.m_nview != null ? Player.m_localPlayer.m_nview.GetZDO() : null;
      if (playerZdo != null)
      {
        playerZdo.Set(Key_BoatSpawnVehicleId, 0);
        playerZdo.Set(Key_BoatSpawnBedUid, ZDOID.None);
      }
    }

    LoggerProvider.LogInfo("[BoatBedSpawn] Cleared boat bed spawn (reverted to land/vanilla spawn)");
  }

  public static Vector3 GetBoundBedOffset()
  {
    if (Player.m_localPlayer == null) return Vector3.zero;

    if (Player.m_localPlayer.m_customData != null &&
        Player.m_localPlayer.m_customData.TryGetValue(Key_BoatSpawnBedOffsetX, out var sx) && float.TryParse(sx, out var x) &&
        Player.m_localPlayer.m_customData.TryGetValue(Key_BoatSpawnBedOffsetY, out var sy) && float.TryParse(sy, out var y) &&
        Player.m_localPlayer.m_customData.TryGetValue(Key_BoatSpawnBedOffsetZ, out var sz) && float.TryParse(sz, out var z))
    {
      return new Vector3(x, y, z);
    }

    var playerZdo = Player.m_localPlayer.m_nview != null ? Player.m_localPlayer.m_nview.GetZDO() : null;
    if (playerZdo != null)
    {
      return playerZdo.GetVec3(VehicleZdoVars.MBPositionHash, Vector3.zero);
    }

    return Vector3.zero;
  }

  public static void UpdateMinimapBedPin(Minimap minimap)
  {
    if (minimap == null) return;

    if (Time.time - _lastBedPinUpdateTime < 0.5f) return;
    _lastBedPinUpdateTime = Time.time;

    var boundVehicleId = GetBoundVehicleId();
    if (boundVehicleId == 0) return;

    // Strictly ensure the vehicle actually has an onboard bed before targeting it for the map pin!
    if (!VehicleHasOnboardBed(boundVehicleId))
    {
      if (VehiclePiecesController.ActiveInstances.ContainsKey(boundVehicleId))
      {
        ClearBoatSpawn();
      }
      return;
    }

    if (VehicleRecallController.GetVehicleLocation(boundVehicleId, out var targetPos, out var targetRot, out _, out _))
    {
      var offset = GetBoundBedOffset();
      var bedWorldPos = targetPos + targetRot * offset;

      if (Game.instance?.GetPlayerProfile() != null)
      {
        Game.instance.GetPlayerProfile().SetCustomSpawnPoint(bedWorldPos);
      }

      if (minimap.m_spawnPointPin != null)
      {
        minimap.m_spawnPointPin.m_pos = bedWorldPos;
        if (minimap.m_spawnPointPin.m_uiElement != null)
        {
          minimap.m_spawnPointPin.m_uiElement.gameObject.SetActive(true);
        }
        if (minimap.m_spawnPointPin.m_NamePinData?.PinNameGameObject != null)
        {
          minimap.m_spawnPointPin.m_NamePinData.PinNameGameObject.SetActive(true);
        }
      }
    }
    else
    {
      if (!VehicleRecallController.DoesVehicleExist(boundVehicleId))
      {
        ClearBoatSpawn();
      }
    }
  }
}
