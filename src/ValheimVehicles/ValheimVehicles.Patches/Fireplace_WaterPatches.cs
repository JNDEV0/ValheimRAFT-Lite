using HarmonyLib;
using ValheimVehicles.Controllers;


namespace ValheimVehicles.Patches;

public static class Fireplace_WaterPatches
{
  [HarmonyPatch(typeof(Fireplace), nameof(Fireplace.IsBurning))]
  [HarmonyPostfix]
  public static void IsBurning_Underwater(Fireplace __instance,
    ref bool __result)
  {
    if (__result == false && IsFireInDisplacedVehicle(__instance))
    {
      __result = true;
    }
  }

  public static bool IsFireInDisplacedVehicle(Fireplace __instance)
  {
    return VehiclePiecesController.IsWithin(__instance.transform,
      out _);
  }

  [HarmonyPatch(typeof(Fireplace), "CheckUnderTerrain")]
  [HarmonyPrefix]
  public static bool CheckUnderTerrain_VehicleGuard(Fireplace __instance)
  {
    if (__instance == null) return true;
    if (__instance.GetComponentInParent<VehiclePiecesController>() != null)
      return false;
    var nview = __instance.GetComponent<ZNetView>();
    if (nview != null && nview.GetZDO() != null && VehiclePiecesController.GetParentID(nview.GetZDO()) != 0)
      return false;
    if (IsFireInDisplacedVehicle(__instance))
      return false;
    return true;
  }
}