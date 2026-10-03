using System;
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
    "HelmetHat7",        // Red Twisted Headscarf
    "HelmetHat6",        // Yellow Tied Headscarf
    "HelmetStrawHat",    // Straw Hat
    "HelmetFishingHat"   // Fishing Hat
  ];

  public static bool IsSailorHat(string prefabName)
  {
    if (string.IsNullOrEmpty(prefabName)) return false;
    foreach (var hat in SailorHats)
    {
      if (hat.Equals(prefabName, StringComparison.OrdinalIgnoreCase)) return true;
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

  // Combat cooldowns
  private float _nextRockThrowTime;
  private float _nextTauntTime;
  private float _nextHealTime;

  // Deck rooting & safe anchoring
  private Vector3? _deckLocalPos;
  private Quaternion? _deckLocalRot;
  private bool _isAnchoredOnDeck;
  private Vector3? _lastSafeLocalPos;
  private Quaternion? _lastSafeLocalRot;

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

    if (_character != null && _character.m_body != null)
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

    UpdateUpkeep(Time.deltaTime);
    UpdateDeckStation(Time.deltaTime);
    UpdateWaterAndLadderRecovery(Time.deltaTime);
    UpdateCombatDefense();
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
      }

      if (Loyalty == SailorLoyalty.Mutineer)
      {
        TriggerMutiny();
      }
      else
      {
        string msg = Loyalty switch
        {
          SailorLoyalty.Unsatisfied => "Sailor Greydwarf is Unsatisfied! (No resin found in chests)",
          SailorLoyalty.Hungry => "Sailor Greydwarf is Hungry! (No resin found in chests)",
          SailorLoyalty.NearMutiny => "WARNING: Sailor Greydwarf is Near-Mutiny! Provide resin immediately!",
          _ => "Sailor Greydwarf needs resin!"
        };
        MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center, msg);
      }
    }
  }

  private void TriggerMutiny()
  {
    if (_character == null) return;

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
    bool isShipMoving = false;
    var moveCtrl = PiecesController.MovementController;
    if (moveCtrl != null && moveCtrl.GetSpeedSetting() != Ship.Speed.Stop)
    {
      isShipMoving = true;
    }
    else
    {
      var rb = PiecesController.m_syncRigidbody != null ? PiecesController.m_syncRigidbody : PiecesController.m_localRigidbody;
      if (rb != null && (rb.linearVelocity.sqrMagnitude > 0.04f || rb.angularVelocity.sqrMagnitude > 0.005f))
      {
        isShipMoving = true;
      }
    }

    if (isShipMoving)
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

      // Hull edge guard: Raycast downwards to make sure sailor does not walk off the ship's hull
      Ray ray = new Ray(transform.position + Vector3.up * 0.5f, Vector3.down);
      if (Physics.Raycast(ray, out var hit, 3.5f))
      {
        var hitVpc = hit.collider.GetComponentInParent<VehiclePiecesController>();
        if (hitVpc == PiecesController)
        {
          _lastSafeLocalPos = transform.localPosition;
          _lastSafeLocalRot = transform.localRotation;
        }
        else if (_lastSafeLocalPos.HasValue)
        {
          // Stepped over edge or foreign collider: keep rooted safely on deck
          transform.localPosition = _lastSafeLocalPos.Value;
          if (_lastSafeLocalRot.HasValue) transform.localRotation = _lastSafeLocalRot.Value;
          if (_character.m_body != null) _character.m_body.linearVelocity = Vector3.zero;
        }
      }
      else if (_lastSafeLocalPos.HasValue)
      {
        // Stepped over open ocean: immediately snap back onto deck
        transform.localPosition = _lastSafeLocalPos.Value;
        if (_lastSafeLocalRot.HasValue) transform.localRotation = _lastSafeLocalRot.Value;
        if (_character.m_body != null) _character.m_body.linearVelocity = Vector3.zero;
      }
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
          if (_character.m_body != null) _character.m_body.linearVelocity = Vector3.zero;
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
        if (_character.m_body != null)
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

  private void UpdateCombatDefense()
  {
    if (_character == null || PiecesController == null) return;

    // Scan for hostiles within 18m of ship that are actively alerted (exclamation mark)
    var colliders = Physics.OverlapSphere(transform.position, 18f, LayerMask.GetMask("character"));
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

    if (target == null) return;

    switch (DwarfType)
    {
      case GreydwarfSailorType.Regular:
        if (Time.time >= _nextRockThrowTime)
        {
          _nextRockThrowTime = Time.time + 6f;
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
    if (target == null) return;
    var targetAI = target.GetBaseAI();
    if (targetAI == null || !targetAI.IsAlerted()) return;

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

  public void AttachSailorHat(string hatPrefabName, bool forceReplace = false)
  {
    if (string.IsNullOrEmpty(hatPrefabName)) return;

    Transform? headBone = FindHeadBone(_character);
    if (headBone == null) return;

    Transform existingHat = headBone.Find("SailorHatVisual");
    if (existingHat != null)
    {
      if (!forceReplace) return;
      Destroy(existingHat.gameObject);
    }

    GameObject? hatPrefab = ObjectDB.instance?.GetItemPrefab(hatPrefabName);
    if (hatPrefab == null)
    {
      hatPrefab = ZNetScene.instance?.GetPrefab(hatPrefabName);
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

    foreach (var rend in hatVisual.GetComponentsInChildren<Renderer>(true))
    {
      rend.enabled = true;
    }

    // Rotate 90, 180, 0 degrees
    hatVisual.transform.localRotation = Quaternion.Euler(90f, 180f, 0f);
    hatVisual.transform.localPosition = Vector3.zero;

    // Compensate for parent bone scale so world size is always exact
    Vector3 parentLossy = headBone.lossyScale;
    float targetScale = DwarfType == GreydwarfSailorType.Brute ? 0.95f : 0.70f;
    hatVisual.transform.localScale = new Vector3(
      targetScale / (Mathf.Abs(parentLossy.x) > 0.001f ? Mathf.Abs(parentLossy.x) : 1f),
      targetScale / (Mathf.Abs(parentLossy.y) > 0.001f ? Mathf.Abs(parentLossy.y) : 1f),
      targetScale / (Mathf.Abs(parentLossy.z) > 0.001f ? Mathf.Abs(parentLossy.z) : 1f)
    );

    // Automatically eliminate any prefab offset by centering the hat mesh directly on the head with +0.25 Y offset
    var rends = hatVisual.GetComponentsInChildren<Renderer>(true);
    Bounds meshBounds = new Bounds();
    bool hasBounds = false;
    foreach (var r in rends)
    {
      if (r is MeshRenderer || r is SkinnedMeshRenderer)
      {
        if (!hasBounds)
        {
          meshBounds = r.bounds;
          hasBounds = true;
        }
        else
        {
          meshBounds.Encapsulate(r.bounds);
        }
      }
    }

    if (hasBounds)
    {
      Vector3 targetHeadPos = headBone.position + Vector3.up * 0.25f;
      Vector3 shift = targetHeadPos - meshBounds.center;
      hatVisual.transform.position += shift;
    }
    else
    {
      hatVisual.transform.localPosition = new Vector3(0f, 0.25f, 0f);
    }
  }

  public void RemoveSailorHat()
  {
    Transform? headBone = FindHeadBone(_character);
    if (headBone != null)
    {
      Transform existingHat = headBone.Find("SailorHatVisual");
      if (existingHat != null)
      {
        Destroy(existingHat.gameObject);
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

    var humanoid = character as Humanoid;
    if (humanoid != null && humanoid.m_head != null)
    {
      return humanoid.m_head;
    }

    var anim = character.GetComponentInChildren<Animator>();
    if (anim != null && anim.isHuman)
    {
      var b = anim.GetBoneTransform(HumanBodyBones.Head);
      if (b != null) return b;
    }

    var all = character.GetComponentsInChildren<Transform>(true);
    foreach (var t in all)
    {
      if (t.name.Equals("Head", StringComparison.OrdinalIgnoreCase))
      {
        return t;
      }
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
