using UnityEngine;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using ValheimVehicles.Components;
using ValheimVehicles.SharedScripts;
using Object = UnityEngine.Object;

namespace ValheimVehicles.Prefabs.Registry;

public class GreydwarfRowingSeatPrefab : RegisterPrefab<GreydwarfRowingSeatPrefab>
{
  public override void OnRegister()
  {
    var prefab = PrefabManager.Instance.CreateClonedPrefab(
      PrefabNames.GreydwarfRowingSeat,
      "piece_bench01"
    );

    if (prefab == null)
    {
      ZLog.LogError("ValheimRAFT: Failed to clone piece_bench01 for GreydwarfRowingSeat");
      return;
    }

    var piece = prefab.GetComponent<Piece>();
    if (piece == null) piece = prefab.AddComponent<Piece>();
    piece.m_name = "$valheim_vehicles_greydwarf_rowing_seat";
    piece.m_description = "$valheim_vehicles_greydwarf_rowing_seat_desc";
    piece.m_clipEverything = true;
    piece.m_clipGround = true;
    piece.m_noClipping = false;
    piece.m_spaceRequirement = 0f;
    piece.m_groundOnly = false;
    piece.m_groundPiece = false;
    piece.m_allowedInDungeons = false;
    piece.m_canRotate = true;
    if (LoadValheimAssets.woodFloorPiece != null)
    {
      piece.m_placeEffect = LoadValheimAssets.woodFloorPiece.m_placeEffect;
    }

    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);

    // Disable outer 2 seats, keep only the middle/center seat
    var chairs = prefab.GetComponentsInChildren<Chair>(true);
    Chair? centerChair = null;
    float closestDist = float.MaxValue;

    foreach (var ch in chairs)
    {
      if (ch.m_attachPoint != null)
      {
        float dist = Mathf.Abs(ch.m_attachPoint.localPosition.x);
        if (dist < closestDist)
        {
          closestDist = dist;
          centerChair = ch;
        }
      }
    }

    foreach (var ch in chairs)
    {
      if (ch != centerChair)
      {
        ch.gameObject.SetActive(false);
        Object.Destroy(ch);
      }
      else
      {
        ch.m_inShip = true;
      }
    }

    var seatComp = prefab.AddComponent<GreydwarfRowingSeatComponent>();
    seatComp.m_chair = centerChair;

    PrefabRegistryHelpers.SetWearNTear(prefab);
    PrefabRegistryHelpers.FixCollisionLayers(prefab);

    PrefabRegistryController.AddPiece(new CustomPiece(prefab, false, new PieceConfig
    {
      PieceTable = PrefabRegistryController.GetPieceTableName(),
      Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Misc),
      Enabled = true,
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 6,
          Item = "Wood",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 2,
          Item = "Resin",
          Recover = true
        }
      ]
    }));
  }
}
