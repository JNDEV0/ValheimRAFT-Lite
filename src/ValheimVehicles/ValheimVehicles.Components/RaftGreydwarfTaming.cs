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
      EnsureSailorComponent();
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

  public bool Interact(Humanoid user, bool hold, bool alt)
  {
    if (hold) return false;
    if (user == null || !user.IsPlayer()) return false;
    if (_character != null && _character.IsTamed()) return false;

    var player = user as Player;
    if (player == null) return false;

    var inv = player.GetInventory();
    if (inv == null) return false;

    // 10 Coins = 100% success
    if (inv.CountItems("Coins") >= 10)
    {
      inv.RemoveItem("Coins", 10);
      ExecuteTaming(player, success: true);
      return true;
    }

    // 5 Coins = 50% success
    if (inv.CountItems("Coins") >= 5)
    {
      inv.RemoveItem("Coins", 5);
      bool success = UnityEngine.Random.value <= 0.5f;
      ExecuteTaming(player, success);
      return true;
    }

    // 5 Dandelions = 50% success
    if (inv.CountItems("Dandelion") >= 5)
    {
      inv.RemoveItem("Dandelion", 5);
      bool success = UnityEngine.Random.value <= 0.5f;
      ExecuteTaming(player, success);
      return true;
    }

    // Insufficient items
    player.Message(MessageHud.MessageType.Center,
      Localization.instance.Localize("$valheim_vehicles_tame_insufficient"));
    return false;
  }

  public bool UseItem(Humanoid user, ItemDrop.ItemData item)
  {
    return false;
  }

  private void ExecuteTaming(Player player, bool success)
  {
    if (success)
    {
      if (_character != null)
      {
        _character.SetTamed(true);
        _character.m_faction = Character.Faction.Players;
      }

      if (_monsterAI != null)
      {
        _monsterAI.SetAlerted(false);
        _monsterAI.SetTarget(null);
      }

      SpawnTameEffects();
      var sailor = EnsureSailorComponent();

      // Find nearest ship to assign candidate
      var nearestShip = FindNearestShip(transform.position);
      if (nearestShip != null && sailor != null)
      {
        sailor.SetAssignedShip(nearestShip);
        nearestShip.RegisterSailor(sailor);
      }

      player.Message(MessageHud.MessageType.Center,
        Localization.instance.Localize("$valheim_vehicles_tame_success"));
    }
    else
    {
      player.Message(MessageHud.MessageType.Center,
        Localization.instance.Localize("$valheim_vehicles_tame_failed"));
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

  private static VehiclePiecesController? FindNearestShip(Vector3 pos, float maxDistance = 150f)
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
}
