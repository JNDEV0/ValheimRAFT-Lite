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

  [HarmonyPatch(typeof(Character), nameof(Character.GetHoverText))]
  [HarmonyPostfix]
  public static void Character_GetHoverText_Postfix(Character __instance, ref string __result)
  {
    if (__instance == null) return;
    var taming = __instance.GetComponent<RaftGreydwarfTaming>();
    if (taming != null)
    {
      __result = taming.GetHoverText();
    }
  }

  [HarmonyPatch(typeof(Character), nameof(Character.GetHoverName))]
  [HarmonyPostfix]
  public static void Character_GetHoverName_Postfix(Character __instance, ref string __result)
  {
    if (__instance == null) return;
    if (__instance.IsTamed())
    {
      var sailor = __instance.GetComponent<RaftGreydwarfSailorComponent>();
      if (sailor != null)
      {
        __result = Localization.instance.Localize("$valheim_vehicles_sailor_greydwarf");
      }
    }
  }

  [HarmonyPatch(typeof(Player), nameof(Player.Interact))]
  [HarmonyPrefix]
  public static bool Player_Interact_Prefix(Player __instance, GameObject go, bool hold, bool alt)
  {
    if (hold) return true;
    if (__instance == null || !__instance.IsPlayer()) return true;

    var target = __instance.GetHoverCreature() != null ? __instance.GetHoverCreature().gameObject : go;
    if (target != null)
    {
      var taming = target.GetComponentInParent<RaftGreydwarfTaming>();
      var ch = target.GetComponentInParent<Character>();
      if (taming != null && ch != null)
      {
        if (!ch.IsTamed())
        {
          taming.Interact(__instance, hold, alt);
          return false;
        }
        else
        {
          // Tamed sailor: check for Shift+E dismiss
          bool isShift = alt || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
          if (isShift)
          {
            var sailor = target.GetComponentInParent<RaftGreydwarfSailorComponent>();
            if (sailor != null)
            {
              sailor.Dismiss(__instance);
              return false;
            }
          }
        }
      }
    }
    return true;
  }

  [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UseItem))]
  [HarmonyPrefix]
  public static bool Humanoid_UseItem_Prefix(Humanoid __instance, Inventory inventory, ItemDrop.ItemData item, bool fromInventoryGui)
  {
    if (__instance == null || item == null) return true;
    var player = __instance as Player;
    if (player == null) return true;

    var target = player.GetHoverCreature() != null ? player.GetHoverCreature().gameObject : player.GetHoverObject();
    if (target != null)
    {
      var taming = target.GetComponentInParent<RaftGreydwarfTaming>();
      if (taming != null)
      {
        if (taming.UseItem(player, item))
        {
          return false;
        }
      }
    }
    return true;
  }

  [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.IsAggravatable))]
  [HarmonyPostfix]
  public static void BaseAI_IsAggravatable_Postfix(BaseAI __instance, ref bool __result)
  {
    if (__instance != null && __instance.GetComponent<RaftGreydwarfSailorComponent>() != null)
    {
      __result = true;
    }
  }

  [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
  [HarmonyPrefix]
  public static void Character_RPC_Damage_Prefix(Character __instance, HitData hit)
  {
    if (__instance == null || hit == null) return;
    if (__instance.GetComponent<RaftGreydwarfSailorComponent>() != null)
    {
      hit.m_ignorePVP = true;
    }
  }
}
