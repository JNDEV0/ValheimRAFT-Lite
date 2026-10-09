using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ValheimVehicles.Controllers;

namespace ValheimVehicles.Components;

public enum SailorLoyalty
{
  Satisfied = 0,
  Unsatisfied = 1,
  Hungry = 2,
  NearMutiny = 3,
  Mutineer = 4
}

public class RaftGreydwarfSailorComponent : MonoBehaviour
{
  public static readonly string[] SailorHats =
  [
    "HelmetHat1",        // Blue Tied Headscarf
    "HelmetHat2",        // Green Twisted Headscarf
    "HelmetHat3",        // Fur Cap Brown
    "HelmetHat4",        // Extravagant Cap Green
    "HelmetHat5",        // Simple Cap Red
    "HelmetHat6",        // Yellow Tied Headscarf
    "HelmetHat7",        // Red Twisted Headscarf
    "HelmetHat8",        // Fur Cap Grey
    "HelmetFishing",     // Fishing Hat
    "HelmetMidsummerCrown" // Midsummer Crown
  ];

  // Sailor Hat transform offset controls (for in-game slider adjustments)
  public static Vector3 SailorHatPositionOffset = new Vector3(0.00f, 0.16f, 0.05f);
  public static Vector3 SailorHatRotationEuler = new Vector3(-90.0f, 0.0f, 180.0f);
  public static float SailorHatScale = 0.05f;

  public static bool IsSailorHat(string prefabName)
  {
    if (string.IsNullOrEmpty(prefabName)) return false;
    foreach (var hat in SailorHats)
    {
      if (hat.Equals(prefabName, StringComparison.OrdinalIgnoreCase)) return true;
    }
    if (prefabName.Equals("HelmetFishingHat", StringComparison.OrdinalIgnoreCase) ||
        prefabName.Equals("HelmetStrawHat", StringComparison.OrdinalIgnoreCase))
    {
      return true;
    }
    return false;
  }

  public GreydwarfSailorType DwarfType { get; private set; } =
    GreydwarfSailorType.Regular;

  public VehiclePiecesController? PiecesController { get; private set; }
  public Character? Character => _character;
  public SailorLoyalty Loyalty { get; private set; } = SailorLoyalty.Satisfied;
  public bool IsEnRouteToShip { get; private set; }

  private Character? _character;
  private Humanoid? _humanoid;
  private MonsterAI? _monsterAI;
  private Animator? _animator;
  private ZNetView? _nview;

  // Upkeep & Loyalty (5 minutes = 300s)
  private float _resinCheckTimer = 300f;
  private float _mutinyTickTimer = 60f;

  // Combat cooldowns & state
  private float _nextRockThrowTime;
  private float _nextTauntTime;
  private float _nextHealTime;
  private bool _isInCombat;

  // Deck rooting & safe anchoring
  private Vector3? _deckLocalPos;
  private Quaternion? _deckLocalRot;
  private bool _isAnchoredOnDeck;
  private Vector3? _lastSafeLocalPos;
  private Quaternion? _lastSafeLocalRot;
  private bool _isShipMoving;

  // Edge avoidance pause & forced retreat
  private float _edgeAvoidancePauseTimer;
  private Vector3? _forcedRetreatTarget;
  private Vector3 _lastEdgeTriggerPos;

  // Idle animation pacing (3-second still timeout)
  private float _idleCycleTimer;
  private bool _isIdleFrozen;

  // Overboard recovery
  private float _waterTimer;

  // Top deck / Open-sky roaming
  private readonly List<Vector3> _cachedTopDeckPositions = new();
  private float _topDeckCacheTimer;
  private float _underRoofTimer;

  // Passive utility: Item pickup & Chest deposit
  private ItemDrop? _targetItemDrop;
  private ItemDrop.ItemData? _carriedItem;
  private Container? _targetContainer;
  private float _itemScanTimer;
  private float _itemActionCooldown;

  private void Awake()
  {
    _character = GetComponent<Character>();
    _humanoid = GetComponent<Humanoid>();
    _monsterAI = GetComponent<MonsterAI>();
    _animator = GetComponentInChildren<Animator>();
    _nview = GetComponent<ZNetView>();

    DetermineDwarfType();

    if (_nview != null && _nview.IsValid())
    {
      Loyalty = (SailorLoyalty)_nview.GetZDO().GetInt("SailorLoyalty", (int)SailorLoyalty.Satisfied);
    }
  }

  private void Start()
  {
    if (PiecesController == null)
    {
      PiecesController = FindNearestShip(transform.position);
      PiecesController?.RegisterSailor(this);
    }

    if (_character != null && _character.IsTamed())
    {
      EnsureRandomSailorHat();
      EnsureRockWeaponEquipped();
    }
  }

  private void OnDestroy()
  {
    if (_animator != null) _animator.speed = 1f;
    DropCarriedItem();
  }

  private void OnDisable()
  {
    if (_animator != null) _animator.speed = 1f;
  }

  private void DetermineDwarfType()
  {
    string goName = gameObject.name;
    if (goName.Contains("Shaman"))
    {
      DwarfType = GreydwarfSailorType.Shaman;
    }
    else if (goName.Contains("Elite") || goName.Contains("Brute"))
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

  public void FeedResin()
  {
    Loyalty = SailorLoyalty.Satisfied;
    _resinCheckTimer = 300f;
    if (_nview != null && _nview.IsValid())
    {
      _nview.GetZDO().Set("SailorLoyalty", (int)Loyalty);
    }
  }

  public string GetLoyaltyHoverString()
  {
    return Loyalty switch
    {
      SailorLoyalty.Satisfied => "<color=#00FF00>Satisfied</color>",
      SailorLoyalty.Unsatisfied => "<color=#FFFF00>Unsatisfied</color>",
      SailorLoyalty.Hungry => "<color=#FFA500>Hungry</color>",
      SailorLoyalty.NearMutiny => "<color=#FF4500>Near-Mutiny</color>",
      SailorLoyalty.Mutineer => "<color=#FF0000>Mutineer</color>",
      _ => "<color=#00FF00>Satisfied</color>"
    };
  }

  public void StartEmbarkSequence(Vector3 shipPosition)
  {
    TeleportToShipDeck();
  }

  public void TeleportToShipDeck()
  {
    if (PiecesController == null)
    {
      PiecesController = FindNearestShip(transform.position);
    }
    if (PiecesController == null) return;

    RopeLadderComponent? ladder = PiecesController.RopeLadders.FirstOrDefault(l => l != null && l.m_exitPoint != null);
    Vector3 targetPos = ladder != null ? ladder.m_exitPoint.position : PiecesController.GetPlanterOrSafeDeckPosition();

    if (ZNetScene.instance != null)
    {
      var poof = ZNetScene.instance.GetPrefab("vfx_boar_love") ?? ZNetScene.instance.GetPrefab("vfx_tame");
      if (poof != null)
      {
        Instantiate(poof, transform.position + Vector3.up * 0.8f, Quaternion.identity);
        Instantiate(poof, targetPos + Vector3.up * 0.8f, Quaternion.identity);
      }
    }

    transform.position = targetPos;
    transform.SetParent(PiecesController.transform);

    if (_character != null && _character.m_body != null && !_character.m_body.isKinematic)
    {
      _character.m_body.linearVelocity = Vector3.zero;
      _character.m_body.angularVelocity = Vector3.zero;
    }

    _deckLocalPos = transform.localPosition;
    _deckLocalRot = transform.localRotation;
    _lastSafeLocalPos = transform.localPosition;
    _lastSafeLocalRot = transform.localRotation;
    _isAnchoredOnDeck = false;

    EnsureRandomSailorHat();
    EnsureRockWeaponEquipped();

    if (_monsterAI != null)
    {
      _monsterAI.SetAlerted(false);
      _monsterAI.SetTarget(null);
      _monsterAI.m_targetCreature = null;
    }
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

    float dt = Time.deltaTime;
    UpdateUpkeep(dt);
    UpdateDeckStation(dt);
    UpdateWaterAndLadderRecovery(dt);
    UpdateCombatDefense(dt);

    if (!_isInCombat && !_isShipMoving)
    {
      UpdateItemGathering(dt);
    }
  }

  private void UpdateUpkeep(float dt)
  {
    _resinCheckTimer -= dt;
    if (_resinCheckTimer <= 0f)
    {
      _resinCheckTimer = 300f; // 5 minutes
      ConsumeResin();
    }

    if (Loyalty == SailorLoyalty.NearMutiny)
    {
      _mutinyTickTimer -= dt;
      if (_mutinyTickTimer <= 0f)
      {
        _mutinyTickTimer = 60f;
        // 50% chance per tick to mutiny when Near-Mutiny
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
    List<Container> candidateContainers = new();

    if (PiecesController.Pieces != null)
    {
      foreach (var piece in PiecesController.Pieces)
      {
        if (piece == null) continue;
        var container = piece.GetComponent<Container>() ?? piece.GetComponentInChildren<Container>();
        if (container != null && !candidateContainers.Contains(container))
        {
          candidateContainers.Add(container);
        }
      }
    }

    // Also scan nearby containers on or around the vessel (within 20m)
    var colliders = Physics.OverlapSphere(transform.position, 20f);
    foreach (var col in colliders)
    {
      var container = col.GetComponentInParent<Container>();
      if (container != null && !candidateContainers.Contains(container))
      {
        candidateContainers.Add(container);
      }
    }

    foreach (var container in candidateContainers)
    {
      if (container == null) continue;
      var inv = container.GetInventory();
      if (inv == null) continue;

      ItemDrop.ItemData? resinItem = null;
      foreach (var item in inv.GetAllItems())
      {
        if (item == null) continue;
        string sName = item.m_shared != null ? item.m_shared.m_name : "";
        string pName = item.m_dropPrefab != null ? item.m_dropPrefab.name : "";

        if (sName.Equals("$item_resin", StringComparison.OrdinalIgnoreCase) ||
            sName.Equals("Resin", StringComparison.OrdinalIgnoreCase) ||
            pName.Equals("Resin", StringComparison.OrdinalIgnoreCase))
        {
          resinItem = item;
          break;
        }
      }

      if (resinItem != null)
      {
        inv.RemoveOneItem(resinItem);
        container.Save();
        container.m_openEffects?.Create(container.transform.position, Quaternion.identity);

        if (DamageText.instance != null)
        {
          DamageText.instance.ShowText(DamageText.TextType.Normal,
            container.transform.position + Vector3.up * 1f,
            "-1 Resin (Fed Greydwarf Sailor)");
        }

        fed = true;
        break;
      }
    }

    if (fed)
    {
      Loyalty = SailorLoyalty.Satisfied;
      if (_nview != null && _nview.IsValid())
      {
        _nview.GetZDO().Set("SailorLoyalty", (int)Loyalty);
      }
    }
    else
    {
      // Step down loyalty
      if (Loyalty < SailorLoyalty.Mutineer)
      {
        Loyalty = (SailorLoyalty)((int)Loyalty + 1);
        if (_nview != null && _nview.IsValid())
        {
          _nview.GetZDO().Set("SailorLoyalty", (int)Loyalty);
        }

        MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center,
          Localization.instance.Localize("$valheim_vehicles_sailor_unfed_warning"));
      }

      if (Loyalty == SailorLoyalty.Mutineer)
      {
        TriggerMutiny();
      }
    }
  }

  private void TriggerMutiny()
  {
    if (_character == null) return;

    DropCarriedItem();
    _character.SetTamed(false);
    _character.m_faction = Character.Faction.ForestMonsters;

    RemoveSailorHat();
    PiecesController?.UnregisterSailor(this);

    MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center,
      Localization.instance.Localize("$valheim_vehicles_sailor_mutinied"));

    if (_monsterAI != null)
    {
      _monsterAI.SetAlerted(true);
    }

    Destroy(this);
  }

  public void Dismiss(Player player)
  {
    if (_character == null) return;

    DropCarriedItem();
    _character.SetTamed(false);
    _character.m_faction = Character.Faction.ForestMonsters;

    RemoveSailorHat();
    PiecesController?.UnregisterSailor(this);

    if (transform.parent != null)
    {
      transform.SetParent(null);
    }

    if (_character.m_body != null)
    {
      _character.m_body.isKinematic = false;
      // Launch off the boat into the water away from ship center
      Vector3 shipCenter = PiecesController != null ? PiecesController.transform.position : transform.position;
      Vector3 jumpDir = (transform.position - shipCenter);
      jumpDir.y = 0f;
      if (jumpDir.sqrMagnitude < 0.1f) jumpDir = transform.forward;
      jumpDir.Normalize();
      jumpDir += Vector3.up * 0.35f;
      _character.m_body.linearVelocity = jumpDir.normalized * 7.5f;
    }

    if (_monsterAI != null && player != null)
    {
      _monsterAI.SetAlerted(true);
      _monsterAI.Flee(Time.deltaTime, player.transform.position);
    }

    MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center,
      Localization.instance.Localize("$valheim_vehicles_sailor_dismissed"));

    Destroy(this);
  }

  private void UpdateDeckStation(float dt)
  {
    if (PiecesController == null || _character == null) return;
    if (_character.InWater()) return;

    // Ensure parented to ship so coordinates move with the vessel
    if (transform.parent != PiecesController.transform)
    {
      transform.SetParent(PiecesController.transform);
    }

    // Clear target if target is not actively alerted (no exclamation mark)
    if (_monsterAI != null && _monsterAI.m_targetCreature != null)
    {
      var targetAI = _monsterAI.m_targetCreature.GetBaseAI();
      if (targetAI == null || !targetAI.IsAlerted())
      {
        _monsterAI.SetTarget(null);
        _monsterAI.m_targetCreature = null;
      }
    }

    // Detect if ship is moving or rotating
    _isShipMoving = false;
    var moveCtrl = PiecesController.MovementController;
    if (moveCtrl != null && moveCtrl.isAnchored)
    {
      _isShipMoving = false;
    }
    else if (moveCtrl != null && moveCtrl.GetSpeedSetting() != Ship.Speed.Stop)
    {
      _isShipMoving = true;
    }
    else
    {
      var rb = PiecesController.m_syncRigidbody != null ? PiecesController.m_syncRigidbody : PiecesController.m_localRigidbody;
      if (rb != null && rb.linearVelocity.sqrMagnitude > 0.25f)
      {
        _isShipMoving = true;
      }
    }

    if (_isShipMoving)
    {
      // Root firmly to deck position when ship is in motion
      if (!_isAnchoredOnDeck)
      {
        _deckLocalPos = transform.localPosition;
        _deckLocalRot = transform.localRotation;
        _isAnchoredOnDeck = true;
      }

      if (_character.m_body != null)
      {
        _character.m_body.isKinematic = true;
      }

      if (_deckLocalPos.HasValue)
      {
        transform.localPosition = _deckLocalPos.Value;
      }
      if (_deckLocalRot.HasValue)
      {
        transform.localRotation = _deckLocalRot.Value;
      }

      if (_monsterAI != null)
      {
        _monsterAI.StopMoving();
      }
    }
    else
    {
      // Ship is stopped: allow standing/idle ground physics
      if (_isAnchoredOnDeck)
      {
        _isAnchoredOnDeck = false;
        if (_character.m_body != null)
        {
          _character.m_body.isKinematic = false;
        }
      }

      // 1. Edge avoidance lookahead: prevents walking off edges into open water
      UpdateEdgeAvoidance(dt);

      // 2. Hull edge guard: Raycast downwards to make sure sailor does not step off the ship's hull
      Ray ray = new Ray(transform.position + Vector3.up * 0.5f, Vector3.down);
      bool isOverBoat = false;
      if (Physics.Raycast(ray, out var hit, 3.5f))
      {
        var hitVpc = hit.collider.GetComponentInParent<VehiclePiecesController>();
        if (hitVpc == PiecesController)
        {
          isOverBoat = true;
          _lastSafeLocalPos = transform.localPosition;
          _lastSafeLocalRot = transform.localRotation;
        }
      }

      if (!isOverBoat && _lastSafeLocalPos.HasValue)
      {
        // Stepped over edge or foreign collider: keep rooted safely on deck
        transform.localPosition = _lastSafeLocalPos.Value;
        if (_lastSafeLocalRot.HasValue) transform.localRotation = _lastSafeLocalRot.Value;
        if (_character.m_body != null && !_character.m_body.isKinematic)
        {
          _character.m_body.linearVelocity = Vector3.zero;
          _character.m_body.angularVelocity = Vector3.zero;
        }

        // BREAK TELEPORT/CLIPPING LOOP:
        // Cancel forward motion, trigger 2s pause timeout and 3m inward retreat target
        if (_character != null) _character.m_moveDir = Vector3.zero;
        if (_monsterAI != null)
        {
          _monsterAI.StopMoving();
          _monsterAI.SetTarget(null);
          _monsterAI.m_targetCreature = null;
        }

        _edgeAvoidancePauseTimer = 2.0f;
        _lastEdgeTriggerPos = transform.position;

        Vector3 shipCenter = PiecesController.transform.position;
        Vector3 inwardDir = (shipCenter - transform.position);
        inwardDir.y = 0f;
        if (inwardDir.sqrMagnitude < 0.25f)
        {
          inwardDir = -transform.forward;
          inwardDir.y = 0f;
        }
        _forcedRetreatTarget = transform.position + inwardDir.normalized * 3.0f;
      }

      // 3. Idle top-deck preference when calm:
      // If indoors/under a roof inside the hull, path up to the open top deck
      if (!_isInCombat && _carriedItem == null && _targetItemDrop == null)
      {
        if (IsUnderRoof(transform.position))
        {
          _underRoofTimer += dt;
          if (_underRoofTimer > 2.5f)
          {
            Vector3 topDeckTarget = GetTopDeckPosition(GetInstanceID());
            if (_monsterAI != null)
            {
              _monsterAI.MoveTo(dt, topDeckTarget, 1f, false);
            }
          }
        }
        else
        {
          _underRoofTimer = 0f;
        }
      }
    }

    // 4. Idle animation pacing control (3-second still sentry timeout)
    UpdateIdleAnimationControl(dt);
  }

  private void UpdateIdleAnimationControl(float dt)
  {
    if (_animator == null || _character == null) return;

    bool isMovingOrBusy = _character.m_moveDir.sqrMagnitude > 0.01f ||
                          (_character.m_body != null && _character.m_body.linearVelocity.sqrMagnitude > 0.1f) ||
                          _character.InAttack() ||
                          _character.InWater() ||
                          _isInCombat ||
                          _carriedItem != null ||
                          _targetItemDrop != null ||
                          (_monsterAI != null && _monsterAI.GetTargetCreature() != null);

    if (isMovingOrBusy)
    {
      if (_isIdleFrozen)
      {
        _animator.speed = 1f;
        _isIdleFrozen = false;
      }
      _idleCycleTimer = 0f;
      return;
    }

    _idleCycleTimer += dt;
    if (_isIdleFrozen)
    {
      // In 3-second still timeout
      if (_idleCycleTimer >= 3.0f)
      {
        _animator.speed = 1f;
        _isIdleFrozen = false;
        _idleCycleTimer = 0f;
      }
      else
      {
        _animator.speed = 0f;
      }
    }
    else
    {
      // Playing idle animation for 2.5 seconds: then freeze for 3.0 seconds
      if (_idleCycleTimer >= 2.5f)
      {
        _animator.speed = 0f;
        _isIdleFrozen = true;
        _idleCycleTimer = 0f;
      }
      else
      {
        _animator.speed = 1f;
      }
    }
  }

  private void UpdateEdgeAvoidance(float dt)
  {
    if (PiecesController == null || _character == null || _character.InWater()) return;

    // A. 2-second timeout when an edge or obstacle was encountered: stand completely still
    if (_edgeAvoidancePauseTimer > 0f)
    {
      _edgeAvoidancePauseTimer -= dt;
      _character.m_moveDir = Vector3.zero;
      if (_monsterAI != null)
      {
        _monsterAI.StopMoving();
      }
      return;
    }

    // B. Forced retreat pathing at least 2.5–3.0m away from the danger spot
    if (_forcedRetreatTarget.HasValue)
    {
      Vector3 flatCur = new Vector3(transform.position.x, 0, transform.position.z);
      Vector3 flatOrigin = new Vector3(_lastEdgeTriggerPos.x, 0, _lastEdgeTriggerPos.z);
      Vector3 flatTarget = new Vector3(_forcedRetreatTarget.Value.x, 0, _forcedRetreatTarget.Value.z);

      float distFromEdge = Vector3.Distance(flatCur, flatOrigin);
      float distToTarget = Vector3.Distance(flatCur, flatTarget);

      if (distFromEdge < 2.5f && distToTarget > 0.8f)
      {
        if (_monsterAI != null)
        {
          _monsterAI.MoveTo(dt, _forcedRetreatTarget.Value, 1f, false);
        }
        return;
      }
      else
      {
        _forcedRetreatTarget = null;
      }
    }

    Vector3 moveDir = _character.m_moveDir;
    if (moveDir.sqrMagnitude < 0.01f) return;

    Vector3 forwardDir = moveDir.normalized;
    Vector3 lookaheadPos = transform.position + forwardDir * 1.0f;

    // Raise raycast origin to +2.5f and cast down 5.5f so stairs ascending ahead are detected!
    Ray lookRay = new Ray(lookaheadPos + Vector3.up * 2.5f, Vector3.down);
    bool safeAhead = false;
    bool hitStair = false;
    Collider? stairCollider = null;

    if (Physics.Raycast(lookRay, out var hit, 5.5f, LayerMask.GetMask("piece", "Default", "static_solid")))
    {
      var hitVpc = hit.collider.GetComponentInParent<VehiclePiecesController>();
      if (hitVpc == PiecesController)
      {
        safeAhead = true;
        if (IsStair(hit.collider))
        {
          hitStair = true;
          stairCollider = hit.collider;
        }
      }
    }

    // Also check horizontal forward ray at chest height (+0.8f) for direct stair collider contact
    if (!hitStair)
    {
      Ray forwardRay = new Ray(transform.position + Vector3.up * 0.8f, forwardDir);
      if (Physics.Raycast(forwardRay, out var fHit, 1.5f, LayerMask.GetMask("piece", "Default", "static_solid")))
      {
        var fVpc = fHit.collider.GetComponentInParent<VehiclePiecesController>();
        if (fVpc == PiecesController && IsStair(fHit.collider))
        {
          safeAhead = true;
          hitStair = true;
          stairCollider = fHit.collider;
        }
      }
    }

    // If stair detected, guide pathing toward top of stairs instead of considering it an obstacle
    if (hitStair && stairCollider != null)
    {
      Bounds b = stairCollider.bounds;
      Vector3 stairTop = b.center + Vector3.up * (b.extents.y * 0.9f);
      if (transform.position.y < stairTop.y - 0.3f)
      {
        if (_monsterAI != null)
        {
          _monsterAI.MoveTo(dt, stairTop, 1f, false);
        }
      }
      return;
    }

    if (!safeAhead)
    {
      // Edge / drop-off detected ahead! Stop moving forward immediately
      _character.m_moveDir = Vector3.zero;
      if (_monsterAI != null)
      {
        _monsterAI.StopMoving();
      }

      // Enter 2-second timeout and define 3.0m forced retreat path toward ship interior
      _edgeAvoidancePauseTimer = 2.0f;
      _lastEdgeTriggerPos = transform.position;

      Vector3 shipCenter = PiecesController.transform.position;
      Vector3 inwardDir = (shipCenter - transform.position);
      inwardDir.y = 0f;
      if (inwardDir.sqrMagnitude < 0.25f)
      {
        inwardDir = -forwardDir;
        inwardDir.y = 0f;
      }

      _forcedRetreatTarget = transform.position + inwardDir.normalized * 3.0f;
    }
  }

  private static bool IsStair(Collider col)
  {
    if (col == null) return false;
    var piece = col.GetComponentInParent<Piece>();
    if (piece != null)
    {
      string pName = piece.name.ToLower();
      if (pName.Contains("stair") || pName.Contains("ladder") || pName.Contains("stepladder"))
        return true;
    }
    string colName = col.name.ToLower();
    return colName.Contains("stair") || colName.Contains("ladder") || colName.Contains("stepladder");
  }

  public bool IsUnderRoof(Vector3 pos)
  {
    return Physics.Raycast(pos + Vector3.up * 0.5f, Vector3.up, out _, 12f, LayerMask.GetMask("piece", "Default", "static_solid"));
  }

  public Vector3 GetTopDeckPosition(int sailorIndexSeed = 0)
  {
    if (PiecesController == null) return transform.position;

    if (_cachedTopDeckPositions.Count == 0 || Time.time > _topDeckCacheTimer)
    {
      RefreshTopDeckPositions();
    }

    if (_cachedTopDeckPositions.Count > 0)
    {
      int idx = Math.Abs(sailorIndexSeed) % _cachedTopDeckPositions.Count;
      return _cachedTopDeckPositions[idx];
    }

    return PiecesController.GetPlanterOrSafeDeckPosition();
  }

  private void RefreshTopDeckPositions()
  {
    _cachedTopDeckPositions.Clear();
    _topDeckCacheTimer = Time.time + 4.0f;
    if (PiecesController == null) return;

    var candidates = new List<(Vector3 pos, float localY, bool openSky)>();

    foreach (var p in PiecesController.Pieces)
    {
      if (p == null) continue;
      string pName = p.gameObject.name.ToLower();
      bool isWalkable = pName.Contains("deck") || pName.Contains("floor") || pName.Contains("platform") || pName.Contains("hull") || pName.Contains("stair") || pName.Contains("roof_top");
      if (!isWalkable) continue;

      Vector3 testPoint = p.transform.position + Vector3.up * 0.5f;
      if (Physics.Raycast(testPoint + Vector3.up * 0.5f, Vector3.down, out var gHit, 2.5f, LayerMask.GetMask("piece", "Default")))
      {
        if (gHit.collider.GetComponentInParent<VehiclePiecesController>() != PiecesController) continue;

        Vector3 groundPoint = gHit.point;
        bool openSky = !Physics.Raycast(groundPoint + Vector3.up * 0.3f, Vector3.up, 12f, LayerMask.GetMask("piece", "Default", "static_solid"));
        float localY = PiecesController.transform.InverseTransformPoint(groundPoint).y;
        candidates.Add((groundPoint, localY, openSky));
      }
    }

    var openSkyCandidates = candidates.Where(c => c.openSky).ToList();
    if (openSkyCandidates.Count > 0)
    {
      float maxLocalY = openSkyCandidates.Max(c => c.localY);
      var topTier = openSkyCandidates.Where(c => c.localY >= maxLocalY - 1.5f).Select(c => c.pos).ToList();
      _cachedTopDeckPositions.AddRange(topTier);
    }
    else if (candidates.Count > 0)
    {
      float maxLocalY = candidates.Max(c => c.localY);
      var topTier = candidates.Where(c => c.localY >= maxLocalY - 1.5f).Select(c => c.pos).ToList();
      _cachedTopDeckPositions.AddRange(topTier);
    }
  }

  private void UpdateWaterAndLadderRecovery(float dt)
  {
    if (_character == null || PiecesController == null) return;

    if (_character.InWater())
    {
      if (_isAnchoredOnDeck)
      {
        _isAnchoredOnDeck = false;
      }
      if (_character.m_body != null)
      {
        _character.m_body.isKinematic = false;
      }

      // Check for nearby rope ladder (within 3.5m)
      RopeLadderComponent? targetLadder = null;
      float closestDist = float.MaxValue;
      foreach (var ladder in PiecesController.RopeLadders)
      {
        if (ladder == null || ladder.m_attachPoint == null || ladder.m_exitPoint == null) continue;
        float d = Vector3.Distance(transform.position, ladder.m_attachPoint.position);
        if (d < closestDist)
        {
          closestDist = d;
          targetLadder = ladder;
        }
      }

      if (targetLadder != null && targetLadder.m_attachPoint != null && targetLadder.m_exitPoint != null)
      {
        if (closestDist <= 3.5f)
        {
          // Clamber up ladder to deck
          transform.position = targetLadder.m_exitPoint.position;
          if (transform.parent != PiecesController.transform)
          {
            transform.SetParent(PiecesController.transform);
          }
          _lastSafeLocalPos = transform.localPosition;
          _lastSafeLocalRot = transform.localRotation;
          if (_character.m_body != null && !_character.m_body.isKinematic) _character.m_body.linearVelocity = Vector3.zero;
          _waterTimer = 0f;
          return;
        }
        else if (_monsterAI != null)
        {
          // Swim towards ladder base
          _monsterAI.MoveTo(dt, targetLadder.m_attachPoint.position, 1f, true);
        }
      }

      _waterTimer += dt;
      // 20-second overboard failsafe recovery to Rope Ladder or Safe Deck
      if (_waterTimer >= 20.0f)
      {
        RopeLadderComponent? fallbackLadder = PiecesController.RopeLadders.FirstOrDefault(l => l != null && l.m_exitPoint != null);
        Vector3 safePos = fallbackLadder != null ? fallbackLadder.m_exitPoint.position : PiecesController.GetPlanterOrSafeDeckPosition();
        transform.position = safePos;
        if (transform.parent != PiecesController.transform)
        {
          transform.SetParent(PiecesController.transform);
        }
        _lastSafeLocalPos = transform.localPosition;
        _lastSafeLocalRot = transform.localRotation;
        if (_character.m_body != null && !_character.m_body.isKinematic)
        {
          _character.m_body.linearVelocity = Vector3.zero;
        }
        _waterTimer = 0f;
        ZLog.Log("[ValheimRAFT] Sailor Greydwarf was overboard >20s; safely recovered to Rope Ladder / Deck.");
      }
    }
    else
    {
      _waterTimer = 0f;
    }
  }

  public void EnsureRockWeaponEquipped()
  {
    if (_humanoid == null) return;
    var current = _humanoid.GetCurrentWeapon();
    if (current != null && current.m_shared != null && current.m_shared.m_attack != null && current.m_shared.m_attack.m_attackProjectile != null)
    {
      return; // Already equipped with rock throw weapon!
    }

    var inv = _humanoid.GetInventory();
    if (inv != null)
    {
      foreach (var item in inv.GetAllItems())
      {
        if (item != null && item.m_shared != null && item.m_shared.m_attack != null && item.m_shared.m_attack.m_attackProjectile != null)
        {
          _humanoid.EquipItem(item, false);
          return;
        }
      }
    }

    if (ObjectDB.instance != null)
    {
      var rockPrefab = ObjectDB.instance.GetItemPrefab("Greydwarf_throw");
      if (rockPrefab != null && inv != null)
      {
        if (inv.AddItem(rockPrefab, 1))
        {
          var item = inv.GetAllItems().FirstOrDefault(i => i != null && i.m_shared?.m_attack?.m_attackProjectile != null);
          if (item != null)
          {
            _humanoid.EquipItem(item, false);
          }
        }
      }
    }
  }

  private void UpdateCombatDefense(float dt)
  {
    if (_character == null || PiecesController == null) return;

    // Scan for hostiles within 22m of ship that are actively alerted (exclamation mark)
    var colliders = Physics.OverlapSphere(transform.position, 22f, LayerMask.GetMask("character"));
    Character? target = null;
    foreach (var col in colliders)
    {
      var ch = col.GetComponentInParent<Character>();
      if (ch != null && !ch.IsTamed() && !ch.IsPlayer() && ch.GetFaction() != Character.Faction.Players)
      {
        var ai = ch.GetBaseAI();
        if (ai != null && ai.IsAlerted())
        {
          target = ch;
          break;
        }
      }
    }

    if (target == null)
    {
      _isInCombat = false;
      return;
    }

    _isInCombat = true;

    // 100% PREFER TOP DECK / NO-ROOF DURING COMBAT:
    // If under roof or inside the hull, path to top deck so rocks aren't blocked by hull ceilings
    bool underRoof = IsUnderRoof(transform.position);
    Vector3 topDeckTarget = GetTopDeckPosition(GetInstanceID());
    float distToTopDeck = Vector3.Distance(transform.position, topDeckTarget);

    if (underRoof || distToTopDeck > 2.5f)
    {
      // Run to top deck immediately!
      if (_monsterAI != null)
      {
        _monsterAI.MoveTo(dt, topDeckTarget, 1f, true);
      }
      return; // Do not throw rocks while under roof!
    }

    // On top deck: execute combat abilities
    switch (DwarfType)
    {
      case GreydwarfSailorType.Regular:
        if (Time.time >= _nextRockThrowTime)
        {
          _nextRockThrowTime = Time.time + 5f;
          PerformRockThrow(target);
        }
        break;

      case GreydwarfSailorType.Brute:
        if (Time.time >= _nextTauntTime)
        {
          _nextTauntTime = Time.time + 12f;
          PerformTaunt(target);
        }
        break;

      case GreydwarfSailorType.Shaman:
        if (Time.time >= _nextHealTime)
        {
          _nextHealTime = Time.time + 10f;
          PerformHealing();
        }
        break;
    }
  }

  private void PerformRockThrow(Character target)
  {
    if (target == null) return;
    var targetAI = target.GetBaseAI();
    if (targetAI == null || !targetAI.IsAlerted()) return;

    // Line of sight check to avoid hitting boat structure
    Vector3 eyePos = transform.position + Vector3.up * 1.2f;
    Vector3 targetPos = target.transform.position + Vector3.up * 1.0f;
    Vector3 aimDir = (targetPos - eyePos);

    if (Physics.Raycast(eyePos, aimDir.normalized, out var losHit, aimDir.magnitude, LayerMask.GetMask("piece", "Default", "static_solid")))
    {
      var hitVpc = losHit.collider.GetComponentInParent<VehiclePiecesController>();
      if (hitVpc == PiecesController)
      {
        // LoS blocked by ship piece: reposition toward open deck
        if (_monsterAI != null)
        {
          _monsterAI.MoveTo(Time.deltaTime, GetTopDeckPosition(GetInstanceID()), 1f, true);
        }
        return;
      }
    }

    EnsureRockWeaponEquipped();

    Vector3 dir = target.transform.position - transform.position;
    dir.y = 0f;
    if (dir.sqrMagnitude > 0.01f)
    {
      transform.rotation = Quaternion.LookRotation(dir);
    }

    if (_humanoid != null)
    {
      _humanoid.StartAttack(target, false);
    }
    else if (_monsterAI != null)
    {
      _monsterAI.DoAttack(target, false);
    }
  }

  private void PerformTaunt(Character target)
  {
    if (_animator != null)
    {
      SafeTrigger(_animator, "taunt");
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
      SafeTrigger(_animator, "attack");
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

  // --- PASSIVE UTILITY: ITEM PICKUP & CHEST DEPOSIT ---

  private void UpdateItemGathering(float dt)
  {
    if (PiecesController == null || _character == null || _character.InWater()) return;

    if (_itemActionCooldown > 0f)
    {
      _itemActionCooldown -= dt;
      return;
    }

    // Step 1: If carrying an item, deliver to target chest
    if (_carriedItem != null)
    {
      if (_targetContainer == null || !_targetContainer || _targetContainer.GetInventory() == null)
      {
        _targetContainer = FindChestForItem(_carriedItem);
      }

      if (_targetContainer == null)
      {
        DropCarriedItem();
        return;
      }

      float distToChest = Vector3.Distance(transform.position, _targetContainer.transform.position);
      if (distToChest > 2.2f)
      {
        if (_monsterAI != null)
        {
          _monsterAI.MoveTo(dt, _targetContainer.transform.position, 1f, false);
        }
      }
      else
      {
        // Arrived at chest! Deposit item
        var inv = _targetContainer.GetInventory();
        if (inv != null && inv.CanAddItem(_carriedItem, _carriedItem.m_stack))
        {
          inv.AddItem(_carriedItem);
          _targetContainer.Save();

          SafeTrigger(_animator, "interact");
          _targetContainer.m_openEffects?.Create(_targetContainer.transform.position, Quaternion.identity);

          if (DamageText.instance != null)
          {
            DamageText.instance.ShowText(DamageText.TextType.Normal,
              _targetContainer.transform.position + Vector3.up * 1.2f,
              $"+{_carriedItem.m_stack} {_carriedItem.m_shared.m_name}");
          }

          _carriedItem = null;
          _targetContainer = null;
          _itemActionCooldown = 1.0f;
        }
        else
        {
          _targetContainer = FindChestForItem(_carriedItem);
          if (_targetContainer == null)
          {
            DropCarriedItem();
          }
        }
      }
      return;
    }

    // Step 2: If moving towards a dropped item to pick up
    if (_targetItemDrop != null)
    {
      if (!_targetItemDrop || _targetItemDrop.m_itemData == null || _targetItemDrop.m_nview == null || !_targetItemDrop.m_nview.IsValid())
      {
        _targetItemDrop = null;
        return;
      }

      float distToItem = Vector3.Distance(transform.position, _targetItemDrop.transform.position);
      if (distToItem > 1.8f)
      {
        if (_monsterAI != null)
        {
          _monsterAI.MoveTo(dt, _targetItemDrop.transform.position, 1f, false);
        }
      }
      else
      {
        // Pick up item
        if (_targetItemDrop.m_itemData != null)
        {
          _carriedItem = _targetItemDrop.m_itemData.Clone();
          _targetItemDrop.m_nview.ClaimOwnership();
          ZNetScene.instance.Destroy(_targetItemDrop.gameObject);

          SafeTrigger(_animator, "interact");
          _targetItemDrop = null;
          _targetContainer = FindChestForItem(_carriedItem);
          _itemActionCooldown = 0.5f;
        }
      }
      return;
    }

    // Step 3: Scan for loose items on the boat every 2.5s
    _itemScanTimer -= dt;
    if (_itemScanTimer <= 0f)
    {
      _itemScanTimer = 2.5f;
      _targetItemDrop = FindNearestLooseItemOnBoat();
    }
  }

  private ItemDrop? FindNearestLooseItemOnBoat()
  {
    if (PiecesController == null) return null;

    var colliders = Physics.OverlapSphere(transform.position, 14f, LayerMask.GetMask("item"));
    ItemDrop? bestItem = null;
    float bestDist = float.MaxValue;

    foreach (var col in colliders)
    {
      var itemDrop = col.GetComponentInParent<ItemDrop>();
      if (itemDrop == null || itemDrop.m_itemData == null || itemDrop.m_nview == null || !itemDrop.m_nview.IsValid()) continue;

      // Ensure item is resting on THIS boat
      Ray downRay = new Ray(itemDrop.transform.position + Vector3.up * 0.3f, Vector3.down);
      if (Physics.Raycast(downRay, out var hit, 1.5f, LayerMask.GetMask("piece", "Default")))
      {
        var hitVpc = hit.collider.GetComponentInParent<VehiclePiecesController>();
        if (hitVpc != PiecesController) continue;

        if (FindChestForItem(itemDrop.m_itemData) != null)
        {
          float d = Vector3.Distance(transform.position, itemDrop.transform.position);
          if (d < bestDist)
          {
            bestDist = d;
            bestItem = itemDrop;
          }
        }
      }
    }

    return bestItem;
  }

  private Container? FindChestForItem(ItemDrop.ItemData? item)
  {
    if (PiecesController == null || item == null) return null;

    Container? best = null;
    float bestDist = float.MaxValue;

    foreach (var piece in PiecesController.Pieces)
    {
      if (piece == null) continue;
      var container = piece.GetComponent<Container>() ?? piece.GetComponentInChildren<Container>();
      if (container == null || container.GetInventory() == null) continue;

      // Don't disturb chests in use by players
      if (container.IsInUse()) continue;

      if (container.GetInventory().CanAddItem(item, item.m_stack))
      {
        float d = Vector3.Distance(transform.position, container.transform.position);
        if (d < bestDist)
        {
          bestDist = d;
          best = container;
        }
      }
    }

    return best;
  }

  public void DropCarriedItem()
  {
    if (_carriedItem == null) return;
    ItemDrop.DropItem(_carriedItem, _carriedItem.m_stack, transform.position + Vector3.up * 0.5f, transform.rotation);
    _carriedItem = null;
    _targetContainer = null;
  }

  private static void SafeTrigger(Animator? anim, string param)
  {
    if (anim == null) return;
    foreach (var p in anim.parameters)
    {
      if (p.name == param)
      {
        anim.SetTrigger(param);
        return;
      }
    }
  }

  // --- SAILOR HAT ATTACHMENT ---

  public void EnsureRandomSailorHat()
  {
    string currentHat = "";
    if (_nview != null && _nview.IsValid())
    {
      currentHat = _nview.GetZDO().GetString("SailorHat", "");
    }
    if (string.IsNullOrEmpty(currentHat))
    {
      currentHat = SailorHats[UnityEngine.Random.Range(0, SailorHats.Length)];
      if (_nview != null && _nview.IsValid())
      {
        _nview.GetZDO().Set("SailorHat", currentHat);
      }
    }
    AttachSailorHat(currentHat);
  }

  public void EquipSpecificHat(string hatPrefabName)
  {
    if (!IsSailorHat(hatPrefabName)) return;
    if (_nview != null && _nview.IsValid())
    {
      _nview.GetZDO().Set("SailorHat", hatPrefabName);
    }
    AttachSailorHat(hatPrefabName, forceReplace: true);
  }

  public void ApplyCurrentHatTransform()
  {
    Transform? headBone = FindHeadBone(_character);
    if (headBone == null) return;
    Transform hatVisual = headBone.Find("SailorHatVisual");
    if (hatVisual != null)
    {
      hatVisual.localPosition = SailorHatPositionOffset;
      hatVisual.localRotation = Quaternion.Euler(SailorHatRotationEuler);
      hatVisual.localScale = Vector3.one * SailorHatScale;

      string currentHat = "";
      if (_nview != null && _nview.IsValid())
      {
        currentHat = _nview.GetZDO().GetString("SailorHat", "");
      }
      if (!string.IsNullOrEmpty(currentHat))
      {
        GameObject? hatPrefab = ObjectDB.instance?.GetItemPrefab(currentHat) ?? ZNetScene.instance?.GetPrefab(currentHat);
        Transform? equipoffset = hatPrefab != null ? hatPrefab.transform.Find("equipoffset") : null;
        if (equipoffset != null)
        {
          hatVisual.localPosition += equipoffset.localPosition * 0.5f;
          hatVisual.localRotation *= equipoffset.localRotation;
        }
      }
    }
  }

  public static void UpdateAllSailorHatTransforms()
  {
    foreach (var sailor in UnityEngine.Object.FindObjectsOfType<RaftGreydwarfSailorComponent>())
    {
      if (sailor != null)
      {
        sailor.ApplyCurrentHatTransform();
      }
    }
    ZLog.Log($"[SailorHat Debug] Offset -> Pos: ({SailorHatPositionOffset.x:F2}, {SailorHatPositionOffset.y:F2}, {SailorHatPositionOffset.z:F2}) | Rot: ({SailorHatRotationEuler.x:F2}, {SailorHatRotationEuler.y:F2}, {SailorHatRotationEuler.z:F2}) | Scale: {SailorHatScale:F2}");
  }

  public void CycleNextHat(Player? player = null)
  {
    string currentHat = "";
    if (_nview != null && _nview.IsValid())
    {
      currentHat = _nview.GetZDO().GetString("SailorHat", "");
    }

    int nextIndex = 0;
    if (!string.IsNullOrEmpty(currentHat))
    {
      for (int i = 0; i < SailorHats.Length; i++)
      {
        if (SailorHats[i].Equals(currentHat, StringComparison.OrdinalIgnoreCase))
        {
          nextIndex = (i + 1) % SailorHats.Length;
          break;
        }
      }
    }

    string newHat = SailorHats[nextIndex];
    if (_nview != null && _nview.IsValid())
    {
      _nview.GetZDO().Set("SailorHat", newHat);
    }
    AttachSailorHat(newHat, forceReplace: true);

    if (player != null)
    {
      player.Message(MessageHud.MessageType.Center, $"Sailor Hat: {newHat} ({nextIndex + 1}/{SailorHats.Length})");
    }
    ZLog.Log($"[SailorHat] Cycled hat on Greydwarf to '{newHat}' ({nextIndex + 1}/{SailorHats.Length})");
  }

  public void AttachSailorHat(string hatPrefabName, bool forceReplace = false)
  {
    if (string.IsNullOrEmpty(hatPrefabName)) return;

    if (hatPrefabName.Equals("HelmetFishingHat", StringComparison.OrdinalIgnoreCase))
    {
      hatPrefabName = "HelmetFishing";
    }
    else if (hatPrefabName.Equals("HelmetStrawHat", StringComparison.OrdinalIgnoreCase))
    {
      hatPrefabName = "HelmetHat1";
    }

    // Clean up any existing hats anywhere on character hierarchy
    if (_character != null)
    {
      foreach (var oldHat in _character.GetComponentsInChildren<Transform>(true))
      {
        if (oldHat != null && oldHat.name == "SailorHatVisual")
        {
          if (!forceReplace) return;
          Destroy(oldHat.gameObject);
        }
      }
    }

    Transform? headBone = FindHeadBone(_character);
    if (headBone == null) return;

    GameObject? hatPrefab = ObjectDB.instance?.GetItemPrefab(hatPrefabName);
    if (hatPrefab == null)
    {
      hatPrefab = ZNetScene.instance?.GetPrefab(hatPrefabName);
    }
    if (hatPrefab == null)
    {
      hatPrefab = ObjectDB.instance?.GetItemPrefab("HelmetHat1") ?? ZNetScene.instance?.GetPrefab("HelmetHat1");
    }
    if (hatPrefab == null)
    {
      ZLog.LogWarning($"[ValheimRAFT] Sailor hat prefab not found: {hatPrefabName}");
      return;
    }

    // In Valheim helmet prefabs, child 'attach' or 'attach_skin' holds the fitted wearable hat model
    Transform? attachChild = hatPrefab.transform.Find("attach") ??
                             hatPrefab.transform.Find("attach_skin");

    GameObject hatVisual = attachChild != null
      ? Instantiate(attachChild.gameObject, headBone)
      : Instantiate(hatPrefab, headBone);

    hatVisual.name = "SailorHatVisual";

    // Clean up unnecessary components
    var itemDrop = hatVisual.GetComponent<ItemDrop>();
    if (itemDrop != null) Destroy(itemDrop);
    var zNetView = hatVisual.GetComponent<ZNetView>();
    if (zNetView != null) Destroy(zNetView);
    var rb = hatVisual.GetComponent<Rigidbody>();
    if (rb != null) Destroy(rb);
    foreach (var col in hatVisual.GetComponentsInChildren<Collider>(true))
    {
      Destroy(col);
    }

    // Convert any SkinnedMeshRenderer into a baked static MeshRenderer
    // so it doesn't need humanoid player skeleton bones to render!
    var skinnedRenderers = hatVisual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
    foreach (var smr in skinnedRenderers)
    {
      Mesh baked = new Mesh();
      smr.BakeMesh(baked);
      var go = smr.gameObject;
      var mf = go.AddComponent<MeshFilter>();
      mf.sharedMesh = baked;
      var mr = go.AddComponent<MeshRenderer>();
      mr.sharedMaterials = smr.sharedMaterials;
      mr.enabled = true;
      Destroy(smr);
    }

    // Ensure all renderers are active, visible, and cast shadows
    foreach (var rend in hatVisual.GetComponentsInChildren<Renderer>(true))
    {
      rend.enabled = true;
      rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
      rend.receiveShadows = true;
    }

    // Match layer of head bone so camera never culls the hat
    int layer = headBone.gameObject.layer;
    foreach (var t in hatVisual.GetComponentsInChildren<Transform>(true))
    {
      t.gameObject.layer = layer;
    }

    // Position and orientation:
    // Align with head bone. Greydwarf head bone is slightly tilted forward.
    // Local offset puts the cap squarely on top of the Greydwarf's head.
    Transform? equipoffset = hatPrefab.transform.Find("equipoffset");

    hatVisual.transform.localPosition = SailorHatPositionOffset;
    hatVisual.transform.localRotation = Quaternion.Euler(SailorHatRotationEuler);
    hatVisual.transform.localScale = Vector3.one * SailorHatScale;

    if (equipoffset != null)
    {
      hatVisual.transform.localPosition += equipoffset.localPosition * 0.5f;
      hatVisual.transform.localRotation *= equipoffset.localRotation;
    }

    ZLog.Log($"[SailorHat] Attached hat '{hatPrefabName}' to bone '{headBone.name}'. " +
      $"Offset values -> localPosition: {hatVisual.transform.localPosition}, " +
      $"localRotation Euler: {hatVisual.transform.localRotation.eulerAngles}, " +
      $"localScale: {hatVisual.transform.localScale}");
  }

  public void RemoveSailorHat()
  {
    if (_character != null)
    {
      foreach (var oldHat in _character.GetComponentsInChildren<Transform>(true))
      {
        if (oldHat != null && oldHat.name == "SailorHatVisual")
        {
          Destroy(oldHat.gameObject);
        }
      }
    }
    if (_nview != null && _nview.IsValid())
    {
      _nview.GetZDO().Set("SailorHat", "");
    }
  }

  public static Transform? FindHeadBone(Character? character)
  {
    if (character == null) return null;

    // 1. SkinnedMeshRenderer bones search (actual animated skeleton bone deformed by animations)
    var smrs = character.GetComponentsInChildren<SkinnedMeshRenderer>(true);
    foreach (var smr in smrs)
    {
      if (smr.bones == null) continue;
      foreach (var bone in smr.bones)
      {
        if (bone != null && bone.name.IndexOf("head", StringComparison.OrdinalIgnoreCase) >= 0)
        {
          return bone;
        }
      }
    }

    // 2. Humanoid Animator Head bone (if avatar is humanoid)
    var anim = character.GetComponentInChildren<Animator>();
    if (anim != null && anim.isHuman)
    {
      var b = anim.GetBoneTransform(HumanBodyBones.Head);
      if (b != null) return b;
    }

    // 3. Fallback search through all hierarchy transforms for bone containing 'head' (excluding 'eye')
    var all = character.GetComponentsInChildren<Transform>(true);
    foreach (var t in all)
    {
      if (t.name.IndexOf("head", StringComparison.OrdinalIgnoreCase) >= 0 &&
          t.name.IndexOf("eye", StringComparison.OrdinalIgnoreCase) < 0)
      {
        return t;
      }
    }

    // 4. Fallback to character.m_head or humanoid.m_head
    if (character.m_head != null)
    {
      return character.m_head;
    }

    var humanoid = character as Humanoid;
    if (humanoid != null)
    {
      if (humanoid.m_visEquipment != null && humanoid.m_visEquipment.m_helmet != null)
      {
        return humanoid.m_visEquipment.m_helmet;
      }
      if (humanoid.m_head != null)
      {
        return humanoid.m_head;
      }
    }

    // 5. Last resort fallback to m_eye
    if (character.m_eye != null)
    {
      return character.m_eye;
    }

    return character.transform;
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
}
