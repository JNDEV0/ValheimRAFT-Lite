using System.Collections.Generic;
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
    // Only regular Greydwarf can be hired as sailors (exclude Shaman, Brute/Elite)
    if (name.StartsWith("Greydwarf", StringComparison.OrdinalIgnoreCase) &&
        !name.Contains("Shaman", StringComparison.OrdinalIgnoreCase) &&
        !name.Contains("Elite", StringComparison.OrdinalIgnoreCase) &&
        !name.Contains("Brute", StringComparison.OrdinalIgnoreCase))
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

  [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipBestWeapon))]
  [HarmonyPrefix]
  public static bool Humanoid_EquipBestWeapon_Prefix(Humanoid __instance)
  {
    if (__instance != null)
    {
      var sailor = __instance.GetComponent<RaftGreydwarfSailorComponent>();
      if (sailor != null)
      {
        sailor.EnsureRockWeaponEquipped();
        return false;
      }
    }
    return true;
  }

  [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.DoAttack))]
  [HarmonyPrefix]
  public static bool MonsterAI_DoAttack_Prefix(MonsterAI __instance, Character target, bool isFriend)
  {
    if (__instance != null && __instance.GetComponent<RaftGreydwarfSailorComponent>() != null)
    {
      // Only attack hostile enemies that are alerted (have an exclamation mark)
      if (target == null) return false;
      var targetAI = target.GetBaseAI();
      if (targetAI == null || !targetAI.IsAlerted())
      {
        return false;
      }
    }
    return true;
  }

  [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack))]
  [HarmonyPrefix]
  public static bool Humanoid_StartAttack_Prefix(Humanoid __instance, Character target, bool secondaryAttack)
  {
    if (__instance != null && __instance.GetComponent<RaftGreydwarfSailorComponent>() != null)
    {
      // 1. Only allow attacks against hostile, alerted enemies (with an exclamation mark)
      if (target != null)
      {
        var targetAI = target.GetBaseAI();
        if (targetAI == null || !targetAI.IsAlerted())
        {
          return false;
        }
      }

      // 2. Disable melee claw swings for sailors: in combat they only throw rocks from the boat
      var weapon = __instance.GetCurrentWeapon();
      if (weapon != null && weapon.m_shared != null)
      {
        var attack = secondaryAttack ? weapon.m_shared.m_secondaryAttack : weapon.m_shared.m_attack;
        if (attack != null && attack.m_attackProjectile == null)
        {
          return false;
        }
      }
    }
    return true;
  }

  [HarmonyPatch(typeof(Character), nameof(Character.UpdateMotion))]
  [HarmonyPrefix]
  public static bool Character_UpdateMotion_Prefix(Character __instance)
  {
    if (__instance != null && __instance.m_body != null && __instance.m_body.isKinematic)
    {
      return false;
    }
    return true;
  }

  [HarmonyPatch(typeof(Character), nameof(Character.SyncVelocity))]
  [HarmonyPrefix]
  public static bool Character_SyncVelocity_Prefix(Character __instance)
  {
    if (__instance != null && __instance.m_body != null && __instance.m_body.isKinematic)
    {
      return false;
    }
    return true;
  }

  [HarmonyPatch(typeof(Character), nameof(Character.SetVelocity))]
  [HarmonyPrefix]
  public static bool Character_SetVelocity_Prefix(Character __instance)
  {
    if (__instance != null && __instance.m_body != null && __instance.m_body.isKinematic)
    {
      return false;
    }
    return true;
  }

  [HarmonyPatch(typeof(Character), nameof(Character.StopMovement))]
  [HarmonyPrefix]
  public static bool Character_StopMovement_Prefix(Character __instance)
  {
    if (__instance != null && __instance.m_body != null && __instance.m_body.isKinematic)
    {
      return false;
    }
    return true;
  }

  [HarmonyPatch(typeof(Character), nameof(Character.StopMovementXZ))]
  [HarmonyPrefix]
  public static bool Character_StopMovementXZ_Prefix(Character __instance)
  {
    if (__instance != null && __instance.m_body != null && __instance.m_body.isKinematic)
    {
      return false;
    }
    return true;
  }

  [HarmonyPatch(typeof(Character), nameof(Character.UnderWorldCheck))]
  [HarmonyPrefix]
  public static bool Character_UnderWorldCheck_Prefix(Character __instance)
  {
    if (__instance != null && __instance.m_body != null && __instance.m_body.isKinematic)
    {
      return false;
    }
    return true;
  }

  [HarmonyPatch(typeof(EnemyHud), "UpdateHuds")]
  [HarmonyPostfix]
  public static void EnemyHud_UpdateHuds_Postfix(EnemyHud __instance, Player player)
  {
    if (__instance == null) return;
    var huds = Traverse.Create(__instance).Field("m_huds").GetValue() as System.Collections.IDictionary;
    if (huds == null) return;

    Character hoverCreature = player != null ? player.GetHoverCreature() : null;
    List<Character>? toRemove = null;

    foreach (System.Collections.DictionaryEntry entry in huds)
    {
      var c = entry.Key as Character;
      if (c == null) continue;

      if (c.GetComponent<RaftGreydwarfSailorComponent>() != null)
      {
        var hudDataTraverse = Traverse.Create(entry.Value);
        float hoverTimer = hudDataTraverse.Field<float>("m_hoverTimer").Value;
        var gui = hudDataTraverse.Field<GameObject>("m_gui").Value;

        if (c == hoverCreature)
        {
          hudDataTraverse.Field("m_hoverTimer").SetValue(0f);
          if (gui != null) gui.SetActive(true);
        }
        else
        {
          if (hoverTimer > 3.0f)
          {
            if (gui != null) gui.SetActive(false);
            if (hoverTimer > 6.0f)
            {
              toRemove ??= new List<Character>();
              toRemove.Add(c);
            }
          }
        }
      }
    }

    if (toRemove != null)
    {
      foreach (var c in toRemove)
      {
        if (huds.Contains(c))
        {
          var hudObj = huds[c];
          var gui = Traverse.Create(hudObj).Field<GameObject>("m_gui").Value;
          if (gui != null) UnityEngine.Object.Destroy(gui);
          huds.Remove(c);
        }
      }
    }
  }
}
