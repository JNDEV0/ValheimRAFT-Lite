using System;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Components;
using ValheimVehicles.Controllers;
using ValheimVehicles.Helpers;
using ValheimVehicles.Shared.Constants;
using Zolantris.Shared;

namespace ValheimVehicles.Patches;

[HarmonyPatch]
public static class BoatBedSpawn_Patches
{
  public static int GetBedVehicleId(Bed bed)
  {
    if (bed == null) return 0;
    var bedZdo = bed.m_nview != null ? bed.m_nview.GetZDO() : null;
    int vehicleId = 0;

    if (bedZdo != null)
    {
      vehicleId = bedZdo.GetInt(VehicleZdoVars.MBParentId, 0);
      if (vehicleId == 0)
      {
        vehicleId = VehiclePiecesController.GetParentID(bedZdo);
      }

      if (vehicleId != 0 && VehiclePiecesController.ActiveInstances.TryGetValue(vehicleId, out var activeVpc) && activeVpc != null)
      {
        if (!activeVpc.m_bedPieces.Contains(bed) && !bed.transform.IsChildOf(activeVpc.transform))
        {
          if (bed.m_nview != null && bed.m_nview.IsOwner())
          {
            bedZdo.RemoveInt(VehicleZdoVars.MBParentId);
          }
          vehicleId = 0;
        }
      }
    }

    if (vehicleId == 0)
    {
      var vpc = bed.GetComponentInParent<VehiclePiecesController>();
      if (vpc != null) vehicleId = vpc.PersistentZdoId;
    }

    if (vehicleId == 0)
    {
      var vm = bed.GetComponentInParent<VehicleManager>();
      if (vm != null) vehicleId = vm.PersistentZdoId;
    }

    if (vehicleId == 0)
    {
      foreach (var kvp in VehiclePiecesController.ActiveInstances)
      {
        if (kvp.Value != null && kvp.Value.m_bedPieces.Contains(bed))
        {
          vehicleId = kvp.Key;
          break;
        }
      }
    }

    return vehicleId;
  }

  [HarmonyPatch(typeof(Bed), nameof(Bed.Interact))]
  [HarmonyPostfix]
  public static void Bed_Interact_Postfix(Bed __instance, Humanoid human, bool repeat, bool alt, bool __result)
  {
    if (repeat) return;
    if (human != Player.m_localPlayer) return;

    var vehicleId = GetBedVehicleId(__instance);

    if (vehicleId != 0)
    {
      if (__instance.IsMine())
      {
        var bedZdo = __instance.m_nview != null ? __instance.m_nview.GetZDO() : null;
        if (bedZdo != null && bedZdo.GetInt(VehicleZdoVars.MBParentId, 0) == 0)
        {
          bedZdo.Set(VehicleZdoVars.MBParentId, vehicleId);
        }

        if (VehiclePiecesController.ActiveInstances.TryGetValue(vehicleId, out var vpc) && vpc != null)
        {
          var localPos = vpc.transform.InverseTransformPoint(__instance.GetSpawnPoint());
          if (bedZdo != null)
          {
            bedZdo.Set(VehicleZdoVars.MBPositionHash, localPos);
          }
        }

        BoatBedSpawnController.SetBoatSpawn(__instance, vehicleId);
      }
    }
    else
    {
      // Interacted with an off-vehicle / land bed!
      if (__instance.IsMine())
      {
        BoatBedSpawnController.ClearBoatSpawn();
      }
    }
  }

  [HarmonyPatch(typeof(Bed), nameof(Bed.IsCurrent))]
  [HarmonyPrefix]
  public static bool Bed_IsCurrent_Prefix(Bed __instance, ref bool __result)
  {
    if (!__instance.IsMine()) return true;

    var vehicleId = GetBedVehicleId(__instance);

    if (vehicleId != 0 && BoatBedSpawnController.IsBoatSpawnActiveForVehicle(vehicleId))
    {
      __result = true;
      return false; // Skip vanilla 1-meter check on moving boats!
    }

    return true;
  }

  [HarmonyPatch(typeof(Bed), nameof(Bed.GetHoverText))]
  [HarmonyPostfix]
  public static void Bed_GetHoverText_Postfix(Bed __instance, ref string __result)
  {
    var vehicleId = GetBedVehicleId(__instance);

    if (vehicleId != 0)
    {
      bool isBoatSpawn = BoatBedSpawnController.IsBoatSpawnActiveForVehicle(vehicleId);

      // If boat bed spawn is active and player owns this bed, replace "Set spawn point" with "Sleep"
      if (isBoatSpawn && __instance.IsMine())
      {
        var setSpawnToken = Localization.instance != null ? Localization.instance.Localize("$piece_bed_setspawn") : "Set spawn point";
        var sleepToken = Localization.instance != null ? Localization.instance.Localize("$piece_bed_sleep") : "Sleep";
        if (__result.Contains(setSpawnToken))
        {
          __result = __result.Replace(setSpawnToken, sleepToken);
        }
      }

      // Add status line: "Boat bed spawn: Active" / "Boat bed spawn: Inactive"
      string statusText = isBoatSpawn ? "<color=green>Active</color>" : "<color=orange>Inactive</color>";
      __result += $"\nBoat bed spawn: {statusText}";
    }
  }

  [HarmonyPatch(typeof(Piece), nameof(Piece.OnDestroy))]
  [HarmonyPostfix]
  public static void Piece_OnDestroy_Postfix(Piece __instance)
  {
    if (__instance == null || __instance.m_nview == null) return;
    var bed = __instance.GetComponent<Bed>();
    if (bed == null) return;

    var bedZdo = __instance.m_nview.GetZDO();
    if (bedZdo == null) return;

    var boundUid = BoatBedSpawnController.GetBoundBedUid();
    if (boundUid != ZDOID.None && bedZdo.m_uid == boundUid)
    {
      LoggerProvider.LogInfo($"[BoatBedSpawn] Bound bed {boundUid} was destroyed. Clearing boat spawn.");
      BoatBedSpawnController.ClearBoatSpawn();
      Game.instance?.GetPlayerProfile()?.ClearCustomSpawnPoint();
    }
  }

  [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.FindFloor))]
  [HarmonyPrefix]
  public static bool ZoneSystem_FindFloor_Prefix(ZoneSystem __instance, Vector3 p, ref float height, ref bool __result)
  {
    // Check if there is a vehicle deck/piece under this point
    var mask = __instance.m_solidRayMask | (1 << LayerHelpers.CustomRaftLayer);
    if (Physics.Raycast(p + Vector3.up * 1.5f, Vector3.down, out var hit, 1000f, mask))
    {
      height = hit.point.y;
      __result = true;
      return false; // Found floor on vehicle deck or terrain!
    }
    return true; // Let vanilla handle
  }

  [HarmonyPatch(typeof(Game), "FindSpawnPoint")]
  [HarmonyPrefix]
  public static bool FindSpawnPoint_Prefix(Game __instance, ref Vector3 point, ref bool usedLogoutPoint, float dt, ref bool __result)
  {
    var boundVehicleId = BoatBedSpawnController.GetBoundVehicleId();
    if (boundVehicleId == 0) return true; // Land bed / vanilla spawn

    if (!VehicleRecallController.GetVehicleLocation(boundVehicleId, out var targetPos, out var targetRot, out var isLoaded, out var vm))
    {
      LoggerProvider.LogWarning($"[BoatBedSpawn] Vehicle #{boundVehicleId} not found; clearing boat spawn.");
      BoatBedSpawnController.ClearBoatSpawn();
      var profile = __instance.GetPlayerProfile();
      profile?.ClearCustomSpawnPoint();
      return true; // Let vanilla fallback to world altar
    }

    var offset = BoatBedSpawnController.GetBoundBedOffset();
    var bedWorldPos = targetPos + targetRot * offset + Vector3.up * 0.5f;

    var p = __instance.GetPlayerProfile();
    p?.SetCustomSpawnPoint(bedWorldPos);

    __instance.m_respawnWait += dt;
    usedLogoutPoint = false;

    if (ZNet.instance != null)
    {
      ZNet.instance.SetReferencePosition(bedWorldPos);
    }

    bool areaReady = ZNetScene.instance != null && ZNetScene.instance.IsAreaReady(bedWorldPos);
    if ((__instance.m_respawnWait > __instance.m_respawnLoadDuration && areaReady) || __instance.m_respawnWait > 20f)
    {
      point = bedWorldPos;
      __result = true;
      return false; // Successfully found boat bed spawn point! Skip vanilla FindBedNearby!
    }

    point = Vector3.zero;
    __result = false;
    return false; // Still waiting for area load
  }

  [HarmonyPatch(typeof(Minimap), nameof(Minimap.UpdatePins))]
  [HarmonyPostfix]
  public static void Minimap_UpdatePins_Postfix(Minimap __instance)
  {
    BoatBedSpawnController.UpdateMinimapBedPin(__instance);
  }

  [HarmonyPatch(typeof(Game), nameof(Game.SpawnPlayer))]
  [HarmonyPostfix]
  public static void SpawnPlayer_Postfix(Game __instance, Player __result)
  {
    if (__result == null || __result != Player.m_localPlayer) return;
    var boundVehicleId = BoatBedSpawnController.GetBoundVehicleId();
    if (boundVehicleId == 0) return;

    if (VehiclePiecesController.ActiveInstances.TryGetValue(boundVehicleId, out var vpc) && vpc != null)
    {
      if (vpc.Manager != null && vpc.Manager.OnboardController != null)
      {
        vpc.Manager.OnboardController.AddPlayerToLocalShip(__result);
      }
      if (__result.m_body != null && !__result.m_body.isKinematic)
      {
        __result.m_body.linearVelocity = Vector3.zero;
      }
    }
  }
}