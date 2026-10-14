using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Components;
using ValheimVehicles.Controllers;
using ValheimVehicles.Shared.Constants;
using ZdoWatcher;

namespace ValheimVehicles.Patches;

/// <summary>
/// Scraps/bypasses ownership and creator restrictions on vehicle pieces and containers.
/// Allows any player to add, open chests, repair, or hammer/dismantle parts on the ship.
/// </summary>
[HarmonyPatch]
public static class VehicleOwnership_Patches
{
  public static bool IsVehiclePiece(GameObject? go)
  {
    if (go == null) return false;

    var netView = go.GetComponentInParent<ZNetView>();
    if (netView != null && netView.IsValid())
    {
      var zdo = netView.GetZDO();
      if (zdo != null)
      {
        if (zdo.GetInt(VehicleZdoVars.MBParentId, 0) != 0) return true;
        if (zdo.GetInt(VehicleZdoVars.TempPieceParentId, 0) != 0) return true;
        if (ZdoWatchController.GetPersistentID(zdo, out var pId) &&
            (VehicleManager.VehicleInstances.ContainsKey(pId) || VehiclePiecesController.ActiveInstances.ContainsKey(pId)))
          return true;
        if (VehiclePiecesController.VehicleParentIdCache.ContainsKey(zdo.m_uid)) return true;
      }
    }

    if (go.GetComponentInParent<VehicleManager>() != null ||
        go.GetComponentInParent<VehiclePiecesController>() != null)
      return true;

    // Check if the object is physically within any active vehicle's deck bounds
    foreach (var controller in VehiclePiecesController.ActiveInstances.Values)
    {
      if (controller != null && controller.isActiveAndEnabled && controller.OnboardCollider != null)
      {
        if (controller.OnboardCollider.bounds.Contains(go.transform.position))
          return true;
      }
    }

    return false;
  }

  [HarmonyPatch(typeof(Container), "CheckAccess", typeof(long))]
  [HarmonyPrefix]
  public static bool Container_CheckAccess(Container __instance, long playerID, ref bool __result)
  {
    if (IsVehiclePiece(__instance.gameObject))
    {
      __result = true;
      return false;
    }
    return true;
  }

  [HarmonyPatch(typeof(Container), nameof(Container.Interact))]
  [HarmonyPrefix]
  public static void Container_Interact(Container __instance)
  {
    if (IsVehiclePiece(__instance.gameObject))
    {
      if (__instance.m_nview != null && __instance.m_nview.IsValid())
      {
        if (!__instance.m_nview.IsOwner())
        {
          __instance.m_nview.ClaimOwnership();
        }
      }
    }
  }

  [HarmonyPatch(typeof(Container), "RPC_RequestOpen")]
  [HarmonyPrefix]
  public static bool Container_RPC_RequestOpen(Container __instance, long uid, long playerID)
  {
    if (IsVehiclePiece(__instance.gameObject))
    {
      if (__instance.IsInUse() && uid != ZNet.GetUID())
      {
        __instance.m_nview.InvokeRPC(uid, "RPC_OpenResponse", false);
        return false;
      }
      ZDOMan.instance.ForceSendZDO(uid, __instance.m_nview.GetZDO().m_uid);
      __instance.m_nview.GetZDO().SetOwner(uid);
      __instance.m_nview.InvokeRPC(uid, "RPC_OpenResponse", true);
      return false;
    }
    return true;
  }

  [HarmonyPatch(typeof(Container), nameof(Container.CanBeRemoved))]
  [HarmonyPrefix]
  public static bool Container_CanBeRemoved(Container __instance, ref bool __result)
  {
    if (IsVehiclePiece(__instance.gameObject))
    {
      __result = true;
      return false;
    }
    return true;
  }

  [HarmonyPatch(typeof(Piece), nameof(Piece.CanBeRemoved))]
  [HarmonyPrefix]
  public static bool Piece_CanBeRemoved(Piece __instance, ref bool __result)
  {
    if (IsVehiclePiece(__instance.gameObject))
    {
      __instance.m_canBeRemoved = true;
      __result = true;
      return false;
    }
    return true;
  }

  [HarmonyPatch(typeof(Player), "CheckCanRemovePiece")]
  [HarmonyPrefix]
  public static bool Player_CheckCanRemovePiece(Piece piece, ref bool __result)
  {
    if (piece != null && IsVehiclePiece(piece.gameObject))
    {
      __result = true;
      return false;
    }
    return true;
  }

  private static string? _lastDemolishTargetId;
  private static float _lastDemolishTime;
  private const float DemolishConfirmWindow = 3.0f;

  public static bool IsValheimRaftPiece(Piece? piece)
  {
    if (piece == null) return false;
    if (IsVehiclePiece(piece.gameObject)) return true;
    var name = piece.gameObject.name;
    if (name.StartsWith("ValheimVehicles", System.StringComparison.OrdinalIgnoreCase)) return true;
    if (name.StartsWith("Ship", System.StringComparison.OrdinalIgnoreCase)) return true;
    if (name.IndexOf("vehicle", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
    if (name.IndexOf("raft", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
    if (name.IndexOf("sail", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
    if (name.IndexOf("rudder", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
    if (name.IndexOf("anchor", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
    if (name.IndexOf("keel", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
    if (name.IndexOf("swivel", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
    if (name.IndexOf("hull", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
    return false;
  }

  [HarmonyPatch(typeof(Player), "RemovePiece")]
  [HarmonyPrefix]
  public static bool Player_RemovePiece(Player __instance, ref bool __result)
  {
    var hoveringPiece = __instance.GetHoveringPiece();
    if (hoveringPiece == null)
    {
      if (Physics.Raycast(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, out var hitInfo, 50f, __instance.m_removeRayMask) &&
          Vector3.Distance(hitInfo.point, __instance.m_eye.position) < __instance.m_maxPlaceDistance)
      {
        hoveringPiece = hitInfo.collider.GetComponentInParent<Piece>();
      }
    }

    if (hoveringPiece != null && IsValheimRaftPiece(hoveringPiece))
    {
      if (Player_Patch.IsBoatHammer(__instance.GetRightItem()))
      {
        var nvTarget = hoveringPiece.GetComponent<ZNetView>();
        var targetId = nvTarget != null && nvTarget.GetZDO() != null
          ? nvTarget.GetZDO().m_uid.ToString()
          : hoveringPiece.GetInstanceID().ToString();

        var now = Time.time;
        if (_lastDemolishTargetId == targetId && (now - _lastDemolishTime) <= DemolishConfirmWindow)
        {
          _lastDemolishTargetId = null;
          _lastDemolishTime = 0f;

          hoveringPiece.m_canBeRemoved = true;
          if (nvTarget != null && nvTarget.IsValid() && !nvTarget.IsOwner())
          {
            nvTarget.ClaimOwnership();
          }
          return true;
        }

        _lastDemolishTargetId = targetId;
        _lastDemolishTime = now;

        var pieceName = Localization.instance != null ? Localization.instance.Localize(hoveringPiece.m_name) : hoveringPiece.m_name;
        if (string.IsNullOrWhiteSpace(pieceName) || pieceName.StartsWith("$"))
        {
          pieceName = hoveringPiece.name.Replace("(Clone)", "").Trim();
        }

        __instance.Message(MessageHud.MessageType.Center, $"destroy {pieceName}? click again to confirm");
        __result = false;
        return false;
      }

      hoveringPiece.m_canBeRemoved = true;
      var nv = hoveringPiece.GetComponent<ZNetView>();
      if (nv != null && nv.IsValid() && !nv.IsOwner())
      {
        nv.ClaimOwnership();
      }
    }

    return true;
  }

  [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Remove))]
  [HarmonyPrefix]
  public static void WearNTear_Remove(WearNTear __instance)
  {
    if (IsVehiclePiece(__instance.gameObject))
    {
      if (__instance.m_nview != null && __instance.m_nview.IsValid() && !__instance.m_nview.IsOwner())
      {
        __instance.m_nview.ClaimOwnership();
      }
    }
  }

  [HarmonyPatch(typeof(WearNTear), "RPC_Remove")]
  [HarmonyPrefix]
  public static bool WearNTear_RPC_Remove(WearNTear __instance, long sender, bool blockDrop)
  {
    if (IsVehiclePiece(__instance.gameObject))
    {
      if (__instance.m_nview != null && __instance.m_nview.IsValid())
      {
        if (!__instance.m_nview.IsOwner())
        {
          __instance.m_nview.ClaimOwnership();
        }
        __instance.Destroy(null, blockDrop);
        return false;
      }
    }
    return true;
  }

  [HarmonyPatch(typeof(PrivateArea), nameof(PrivateArea.CheckAccess))]
  [HarmonyPrefix]
  public static bool PrivateArea_CheckAccess(Vector3 point, ref bool __result)
  {
    foreach (var controller in VehiclePiecesController.ActiveInstances.Values)
    {
      if (controller != null && controller.isActiveAndEnabled && controller.OnboardCollider != null)
      {
        if (controller.OnboardCollider.bounds.Contains(point))
        {
          __result = true;
          return false;
        }
      }
    }
    return true;
  }
}
