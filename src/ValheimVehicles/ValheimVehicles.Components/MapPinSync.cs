using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DynamicLocations.Constants;
using DynamicLocations.Controllers;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.Controllers;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Shared.Constants;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.Enums;
using ZdoWatcher;
using Zolantris.Shared;

namespace ValheimVehicles.Components;

public class MapPinSync : MonoBehaviour
{
  public class VehiclePinData
  {
    public Minimap.PinData pinData = null!;
    public int vehicleId;
    public ZDO? zdo;
    public Vector3 lastKnownPosition;
    public bool isAnchored;
  }

  private readonly Dictionary<int, VehiclePinData> _vehiclePins = new();
  public static MapPinSync Instance = null!;
  private CoroutineHandle? refreshDynamicSpawnPinRoutine;
  private CoroutineHandle? refreshVehiclePinsRoutine;
  private CoroutineHandle? zdoSyncCoroutine;

  public string GetOwnerNameFromZdo(ZDO zdo)
  {
    return CensorShittyWords.FilterUGC(zdo.GetString(ZDOVars.s_ownerName),
      UGCType.CharacterName, Player.m_localPlayer.GetOwner());
  }

  public bool hasInitialized = false;

  public void Awake()
  {
    refreshDynamicSpawnPinRoutine ??= new CoroutineHandle(this);
    refreshVehiclePinsRoutine ??= new CoroutineHandle(this);
    zdoSyncCoroutine ??= new CoroutineHandle(this);

    Instance = this;
  }

  private void OnEnable()
  {
    LoadSprites();
    refreshDynamicSpawnPinRoutine ??= new CoroutineHandle(this);
    refreshVehiclePinsRoutine ??= new CoroutineHandle(this);
    zdoSyncCoroutine ??= new CoroutineHandle(this);
    MinimapManager.OnVanillaMapDataLoaded += OnMapReady;
  }

  private void OnDisable()
  {
    MinimapManager.OnVanillaMapDataLoaded -= OnMapReady;

    refreshDynamicSpawnPinRoutine?.Stop();
    refreshVehiclePinsRoutine?.Stop();
    zdoSyncCoroutine?.Stop();

    ClearAllVehiclePins();
    refreshVehiclePinsRoutine = null;
    hasInitialized = false;
  }

  private void OnMapReady()
  {
    if (ZNet.instance == null || Minimap.instance == null) return;
    ZdoWatchController.Instance?.GetAllZdoIdGuids();

    StopAllCoroutines();
    ClearAllVehiclePins();

    StartDynamicSpawnPinSync();
    StartVehiclePinSync();
    StartRefreshAllVehicleZDos();

    hasInitialized = true;
  }

  public void StartRefreshAllVehicleZDos()
  {
    zdoSyncCoroutine?.Start(RefreshAllVehicleZDOs());
  }

  public void StartVehiclePinSync()
  {
    refreshVehiclePinsRoutine?.Start(RefreshVehiclePins());
  }

  public void StartDynamicSpawnPinSync()
  {
    refreshDynamicSpawnPinRoutine?.Start(RefreshDynamicSpawnPin());
  }

  private bool IsWithinVisibleRadius(Vector3 point)
  {
    if (MinimapConfig.ShowAllVehiclesOnMap.Value) return true;
    if (Player.m_localPlayer == null) return false;

    var playerPosition = Player.m_localPlayer.transform.position;
    var dx = playerPosition.x - point.x;
    var dz = playerPosition.z - point.z;
    var visibleRadius = MinimapConfig.VisibleVehicleRadius.Value;

    return dx * dx + dz * dz <= visibleRadius * visibleRadius;
  }

  private IEnumerator UpdatePlayerSpawnPin()
  {
    yield break;
  }

  private readonly WaitForSeconds oneSecondWait = new(1f);

  private enum SyncPreconditionState
  {
    Ready,
    Wait,
    Stop
  }

  private SyncPreconditionState EvaluateSyncPreconditions(out YieldInstruction waitInstruction)
  {
    waitInstruction = oneSecondWait;

    if (ZNet.instance == null)
    {
      return SyncPreconditionState.Wait;
    }

    if (ZNet.instance.IsServer() && ZNet.instance.IsDedicated()) return SyncPreconditionState.Stop;

    if (Player.m_localPlayer == null) return SyncPreconditionState.Wait;

    return SyncPreconditionState.Ready;
  }

  public IEnumerator RefreshDynamicSpawnPin()
  {
    while (isActiveAndEnabled)
    {
      var preconditionState = EvaluateSyncPreconditions(out var waitInstruction);
      if (preconditionState == SyncPreconditionState.Stop) yield break;
      if (preconditionState == SyncPreconditionState.Wait)
      {
        yield return waitInstruction;
        continue;
      }

      yield return UpdatePlayerSpawnPin();
      yield return new WaitForSeconds(Mathf.Max(MinimapConfig.BedPinSyncInterval.Value, 1f));
    }
  }

  public IEnumerator RefreshAllVehicleZDOs()
  {
    while (isActiveAndEnabled)
    {
      var preconditionState = EvaluateSyncPreconditions(out var waitInstruction);
      if (preconditionState == SyncPreconditionState.Stop) yield break;
      if (preconditionState == SyncPreconditionState.Wait)
      {
        yield return waitInstruction;
        continue;
      }

      ZdoWatchController.Instance?.RequestAllPersistentZdosFromServer();
      yield return new WaitForSeconds(30f);
    }
  }

  private static Sprite? _vehicleLandMapSprite;
  private static Sprite? _vehicleWaterMapSprite;
  private static Sprite? _vehicleAirMapSprite;

  public static void LoadSprites()
  {
    try
    {
      _vehicleLandMapSprite = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames.LandVehicle);
      _vehicleWaterMapSprite = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames.WaterVehicle);
      _vehicleAirMapSprite = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames.AirVehicle);
    }
    catch (Exception ex)
    {
      Debug.LogError($"Error loading vehicle map sprites: {ex}");
    }
  }

  public void UpdatePinIconForZdo(ZDO zdo, bool isFlightMode)
  {
    if (Minimap.instance == null || zdo == null) return;
    var xSprite = Minimap.instance.GetSprite(Minimap.PinType.Icon4);
    var targetSprite = isFlightMode ? _vehicleAirMapSprite : xSprite;

    foreach (var entry in _vehiclePins.Values)
    {
      if (entry.zdo == zdo || (entry.zdo != null && entry.zdo.m_uid == zdo.m_uid))
      {
        entry.pinData.m_icon = targetSprite;
        if (entry.pinData.m_iconElement != null)
        {
          entry.pinData.m_iconElement.sprite = targetSprite;
        }
        return;
      }
    }
  }

  private static Sprite? GetVehicleMapSprite(ZDO zdo)
  {
    var prefab = ZNetScene.instance?.GetPrefab(zdo.GetPrefab());
    if (prefab == null) return null;

    var isLandVehicle = prefab.name.StartsWith(PrefabNames.LandVehicle);
    if (isLandVehicle) return _vehicleLandMapSprite;

    var isFlightMode = zdo.GetBool(VehicleZdoVars.VehicleFlightMode, false);
    if (isFlightMode)
    {
      return _vehicleAirMapSprite;
    }

    // In Float Mode (water vehicle), return the native red 'X' PinType.Icon4 sprite
    return Minimap.instance?.GetSprite(Minimap.PinType.Icon4);
  }

  private void UpdateVehiclePins()
  {
    if (Minimap.instance == null || Minimap.instance.m_pins == null) return;
    if (ZdoWatchController.Instance == null || ZNetScene.instance == null) return;

    var guids = ZdoWatchController.Instance.GetAllZdoIdGuids();

    var activeVehicleIds = new HashSet<int>();
    if (guids != null)
    {
      foreach (var kvp in guids)
      {
        if (kvp.Key != 0) activeVehicleIds.Add(kvp.Key);
      }
    }
    foreach (var loadedId in VehicleManager.VehicleInstances.Keys)
    {
      if (loadedId != 0) activeVehicleIds.Add(loadedId);
    }

    var deadVehicleIds = new List<int>();
    foreach (var kvp in _vehiclePins)
    {
      if (!VehicleRecallController.DoesVehicleExist(kvp.Key))
      {
        deadVehicleIds.Add(kvp.Key);
      }
    }
    foreach (var deadId in deadVehicleIds)
    {
      if (_vehiclePins.TryGetValue(deadId, out var deadEntry))
      {
        if (deadEntry.pinData != null) Minimap.instance.RemovePin(deadEntry.pinData);
        _vehiclePins.Remove(deadId);
      }
    }

    foreach (var vehicleId in activeVehicleIds)
    {
      bool hasLoc = VehicleRecallController.GetVehicleLocation(vehicleId, out var currentPos, out var currentRot, out var isLoaded, out var vm);

      ZDO? zdo = vm?.m_nview?.GetZDO() ?? (ZdoWatchController.Instance != null ? ZdoWatchController.Instance.GetZdo(vehicleId) : null);
      if (zdo == null && ZDOMan.instance != null && guids != null && guids.TryGetValue(vehicleId, out var zdoid))
      {
        zdo = ZDOMan.instance.GetZDO(zdoid);
      }

      bool isAnchored = false;
      if (isLoaded && vm != null && vm.MovementController != null)
      {
        isAnchored = vm.MovementController.isAnchored || vm.MovementController.vehicleAnchorState == AnchorState.Anchored;
      }
      else if (zdo != null)
      {
        isAnchored = zdo.GetInt(VehicleZdoVars.VehicleAnchorState, 0) == (int)AnchorState.Anchored;
      }

      var zdoVehicleName = zdo?.GetString(VehicleCustomConfig.Key_VehicleName, "") ?? "";
      if (string.IsNullOrEmpty(zdoVehicleName) || zdoVehicleName == "Unnamed")
      {
        zdoVehicleName = "ValheimRAFT";
      }
      var isSpawnSet = BoatBedSpawnController.IsBoatSpawnActiveForVehicle(vehicleId);
      var displayLabel = isSpawnSet ? $"{zdoVehicleName} [Spawn]" : zdoVehicleName;

      var sprite = zdo != null ? GetVehicleMapSprite(zdo) : Minimap.instance?.GetSprite(Minimap.PinType.Icon4);

      if (_vehiclePins.TryGetValue(vehicleId, out var existingEntry))
      {
        existingEntry.isAnchored = isAnchored;

        // If the ship is anchored, pause position updates (it won't move on its own)
        // If not anchored and position is available, update position
        if (!isAnchored && hasLoc)
        {
          existingEntry.lastKnownPosition = currentPos;
          existingEntry.pinData.m_pos = currentPos;
        }

        if (existingEntry.pinData.m_name != displayLabel)
        {
          existingEntry.pinData.m_name = displayLabel;
          if (existingEntry.pinData.m_NamePinData?.PinNameText != null)
          {
            existingEntry.pinData.m_NamePinData.PinNameText.text = displayLabel;
          }
        }

        if (sprite != null && existingEntry.pinData.m_icon != sprite)
        {
          existingEntry.pinData.m_icon = sprite;
          if (existingEntry.pinData.m_iconElement != null)
          {
            existingEntry.pinData.m_iconElement.sprite = sprite;
          }
        }
      }
      else
      {
        if (!hasLoc) continue;

        var localPlayerId = Player.m_localPlayer != null ? Player.m_localPlayer.GetPlayerID() : 0;
        bool isCreator = zdo != null && zdo.GetLong(ZDOVars.s_creator, 0) == localPlayerId;

        bool isVisible = MinimapConfig.ShowAllVehiclesOnMap.Value ||
                         IsWithinVisibleRadius(currentPos) ||
                         isAnchored ||
                         isCreator ||
                         isSpawnSet;

        if (isVisible)
        {
          var pinData = Minimap.instance.AddPin(currentPos,
            Minimap.PinType.Icon4,
            displayLabel, false, false, zdo?.GetOwner() ?? 0);

          if (sprite != null)
          {
            pinData.m_icon = sprite;
            if (pinData.m_iconElement != null)
            {
              pinData.m_iconElement.sprite = sprite;
            }
          }

          _vehiclePins[vehicleId] = new VehiclePinData
          {
            pinData = pinData,
            vehicleId = vehicleId,
            zdo = zdo,
            lastKnownPosition = currentPos,
            isAnchored = isAnchored
          };
        }
      }
    }
  }

  public Minimap.PinData? GetVehiclePin(int vehicleId)
  {
    if (vehicleId == 0) return null;
    return _vehiclePins.TryGetValue(vehicleId, out var entry) ? entry.pinData : null;
  }

  public IEnumerator RefreshVehiclePins()
  {
    while (isActiveAndEnabled)
    {
      var preconditionState = EvaluateSyncPreconditions(out var waitInstruction);
      if (preconditionState == SyncPreconditionState.Stop) yield break;
      if (preconditionState == SyncPreconditionState.Wait)
      {
        yield return waitInstruction;
        continue;
      }

      UpdateVehiclePins();

      yield return new WaitForSeconds(Mathf.Max(MinimapConfig.VehiclePinSyncInterval.Value, 1f));
    }
  }

  private void ClearAllVehiclePins()
  {
    if (Minimap.instance == null || Minimap.instance.m_pins == null) return;

    foreach (var entry in _vehiclePins.Values)
    {
      if (entry.pinData != null && Minimap.instance.m_pins.Contains(entry.pinData))
      {
        Minimap.instance.RemovePin(entry.pinData);
      }
    }

    _vehiclePins.Clear();
  }
}