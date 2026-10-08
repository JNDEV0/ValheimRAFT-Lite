using System;
using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.Components;
using ValheimVehicles.Controllers;
using ValheimVehicles.Helpers;
using ValheimVehicles.Patches;
using ValheimVehicles.Propulsion.Rudder;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.Enums;
using ValheimVehicles.Shared.Constants;
using ZdoWatcher;
using Zolantris.Shared;
using Object = UnityEngine.Object;

namespace ValheimVehicles.Controllers;

public static class VehicleRecallController
{
  public const string AttunedVesselZdoKey = "VR_AttunedVesselId";

  public static int ResolveTargetVehicle(Player player)
  {
    if (player == null) return 0;

    // 1. Check if player has explicitly attuned a vehicle ID
    var playerZdo = player.m_nview != null ? player.m_nview.GetZDO() : null;
    if (playerZdo != null)
    {
      var attunedId = playerZdo.GetInt(AttunedVesselZdoKey, 0);
      if (attunedId != 0 && DoesVehicleExist(attunedId))
      {
        if (LoopTracker.Enabled) LoggerProvider.LogInfo($"[VesselRecall] Found explicitly attuned vessel ID: {attunedId}");
        return attunedId;
      }
    }

    // 2. Check if player is currently standing on a vehicle
    var vpcOnPlayer = player.GetComponentInParent<VehiclePiecesController>() ??
                      player.transform.root.GetComponentInChildren<VehiclePiecesController>();
    if (vpcOnPlayer != null && vpcOnPlayer.PersistentZdoId != 0)
    {
      if (LoopTracker.Enabled) LoggerProvider.LogInfo($"[VesselRecall] Player is standing on vessel ID: {vpcOnPlayer.PersistentZdoId}");
      return vpcOnPlayer.PersistentZdoId;
    }

    var playerId = player.GetPlayerID();

    // 3. Check loaded vehicles created by this player
    foreach (var kvp in VehicleManager.VehicleInstances)
    {
      var vm = kvp.Value;
      if (vm != null && vm.m_nview != null && vm.m_nview.GetZDO() != null)
      {
        var zdo = vm.m_nview.GetZDO();
        if (zdo.GetLong(ZDOVars.s_creator, 0) == playerId)
        {
          if (LoopTracker.Enabled) LoggerProvider.LogInfo($"[VesselRecall] Found loaded vessel ID {kvp.Key} created by player ({playerId})");
          return kvp.Key;
        }
      }
    }

    // 4. Scan all ZDOs in ZDOMan for vehicles created by this player
    if (ZDOMan.instance != null && ZDOMan.instance.m_objectsByID != null)
    {
      var waterShipPrefab = PrefabNames.WaterVehicleShip.GetStableHashCode();
      var landShipPrefab = PrefabNames.LandVehicle.GetStableHashCode();

      foreach (var kvp in ZDOMan.instance.m_objectsByID)
      {
        var zdo = kvp.Value;
        if (zdo == null || !zdo.IsValid()) continue;
        var prefab = zdo.GetPrefab();
        if (prefab == waterShipPrefab || prefab == landShipPrefab)
        {
          if (zdo.GetLong(ZDOVars.s_creator, 0) == playerId)
          {
            var pId = zdo.GetInt(ZdoVarController.PersistentUidHash, 0);
            if (pId != 0)
            {
              if (LoopTracker.Enabled) LoggerProvider.LogInfo($"[VesselRecall] Found distant ZDO vessel ID {pId} created by player ({playerId})");
              return pId;
            }
          }
        }
      }
    }

    if (LoopTracker.Enabled) LoggerProvider.LogInfo("[VesselRecall] No attuned or created vessel found for player.");
    return 0;
  }

  public static int GetVehicleIdFromWheel(SteeringWheelComponent wheel)
  {
    if (wheel == null) return 0;

    if (wheel.ControllersInstance?.Manager != null && wheel.ControllersInstance.Manager.PersistentZdoId != 0)
    {
      return wheel.ControllersInstance.Manager.PersistentZdoId;
    }

    var vpc = wheel.GetComponentInParent<VehiclePiecesController>() ??
              wheel.transform.root.GetComponentInChildren<VehiclePiecesController>();
    if (vpc != null && vpc.PersistentZdoId != 0)
    {
      return vpc.PersistentZdoId;
    }

    var vm = wheel.GetComponentInParent<VehicleManager>() ??
             wheel.transform.root.GetComponentInChildren<VehicleManager>();
    if (vm != null && vm.PersistentZdoId != 0)
    {
      return vm.PersistentZdoId;
    }

    var nv = wheel.GetComponentInParent<ZNetView>();
    if (nv != null && nv.GetZDO() != null)
    {
      var parentId = nv.GetZDO().GetInt(VehicleZdoVars.MBParentId, 0);
      if (parentId != 0) return parentId;

      var pId = nv.GetZDO().GetInt(ZdoVarController.PersistentUidHash, 0);
      if (pId != 0) return pId;
    }

    return 0;
  }

  public static bool DoesVehicleExist(int vehicleId)
  {
    if (vehicleId == 0) return false;
    if (VehicleManager.VehicleInstances.ContainsKey(vehicleId)) return true;
    if (ZdoWatchController.Instance != null && ZdoWatchController.Instance.GetZdo(vehicleId) != null) return true;

    if (ZDOMan.instance != null && ZDOMan.instance.m_objectsByID != null)
    {
      foreach (var kvp in ZDOMan.instance.m_objectsByID)
      {
        if (kvp.Value != null && kvp.Value.GetInt(ZdoVarController.PersistentUidHash, 0) == vehicleId)
        {
          return true;
        }
      }
    }
    return false;
  }

  public static bool GetVehicleLocation(int vehicleId, out Vector3 position, out Quaternion rotation, out bool isLoaded, out VehicleManager? vehicleManager)
  {
    position = Vector3.zero;
    rotation = Quaternion.identity;
    isLoaded = false;
    vehicleManager = null;

    if (vehicleId == 0) return false;

    if (VehicleManager.VehicleInstances.TryGetValue(vehicleId, out vehicleManager) && vehicleManager != null)
    {
      position = vehicleManager.transform.position;
      rotation = vehicleManager.transform.rotation;
      isLoaded = true;
      if (LoopTracker.Enabled) LoggerProvider.LogInfo($"[VesselRecall] Vehicle #{vehicleId} is loaded at position {position}");
      return true;
    }

    ZDO? zdo = null;
    if (ZdoWatchController.Instance != null)
    {
      zdo = ZdoWatchController.Instance.GetZdo(vehicleId);
    }

    if (zdo == null && ZDOMan.instance != null && ZDOMan.instance.m_objectsByID != null)
    {
      foreach (var kvp in ZDOMan.instance.m_objectsByID)
      {
        if (kvp.Value != null && kvp.Value.GetInt(ZdoVarController.PersistentUidHash, 0) == vehicleId)
        {
          zdo = kvp.Value;
          break;
        }
      }
    }

    if (zdo != null && zdo.IsValid())
    {
      position = zdo.GetPosition();
      rotation = zdo.GetRotation();
      isLoaded = false;
      if (LoopTracker.Enabled) LoggerProvider.LogInfo($"[VesselRecall] Vehicle #{vehicleId} is unloaded (ZDO sector: {zdo.GetSectorIndex()}) at position {position}");
      return true;
    }

    if (LoopTracker.Enabled) LoggerProvider.LogInfo($"[VesselRecall] Could not find location for vehicle #{vehicleId}");
    return false;
  }

  public static bool TeleportPlayerToVehicle(Player player, int vehicleId)
  {
    if (player == null || player.IsTeleporting()) return false;

    if (!GetVehicleLocation(vehicleId, out var targetPos, out var targetRot, out var isLoaded, out var vm))
    {
      LoggerProvider.LogWarning($"[VesselRecall] Teleport failed: could not locate vessel #{vehicleId}");
      player.Message(MessageHud.MessageType.Center, "Could not locate boat position!");
      return false;
    }

    Vector3 landingPos;
    Quaternion landingRot = targetRot;
    ZDO? wheelZdo = null;

    if (isLoaded && vm != null)
    {
      var wheel = vm.GetComponentInChildren<SteeringWheelComponent>();
      if (wheel != null)
      {
        var wheelNv = wheel.GetComponent<ZNetView>();
        if (wheelNv != null && wheelNv.IsValid()) wheelZdo = wheelNv.GetZDO();
        if (wheel.AttachPoint != null)
        {
          landingPos = wheel.AttachPoint.position;
        }
        else
        {
          landingPos = wheel.transform.position - wheel.transform.forward * 0.8f;
        }
        landingRot = wheel.transform.rotation;
        if (LoopTracker.Enabled) LoggerProvider.LogInfo($"[VesselRecall] Target wheel found at {wheel.transform.position}, landing player at {landingPos}");
      }
      else if (vm.RudderObject != null)
      {
        landingPos = vm.RudderObject.transform.position + Vector3.up * 0.5f;
        landingRot = vm.RudderObject.transform.rotation;
        if (LoopTracker.Enabled) LoggerProvider.LogInfo($"[VesselRecall] Rudder found, landing player at {landingPos}");
      }
      else
      {
        landingPos = targetPos + targetRot * Vector3.up * 1.5f;
        if (LoopTracker.Enabled) LoggerProvider.LogInfo($"[VesselRecall] No wheel found, landing player at vehicle origin {landingPos}");
      }
    }
    else
    {
      // Unloaded vessel: try to find steering wheel ZDO offset
      var wheelHash = PrefabNames.ShipSteeringWheel.GetStableHashCode();

      var pieces = VehiclePiecesController.EnsurePiecesForVehicle(vehicleId);
      if (ZDOMan.instance != null)
      {
        foreach (var pzId in pieces)
        {
          var pz = ZDOMan.instance.GetZDO(pzId);
          if (pz != null && pz.IsValid() && pz.GetPrefab() == wheelHash)
          {
            wheelZdo = pz;
            break;
          }
        }
      }

      if (wheelZdo == null && ZDOMan.instance != null && ZDOMan.instance.m_objectsByID != null)
      {
        foreach (var kvp in ZDOMan.instance.m_objectsByID)
        {
          var z = kvp.Value;
          if (z != null && z.IsValid() && z.GetInt(VehicleZdoVars.MBParentId, 0) == vehicleId && z.GetPrefab() == wheelHash)
          {
            wheelZdo = z;
            break;
          }
        }
      }

      if (wheelZdo != null)
      {
        var wheelOffset = wheelZdo.GetVec3(VehicleZdoVars.MBPositionHash, Vector3.zero);
        Quaternion wheelRotOffset;
        var eulerVec = wheelZdo.GetVec3(VehicleZdoVars.MBRotationVecHash, Vector3.negativeInfinity);
        if (eulerVec != Vector3.negativeInfinity)
        {
          wheelRotOffset = Quaternion.Euler(eulerVec);
        }
        else
        {
          wheelRotOffset = wheelZdo.GetQuaternion(VehicleZdoVars.MBRotationHash, Quaternion.identity);
        }
        var wheelWorldRot = targetRot * wheelRotOffset;
        landingPos = targetPos + targetRot * wheelOffset - wheelWorldRot * Vector3.forward * 0.8f;
        landingRot = wheelWorldRot;
        if (LoopTracker.Enabled) LoggerProvider.LogInfo($"[VesselRecall] Unloaded wheel ZDO found, landing player at offset {landingPos}");
      }
      else
      {
        landingPos = targetPos + Vector3.up * 2.0f;
        if (LoopTracker.Enabled) LoggerProvider.LogInfo($"[VesselRecall] Unloaded vehicle origin landing at {landingPos}");
      }
    }

    PlaySfxAt("sfx_portal_activate", player.transform.position);

    // Register into Teleport_Patch so vehicle deck is recognized and teleport animation does not hang
    var targetZdoUid = wheelZdo != null ? wheelZdo.m_uid : (vm != null && vm.m_nview != null ? vm.m_nview.GetZDO().m_uid : ZDOID.None);
    if (targetZdoUid != ZDOID.None)
    {
      Teleport_Patch.m_teleportTarget[player] = targetZdoUid;
    }

    if (LoopTracker.Enabled) LoggerProvider.LogInfo($"[VesselRecall] Teleporting player {player.GetPlayerName()} to {landingPos} (Distant: {!isLoaded})");
    var accepted = player.TeleportTo(landingPos, landingRot, distantTeleport: !isLoaded);
    if (accepted)
    {
      player.Message(MessageHud.MessageType.Center, "Teleported to boat!");
    }
    return accepted;
  }

  public static void AttunePlayerToVehicle(Player player, int vehicleId)
  {
    if (player == null || vehicleId == 0) return;

    var playerZdo = player.m_nview != null ? player.m_nview.GetZDO() : null;
    if (playerZdo != null)
    {
      playerZdo.Set(AttunedVesselZdoKey, vehicleId);
    }

    PlaySfxAt("sfx_cheers", player.transform.position);
    if (LoopTracker.Enabled) LoggerProvider.LogInfo($"[VesselRecall] Successfully bound player {player.GetPlayerName()} to vessel #{vehicleId}");
    player.Message(MessageHud.MessageType.Center, "Horn bound to boat!");
  }

  public static bool TryGetSacrificialStonesPosition(out Vector3 position)
  {
    position = Vector3.zero;
    if (ZoneSystem.instance != null)
    {
      if (ZoneSystem.instance.FindClosestLocation("StartTemple", Vector3.zero, out var loc))
      {
        position = loc.m_position;
        if (ZoneSystem.instance.GetGroundHeight(position, out var gh))
        {
          position.y = Mathf.Max(position.y, gh) + 0.5f;
        }
        else
        {
          position.y += 0.5f;
        }
        return true;
      }

      if (ZoneSystem.instance.GetLocationIcon("StartTemple", out var iconPos))
      {
        position = iconPos;
        if (ZoneSystem.instance.GetGroundHeight(position, out var gh))
        {
          position.y = Mathf.Max(position.y, gh) + 0.5f;
        }
        else
        {
          position.y += 0.5f;
        }
        return true;
      }
    }

    return false;
  }

  public static bool TeleportPlayerToSacrificialStones(Player player)
  {
    if (player == null) return false;

    if (!TryGetSacrificialStonesPosition(out var stonePos))
    {
      player.Message(MessageHud.MessageType.Center, "Sacrificial Stones not found!");
      return false;
    }

    PlaySfxAt("sfx_portal_activate", player.transform.position);

    if (LoopTracker.Enabled) LoggerProvider.LogInfo($"[VesselRecall] Teleporting player {player.GetPlayerName()} to Sacrificial Stones at {stonePos}");
    var accepted = player.TeleportTo(stonePos, Quaternion.identity, distantTeleport: true);
    if (accepted)
    {
      player.Message(MessageHud.MessageType.Center, "Teleported to Sacrificial Stones!");
    }
    return accepted;
  }

  private static void PlaySfxAt(string prefabName, Vector3 position)
  {
    if (ZNetScene.instance == null) return;
    var sfx = ZNetScene.instance.GetPrefab(prefabName);
    if (sfx != null)
    {
      Object.Instantiate(sfx, position, Quaternion.identity);
    }
  }
}
