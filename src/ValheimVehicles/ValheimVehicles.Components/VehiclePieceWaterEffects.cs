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
    _effectInstance.transform.localPosition = new Vector3(0f, -0.2f, -0.6f);
    _effectInstance.transform.localRotation = Quaternion.identity;

    _particles = _effectInstance.GetComponentsInChildren<ParticleSystem>(true);
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
      var effPos = _effectInstance.transform.position;
      float waterY;
      if (_vehicleManager != null && _vehicleManager.MovementController != null)
      {
        waterY = _vehicleManager.MovementController.ShipFloatationObj.AverageWaterHeight;
      }
      else
      {
        waterY = Floating.GetWaterLevel(effPos, ref _previousWaterVolume);
      }

      if (waterY > -1000f)
      {
        effPos.y = waterY;
        _effectInstance.transform.position = effPos;
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
