using System;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Components;

namespace ValheimVehicles.Patches;

[HarmonyPatch]
public class GreydwarfTaming_Patch
{
  [HarmonyPatch(typeof(MonsterAI), "Awake")]
  [HarmonyPostfix]
  public static void MonsterAI_Awake_Postfix(MonsterAI __instance)
  {
    if (__instance == null || __instance.gameObject == null) return;
    var name = __instance.gameObject.name;
    if (name.StartsWith("Greydwarf", StringComparison.OrdinalIgnoreCase))
    {
      if (__instance.GetComponent<RaftGreydwarfTaming>() == null)
      {
        __instance.gameObject.AddComponent<RaftGreydwarfTaming>();
      }
    }
  }
}
