using System;

using System.Collections;

using System.Collections.Generic;

using System.Linq;

using System.Security.Cryptography.X509Certificates;

using DynamicLocations.Constants;

using DynamicLocations.Controllers;

using Jotunn.Managers;

using UnityEngine;

using ValheimVehicles.BepInExConfig;
using ValheimVehicles.Controllers;

using ValheimVehicles.Prefabs;

using ValheimVehicles.Shared.Constants;

using ValheimVehicles.SharedScripts;

using ZdoWatcher;

using Zolantris.Shared;



namespace ValheimVehicles.Components;



public class MapPinSync : MonoBehaviour

{

  public struct CustomPinZdoData

  {

    public Minimap.PinData pinData;

    public ZDO zdo;

  }



  private Dictionary<Vector3, CustomPinZdoData> _vehiclePins = new();

  public static MapPinSync Instance;

  private ZDO? cachedPlayerSpawnZdo = null;

  private Vector3? cachedLastBedVector = null;

  private Minimap.PinData? cachedLastBedPinData = null;

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

    _vehiclePins.Clear();

    refreshVehiclePinsRoutine = null;

    hasInitialized = false;

  }



  private void OnMapReady()

  {

    if (ZNet.instance == null || Minimap.instance == null) return;

    ZdoWatchController.Instance.GetAllZdoIdGuids();

    // clear everything first

    StopAllCoroutines();

    ClearAllVehiclePins();

    _vehiclePins.Clear();



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



  private void ClearSpawnPin(Minimap.PinData? pinData)

  {

    if (pinData != null) Minimap.instance.RemovePin(pinData);



    cachedLastBedPinData = null;

    cachedLastBedVector = null;

  }



  private IEnumerator UpdatePlayerSpawnPin()
  {
    if (cachedLastBedPinData != null)
    {
      ClearSpawnPin(cachedLastBedPinData);
    }
    yield break;
  }





  private readonly WaitForSeconds oneSecondWait = new(1f);



  private enum SyncPreconditionState

  {

    Ready,

    Wait,

    Stop

  }



  // Shared gate for client-side sync loops: wait when not ready, stop on dedicated server.

  // waitInstruction is ALWAYS non-null when state == Wait to prevent tight spin loops (yield return null every frame).

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



      // heavy call but needed for all vehicles to be searchable on the map.

      ZdoWatchController.Instance.RequestAllPersistentZdosFromServer();

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

    foreach (var pair in _vehiclePins)
    {
      if (pair.Value.zdo == zdo || pair.Value.zdo.m_uid == zdo.m_uid)
      {
        pair.Value.pinData.m_icon = targetSprite;
        if (pair.Value.pinData.m_iconElement != null)
        {
          pair.Value.pinData.m_iconElement.sprite = targetSprite;
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

    if (Minimap.instance == null) return;

    if (Minimap.instance.m_pins == null) return;

    if (ZdoWatchController.Instance == null) return;

    if (ZNetScene.instance == null) return;



    if (ZDOMan.instance == null) return;
    var guids = ZdoWatchController.Instance.GetAllZdoIdGuids();



    // Build vehicle ZDO list without extra ToHashSet allocation on a LINQ chain

    var vehicleZdos = new HashSet<ZDO>();

    foreach (var pair in guids)

    {

      var zdo = ZDOMan.instance.GetZDO(pair.Value);
      if (zdo == null) continue;

      var prefab = ZNetScene.instance.GetPrefab(zdo.GetPrefab());

      if (prefab != null && PrefabNames.IsVehicle(prefab.name))

        vehicleZdos.Add(zdo);

    }



    // Build an O(1) lookup from position -> pin to avoid O(n) List.Find per vehicle pin

    var pinsByPos = new Dictionary<Vector3, Minimap.PinData>(Minimap.instance.m_pins.Count);

    foreach (var pin in Minimap.instance.m_pins)

      pinsByPos[pin.m_pos] = pin;



    var pinZdosToSkip = new HashSet<ZDO>();



    // Update existing pins

    var keysToUpdate = new List<(Vector3 oldKey, Vector3 newKey, CustomPinZdoData data)>();

    foreach (var vehiclePin in _vehiclePins)

    {

      if (!pinsByPos.TryGetValue(vehiclePin.Key, out var getPin)) continue;



      var zdoPosition = vehiclePin.Value.zdo.GetPosition();

      getPin.m_pos = zdoPosition;

      // Update name to ValheimRAFT if unnamed or empty
      var zdoVehicleName = vehiclePin.Value.zdo.GetString(VehicleCustomConfig.Key_VehicleName, "");
      if (string.IsNullOrEmpty(zdoVehicleName) || zdoVehicleName == "Unnamed")
      {
        zdoVehicleName = "ValheimRAFT";
      }

      var vehicleId = vehiclePin.Value.zdo.GetInt(ZdoVarController.PersistentUidHash, 0);
      var isSpawnSet = vehicleId != 0 && BoatBedSpawnController.IsBoatSpawnActiveForVehicle(vehicleId);
      var displayName = isSpawnSet ? $"{zdoVehicleName} [Spawn]" : zdoVehicleName;

      if (getPin.m_name != displayName)
      {
        getPin.m_name = displayName;
        if (getPin.m_NamePinData?.PinNameText != null)
        {
          getPin.m_NamePinData.PinNameText.text = displayName;
        }
      }

      var newSprite = GetVehicleMapSprite(vehiclePin.Value.zdo);
      if (getPin.m_icon != newSprite)
      {
        getPin.m_icon = newSprite;
        if (getPin.m_iconElement != null)
        {
          getPin.m_iconElement.sprite = newSprite;
        }
      }

      var isVisible = IsWithinVisibleRadius(zdoPosition);



      if (!vehiclePin.Key.Equals(zdoPosition))

        keysToUpdate.Add((vehiclePin.Key, zdoPosition, vehiclePin.Value));



      if (isVisible || vehiclePin.Key.Equals(zdoPosition))

        pinZdosToSkip.Add(vehiclePin.Value.zdo);

    }



    // Apply key updates outside enumeration

    foreach (var (oldKey, newKey, data) in keysToUpdate)

    {

      _vehiclePins.Remove(oldKey);

      if (IsWithinVisibleRadius(newKey))

        _vehiclePins[newKey] = data;

    }



    // Add new pins for ZDOs not already processed

    foreach (var zdo in vehicleZdos)

    {

      if (pinZdosToSkip.Contains(zdo)) continue;



      var position = zdo.GetPosition();

      var isVisible = IsWithinVisibleRadius(position);

      if (isVisible)

      {

        var zdoOwner = zdo.GetOwner();

        var zdoVehicleName = zdo.GetString(VehicleCustomConfig.Key_VehicleName, "");
        if (string.IsNullOrEmpty(zdoVehicleName) || zdoVehicleName == "Unnamed")
        {
          zdoVehicleName = "ValheimRAFT";
        }
        var vehicleId = zdo.GetInt(ZdoVarController.PersistentUidHash, 0);
        var isSpawnSet = vehicleId != 0 && BoatBedSpawnController.IsBoatSpawnActiveForVehicle(vehicleId);
        var pinLabel = isSpawnSet ? $"{zdoVehicleName} [Spawn]" : zdoVehicleName;

        var pinData = Minimap.instance.AddPin(position,
          Minimap.PinType.Icon4,
          pinLabel, false, false, zdoOwner);

        var vehicleSprite = GetVehicleMapSprite(zdo);
        if (vehicleSprite != null)
        {
          pinData.m_icon = vehicleSprite;
          if (pinData.m_iconElement != null)
          {
            pinData.m_iconElement.sprite = vehicleSprite;
          }
        }



        _vehiclePins[position] = new CustomPinZdoData

          { pinData = pinData, zdo = zdo };

      }
    }
  }

  public Minimap.PinData? GetVehiclePin(int vehicleId)
  {
    if (vehicleId == 0) return null;
    foreach (var pair in _vehiclePins)
    {
      if (pair.Value.zdo != null)
      {
        var pId = pair.Value.zdo.GetInt(ZdoVarController.PersistentUidHash, 0);
        if (pId == vehicleId) return pair.Value.pinData;
      }
    }
    return null;
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



      // Clear existing pins from both dictionaries

      ClearAllVehiclePins();



      // Yield a frame between clear and rebuild to spread cost

      yield return null;



      // Regenerate all pins

      UpdateVehiclePins();



      yield return new WaitForSeconds(Mathf.Max(MinimapConfig.VehiclePinSyncInterval.Value, 1));

    }

  }

  /// <summary>

  /// This will attempt to clean pins on a rapid reload so there are no null references.

  /// </summary>

  private void ClearAllVehiclePins()

  {

    if (Minimap.instance == null) return;

    if (Minimap.instance.m_pins == null) return;

    // Remove each pin in _vehiclePins from m_locationPins

    foreach (var vehiclePin in _vehiclePins.Where(vehiclePin =>

               Minimap.instance.m_pins.Contains(vehiclePin.Value.pinData)))

      Minimap.instance.RemovePin(vehiclePin.Value.pinData);



    // Clear the local _vehiclePins dictionary

    _vehiclePins.Clear();

  }

}