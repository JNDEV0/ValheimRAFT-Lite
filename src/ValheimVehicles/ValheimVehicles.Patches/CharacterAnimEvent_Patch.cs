using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Components;
using ValheimVehicles.Interfaces;

namespace ValheimVehicles.Patches;

[HarmonyPatch]
public class CharacterAnimEvent_Patch
{
  public static Dictionary<Animator, IAnimatorHandler> m_animatedHumanoids = new();

  [HarmonyPatch(typeof(CharacterAnimEvent), "OnAnimatorIK")]
  [HarmonyPrefix]
  private static bool OnAnimatorIK(CharacterAnimEvent __instance,
    int layerIndex)
  {
    if (m_animatedHumanoids.Count > 0 && __instance.m_animator != null && m_animatedHumanoids.TryGetValue(__instance.m_animator, out var activator))
    {
      if (activator == null)
      {
        m_animatedHumanoids.Remove(__instance.m_animator);
        return false;
      }
      activator.UpdateIK(__instance.m_animator);
      return false;
    }
    if (__instance.m_character is Player player && player.IsAttached() &&
        (bool)player.m_attachPoint && (bool)player.m_attachPoint.parent)
    {
      var animator = player.m_attachPoint.GetComponentInParent<IAnimatorHandler>();
      if (animator != null)
      {
        animator.UpdateIK(player.m_animator);
        return false;
      }
    }

    return true;
  }

  [HarmonyPatch(typeof(CharacterAnimEvent), "UpdateFootIK")]
  [HarmonyPrefix]
  private static bool UpdateFootIK(CharacterAnimEvent __instance)
  {
    if (__instance.m_character is Player player && player.IsAttached() &&
        (bool)player.m_attachPoint && (bool)player.m_attachPoint.parent)
    {
      var ladder = player.m_attachPoint.GetComponentInParent<RopeLadderComponent>();
      if (ladder != null)
      {
        if (__instance.m_feets != null && __instance.m_animator != null)
        {
          foreach (var foot in __instance.m_feets)
          {
            __instance.m_animator.SetIKPositionWeight(foot.m_ikHandle, 0f);
            __instance.m_animator.SetIKRotationWeight(foot.m_ikHandle, 0f);
            foot.m_ikWeight = 0f;
            foot.m_isPlanted = false;
          }
        }
        return false;
      }
    }
    return true;
  }

  [HarmonyPatch(typeof(CharacterAnimEvent), "CustomLateUpdate")]
  [HarmonyPostfix]
  private static void CustomLateUpdate(CharacterAnimEvent __instance)
  {
    if (__instance.m_character is Player player && player.IsAttached() &&
        (bool)player.m_attachPoint && (bool)player.m_attachPoint.parent)
    {
      var ladder = player.m_attachPoint.GetComponentInParent<RopeLadderComponent>();
      if (ladder != null && __instance.m_animator != null)
      {
        ladder.SnapLimbs(__instance.m_animator, player);
      }
    }
  }
}