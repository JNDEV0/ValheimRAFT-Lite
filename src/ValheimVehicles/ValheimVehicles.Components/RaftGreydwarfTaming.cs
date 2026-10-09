using System;
using UnityEngine;
using ValheimVehicles.Controllers;

namespace ValheimVehicles.Components;

public class RaftGreydwarfTaming : MonoBehaviour, Hoverable, Interactable
{
  private Character? _character;
  private MonsterAI? _monsterAI;
  private ZNetView? _nview;

  private void Awake()
  {
    _character = GetComponent<Character>();
    _monsterAI = GetComponent<MonsterAI>();
    _nview = GetComponent<ZNetView>();
  }

  private void Start()
  {
    if (_character != null && _character.IsTamed())
    {
      var sailor = EnsureSailorComponent();
      sailor?.EnsureRandomSailorHat();
    }
  }

  public string GetHoverText()
  {
    if (_character != null && _character.IsTamed())
    {
      var sailor = GetComponent<RaftGreydwarfSailorComponent>();
      string loyaltyStr = sailor != null ? sailor.GetLoyaltyHoverString() : "<color=#00FF00>Satisfied</color>";
      string dismissPrompt = Localization.instance.Localize("$valheim_vehicles_sailor_dismiss");
      return Localization.instance.Localize(
        $"$valheim_vehicles_sailor_greydwarf\nLoyalty: {loyaltyStr}\n[<color=yellow><b>Shift+$KEY_Use</b></color>] {dismissPrompt}\n<color=grey>Keep resin available in chests</color>");
    }

    // Only regular Greydwarfs can be hired as sailors
    if (_character != null)
    {
      string cname = _character.gameObject.name;
      if (cname.Contains("Shaman", StringComparison.OrdinalIgnoreCase) ||
          cname.Contains("Elite", StringComparison.OrdinalIgnoreCase) ||
          cname.Contains("Brute", StringComparison.OrdinalIgnoreCase))
      {
        return "";
      }
    }

    // ONLY show the hover text if the player has 10+ coins in the action bar, not just anywhere in the inventory
    var localPlayer = Player.m_localPlayer;
    if (localPlayer == null) return "";
    var inv = localPlayer.GetInventory();
    if (inv == null) return "";
    int actionBarCoins = GetActionBarItemCount(inv, "Coins", "$item_coins");
    if (actionBarCoins < 10)
    {
      return "";
    }

    var nearestShip = FindNearestShip(transform.position, 250f);
    if (nearestShip == null)
    {
      // Grey out the text "Hire Sailor" and show an additional line under it "Ship is too far"
      return Localization.instance.Localize(
        "<color=grey>[<b>$KEY_Use</b>] $valheim_vehicles_tame_prompt\n$valheim_vehicles_tame_ship_too_far</color>");
    }

    // Simplified taming hover text: "[E] Hire Sailor"
    return Localization.instance.Localize(
      "[<color=yellow><b>$KEY_Use</b></color>] $valheim_vehicles_tame_prompt");
  }

  public string GetHoverName()
  {
    return _character != null ? _character.GetHoverName() : "Greydwarf";
  }

  public float GetHoverOffset()
  {
    return 0f;
  }

  private bool CanHireSailor(Player player, out string reason, out VehiclePiecesController? nearestShip)
  {
    nearestShip = null;

    if (_character != null)
    {
      string cname = _character.gameObject.name;
      if (cname.Contains("Shaman", StringComparison.OrdinalIgnoreCase) ||
          cname.Contains("Elite", StringComparison.OrdinalIgnoreCase) ||
          cname.Contains("Brute", StringComparison.OrdinalIgnoreCase))
      {
        reason = "Only regular Greydwarfs can be hired as sailors.";
        return false;
      }
    }

    nearestShip = FindNearestShip(transform.position, 250f);
    if (nearestShip == null)
    {
      reason = ""; // No UI message when ship is too far
      return false;
    }

    reason = "";
    return true;
  }

  public bool Interact(Humanoid user, bool hold, bool alt)
  {
    if (hold) return false;
    if (user == null || !user.IsPlayer()) return false;

    var player = user as Player;
    if (player == null) return false;

    // If tamed, check for Shift+E dismiss
    if (_character != null && _character.IsTamed())
    {
      bool isShift = alt || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
      if (isShift)
      {
        var sailor = GetComponent<RaftGreydwarfSailorComponent>();
        if (sailor != null)
        {
          sailor.Dismiss(player);
          return true;
        }
      }
      return false;
    }

    if (!CanHireSailor(player, out string reason, out var nearestShip))
    {
      if (!string.IsNullOrEmpty(reason))
      {
        player.Message(MessageHud.MessageType.Center, reason);
      }
      return false;
    }

    var inv = player.GetInventory();
    if (inv == null) return false;

    int actionBarCoins = GetActionBarItemCount(inv, "Coins", "$item_coins");
    if (actionBarCoins < 10)
    {
      return false;
    }

    RemoveActionBarItemCount(inv, "Coins", "$item_coins", 10);
    ExecuteTaming(player, success: true, nearestShip: nearestShip);
    return true;
  }

  public bool UseItem(Humanoid user, ItemDrop.ItemData item)
  {
    if (user == null || !user.IsPlayer() || item == null) return false;
    var player = user as Player;
    if (player == null) return false;

    var inv = player.GetInventory();

    // If already tamed
    if (_character != null && _character.IsTamed())
    {
      // 1. Check if offering/changing a Sailor Hat!
      string prefabName = item.m_dropPrefab != null ? item.m_dropPrefab.name : "";
      if (RaftGreydwarfSailorComponent.IsSailorHat(prefabName))
      {
        var sailorComp = EnsureSailorComponent();
        if (sailorComp != null)
        {
          sailorComp.EquipSpecificHat(prefabName);
          player.Message(MessageHud.MessageType.Center,
            $"Equipped {item.m_shared.m_name} on Greydwarf Sailor!");
          return true;
        }
      }

      // 2. Check if feeding Resin directly to restore loyalty
      if (IsItemMatch(item, "Resin", "$item_resin"))
      {
        var sailorComp = EnsureSailorComponent();
        if (sailorComp != null && sailorComp.Loyalty == SailorLoyalty.Satisfied)
        {
          player.Message(MessageHud.MessageType.Center,
            "Greydwarf Sailor is already satisfied!");
          return true;
        }

        if (inv != null && GetItemCount(inv, "Resin", "$item_resin") >= 1)
        {
          RemoveItemCount(inv, "Resin", "$item_resin", 1);
          sailorComp?.FeedResin();
          SpawnFeedEffects();
          player.Message(MessageHud.MessageType.Center,
            "Fed Greydwarf Sailor 1 Resin! Loyalty: Satisfied.");
          return true;
        }
      }

      return false;
    }

    // If not tamed: only Coins
    if (inv == null) return false;

    bool isCoins = IsItemMatch(item, "Coins", "$item_coins");
    if (!isCoins)
    {
      return false;
    }

    if (!CanHireSailor(player, out string reason, out var nearestShip))
    {
      if (!string.IsNullOrEmpty(reason))
      {
        player.Message(MessageHud.MessageType.Center, reason);
      }
      return true; // handled, no message if reason is empty
    }

    int actionBarCoins = GetActionBarItemCount(inv, "Coins", "$item_coins");
    if (actionBarCoins < 10)
    {
      player.Message(MessageHud.MessageType.Center,
        Localization.instance.Localize("$valheim_vehicles_tame_insufficient"));
      return true;
    }

    RemoveActionBarItemCount(inv, "Coins", "$item_coins", 10);
    ExecuteTaming(player, success: true, nearestShip: nearestShip);
    return true;
  }

  private void ExecuteTaming(Player player, bool success, VehiclePiecesController? nearestShip)
  {
    if (_character == null) return;

    if (success)
    {
      _character.SetTamed(true);
      _character.m_faction = Character.Faction.Players;

      if (_monsterAI != null)
      {
        _monsterAI.SetAlerted(false);
        _monsterAI.SetTarget(null);
        _monsterAI.m_targetCreature = null;
      }

      SpawnTameEffects();
      var sailor = EnsureSailorComponent();

      if (nearestShip != null && sailor != null)
      {
        sailor.SetAssignedShip(nearestShip);
        nearestShip.RegisterSailor(sailor);
        sailor.TeleportToShipDeck();
      }

      player.Message(MessageHud.MessageType.Center,
        Localization.instance.Localize("$valheim_vehicles_tame_success"));
    }
    else
    {
      SpawnTameFailedEffects();
      player.Message(MessageHud.MessageType.Center,
        Localization.instance.Localize("$valheim_vehicles_tame_failed"));
    }
  }

  private void SpawnTameFailedEffects()
  {
    if (ZNetScene.instance == null) return;
    var vfx = ZNetScene.instance.GetPrefab("vfx_greydwarf_hit") ?? ZNetScene.instance.GetPrefab("vfx_damage_slash");
    if (vfx != null)
    {
      Instantiate(vfx, transform.position + Vector3.up * 1f, Quaternion.identity);
    }
  }

  private void SpawnFeedEffects()
  {
    if (ZNetScene.instance == null) return;
    var vfx = ZNetScene.instance.GetPrefab("vfx_boar_love") ?? ZNetScene.instance.GetPrefab("vfx_tame");
    if (vfx != null)
    {
      Instantiate(vfx, transform.position + Vector3.up * 1.2f, Quaternion.identity);
    }
  }

  private RaftGreydwarfSailorComponent? EnsureSailorComponent()
  {
    var sailor = GetComponent<RaftGreydwarfSailorComponent>();
    if (sailor == null)
    {
      sailor = gameObject.AddComponent<RaftGreydwarfSailorComponent>();
    }
    return sailor;
  }

  private void SpawnTameEffects()
  {
    if (ZNetScene.instance == null) return;
    var vfx = ZNetScene.instance.GetPrefab("vfx_boar_love") ?? ZNetScene.instance.GetPrefab("vfx_tame");
    if (vfx != null)
    {
      Instantiate(vfx, transform.position + Vector3.up * 1f, Quaternion.identity);
    }
  }

  public static VehiclePiecesController? FindNearestShip(Vector3 pos, float maxDistance = 250f)
  {
    VehiclePiecesController? nearest = null;
    float minDist = maxDistance * maxDistance;
    foreach (var move in VehicleMovementController.Instances)
    {
      if (move == null || move.PiecesController == null) continue;
      float d2 = (move.transform.position - pos).sqrMagnitude;
      if (d2 < minDist)
      {
        minDist = d2;
        nearest = move.PiecesController;
      }
    }
    if (nearest == null && VehiclePiecesController.ActiveInstances != null)
    {
      foreach (var kvp in VehiclePiecesController.ActiveInstances)
      {
        var vpc = kvp.Value;
        if (vpc == null) continue;
        float d2 = (vpc.transform.position - pos).sqrMagnitude;
        if (d2 < minDist)
        {
          minDist = d2;
          nearest = vpc;
        }
      }
    }
    return nearest;
  }

  private static int GetActionBarItemCount(Inventory inv, string prefabName, string token)
  {
    if (inv == null) return 0;
    int count = 0;
    int width = Math.Min(8, inv.GetWidth());
    for (int x = 0; x < width; x++)
    {
      var item = inv.GetItemAt(x, 0);
      if (item != null && IsItemMatch(item, prefabName, token))
      {
        count += item.m_stack;
      }
    }
    return count;
  }

  private static void RemoveActionBarItemCount(Inventory inv, string prefabName, string token, int amount)
  {
    if (inv == null || amount <= 0) return;
    int remaining = amount;
    int width = Math.Min(8, inv.GetWidth());
    for (int x = 0; x < width; x++)
    {
      var item = inv.GetItemAt(x, 0);
      if (item != null && IsItemMatch(item, prefabName, token))
      {
        if (item.m_stack <= remaining)
        {
          remaining -= item.m_stack;
          inv.RemoveItem(item);
        }
        else
        {
          item.m_stack -= remaining;
          remaining = 0;
          break;
        }
        if (remaining <= 0) break;
      }
    }
    if (remaining > 0)
    {
      RemoveItemCount(inv, prefabName, token, remaining);
    }
  }

  private static bool IsItemMatch(ItemDrop.ItemData item, string prefabName, string token)
  {
    if (item == null) return false;
    if (item.m_dropPrefab != null && item.m_dropPrefab.name.Equals(prefabName, StringComparison.OrdinalIgnoreCase))
      return true;
    if (!string.IsNullOrEmpty(item.m_shared?.m_name) &&
        (item.m_shared.m_name.Equals(token, StringComparison.OrdinalIgnoreCase) ||
         item.m_shared.m_name.Equals(prefabName, StringComparison.OrdinalIgnoreCase)))
      return true;
    return false;
  }

  private static int GetItemCount(Inventory inv, string prefabName, string token)
  {
    if (inv == null) return 0;
    int c1 = inv.CountItems(prefabName);
    int c2 = inv.CountItems(token);
    return Math.Max(c1, c2);
  }

  private static void RemoveItemCount(Inventory inv, string prefabName, string token, int amount)
  {
    if (inv == null || amount <= 0) return;
    if (inv.CountItems(prefabName) >= amount)
    {
      inv.RemoveItem(prefabName, amount);
    }
    else
    {
      inv.RemoveItem(token, amount);
    }
  }
}
