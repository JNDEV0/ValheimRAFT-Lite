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
  public const string Key_BoatSpawnBedUid = "VR_BoatSpawnBedUid";
  public const string Key_BoatSpawnBedOffsetX = "VR_BoatSpawnBedOffsetX";
  public const string Key_BoatSpawnBedOffsetY = "VR_BoatSpawnBedOffsetY";
  public const string Key_BoatSpawnBedOffsetZ = "VR_BoatSpawnBedOffsetZ";

  public static int GetBoundVehicleId()
  {
    if (Player.m_localPlayer == null) return 0;

    // 1. Check customData (persisted in character .fch file)
    if (Player.m_localPlayer.m_customData != null &&
        Player.m_localPlayer.m_customData.TryGetValue(Key_BoatSpawnVehicleId, out var valStr) &&
        int.TryParse(valStr, out var vId) && vId != 0)
    {
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
    var playerZdo = Player.m_localPlayer.m_nview != null ? Player.m_localPlayer.m_nview.GetZDO() : null;
    if (playerZdo != null)
    {
      return playerZdo.GetZDOID(Key_BoatSpawnBedUid);
    }
    return ZDOID.None;
  }

  public static bool IsBoatSpawnActiveForVehicle(int vehicleId)
  {
    if (vehicleId == 0) return false;
    return GetBoundVehicleId() == vehicleId;
  }

  public static void SetBoatSpawn(Bed bed, int vehicleId)
  {
    if (Player.m_localPlayer == null || bed == null) return;

    var bedZdo = bed.m_nview != null ? bed.m_nview.GetZDO() : null;
    var bedUid = bedZdo != null ? bedZdo.m_uid : ZDOID.None;
    var localOffset = bedZdo != null ? bedZdo.GetVec3(VehicleZdoVars.MBPositionHash, Vector3.zero) : Vector3.zero;

    if (Player.m_localPlayer.m_customData != null)
    {
      Player.m_localPlayer.m_customData[Key_BoatSpawnVehicleId] = vehicleId.ToString();
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
    if (minimap == null || minimap.m_spawnPointPin == null)
      return;

    var boundVehicleId = GetBoundVehicleId();
    if (boundVehicleId == 0)
    {
      // Land bed or no bed: vanilla handles m_spawnPointPin normally at land bed
      return;
    }

    var profile = Game.instance?.GetPlayerProfile();
    if (profile == null || !profile.HaveCustomSpawnPoint())
    {
      ClearBoatSpawn();
      return;
    }

    if (!VehicleRecallController.GetVehicleLocation(boundVehicleId, out var targetPos, out var targetRot, out _, out _))
    {
      // Boat was sunk, destroyed, or not found
      ClearBoatSpawn();
      return;
    }

    var expectedBedPos = targetPos + targetRot * GetBoundBedOffset();

    // If player sets another bed offvehicle as spawn, profile spawn point will be far from the boat (> 35m)
    var currentCustomSpawn = profile.GetCustomSpawnPoint();
    if (Vector3.Distance(currentCustomSpawn, expectedBedPos) > 35f)
    {
      ClearBoatSpawn();
      return;
    }

    // Keep world coordinates synced to boat location so vanilla IsPointVisible doesn't destroy the pin marker
    profile.SetCustomSpawnPoint(expectedBedPos);
    minimap.m_spawnPointPin.m_pos = expectedBedPos;

    if (minimap.m_spawnPointPin.m_uiElement == null || !minimap.m_spawnPointPin.m_uiElement.gameObject.activeInHierarchy)
    {
      return;
    }

    var vehiclePin = MapPinSync.Instance?.GetVehiclePin(boundVehicleId);
    if (vehiclePin == null || vehiclePin.m_uiElement == null || !vehiclePin.m_uiElement.gameObject.activeInHierarchy)
    {
      return;
    }

    // Dynamic offset based on minimap pin size:
    var pinSize = minimap.m_mode == Minimap.MapMode.Large ? minimap.m_pinSizeLarge : minimap.m_pinSizeSmall;
    var offsetY = pinSize * 0.85f; // Positioned cleanly above the boat/plane icon
    if (MinimapConfig.BoatBedPinOffsetPosition != null &&
        MinimapConfig.BoatBedPinOffsetPosition.Value == MinimapConfig.BoatBedPinPosition.UnderVehicleText)
    {
      offsetY = -pinSize * 1.15f; // Positioned under the ValheimRAFT vehicle name text
    }

    var vehGuiPos = vehiclePin.m_uiElement.anchoredPosition;
    minimap.m_spawnPointPin.m_uiElement.anchoredPosition = vehGuiPos + new Vector2(0f, offsetY);
    minimap.m_spawnPointPin.m_uiElement.gameObject.SetActive(true);

    // Hide any name text on the bed pin so only the clean bed icon appears
    if (minimap.m_spawnPointPin.m_NamePinData?.PinNameGameObject != null)
    {
      minimap.m_spawnPointPin.m_NamePinData.PinNameGameObject.SetActive(false);
    }
  }
}
