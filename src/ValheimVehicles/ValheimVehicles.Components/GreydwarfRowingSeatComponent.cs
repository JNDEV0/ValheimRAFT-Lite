using System;
using UnityEngine;
using ValheimVehicles.Controllers;
using ValheimVehicles.Prefabs;

namespace ValheimVehicles.Components;

public enum GreydwarfSailorType
{
  None,
  Regular,
  Shaman,
  Brute
}

public class GreydwarfRowingSeatComponent : MonoBehaviour
{
  public Chair? m_chair;
  public Character? m_seatedGreydwarf;
  public GreydwarfSailorType m_greydwarfType = GreydwarfSailorType.None;

  public Character? m_reservedBy;

  private GameObject? _oarRoot;
  private bool _isPortSide = true;
  private VehiclePiecesController? _vehiclePiecesController;

  public bool IsOccupiedByGreydwarf => m_seatedGreydwarf != null && (bool)m_seatedGreydwarf;
  public bool IsOccupiedByPlayer => m_chair != null && m_chair.IsInUse();
  public bool IsOccupied => IsOccupiedByGreydwarf || IsOccupiedByPlayer;

  public bool IsReserved => m_reservedBy != null && (bool)m_reservedBy && m_reservedBy != m_seatedGreydwarf;

  public Chair? CenterChair => m_chair;

  public Character? OccupantCharacter
  {
    get => m_seatedGreydwarf;
    set
    {
      if (value != null)
      {
        TakeSeat(value);
      }
      else
      {
        Dismount();
      }
    }
  }

  public Vector3 GetSeatPosition()
  {
    return m_chair != null && m_chair.m_attachPoint != null ? m_chair.m_attachPoint.position : transform.position;
  }

  private void Awake()
  {
    if (m_chair == null)
    {
      m_chair = GetComponentInChildren<Chair>();
    }

    if (m_chair != null)
    {
      m_chair.m_inShip = true;
    }
  }

  private void Start()
  {
    CacheVehicle();
  }

  public void CacheVehicle()
  {
    _vehiclePiecesController = GetComponentInParent<VehiclePiecesController>();
    if (_vehiclePiecesController != null)
    {
      var localPos = _vehiclePiecesController.transform.InverseTransformPoint(transform.position);
      _isPortSide = localPos.x < 0f;
    }
  }

  private void Update()
  {
    // Clean up if the seated Greydwarf died, despawned, or detached
    if (m_seatedGreydwarf != null)
    {
      if (!m_seatedGreydwarf || m_seatedGreydwarf.IsDead() || !m_seatedGreydwarf.IsAttached())
      {
        Dismount();
      }
      else
      {
        UpdateOarAnimation();
      }
    }
  }

  public bool Reserve(Character dwarf)
  {
    if (IsOccupied || (IsReserved && m_reservedBy != dwarf)) return false;
    m_reservedBy = dwarf;
    return true;
  }

  public void ReleaseReservation(Character dwarf)
  {
    if (m_reservedBy == dwarf)
    {
      m_reservedBy = null;
    }
  }

  public bool TakeSeat(Character greydwarf)
  {
    if (IsOccupiedByPlayer || (IsOccupiedByGreydwarf && m_seatedGreydwarf != greydwarf))
    {
      return false;
    }

    if (m_chair == null || m_chair.m_attachPoint == null)
    {
      return false;
    }

    m_seatedGreydwarf = greydwarf;
    m_reservedBy = null;

    // Detect type
    string name = greydwarf.gameObject.name;
    if (name.Contains("Shaman"))
    {
      m_greydwarfType = GreydwarfSailorType.Shaman;
    }
    else if (name.Contains("Elite") || name.Contains("Brute"))
    {
      m_greydwarfType = GreydwarfSailorType.Brute;
    }
    else
    {
      m_greydwarfType = GreydwarfSailorType.Regular;
    }

    // Attach to chair attachpoint with onShip = true
    greydwarf.AttachStart(
      m_chair.m_attachPoint,
      gameObject,
      hideWeapons: true,
      isBed: false,
      onShip: true,
      m_chair.m_attachAnimation,
      m_chair.m_detachOffset
    );

    // Disable AI pathfinding / physics while seated
    var monsterAI = greydwarf.GetComponent<MonsterAI>();
    if (monsterAI != null)
    {
      monsterAI.m_targetCreature = null;
    }

    var rb = greydwarf.GetComponent<Rigidbody>();
    if (rb != null)
    {
      rb.isKinematic = true;
    }

    SpawnOar();
    return true;
  }

  public void Dismount()
  {
    if (m_seatedGreydwarf != null && (bool)m_seatedGreydwarf)
    {
      if (m_seatedGreydwarf.IsAttached())
      {
        m_seatedGreydwarf.AttachStop();
      }

      var rb = m_seatedGreydwarf.GetComponent<Rigidbody>();
      if (rb != null)
      {
        rb.isKinematic = false;
      }
    }

    m_seatedGreydwarf = null;
    m_greydwarfType = GreydwarfSailorType.None;
    m_reservedBy = null;

    DespawnOar();
  }

  public bool IsActivelyRowing(out GreydwarfSailorType dwarfType)
  {
    if (IsOccupiedByGreydwarf && m_seatedGreydwarf != null && m_seatedGreydwarf.IsTamed())
    {
      dwarfType = m_greydwarfType;
      return true;
    }

    dwarfType = GreydwarfSailorType.None;
    return false;
  }

  public float GetRowingSpeedBonus()
  {
    if (!IsActivelyRowing(out var type)) return 0f;

    return type switch
    {
      GreydwarfSailorType.Regular => 1.0f,
      GreydwarfSailorType.Shaman => 1.5f,
      GreydwarfSailorType.Brute => 2.0f,
      _ => 1.0f
    };
  }

  private void SpawnOar()
  {
    DespawnOar();
    CacheVehicle();

    _oarRoot = new GameObject("GreydwarfRowingOar");
    _oarRoot.transform.SetParent(transform, false);

    // Position at bench side
    float sideOffset = _isPortSide ? -0.8f : 0.8f;
    _oarRoot.transform.localPosition = new Vector3(sideOffset, 0.4f, 0f);

    Material? woodMat = null;
    if (LoadValheimAssets.woodFloorPiece != null)
    {
      var rend = LoadValheimAssets.woodFloorPiece.GetComponentInChildren<Renderer>();
      if (rend != null) woodMat = rend.sharedMaterial;
    }

    // Shaft
    var shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
    shaft.name = "OarShaft";
    var shaftCol = shaft.GetComponent<Collider>();
    if (shaftCol != null) Destroy(shaftCol);

    shaft.transform.SetParent(_oarRoot.transform, false);
    shaft.transform.localScale = new Vector3(0.06f, 1.4f, 0.06f);

    // Orient shaft outwards and down
    shaft.transform.localRotation = Quaternion.Euler(_isPortSide ? -65f : 65f, 0f, 0f);
    shaft.transform.localPosition = new Vector3(_isPortSide ? -0.7f : 0.7f, -0.6f, 0f);

    if (woodMat != null)
    {
      var shaftRend = shaft.GetComponent<Renderer>();
      if (shaftRend != null) shaftRend.sharedMaterial = woodMat;
    }

    // Blade
    var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
    blade.name = "OarBlade";
    var bladeCol = blade.GetComponent<Collider>();
    if (bladeCol != null) Destroy(bladeCol);

    blade.transform.SetParent(shaft.transform, false);
    blade.transform.localScale = new Vector3(1.5f, 0.45f, 0.2f);
    blade.transform.localPosition = new Vector3(0f, -0.9f, 0f);

    if (woodMat != null)
    {
      var bladeRend = blade.GetComponent<Renderer>();
      if (bladeRend != null) bladeRend.sharedMaterial = woodMat;
    }
  }

  private void DespawnOar()
  {
    if (_oarRoot != null)
    {
      Destroy(_oarRoot);
      _oarRoot = null;
    }
  }

  private void UpdateOarAnimation()
  {
    if (_oarRoot == null) return;

    if (_vehiclePiecesController == null || _vehiclePiecesController.MovementController == null)
    {
      _oarRoot.transform.localRotation = Quaternion.identity;
      return;
    }

    var moveCtrl = _vehiclePiecesController.MovementController;
    if (moveCtrl.GetSpeedSetting() == Ship.Speed.Slow && !moveCtrl.isAnchored)
    {
      float strokeCycle = (Time.time % 2.2f) / 2.2f;
      float pitch = Mathf.Sin(strokeCycle * Mathf.PI * 2f) * 12f;
      float yaw = Mathf.Cos(strokeCycle * Mathf.PI * 2f) * (_isPortSide ? 25f : -25f);
      _oarRoot.transform.localRotation = Quaternion.Euler(pitch, yaw, _isPortSide ? -15f : 15f);
    }
    else
    {
      // Rest / Stowed angle
      _oarRoot.transform.localRotation = Quaternion.Euler(0f, _isPortSide ? 10f : -10f, 0f);
    }
  }

  private void OnDestroy()
  {
    DespawnOar();
    if (m_seatedGreydwarf != null && (bool)m_seatedGreydwarf && m_seatedGreydwarf.IsAttached())
    {
      m_seatedGreydwarf.AttachStop();
    }
  }
}
