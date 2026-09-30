using System;
using UnityEngine;
using ValheimVehicles.Controllers;

namespace ValheimVehicles.Components;

public class RaftGreydwarfSailorComponent : MonoBehaviour
{
  public GreydwarfSailorType DwarfType { get; private set; } =
    GreydwarfSailorType.Regular;

  public VehiclePiecesController? PiecesController { get; private set; }
  public Character? Character => _character;
  public bool IsUnfed => _isUnfed;
  public bool IsSeated => _isSeated;

  private Character? _character;
  private Humanoid? _humanoid;
  private MonsterAI? _monsterAI;
  private Animator? _animator;
  private ZNetView? _nview;

  private GreydwarfRowingSeatComponent? _currentSeat;
  private bool _isSeated;
  private float _manningTimer;

  // Upkeep & Mutiny (15 minutes = 900s)
  private float _resinCheckTimer = 900f;
  private float _mutinyTickTimer = 60f;
  private bool _isUnfed;

  // Combat cooldowns
  private float _nextRockThrowTime;
  private float _nextTauntTime;
  private float _nextHealTime;

  // Overboard recovery
  private float _waterTimer;

  private void Awake()
  {
    _character = GetComponent<Character>();
    _humanoid = GetComponent<Humanoid>();
    _monsterAI = GetComponent<MonsterAI>();
    _animator = GetComponentInChildren<Animator>();
    _nview = GetComponent<ZNetView>();

    DetermineDwarfType();
  }

  private void Start()
  {
    if (PiecesController == null)
    {
      PiecesController = FindNearestShip(transform.position);
      PiecesController?.RegisterSailor(this);
    }
  }

  private void DetermineDwarfType()
  {
    string name = gameObject.name;
    if (name.IndexOf("Shaman", StringComparison.OrdinalIgnoreCase) >= 0)
    {
      DwarfType = GreydwarfSailorType.Shaman;
    }
    else if (name.IndexOf("Elite", StringComparison.OrdinalIgnoreCase) >= 0 ||
             name.IndexOf("Brute", StringComparison.OrdinalIgnoreCase) >= 0)
    {
      DwarfType = GreydwarfSailorType.Brute;
    }
    else
    {
      DwarfType = GreydwarfSailorType.Regular;
    }
  }

  public void SetAssignedShip(VehiclePiecesController ship)
  {
    PiecesController = ship;
  }

  private void Update()
  {
    if (_character == null || !_character.IsTamed()) return;

    if (PiecesController == null)
    {
      PiecesController = FindNearestShip(transform.position);
      PiecesController?.RegisterSailor(this);
      if (PiecesController == null) return;
    }

    UpdateUpkeep(Time.deltaTime);
    UpdateManningAndSeating(Time.deltaTime);
    UpdateWaterAndLadderRecovery(Time.deltaTime);
    UpdateCombatDefense();
  }

  private void UpdateUpkeep(float dt)
  {
    _resinCheckTimer -= dt;
    if (_resinCheckTimer <= 0f)
    {
      _resinCheckTimer = 900f; // 15 minutes
      ConsumeResin();
    }

    if (_isUnfed)
    {
      _mutinyTickTimer -= dt;
      if (_mutinyTickTimer <= 0f)
      {
        _mutinyTickTimer = 60f;
        // 50% chance per tick to mutiny
        if (UnityEngine.Random.value <= 0.5f)
        {
          TriggerMutiny();
        }
      }
    }
  }

  private void ConsumeResin()
  {
    if (PiecesController == null) return;

    bool fed = false;
    foreach (var piece in PiecesController.Pieces)
    {
      if (piece == null) continue;
      var container = piece.GetComponent<Container>() ?? piece.GetComponentInChildren<Container>();
      if (container != null && container.GetInventory() != null)
      {
        if (container.GetInventory().CountItems("Resin") >= 1)
        {
          container.GetInventory().RemoveItem("Resin", 1);
          fed = true;
          break;
        }
      }
    }

    if (fed)
    {
      _isUnfed = false;
    }
    else
    {
      _isUnfed = true;
      MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center,
        Localization.instance.Localize("$valheim_vehicles_sailor_unfed_warning"));
    }
  }

  private void TriggerMutiny()
  {
    if (_character == null) return;

    _character.SetTamed(false);
    _character.m_faction = Character.Faction.ForestMonsters;

    if (_isSeated && _currentSeat != null)
    {
      _currentSeat.OccupantCharacter = null;
      _currentSeat = null;
      _isSeated = false;
      if (_character.m_body != null) _character.m_body.isKinematic = false;
    }

    PiecesController?.UnregisterSailor(this);

    MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center,
      Localization.instance.Localize("$valheim_vehicles_sailor_mutinied"));

    // Flee or swim towards shore
    if (_monsterAI != null)
    {
      _monsterAI.SetAlerted(true);
    }

    Destroy(this);
  }

  private void UpdateManningAndSeating(float dt)
  {
    if (PiecesController == null) return;
    var moveCtrl = PiecesController.MovementController;
    bool isShipMoving = moveCtrl != null && moveCtrl.GetSpeedSetting() != Ship.Speed.Stop && !moveCtrl.isAnchored;

    if (isShipMoving)
    {
      if (!_isSeated)
      {
        _manningTimer += dt;
        var availableSeat = FindAvailableSeat();
        if (availableSeat != null)
        {
          // 5-second grace period: if pathing or waiting exceeds 5s, teleport to seat
          if (_manningTimer >= 5.0f || Vector3.Distance(transform.position, availableSeat.GetSeatPosition()) <= 1.5f)
          {
            SeatAtStation(availableSeat);
            _manningTimer = 0f;
          }
        }
      }
    }
    else
    {
      _manningTimer = 0f;
      if (_isSeated)
      {
        // Ship stopped/anchored: unseat
        UnseatFromStation();
      }
    }
  }

  private GreydwarfRowingSeatComponent? FindAvailableSeat()
  {
    if (PiecesController == null) return null;
    GreydwarfRowingSeatComponent? closest = null;
    float minDist = float.MaxValue;

    foreach (var seat in PiecesController.GreydwarfRowingSeats)
    {
      if (seat == null || seat.OccupantCharacter != null) continue;
      float d = Vector3.Distance(transform.position, seat.GetSeatPosition());
      if (d < minDist)
      {
        minDist = d;
        closest = seat;
      }
    }
    return closest;
  }

  private void SeatAtStation(GreydwarfRowingSeatComponent seat)
  {
    _currentSeat = seat;
    _currentSeat.OccupantCharacter = _character;
    _isSeated = true;

    transform.position = seat.GetSeatPosition();
    transform.rotation = seat.transform.rotation;

    if (_character != null && _character.m_body != null)
    {
      _character.m_body.isKinematic = true;
    }
  }

  private void UnseatFromStation()
  {
    if (_currentSeat != null)
    {
      _currentSeat.OccupantCharacter = null;
      _currentSeat = null;
    }
    _isSeated = false;

    if (_character != null && _character.m_body != null)
    {
      _character.m_body.isKinematic = false;
    }
  }

  private void UpdateWaterAndLadderRecovery(float dt)
  {
    if (_character == null || PiecesController == null) return;

    if (_character.InWater())
    {
      // Check for nearby rope ladder
      foreach (var ladder in PiecesController.RopeLadders)
      {
        if (ladder == null || ladder.m_attachPoint == null || ladder.m_exitPoint == null) continue;
        if (Vector3.Distance(transform.position, ladder.m_attachPoint.position) <= 3.5f)
        {
          // Clamber up ladder to deck
          transform.position = ladder.m_exitPoint.position;
          _waterTimer = 0f;
          return;
        }
      }

      _waterTimer += dt;
      // 10-second overboard failsafe recovery to Planter
      if (_waterTimer >= 10.0f)
      {
        Vector3 safePos = PiecesController.GetPlanterOrSafeDeckPosition();
        transform.position = safePos;
        _waterTimer = 0f;
        ZLog.Log("[ValheimRAFT] Sailor Greydwarf was overboard >10s; safely recovered to Ship Planter.");
      }
    }
    else
    {
      _waterTimer = 0f;
    }
  }

  private void UpdateCombatDefense()
  {
    if (_character == null || PiecesController == null) return;

    // Scan for hostiles within 20m of ship
    var colliders = Physics.OverlapSphere(transform.position, 20f, LayerMask.GetMask("character"));
    Character? target = null;
    foreach (var col in colliders)
    {
      var ch = col.GetComponentInParent<Character>();
      if (ch != null && !ch.IsTamed() && !ch.IsPlayer() && ch.GetFaction() != Character.Faction.Players)
      {
        target = ch;
        break;
      }
    }

    if (target == null) return;

    switch (DwarfType)
    {
      case GreydwarfSailorType.Regular:
        if (Time.time >= _nextRockThrowTime)
        {
          _nextRockThrowTime = Time.time + 10f;
          PerformRockThrow(target);
        }
        break;

      case GreydwarfSailorType.Brute:
        if (Time.time >= _nextTauntTime)
        {
          _nextTauntTime = Time.time + 15f;
          PerformTaunt(target);
        }
        break;

      case GreydwarfSailorType.Shaman:
        if (Time.time >= _nextHealTime)
        {
          _nextHealTime = Time.time + 12f;
          PerformHealing();
        }
        break;
    }
  }

  private void PerformRockThrow(Character target)
  {
    if (_monsterAI != null)
    {
      _monsterAI.DoAttack(target, false);
    }
    else if (_humanoid != null)
    {
      _humanoid.StartAttack(target, false);
    }
  }

  private void PerformTaunt(Character target)
  {
    if (_animator != null)
    {
      _animator.SetTrigger("taunt");
    }

    var monsterAI = target.GetComponent<MonsterAI>();
    if (monsterAI != null)
    {
      monsterAI.SetTarget(_character);
      monsterAI.SetAlerted(true);
    }

    if (ZNetScene.instance != null)
    {
      var vfx = ZNetScene.instance.GetPrefab("vfx_greydwarf_elite_growl") ??
                ZNetScene.instance.GetPrefab("sfx_greydwarf_elite_alert");
      if (vfx != null)
      {
        Instantiate(vfx, transform.position + Vector3.up * 1.5f, Quaternion.identity);
      }
    }
  }

  private void PerformHealing()
  {
    if (_animator != null)
    {
      _animator.SetTrigger("attack");
    }

    if (PiecesController != null)
    {
      foreach (var sailor in PiecesController.ActiveSailors)
      {
        if (sailor != null && sailor.Character != null)
        {
          sailor.Character.Heal(25f);
        }
      }
    }

    if (Player.m_localPlayer != null &&
        Vector3.Distance(transform.position, Player.m_localPlayer.transform.position) <= 15f)
    {
      Player.m_localPlayer.Heal(25f);
    }

    if (ZNetScene.instance != null)
    {
      var healVfx = ZNetScene.instance.GetPrefab("vfx_greydwarf_shaman_heal") ??
                    ZNetScene.instance.GetPrefab("vfx_Heal");
      if (healVfx != null)
      {
        Instantiate(healVfx, transform.position + Vector3.up * 1f, Quaternion.identity);
      }
    }
  }

  private void OnDestroy()
  {
    UnseatFromStation();
    PiecesController?.UnregisterSailor(this);
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
