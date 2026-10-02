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
      string status = sailor != null && sailor.IsUnfed ? "<color=red>Unfed</color>" : "<color=green>Ready</color>";
      return Localization.instance.Localize($"$valheim_vehicles_sailor_greydwarf ({status})");
    }

    var nearestShip = FindNearestShip(transform.position, 250f);
    string capacityStatus;
    if (nearestShip == null)
    {
      capacityStatus = "<color=red>(No ship nearby)</color>";
    }
    else if (nearestShip.TotalRowingSeatsCount == 0)
    {
      capacityStatus = "<color=orange>(Requires Greydwarf Rowing Seat on ship)</color>";
    }
    else if (nearestShip.ActiveSailorsCount >= nearestShip.TotalRowingSeatsCount)
    {
      capacityStatus = $"<color=red>(Ship Full: {nearestShip.ActiveSailorsCount}/{nearestShip.TotalRowingSeatsCount} Seats)</color>";
    }
    else
    {
      capacityStatus = $"<color=cyan>(Crew: {nearestShip.ActiveSailorsCount}/{nearestShip.TotalRowingSeatsCount} Seats)</color>";
    }

    return Localization.instance.Localize(
      $"[<color=yellow><b>$KEY_Use</b></color>] $valheim_vehicles_tame_prompt\n{capacityStatus}");
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
    nearestShip = FindNearestShip(transform.position, 250f);
    if (nearestShip == null)
    {
      reason = Localization.instance.Localize("$valheim_vehicles_tame_no_ship");
      return false;
    }

    if (nearestShip.TotalRowingSeatsCount == 0)
    {
      reason = Localization.instance.Localize("$valheim_vehicles_tame_no_seats");
      return false;
    }

    if (nearestShip.ActiveSailorsCount >= nearestShip.TotalRowingSeatsCount)
    {
      reason = Localization.instance.Localize("$valheim_vehicles_tame_crew_full");
      return false;
    }

    reason = "";
    return true;
  }

  public bool Interact(Humanoid user, bool hold, bool alt)
  {
    if (hold) return false;
    if (user == null || !user.IsPlayer()) return false;
    if (_character != null && _character.IsTamed()) return false;

    var player = user as Player;
    if (player == null) return false;

    if (!CanHireSailor(player, out string reason, out var nearestShip))
    {
      player.Message(MessageHud.MessageType.Center, reason);
      return false;
    }

    var inv = player.GetInventory();
    if (inv == null) return false;

    int coins = GetItemCount(inv, "Coins", "$item_coins");
    int dandelions = GetItemCount(inv, "Dandelion", "$item_dandelion");

    // 10 Coins = 100% success
    if (coins >= 10)
    {
      RemoveItemCount(inv, "Coins", "$item_coins", 10);
      ExecuteTaming(player, success: true, nearestShip: nearestShip);
      return true;
    }

    // 5 Coins = 50% success
    if (coins >= 5)
    {
      RemoveItemCount(inv, "Coins", "$item_coins", 5);
      bool success = UnityEngine.Random.value <= 0.5f;
      ExecuteTaming(player, success, nearestShip: nearestShip);
      return true;
    }

    // 5 Dandelions = 50% success
    if (dandelions >= 5)
    {
      RemoveItemCount(inv, "Dandelion", "$item_dandelion", 5);
      bool success = UnityEngine.Random.value <= 0.5f;
      ExecuteTaming(player, success, nearestShip: nearestShip);
      return true;
    }

    // Insufficient items
    player.Message(MessageHud.MessageType.Center,
      Localization.instance.Localize("$valheim_vehicles_tame_insufficient"));
    return false;
  }

  public bool UseItem(Humanoid user, ItemDrop.ItemData item)
  {
    if (user == null || !user.IsPlayer() || item == null) return false;
    var player = user as Player;
    if (player == null) return false;

    // If already tamed, check if player is offering/changing a Sailor Hat!
    if (_character != null && _character.IsTamed())
    {
      string prefabName = item.m_dropPrefab != null ? item.m_dropPrefab.name : "";
      if (RaftGreydwarfSailorComponent.IsSailorHat(prefabName))
      {
        var sailorComp = EnsureSailorComponent();
        if (sailorComp != null)
        {
          sailorComp.EquipSpecificHat(prefabName);
          player.Message(MessageHud.MessageType.Center,
            $"Equipped {item.m_shared.m_name} on Sailor Greydwarf!");
          return true;
        }
      }
      return false;
    }

    var inv = player.GetInventory();
    if (inv == null) return false;

    bool isCoins = IsItemMatch(item, "Coins", "$item_coins");
    bool isDandelion = IsItemMatch(item, "Dandelion", "$item_dandelion");

    if (!isCoins && !isDandelion)
    {
      return false;
    }

    if (!CanHireSailor(player, out string reason, out var nearestShip))
    {
      player.Message(MessageHud.MessageType.Center, reason);
      return true; // handled, don't consume or equip item
    }

    if (isCoins)
    {
      int coinCount = GetItemCount(inv, "Coins", "$item_coins");
      if (coinCount >= 10)
      {
        RemoveItemCount(inv, "Coins", "$item_coins", 10);
        ExecuteTaming(player, success: true, nearestShip: nearestShip);
        return true;
      }
      if (coinCount >= 5)
      {
        RemoveItemCount(inv, "Coins", "$item_coins", 5);
        bool success = UnityEngine.Random.value <= 0.5f;
        ExecuteTaming(player, success, nearestShip: nearestShip);
        return true;
      }
      player.Message(MessageHud.MessageType.Center,
        Localization.instance.Localize("$valheim_vehicles_tame_insufficient"));
      return true;
    }

    if (isDandelion)
    {
      int dandelionCount = GetItemCount(inv, "Dandelion", "$item_dandelion");
      if (dandelionCount >= 5)
      {
        RemoveItemCount(inv, "Dandelion", "$item_dandelion", 5);
        bool success = UnityEngine.Random.value <= 0.5f;
        ExecuteTaming(player, success, nearestShip: nearestShip);
        return true;
      }
      player.Message(MessageHud.MessageType.Center,
        Localization.instance.Localize("$valheim_vehicles_tame_insufficient"));
      return true;
    }

    return false;
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
        sailor.EnsureRandomSailorHat();
        nearestShip.RegisterSailor(sailor);
        sailor.StartEmbarkSequence(nearestShip.transform.position);
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

  private static VehiclePiecesController? FindNearestShip(Vector3 pos, float maxDistance = 250f)
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
    return nearest;
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
