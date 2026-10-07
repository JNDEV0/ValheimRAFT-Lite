#region

  using HarmonyLib;
  using ValheimVehicles.Components;
  using ValheimVehicles.Controllers;
  using ValheimVehicles.Interfaces;

#endregion

  namespace ValheimVehicles.Patches;

  [HarmonyPatch]
  public class ZNetView_Patch
  {
    [HarmonyPatch(typeof(ZNetView), "ResetZDO")]
    [HarmonyPrefix]
    private static bool ZNetView_ResetZDO(ZNetView __instance)
    {
      if (__instance == null || __instance.m_zdo == null)
      {
        return false;
      }

      if (ZNetView.m_forceDisableInit)
      {
        return false;
      }

      return true;
    }

    [HarmonyPatch(typeof(ZNetView), "Awake")]
    [HarmonyPostfix]
    private static void ZNetView_Awake(ZNetView __instance)
    {
      if (__instance == null || !__instance || __instance.m_zdo == null || __instance.m_ghost) return;

      // Automatically attempts to attach/register to its parent vehicle if MBParentId is set
      if (VehiclePiecesController.TryInitPieceFromNetView(__instance))
      {
        if (__instance != null && __instance && __instance.m_zdo != null)
        {
          CultivatableComponent.InitPiece(__instance);
        }
        return;
      }

      if (__instance == null || !__instance || __instance.m_zdo == null) return;

      // for any vehicle like components
      BasePieceActivatorComponent.InitPiece(__instance);

      if (__instance == null || !__instance || __instance.m_zdo == null) return;

      // other components
      CultivatableComponent.InitPiece(__instance);
    }

    [HarmonyPatch(typeof(ZNetView), "OnDestroy")]
    [HarmonyPrefix]
    private static bool ZNetView_OnDestroy(ZNetView __instance)
    {
      var controller = __instance.GetComponentInParent<IPieceController>();
      if (controller != null)
      {
        controller.RemovePiece(__instance);
      }

      return true;
    }
  }