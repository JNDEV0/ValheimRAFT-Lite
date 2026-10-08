using HarmonyLib;
using UnityEngine;
using Zolantris.Shared;

namespace ValheimVehicles.Patches;

[HarmonyPatch(typeof(ReflectionUpdate))]
public static class ReflectionUpdate_Patches
{
  private static void SanitizeReflectionProbes(ReflectionUpdate instance)
  {
    if (instance == null) return;
    int excludeMask = (1 << LayerHelpers.CustomRaftLayer) |
                      (1 << LayerHelpers.PieceNonSolidLayer);
    int vehicleLayer = LayerMask.NameToLayer("vehicle");
    if (vehicleLayer >= 0)
    {
      excludeMask |= (1 << vehicleLayer);
    }
    int ignoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
    if (ignoreRaycast >= 0)
    {
      excludeMask |= (1 << ignoreRaycast);
    }

    if (instance.m_probe1 != null)
    {
      instance.m_probe1.cullingMask &= ~excludeMask;
    }
    if (instance.m_probe2 != null)
    {
      instance.m_probe2.cullingMask &= ~excludeMask;
    }
  }

  [HarmonyPatch("Start")]
  [HarmonyPostfix]
  public static void Start_Postfix(ReflectionUpdate __instance)
  {
    SanitizeReflectionProbes(__instance);
  }

  [HarmonyPatch("UpdateReflection")]
  [HarmonyPrefix]
  public static void UpdateReflection_Prefix(ReflectionUpdate __instance)
  {
    SanitizeReflectionProbes(__instance);
  }
}
