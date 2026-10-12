using System;
using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.Interfaces;
using ValheimVehicles.Shared.Constants;
using ValheimVehicles.Structs;
using ValheimVehicles.Controllers;
using ZdoWatcher;
using ZdoWatcher.ZdoWatcher.Utils;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Components;

public class CultivatableComponent : MonoBehaviour, Interactable, Hoverable
{
  private ZNetView m_nview;

  private static Dictionary<int, List<int>> m_childObjects = new();

  private float textureScale = 8f;

  public bool isCultivatable { get; set; } = true;

  public Heightmap m_heightmap;

  private static readonly Dictionary<string, string> FallbackSeedToPrefab = new(StringComparer.OrdinalIgnoreCase)
  {
    // Regular seeds -> crops
    { "CarrotSeeds", "sapling_carrot" },
    { "TurnipSeeds", "sapling_turnip" },
    { "OnionSeeds", "sapling_onion" },
    { "Barley", "sapling_barley" },
    { "Flax", "sapling_flax" },
    { "MushroomJotunpuffs", "sapling_jotunpuffs" },
    { "MushroomMagecap", "sapling_magecap" },
    { "VineGreenSeeds", "VineGreen_sapling" },
    { "VineAshSeeds", "VineAsh_sapling" },
    { "VineSeeds", "VineAsh_sapling" },
    { "FiddleheadSeeds", "sapling_seedkale" },
    { "MushroomSmokePuffSeeds", "sapling_jotunpuffs" },
    // Replanting crops -> seed plants
    { "Carrot", "sapling_seedcarrot" },
    { "Turnip", "sapling_seedturnip" },
    { "Onion", "sapling_seedonion" },
    // Tree seeds
    { "BeechSeeds", "Beech_Sapling" },
    { "BirchSeeds", "Birch_Sapling" },
    { "Acorn", "Oak_Sapling" },
    { "PineCone", "PineTree_Sapling" },
    { "FirCone", "FirTree_Sapling" }
  };

  public void Awake()
  {
    m_nview = GetComponent<ZNetView>();
    var wnt = GetComponent<WearNTear>();
    if ((bool)wnt)
      wnt.m_onDestroyed =
        (Action)Delegate.Combine(wnt.m_onDestroyed, new Action(OnDestroyed));

    m_heightmap = gameObject.AddComponent<Heightmap>();
    m_heightmap.m_width = 2;
    m_heightmap.m_scale = 1f;
    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
    for (var y = 0; y < 2; y++)
      for (var x = 0; x < 2; x++)
        tex.SetPixel(x, y, new Color(0f, 1f, 0f, 1f));
    tex.Apply();
    m_heightmap.m_paintMask = tex;
  }

  public void Start()
  {
    UpdateMaterial();
  }

  public void UpdateMaterial()
  {
    var uvoffset = transform.parent
      ? new Vector2(0f - transform.localPosition.x,
        0f - transform.localPosition.z)
      : new Vector2(0f - transform.position.x, 0f - transform.position.z);
    uvoffset /= textureScale;
    var uvrotation = transform.parent
      ? 0f - transform.localEulerAngles.y
      : 0f - transform.eulerAngles.y;
    uvrotation /= 360f;
    var uvscale = new Vector2(transform.localScale.x, transform.localScale.z);
    uvscale /= textureScale;
    var renderers = GetComponentsInChildren<MeshRenderer>();
    for (var i = 0; i < renderers.Length; i++)
    {
      var mat = renderers[i].material;
      mat.SetTextureOffset("_MainTex", uvoffset);
      mat.SetTextureScale("_MainTex", uvscale);
      mat.SetTextureOffset("_MainNormal", uvoffset);
      mat.SetTextureScale("_MainNormal", uvscale);
      mat.SetFloat("_MainRotation", uvrotation);
    }
  }

  private void OnDestroyed()
  {
    var myid =
      ZdoWatchController.Instance.GetOrCreatePersistentID(m_nview.m_zdo);
    if (!m_childObjects.TryGetValue(myid, out var list)) return;

    for (var i = 0; i < list.Count; i++)
    {
      var zdo = ZdoWatchController.Instance.GetZdo(list[i]);
      if (zdo == null) continue;

      var obj = ZNetScene.instance.FindInstance(zdo);
      if ((bool)obj)
      {
        if (gameObject == obj || transform.IsChildOf(obj.transform))
        {
          Logger.LogWarning(
            $" gameObject == obj || transform.IsChildOf(obj.transform) {gameObject} == {obj} || {transform.IsChildOf(obj.transform)}");
          continue;
        }

        var wnt = obj.GetComponent<WearNTear>();
        if ((bool)wnt)
        {
          wnt.Destroy();
          continue;
        }

        var netview = obj.GetComponent<ZNetView>();
        if ((bool)netview) netview.Destroy();
      }
      else
      {
        ZDOMan.instance.DestroyZDO(zdo);
      }
    }

    m_childObjects.Remove(myid);
  }

  public static GameObject FindPlantPrefabForItem(ItemDrop.ItemData item)
  {
    if (item == null) return null;

    // 1. Dynamic lookup from Cultivator PieceTable (supports modded crops like PlantEverything)
    var cultivatorPrefab = ObjectDB.instance?.GetItemPrefab("Cultivator");
    var pieceTable = cultivatorPrefab?.GetComponent<ItemDrop>()?.m_itemData?.m_shared?.m_buildPieces;
    if (pieceTable != null && pieceTable.m_pieces != null)
    {
      var itemName = item.m_shared?.m_name;
      var itemPrefabName = item.m_dropPrefab != null ? item.m_dropPrefab.name : null;

      foreach (var pieceGo in pieceTable.m_pieces)
      {
        if (pieceGo == null) continue;
        var piece = pieceGo.GetComponent<Piece>();
        if (piece == null || piece.m_resources == null) continue;

        foreach (var req in piece.m_resources)
        {
          if (req == null || req.m_resItem == null) continue;

          if ((itemPrefabName != null && req.m_resItem.gameObject.name.Equals(itemPrefabName, StringComparison.OrdinalIgnoreCase)) ||
              (itemName != null && req.m_resItem.m_itemData?.m_shared?.m_name == itemName))
          {
            return pieceGo;
          }
        }
      }
    }

    // 2. Fallback dictionary by prefab name or shared name token
    var nameToCheck = item.m_dropPrefab != null ? item.m_dropPrefab.name : item.m_shared?.m_name;
    if (!string.IsNullOrEmpty(nameToCheck))
    {
      var cleanName = nameToCheck.StartsWith("$item_") ? nameToCheck.Substring(6) : nameToCheck;
      if (FallbackSeedToPrefab.TryGetValue(nameToCheck, out var prefabName) ||
          FallbackSeedToPrefab.TryGetValue(cleanName, out prefabName))
      {
        var prefab = ZNetScene.instance?.GetPrefab(prefabName);
        if (prefab != null) return prefab;
      }
    }

    return null;
  }

  public string GetHoverName()
  {
    var piece = GetComponent<Piece>();
    return piece != null ? piece.m_name : "$mb_dirt_floor_1x1";
  }

  public string GetHoverText()
  {
    if (!isCultivatable) return "";
    var name = GetHoverName();
    return Localization.instance.Localize($"{name}\n[<color=yellow><b>1-8</b></color>] $mb_dirt_floor_use");
  }

  public float GetHoverOffset() => 0f;

  public bool Interact(Humanoid user, bool hold, bool alt)
  {
    return false;
  }

  public bool UseItem(Humanoid user, ItemDrop.ItemData item)
  {
    if (user == null || item == null || !isCultivatable) return false;

    var plantPrefab = FindPlantPrefabForItem(item);
    if (plantPrefab == null)
    {
      return false;
    }

    // Raycast from user's camera / eye towards crosshair to find soil surface
    Ray ray;
    if (GameCamera.instance != null)
    {
      ray = new Ray(GameCamera.instance.transform.position, GameCamera.instance.transform.forward);
    }
    else
    {
      ray = new Ray(user.GetEyePoint(), user.GetLookDir());
    }

    var hitMask = LayerMask.GetMask("piece", "Default", "static_solid", "vehicle");
    var hits = Physics.RaycastAll(ray, 10f, hitMask);
    var foundHit = false;
    RaycastHit bestHit = default;

    foreach (var hit in hits)
    {
      if (hit.collider != null && (hit.collider.transform.IsChildOf(transform) || hit.collider.gameObject == gameObject))
      {
        bestHit = hit;
        foundHit = true;
        break;
      }
    }

    Vector3 plantPosition;
    if (foundHit)
    {
      plantPosition = bestHit.point;
    }
    else
    {
      plantPosition = transform.position + transform.up * 0.15f;
    }

    // Spacing check: don't stack directly on top of an existing plant
    var nearbyColliders = Physics.OverlapSphere(plantPosition, 0.35f, LayerMask.GetMask("piece", "Default", "static_solid"));
    foreach (var col in nearbyColliders)
    {
      if (col != null && col.GetComponentInParent<Plant>() != null)
      {
        user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_needspace"));
        return true;
      }
    }

    // Align plant upright relative to planter surface with random yaw
    var plantRotation = Quaternion.AngleAxis(UnityEngine.Random.Range(0f, 360f), transform.up) * transform.rotation;

    // Instantiate plant
    var plantObj = UnityEngine.Object.Instantiate(plantPrefab, plantPosition, plantRotation);
    var piece = plantObj.GetComponent<Piece>();
    if (piece != null)
    {
      if (user is Player player)
      {
        piece.SetCreator(player.GetPlayerID(), Splatform.PlatformManager.DistributionPlatform.LocalUser.PlatformUserID);
      }
    }

    var netView = plantObj.GetComponent<ZNetView>();
    if (netView != null)
    {
      var pieceController = GetComponentInParent<IPieceController>();
      if (pieceController != null)
      {
        pieceController.AddNewPiece(netView);
      }

      AddNewChild(netView);
    }

    plantObj.GetComponent<WearNTear>()?.OnPlaced();

    // Place effects
    if (piece != null && piece.m_placeEffect != null)
    {
      piece.m_placeEffect.Create(plantPosition, plantRotation, plantObj.transform, 1f, -1, user.GetZDOID());
    }
    else
    {
      var myPiece = GetComponent<Piece>();
      if (myPiece != null && myPiece.m_placeEffect != null)
      {
        myPiece.m_placeEffect.Create(plantPosition, plantRotation, plantObj.transform, 1f, -1, user.GetZDOID());
      }
    }

    // Consume 1 item from inventory
    user.GetInventory().RemoveItem(item, 1);

    var displayName = piece != null ? piece.m_name : item.m_shared?.m_name;
    user.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize("$msg_planted", displayName));

    return true;
  }

  public void AddNewChild(ZNetView child)
  {
    AddNewChild(
      ZdoWatchController.Instance.GetOrCreatePersistentID(m_nview.m_zdo),
      child);
  }

  public static void InitPiece(ZNetView netview)
  {
    if (netview == null || !netview || netview.m_zdo == null) return;
    var id = GetParentID(netview);
    if (id == 0) return;

    AddChild(id, netview);

    var sp = netview.GetComponent<StaticPhysics>();
    if ((bool)sp) Destroy(sp);

    // If already attached to a vehicle and has MBPositionHash, ensure it's registered
    var vehicleId = netview.m_zdo.GetInt(VehicleZdoVars.MBParentId, 0);

    // If vehicleId not set directly on crop, look up parent planter's vehicle
    ZDO? planterZdo = null;
    if (vehicleId == 0)
    {
      planterZdo = ZdoWatchController.Instance.GetZdo(id);
      if (planterZdo == null && ZDOMan.instance != null)
      {
        var zdoid = netview.m_zdo.GetZDOID(VehicleZdoVars.MBCultivatableParentHash);
        if (zdoid != ZDOID.None) planterZdo = ZDOMan.instance.GetZDO(zdoid);
      }

      if (planterZdo != null)
      {
        vehicleId = planterZdo.GetInt(VehicleZdoVars.MBParentId, 0);
      }
    }

    if (vehicleId != 0)
    {
      VehiclePiecesController? vehicleController = null;
      if (VehiclePiecesController.ActiveInstances.TryGetValue(vehicleId, out var vc) && vc != null && vc.isActiveAndEnabled && !vc.IsInvalid())
      {
        vehicleController = vc;
      }
      else
      {
        var vm = VehicleManager.GetVehicle(vehicleId);
        if (vm != null && vm.PiecesController != null && vm.PiecesController.isActiveAndEnabled && !vm.PiecesController.IsInvalid())
        {
          vehicleController = vm.PiecesController;
        }
      }

      if (vehicleController != null)
      {
        AttachCropToVehicle(netview, vehicleController, planterZdo, vehicleId, id);
      }
      else
      {
        // Vehicle controller not active yet during sector load, ensure hashes exist then queue as pending piece
        EnsureCropZdoHashes(netview, planterZdo, vehicleId, id);
        BasePieceActivatorComponent.AddPendingPiece(vehicleId, netview, false, true, false);
      }
    }
  }

  private static void EnsureCropZdoHashes(ZNetView netview, ZDO? planterZdo, int vehicleId, int planterPersistentId)
  {
    if (netview == null || netview.m_zdo == null) return;

    var hasSavedPos = netview.m_zdo.GetVec3(VehicleZdoVars.MBPositionHash, Vector3.negativeInfinity) != Vector3.negativeInfinity;
    if (!hasSavedPos)
    {
      planterZdo ??= ZdoWatchController.Instance.GetZdo(planterPersistentId);
      if (planterZdo != null)
      {
        var planterLocalPos = planterZdo.GetVec3(VehicleZdoVars.MBPositionHash, Vector3.zero);
        var planterLocalRot = planterZdo.GetVec3(VehicleZdoVars.MBRotationVecHash, Vector3.zero);
        netview.m_zdo.Set(VehicleZdoVars.MBPositionHash, planterLocalPos + Vector3.up * 0.15f);
        netview.m_zdo.Set(VehicleZdoVars.MBRotationVecHash, planterLocalRot);
      }
    }

    netview.m_zdo.Set(VehicleZdoVars.MBParentId, vehicleId);
    netview.m_zdo.Set(VehicleZdoVars.IsVehiclePieceHash, 1);
  }

  public static void AttachCropToVehicle(ZNetView netview, VehiclePiecesController vehicleController, ZDO? planterZdo, int vehicleId, int planterPersistentId)
  {
    if (netview == null || netview.m_zdo == null || vehicleController == null) return;

    var hasSavedPos = netview.m_zdo.GetVec3(VehicleZdoVars.MBPositionHash, Vector3.negativeInfinity) != Vector3.negativeInfinity;
    Vector3 localPos;
    Vector3 localEuler;

    if (hasSavedPos)
    {
      localPos = netview.m_zdo.GetVec3(VehicleZdoVars.MBPositionHash, Vector3.zero);
      localEuler = netview.m_zdo.GetVec3(VehicleZdoVars.MBRotationVecHash, Vector3.zero);
    }
    else
    {
      // Recovering a detached / floating crop (such as crops that grew without vehicle parenting)
      planterZdo ??= ZdoWatchController.Instance.GetZdo(planterPersistentId);
      ZNetView? planterNv = null;
      if (planterZdo != null && ZNetScene.instance != null)
      {
        planterNv = ZNetScene.instance.FindInstance(planterZdo);
      }

      if (planterNv != null)
      {
        // Snap crop directly onto planter top soil surface
        var targetWorldPos = planterNv.transform.position + planterNv.transform.up * 0.15f;
        localPos = vehicleController.transform.InverseTransformPoint(targetWorldPos);
        localEuler = (Quaternion.Inverse(vehicleController.transform.rotation) * planterNv.transform.rotation).eulerAngles;
      }
      else if (planterZdo != null)
      {
        var planterLocalPos = planterZdo.GetVec3(VehicleZdoVars.MBPositionHash, Vector3.zero);
        localPos = planterLocalPos + Vector3.up * 0.15f;
        localEuler = planterZdo.GetVec3(VehicleZdoVars.MBRotationVecHash, Vector3.zero);
      }
      else
      {
        localPos = vehicleController.transform.InverseTransformPoint(netview.transform.position);
        localEuler = (Quaternion.Inverse(vehicleController.transform.rotation) * netview.transform.rotation).eulerAngles;
      }

      netview.m_zdo.Set(VehicleZdoVars.MBPositionHash, localPos);
      netview.m_zdo.Set(VehicleZdoVars.MBRotationVecHash, localEuler);
    }

    netview.m_zdo.Set(VehicleZdoVars.MBParentId, vehicleId);
    netview.m_zdo.Set(VehicleZdoVars.IsVehiclePieceHash, 1);

    vehicleController.TrySetPieceToParent(netview);
    netview.transform.localPosition = localPos;
    netview.transform.localRotation = Quaternion.Euler(localEuler);

    VehiclePieceRegistry.RegisterPiece(vehicleId, netview.m_zdo.m_uid);
    vehicleController.AddPiece(netview);
  }

  public static int GetParentID(ZNetView netview)
  {
    if (netview == null || !netview || netview.m_zdo == null) return 0;
    var id = netview.m_zdo.GetInt(VehicleZdoVars.MBCultivatableParentIdHash);
    if (id == 0)
    {
      var zdoid = netview.m_zdo.GetZDOID(VehicleZdoVars.MBCultivatableParentHash);
      if (zdoid != ZDOID.None && ZDOMan.instance != null)
      {
        var zdoparent = ZDOMan.instance.GetZDO(zdoid);
        id = zdoparent == null
          ? ZdoUtils.ZdoIdToId(zdoid)
          : ZdoWatchController.Instance.GetOrCreatePersistentID(zdoparent);
        netview.m_zdo.Set(VehicleZdoVars.MBCultivatableParentIdHash, id);
      }
    }

    return id;
  }

  public static void AddNewChild(int parent, ZNetView child)
  {
    child.m_zdo.Set(VehicleZdoVars.MBCultivatableParentIdHash, parent);
    AddChild(parent, child);
  }

  public static void AddChild(int parent, ZNetView child)
  {
    var sp = child.GetComponent<StaticPhysics>();
    if ((bool)sp) Destroy(sp);

    AddChild(parent,
      ZdoWatchController.Instance.GetOrCreatePersistentID(child.m_zdo));
  }

  private static void AddChild(int parent, int child)
  {
    if (parent != 0)
    {
      if (!m_childObjects.TryGetValue(parent, out var list))
      {
        list = new List<int>();
        m_childObjects.Add(parent, list);
      }

      list.Add(child);
    }
  }
}