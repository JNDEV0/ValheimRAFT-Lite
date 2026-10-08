using System;
using UnityEngine;
using ValheimVehicles.Controllers;
using ValheimVehicles.Prefabs;

namespace ValheimVehicles.Components;

public enum VehicleWaterEffectType
{
  Wake
}

public class VehiclePieceWaterEffects : MonoBehaviour
{
  public VehicleWaterEffectType EffectType = VehicleWaterEffectType.Wake;

  private GameObject? _effectInstance;
  private ParticleSystem[] _particles = Array.Empty<ParticleSystem>();
  private Rigidbody? _vehicleRigidbody;
  private VehicleManager? _vehicleManager;
  private WaterVolume _previousWaterVolume;
  private bool _isEmitting;

  public void Initialize(VehicleWaterEffectType type = VehicleWaterEffectType.Wake, VehicleManager? manager = null)
  {
    EffectType = type;
    _vehicleManager = manager ?? GetComponentInParent<VehicleManager>();
    _vehicleRigidbody = _vehicleManager?.MovementControllerRigidbody ?? _vehicleManager?.PiecesController?.m_syncRigidbody ?? GetComponentInParent<Rigidbody>();
    SpawnEffect();
  }

  private void Awake()
  {
    var existingCutwater = transform.Find("CutwaterWaterEffect");
    if (existingCutwater != null)
    {
      Destroy(existingCutwater.gameObject);
    }
    for (int i = transform.childCount - 1; i >= 0; i--)
    {
      var child = transform.GetChild(i);
      if (child.name.IndexOf("splash", StringComparison.OrdinalIgnoreCase) >= 0 ||
          child.name.IndexOf("cutwater", StringComparison.OrdinalIgnoreCase) >= 0)
      {
        child.gameObject.SetActive(false);
        Destroy(child.gameObject);
      }
    }
  }

  private void Start()
  {
    if (_effectInstance == null)
    {
      if (_vehicleManager == null) _vehicleManager = GetComponentInParent<VehicleManager>();
      if (_vehicleRigidbody == null) _vehicleRigidbody = _vehicleManager?.MovementControllerRigidbody ?? _vehicleManager?.PiecesController?.m_syncRigidbody ?? GetComponentInParent<Rigidbody>();
      SpawnEffect();
    }
  }

  private void SpawnEffect()
  {
    if (_effectInstance != null) return;

    GameObject? template = LoadValheimAssets.WakeParticlesTemplate;
    if (template == null) return;

    _effectInstance = Instantiate(template, transform);
    _effectInstance.name = "WakeWaterEffect";

    // Placed slightly rearward for rudder wake trailing behind the ship
    _effectInstance.transform.localPosition = new Vector3(0f, -0.7f, -0.6f);
    _effectInstance.transform.localRotation = Quaternion.identity;

    var allPs = _effectInstance.GetComponentsInChildren<ParticleSystem>(true);
    var wakePsList = new System.Collections.Generic.List<ParticleSystem>();
    foreach (var ps in allPs)
    {
      if (ps.name.IndexOf("splash", StringComparison.OrdinalIgnoreCase) >= 0 ||
          ps.name.IndexOf("cutwater", StringComparison.OrdinalIgnoreCase) >= 0 ||
          ps.name.IndexOf("spray", StringComparison.OrdinalIgnoreCase) >= 0)
      {
        ps.gameObject.SetActive(false);
        Destroy(ps.gameObject);
      }
      else
      {
        wakePsList.Add(ps);
      }
    }
    _particles = wakePsList.ToArray();
    _effectInstance.SetActive(true);
    SetEmission(false);
  }

  private void LateUpdate()
  {
    if (_particles == null || _particles.Length == 0) return;

    if (_vehicleRigidbody == null)
    {
      _vehicleManager = GetComponentInParent<VehicleManager>();
      _vehicleRigidbody = _vehicleManager?.MovementControllerRigidbody ?? _vehicleManager?.PiecesController?.m_syncRigidbody ?? GetComponentInParent<Rigidbody>();
      if (_vehicleRigidbody == null) return;
    }

    if (_vehicleManager != null && _vehicleManager.MovementController != null)
    {
      if (_vehicleManager.MovementController.IsFlying() || _vehicleManager.MovementController.IsSubmerged())
      {
        if (_isEmitting) SetEmission(false);
        return;
      }
    }

    var speed = _vehicleRigidbody.linearVelocity.magnitude;
    const float minSpeed = 1.2f;

    var pos = transform.position;
    var isNearWater = Floating.IsUnderWater(pos, ref _previousWaterVolume);

    var shouldEmit = speed > minSpeed && isNearWater;

    if (_isEmitting != shouldEmit)
    {
      SetEmission(shouldEmit);
    }

    if (shouldEmit && _effectInstance != null)
    {
      var effPos = transform.TransformPoint(new Vector3(0f, 0f, -0.6f));
      var waterY = Floating.GetWaterLevel(effPos, ref _previousWaterVolume);
      if (waterY > -1000f)
      {
        effPos.y = waterY - 0.5f;
        _effectInstance.transform.position = effPos;
        _effectInstance.transform.rotation = transform.rotation;
      }
    }
  }

  private void SetEmission(bool emit)
  {
    _isEmitting = emit;
    for (int i = 0; i < _particles.Length; i++)
    {
      if (_particles[i] != null)
      {
        var em = _particles[i].emission;
        em.enabled = emit;
      }
    }
  }

  private void OnDestroy()
  {
    if (_effectInstance != null)
    {
      Destroy(_effectInstance);
      _effectInstance = null;
    }
  }
}
