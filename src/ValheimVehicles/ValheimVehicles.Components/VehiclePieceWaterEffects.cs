using System;
using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.Controllers;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Helpers;
using ValheimVehicles.BepInExConfig;
using Zolantris.Shared;

namespace ValheimVehicles.Components;

public enum VehicleWaterEffectType
{
  Wake
}

public class VehiclePieceWaterEffects : MonoBehaviour
{
  public static bool GlobalEnableWaterWake = true;
  public static float FlatFoamOffsetX = 0f;
  public static float FlatFoamOffsetY = 0f;
  public static float FlatFoamOffsetZ = 0f;
  public static float SprayOffsetX = 0f;
  public static float SprayOffsetY = 0f;
  public static float SprayOffsetZ = 0f;

  public VehicleWaterEffectType EffectType = VehicleWaterEffectType.Wake;
  public bool IsFallbackEmitter = false;

  private GameObject? _effectInstance;
  private Transform? _flatFoamRoot;
  private Transform? _sprayRoot;
  private ParticleSystem[] _flatFoamParticles = Array.Empty<ParticleSystem>();
  private ParticleSystem[] _sprayParticles = Array.Empty<ParticleSystem>();
  private ParticleSystem[] _particles = Array.Empty<ParticleSystem>();
  private ParticleSystem.Particle[]? _particleBuffer;
  private Rigidbody? _vehicleRigidbody;
  private VehicleManager? _vehicleManager;
  private WaterVolume _previousWaterVolume;
  private bool _isEmitting;

  public bool GetWaterWakeEnabled()
  {
    var zdo = _vehicleManager?.m_nview?.GetZDO();
    if (zdo != null)
    {
      return zdo.GetBool(Shared.Constants.VehicleZdoVars.ShipWaterWakeEnabled, GlobalEnableWaterWake);
    }
    return GlobalEnableWaterWake;
  }

  public Vector3 GetFlatFoamOffset()
  {
    var zdo = _vehicleManager?.m_nview?.GetZDO();
    if (zdo != null)
    {
      return new Vector3(
        zdo.GetFloat(Shared.Constants.VehicleZdoVars.FlatFoamOffsetX, FlatFoamOffsetX),
        zdo.GetFloat(Shared.Constants.VehicleZdoVars.FlatFoamOffsetY, FlatFoamOffsetY),
        zdo.GetFloat(Shared.Constants.VehicleZdoVars.FlatFoamOffsetZ, FlatFoamOffsetZ)
      );
    }
    return new Vector3(FlatFoamOffsetX, FlatFoamOffsetY, FlatFoamOffsetZ);
  }

  public Vector3 GetSprayOffset()
  {
    var zdo = _vehicleManager?.m_nview?.GetZDO();
    if (zdo != null)
    {
      return new Vector3(
        zdo.GetFloat(Shared.Constants.VehicleZdoVars.SprayOffsetX, SprayOffsetX),
        zdo.GetFloat(Shared.Constants.VehicleZdoVars.SprayOffsetY, SprayOffsetY),
        zdo.GetFloat(Shared.Constants.VehicleZdoVars.SprayOffsetZ, SprayOffsetZ)
      );
    }
    return new Vector3(SprayOffsetX, SprayOffsetY, SprayOffsetZ);
  }

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
    _effectInstance.name = IsFallbackEmitter ? "FallbackWakeWaterEffect" : "WakeWaterEffect";
    _effectInstance.transform.localPosition = Vector3.zero;
    _effectInstance.transform.localRotation = Quaternion.identity;

    var flatFoamObj = new GameObject("FlatFoamRoot");
    flatFoamObj.transform.SetParent(_effectInstance.transform, false);
    flatFoamObj.transform.localPosition = Vector3.zero;
    flatFoamObj.transform.localRotation = Quaternion.identity;
    _flatFoamRoot = flatFoamObj.transform;

    var sprayObj = new GameObject("SprayRoot");
    sprayObj.transform.SetParent(_effectInstance.transform, false);
    sprayObj.transform.localPosition = Vector3.zero;
    sprayObj.transform.localRotation = Quaternion.identity;
    _sprayRoot = sprayObj.transform;

    var allPs = _effectInstance.GetComponentsInChildren<ParticleSystem>(true);
    var flatFoamList = new List<ParticleSystem>();
    var sprayList = new List<ParticleSystem>();
    var allValidPs = new List<ParticleSystem>();

    var validPsCandidates = new List<ParticleSystem>();
    foreach (var ps in allPs)
    {
      if (ps == null) continue;
      var psName = ps.name;
      var psRenderer = ps.GetComponent<ParticleSystemRenderer>();

      // Filter out splashes, cutwater, drops, droplets, mist, and stretched particle streaks
      if (psName.IndexOf("splash", StringComparison.OrdinalIgnoreCase) >= 0 ||
          psName.IndexOf("cutwater", StringComparison.OrdinalIgnoreCase) >= 0 ||
          psName.IndexOf("drop", StringComparison.OrdinalIgnoreCase) >= 0 ||
          psName.IndexOf("mist", StringComparison.OrdinalIgnoreCase) >= 0 ||
          (psRenderer != null && (psRenderer.renderMode == ParticleSystemRenderMode.Stretch || psRenderer.renderMode == ParticleSystemRenderMode.VerticalBillboard)))
      {
        if (VehicleGuiMenuConfig.EnableLoopLogging?.Value ?? false)
        {
          LoggerProvider.LogInfo($"[PieceWaterEffects] Filtered out PS '{psName}', localPos={ps.transform.localPosition}, renderMode={psRenderer?.renderMode}");
        }
        ps.gameObject.SetActive(false);
        Destroy(ps.gameObject);
        continue;
      }

      validPsCandidates.Add(ps);
    }

    for (int i = 0; i < validPsCandidates.Count; i++)
    {
      var ps = validPsCandidates[i];
      var psName = ps.name;
      var psRenderer = ps.GetComponent<ParticleSystemRenderer>();

      bool isSpray = false;
      if (validPsCandidates.Count == 1)
      {
        isSpray = psName.IndexOf("spray", StringComparison.OrdinalIgnoreCase) >= 0 ||
                  psName.IndexOf("trail", StringComparison.OrdinalIgnoreCase) >= 0;
      }
      else
      {
        isSpray = psName.IndexOf("spray", StringComparison.OrdinalIgnoreCase) >= 0 ||
                  psName.IndexOf("trail", StringComparison.OrdinalIgnoreCase) >= 0 ||
                  psName.IndexOf("particle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                  ps.transform.localPosition.z < -0.5f;
      }

      if (isSpray)
      {
        ps.transform.SetParent(_sprayRoot, false);
        ps.transform.localPosition = Vector3.zero;
        sprayList.Add(ps);
      }
      else
      {
        if (psRenderer != null && psRenderer.renderMode == ParticleSystemRenderMode.Billboard)
        {
          psRenderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
        }
        ps.transform.SetParent(_flatFoamRoot, false);
        ps.transform.localPosition = Vector3.zero;
        flatFoamList.Add(ps);
      }

      allValidPs.Add(ps);
      if (VehicleGuiMenuConfig.EnableLoopLogging?.Value ?? false)
      {
        LoggerProvider.LogInfo($"[PieceWaterEffects] Assigned PS '{psName}' as {(isSpray ? "SPRAY" : "FLAT FOAM")}");
      }
    }

    _flatFoamParticles = flatFoamList.ToArray();
    _sprayParticles = sprayList.ToArray();
    _particles = allValidPs.ToArray();
    _effectInstance.SetActive(true);
    SetEmission(false);
  }

  private void LateUpdate()
  {
    if (_particles == null || _particles.Length == 0) return;

    if (!GetWaterWakeEnabled())
    {
      if (_isEmitting) SetEmission(false);
      if (_effectInstance != null && _effectInstance.activeSelf) _effectInstance.SetActive(false);
      return;
    }
    if (_effectInstance != null && !_effectInstance.activeSelf)
    {
      _effectInstance.SetActive(true);
    }

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
    const float minSpeed = 1.0f;

    bool shouldEmit = false;
    Vector3 targetEffPos = Vector3.zero;
    float targetYaw = transform.eulerAngles.y;

    if (IsFallbackEmitter)
    {
      var ruddersCount = _vehicleManager?.PiecesController?.m_rudderPieces?.Count ?? 0;
      if (ruddersCount == 0 && speed > minSpeed)
      {
        Vector3 backPoint;
        if (_vehicleManager?.PiecesController?.FloatCollider != null)
        {
          var col = _vehicleManager.PiecesController.FloatCollider;
          var fwd = _vehicleManager.transform.forward;
          backPoint = col.bounds.center - fwd * (col.size.z * 0.5f);
        }
        else
        {
          backPoint = transform.position - transform.forward * 2f;
        }

        float waterY = Floating.GetWaterLevel(backPoint, ref _previousWaterVolume);
        if (waterY > -1000f)
        {
          shouldEmit = true;
          backPoint.y = waterY - 0.05f;
          targetEffPos = backPoint;
          targetYaw = _vehicleManager != null ? _vehicleManager.transform.eulerAngles.y : transform.eulerAngles.y;
        }
      }
    }
    else
    {
      var rudderPos = transform.position;
      var waterY = Floating.GetWaterLevel(new Vector3(rudderPos.x, 0f, rudderPos.z), ref _previousWaterVolume);
      bool isRudderInWater = (waterY > -1000f) && (waterY >= rudderPos.y - 3.5f) && (waterY <= rudderPos.y + 1.0f);

      shouldEmit = speed > minSpeed && isRudderInWater;

      if (shouldEmit)
      {
        Vector3 shipBackDir = _vehicleManager != null ? -_vehicleManager.transform.forward : -transform.forward;
        targetEffPos = new Vector3(rudderPos.x, waterY - 0.05f, rudderPos.z) + shipBackDir * 0.2f;
        targetYaw = _vehicleManager != null ? _vehicleManager.transform.eulerAngles.y : transform.eulerAngles.y;
      }
    }

    if (_isEmitting != shouldEmit)
    {
      SetEmission(shouldEmit);
    }

    if (shouldEmit && _effectInstance != null)
    {
      _effectInstance.transform.position = targetEffPos;
      _effectInstance.transform.rotation = Quaternion.Euler(0f, targetYaw, 0f);
    }

    // Apply interactive slider offsets to flat foam and spray roots
    if (_flatFoamRoot != null)
    {
      _flatFoamRoot.localPosition = GetFlatFoamOffset();
    }
    if (_sprayRoot != null)
    {
      _sprayRoot.localPosition = GetSprayOffset();
    }

    // Dynamic wave decal projection: update flat foam particles to dynamically hug undulating ocean waves
    for (int pIdx = 0; pIdx < _flatFoamParticles.Length; pIdx++)
    {
      var ps = _flatFoamParticles[pIdx];
      if (ps == null) continue;

      int maxP = ps.main.maxParticles;
      if (_particleBuffer == null || _particleBuffer.Length < maxP)
      {
        _particleBuffer = new ParticleSystem.Particle[Mathf.Max(maxP, 256)];
      }

      int numParticles = ps.GetParticles(_particleBuffer);
      if (numParticles > 0)
      {
        bool changed = false;
        for (int i = 0; i < numParticles; i++)
        {
          Vector3 pPos = _particleBuffer[i].position;
          float currentWaterY = Floating.GetWaterLevel(pPos, ref _previousWaterVolume);
          if (currentWaterY > -1000f)
          {
            _particleBuffer[i].position = new Vector3(pPos.x, currentWaterY - 0.05f, pPos.z);
            changed = true;
          }
        }
        if (changed)
        {
          ps.SetParticles(_particleBuffer, numParticles);
        }
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
