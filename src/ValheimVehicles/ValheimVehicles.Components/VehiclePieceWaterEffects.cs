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
  public static float GlobalWakeFoamRate => VehicleGlobalConfig.DefaultWakeFoamRate?.Value ?? 1.0f;
  public static float GlobalWakeFoamLifetime => VehicleGlobalConfig.DefaultWakeFoamLifetime?.Value ?? 1.0f;
  public static float GlobalWakeSprayRate => VehicleGlobalConfig.DefaultWakeSprayRate?.Value ?? 1.0f;
  public static float GlobalWakeSprayLifetime => VehicleGlobalConfig.DefaultWakeSprayLifetime?.Value ?? 1.0f;

  public float GetWakeFoamRate()
  {
    var zdo = _vehicleManager?.m_nview?.GetZDO();
    return zdo?.GetFloat(Shared.Constants.VehicleZdoVars.WakeFoamRate, GlobalWakeFoamRate) ?? GlobalWakeFoamRate;
  }

  public float GetWakeFoamLifetime()
  {
    var zdo = _vehicleManager?.m_nview?.GetZDO();
    return zdo?.GetFloat(Shared.Constants.VehicleZdoVars.WakeFoamLifetime, GlobalWakeFoamLifetime) ?? GlobalWakeFoamLifetime;
  }

  public float GetWakeSprayRate()
  {
    var zdo = _vehicleManager?.m_nview?.GetZDO();
    return zdo?.GetFloat(Shared.Constants.VehicleZdoVars.WakeSprayRate, GlobalWakeSprayRate) ?? GlobalWakeSprayRate;
  }

  public float GetWakeSprayLifetime()
  {
    var zdo = _vehicleManager?.m_nview?.GetZDO();
    return zdo?.GetFloat(Shared.Constants.VehicleZdoVars.WakeSprayLifetime, GlobalWakeSprayLifetime) ?? GlobalWakeSprayLifetime;
  }

  public VehicleWaterEffectType EffectType = VehicleWaterEffectType.Wake;
  public bool IsFallbackEmitter = false;

  private struct ParticleSystemBaseSettings
  {
    public float StartLifetime;
    public float RateOverTime;
    public float RateOverDistance;
  }
  private readonly Dictionary<ParticleSystem, ParticleSystemBaseSettings> _baseSettings = new();

  private GameObject? _effectInstance;
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

      // Record baseline template settings before any multipliers are applied
      _baseSettings[ps] = new ParticleSystemBaseSettings
      {
        StartLifetime = ps.main.startLifetimeMultiplier,
        RateOverTime = ps.emission.rateOverTimeMultiplier,
        RateOverDistance = ps.emission.rateOverDistanceMultiplier
      };

      // Set both the flat foam and trailing particle spray to 0, 0, 0 relative to the rudder
      ps.transform.localPosition = Vector3.zero;

      // Enforce World simulation space so particles trail behind in the world rather than sticking to the boat
      var main = ps.main;
      main.simulationSpace = ParticleSystemSimulationSpace.World;

      bool isFlatFoam = false;
      if (psRenderer != null && (psRenderer.renderMode == ParticleSystemRenderMode.Billboard || psRenderer.renderMode == ParticleSystemRenderMode.HorizontalBillboard))
      {
        psRenderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
        isFlatFoam = true;
      }
      else if (psName.IndexOf("foam", StringComparison.OrdinalIgnoreCase) >= 0 || psName.IndexOf("wake", StringComparison.OrdinalIgnoreCase) >= 0)
      {
        isFlatFoam = true;
      }

      if (isFlatFoam)
      {
        flatFoamList.Add(ps);
      }
      else
      {
        sprayList.Add(ps);
      }

      allValidPs.Add(ps);
      if (VehicleGuiMenuConfig.EnableLoopLogging?.Value ?? false)
      {
        LoggerProvider.LogInfo($"[PieceWaterEffects] Active wake PS '{psName}' configured at 0,0,0 as {(isFlatFoam ? "FLAT FOAM" : "SPRAY")}");
      }
    }

    if (flatFoamList.Count == 0 && validPsCandidates.Count > 0)
    {
      flatFoamList.AddRange(validPsCandidates);
    }

    _flatFoamParticles = flatFoamList.ToArray();
    _sprayParticles = sprayList.ToArray();
    _particles = allValidPs.ToArray();

    UpdateParticleParameters();

    _effectInstance.SetActive(true);
    SetEmission(false);
  }

  public void UpdateParticleParameters()
  {
    float foamRate = GetWakeFoamRate();
    float foamLifetime = GetWakeFoamLifetime();
    float sprayRate = GetWakeSprayRate();
    float sprayLifetime = GetWakeSprayLifetime();

    for (int i = 0; i < _flatFoamParticles.Length; i++)
    {
      var ps = _flatFoamParticles[i];
      if (ps == null) continue;
      if (_baseSettings.TryGetValue(ps, out var baseVal))
      {
        var main = ps.main;
        main.startLifetimeMultiplier = baseVal.StartLifetime * 0.25f * foamLifetime;

        var emission = ps.emission;
        emission.rateOverTimeMultiplier = baseVal.RateOverTime * 0.25f * foamRate;
        emission.rateOverDistanceMultiplier = baseVal.RateOverDistance * 0.25f * foamRate;
      }
    }

    for (int i = 0; i < _sprayParticles.Length; i++)
    {
      var ps = _sprayParticles[i];
      if (ps == null) continue;
      if (_baseSettings.TryGetValue(ps, out var baseVal))
      {
        var main = ps.main;
        main.startLifetimeMultiplier = baseVal.StartLifetime * sprayLifetime;

        var emission = ps.emission;
        emission.rateOverTimeMultiplier = baseVal.RateOverTime * sprayRate;
        emission.rateOverDistanceMultiplier = baseVal.RateOverDistance * sprayRate;
      }
    }
  }

  public float GetDynamicWaterHeight(Vector3 worldPos)
  {
    float defaultWaterLevel = ZoneSystem.instance != null ? ZoneSystem.instance.m_waterLevel : 30f;
    var floatation = _vehicleManager?.MovementController?.GetShipFloatation();
    if (floatation.HasValue && floatation.Value.AverageWaterHeight > -1000f)
    {
      defaultWaterLevel = floatation.Value.AverageWaterHeight;
    }

    // 1. If cached WaterVolume is available, check if worldPos (XZ) is within its collider bounds
    if (_previousWaterVolume != null && _previousWaterVolume.gameObject != null)
    {
      var col = _previousWaterVolume.gameObject.GetComponent<Collider>();
      if (col != null)
      {
        var b = col.bounds;
        if (worldPos.x >= b.min.x && worldPos.x <= b.max.x && worldPos.z >= b.min.z && worldPos.z <= b.max.z)
        {
          return _previousWaterVolume.GetWaterSurface(worldPos);
        }
      }
      else
      {
        return _previousWaterVolume.GetWaterSurface(worldPos);
      }
    }

    // 2. Query Floating.GetWaterLevel sampled at defaultWaterLevel so OverlapSphere is inside the WaterVolume collider
    Vector3 samplePos = new Vector3(worldPos.x, defaultWaterLevel, worldPos.z);
    try
    {
      float waterLevel = Floating.GetWaterLevel(samplePos, ref _previousWaterVolume);
      if (waterLevel > -1000f && !float.IsNaN(waterLevel))
      {
        return waterLevel;
      }
    }
    catch { }

    // 3. Fallback to GetLiquidLevel
    try
    {
      float liquidLevel = Floating.GetLiquidLevel(samplePos, 1f, LiquidType.Water);
      if (liquidLevel > -1000f && !float.IsNaN(liquidLevel))
      {
        return liquidLevel;
      }
    }
    catch { }

    return defaultWaterLevel;
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

        float waterY = GetDynamicWaterHeight(backPoint);
        shouldEmit = true;
        backPoint.y = waterY - 0.05f;
        targetEffPos = backPoint;
        targetYaw = _vehicleManager != null ? _vehicleManager.transform.eulerAngles.y : transform.eulerAngles.y;
      }
    }
    else
    {
      var rudderPos = transform.position;
      var waterY = GetDynamicWaterHeight(rudderPos);
      bool isRudderInWater = (waterY >= rudderPos.y - 3.5f) && (waterY <= rudderPos.y + 1.0f);

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

    // Dynamic wave decal projection: update all living wake/foam particles to dynamically hug undulating ocean waves
    for (int pIdx = 0; pIdx < _particles.Length; pIdx++)
    {
      var ps = _particles[pIdx];
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
        bool isWorldSpace = ps.main.simulationSpace == ParticleSystemSimulationSpace.World;

        for (int i = 0; i < numParticles; i++)
        {
          Vector3 pPos = _particleBuffer[i].position;
          Vector3 worldPos = isWorldSpace ? pPos : ps.transform.TransformPoint(pPos);

          float currentWaterY = GetDynamicWaterHeight(worldPos);
          if (currentWaterY > -1000f && !float.IsNaN(currentWaterY))
          {
            Vector3 targetPos = new Vector3(worldPos.x, currentWaterY - 0.05f, worldPos.z);
            _particleBuffer[i].position = isWorldSpace ? targetPos : ps.transform.InverseTransformPoint(targetPos);
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
