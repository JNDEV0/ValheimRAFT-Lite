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
          // Tamed sailor: check for Ctrl+E change hat or Shift+E dismiss
          bool isCtrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
          if (isCtrl)
          {
            var sailor = target.GetComponentInParent<RaftGreydwarfSailorComponent>();
            if (sailor != null)
            {
              sailor.CycleNextHat(__instance);
              return false;
            }
          }

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
  // ==========================================
  // Sailor Rock Throwing Aim Improvements
  // ==========================================

  [HarmonyPatch(typeof(Attack), nameof(Attack.Update))]
  [HarmonyPostfix]
  public static void Attack_Update_Postfix(Attack __instance)
  {
    if (__instance == null || __instance.m_character == null) return;
    if (!__instance.m_character.InAttack()) return;
    var sailor = __instance.m_character.GetComponent<RaftGreydwarfSailorComponent>();
    if (sailor == null) return;

    Character? target = __instance.m_baseAI != null ? __instance.m_baseAI.GetTargetCreature() : null;
    if (target == null || target.IsDead()) return;

    Vector3 dir = target.GetCenterPoint() - __instance.m_character.transform.position;
    dir.y = 0;
    if (dir.sqrMagnitude > 0.001f)
    {
      __instance.m_character.transform.rotation = Quaternion.RotateTowards(
        __instance.m_character.transform.rotation,
        Quaternion.LookRotation(dir),
        360f * Time.deltaTime
      );
    }
  }

  [HarmonyPatch(typeof(Attack), nameof(Attack.GetProjectileSpawnPoint))]
  [HarmonyPostfix]
  public static void Attack_GetProjectileSpawnPoint_Postfix(Attack __instance, ref Vector3 spawnPoint, ref Vector3 aimDir)
  {
    if (__instance == null || __instance.m_character == null) return;
    var sailor = __instance.m_character.GetComponent<RaftGreydwarfSailorComponent>();
    if (sailor == null) return;

    Character? target = __instance.m_baseAI != null ? __instance.m_baseAI.GetTargetCreature() : null;
    if (target == null || target.IsDead()) return;

    Vector3 targetPos = target.GetCenterPoint();
    Vector3 targetVel = target.GetVelocity();

    float dist = Vector3.Distance(spawnPoint, targetPos);
    float speed = __instance.m_projectileVel > 1f ? __instance.m_projectileVel : 20f;
    float time = dist / speed;

    // Lead target based on velocity vector and projectile travel time
    Vector3 predictedPos = targetPos + (targetVel * time);

    // Ballistic drop compensation if launch angle is zero
    if (__instance.m_launchAngle <= 0.001f)
    {
      predictedPos.y += 0.5f * 9.81f * time * time * 0.40f;
    }

    Vector3 calculatedAim = (predictedPos - spawnPoint).normalized;
    aimDir = calculatedAim;

    // Orient character towards throw release point
    Vector3 flatLook = calculatedAim;
    flatLook.y = 0;
    if (flatLook.sqrMagnitude > 0.001f)
    {
      __instance.m_character.transform.rotation = Quaternion.LookRotation(flatLook);
    }
  }

  [HarmonyPatch(typeof(Attack), nameof(Attack.FireProjectileBurst))]
  [HarmonyPrefix]
  public static void Attack_FireProjectileBurst_Prefix(Attack __instance, out float __state)
  {
    __state = -1f;
    if (__instance?.m_character?.GetComponent<RaftGreydwarfSailorComponent>() != null)
    {
      __state = __instance.m_projectileAccuracy;
      // High accuracy (low spread) for trained shipboard defenders
      __instance.m_projectileAccuracy = 0.2f;
    }
  }

  [HarmonyPatch(typeof(Attack), nameof(Attack.FireProjectileBurst))]
  [HarmonyPostfix]
  public static void Attack_FireProjectileBurst_Postfix(Attack __instance, float __state)
  {
    if (__state >= 0f && __instance != null)
    {
      __instance.m_projectileAccuracy = __state;
    }
  }
}
