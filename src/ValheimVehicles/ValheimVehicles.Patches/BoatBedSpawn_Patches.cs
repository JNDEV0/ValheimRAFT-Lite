using System;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Controllers;
using ValheimVehicles.Helpers;
using ValheimVehicles.Shared.Constants;
using Zolantris.Shared;

namespace ValheimVehicles.Patches;

[HarmonyPatch]
public static class BoatBedSpawn_Patches
{
  [HarmonyPatch(typeof(Bed), nameof(Bed.Interact))]
  [HarmonyPostfix]
  public static void Bed_Interact_Postfix(Bed __instance, Humanoid user, bool __result)
  {
    if (user != Player.m_localPlayer) return;

    var profile = Game.instance != null ? Game.instance.GetPlayerProfile() : null;
    if (profile == null) return;

    // Check if the bed was just set as custom spawn, or if it is currently player's bed
    var isSpawnSet = Vector3.Distance(profile.GetCustomSpawnPoint(), __instance.GetSpawnPoint()) < 0.2f;
    if (!isSpawnSet && !__instance.IsMine()) return;

    var bedZdo = __instance.m_nview != null ? __instance.m_nview.GetZDO() : null;
    var vehicleId = bedZdo != null ? bedZdo.GetInt(VehicleZdoVars.MBParentId, 0) : 0;
    if (vehicleId == 0)
    {
      var vpc = __instance.GetComponentInParent<VehiclePiecesController>();
      if (vpc != null) vehicleId = vpc.PersistentZdoId;
    }

    if (vehicleId != 0)
    {
      BoatBedSpawnController.SetBoatSpawn(__instance, vehicleId);
    }
    else
    {
      // Bound to an off-vehicle / land bed!
      BoatBedSpawnController.ClearBoatSpawn();
    }
  }

  [HarmonyPatch(typeof(Bed), nameof(Bed.IsCurrent))]
  [HarmonyPrefix]
  public static bool Bed_IsCurrent_Prefix(Bed __instance, ref bool __result)
  {
    if (!__instance.IsMine()) return true;

    var bedZdo = __instance.m_nview != null ? __instance.m_nview.GetZDO() : null;
    if (bedZdo == null) return true;

    var vehicleId = bedZdo.GetInt(VehicleZdoVars.MBParentId, 0);
    if (vehicleId == 0)
    {
      var vpc = __instance.GetComponentInParent<VehiclePiecesController>();
      if (vpc != null) vehicleId = vpc.PersistentZdoId;
    }

    if (vehicleId != 0 && BoatBedSpawnController.IsBoatSpawnActiveForVehicle(vehicleId))
    {
      __result = true;
      return false; // Skip vanilla 1-meter check!
    }

    return true;
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

  [HarmonyPatch(typeof(Game), "FindSpawnPoint")]
  [HarmonyPrefix]
  public static void FindSpawnPoint_Prefix(Game __instance)
  {
    var boundVehicleId = BoatBedSpawnController.GetBoundVehicleId();
    if (boundVehicleId == 0) return;

    if (VehicleRecallController.GetVehicleLocation(boundVehicleId, out var targetPos, out var targetRot, out var isLoaded, out var vm))
    {
      var offset = BoatBedSpawnController.GetBoundBedOffset();
      var bedWorldPos = targetPos + targetRot * offset + Vector3.up * 0.5f;

      var profile = __instance.GetPlayerProfile();
      if (profile != null)
      {
        profile.SetCustomSpawnPoint(bedWorldPos);
      }
    }
    else
    {
      LoggerProvider.LogWarning($"[BoatBedSpawn] Vehicle #{boundVehicleId} not found; clearing boat spawn.");
      BoatBedSpawnController.ClearBoatSpawn();
      var profile = __instance.GetPlayerProfile();
      profile?.ClearCustomSpawnPoint();
    }
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
      if (__result.m_body != null)
      {
        __result.m_body.velocity = Vector3.zero;
      }
    }
  }
}
