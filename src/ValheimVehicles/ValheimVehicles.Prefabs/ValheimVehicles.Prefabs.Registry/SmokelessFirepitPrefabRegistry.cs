using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Prefabs.Registry;
using ValheimVehicles.SharedScripts;
using Zolantris.Shared;

namespace ValheimVehicles.ValheimVehicles.Prefabs.ValheimVehicles.Prefabs.Registry;

public class SmokelessFirepitPrefabRegistry : RegisterPrefab<SmokelessFirepitPrefabRegistry>
{
  public const string PieceName = "$valheim_vehicles_smokeless_firepit";
  public const string PieceDescription = "$valheim_vehicles_smokeless_firepit_desc";

  public override void OnRegister()
  {
    LocalizationManager.Instance.AddToken(
      "valheim_vehicles_smokeless_firepit",
      "Smokeless Campfire",
      false
    );
    LocalizationManager.Instance.AddToken(
      "valheim_vehicles_smokeless_firepit_desc",
      "A cozy, smokeless hearth engineered for ocean voyages. Provides steady warmth and comfort without choking the deck or blinding the helmsman with billowing soot. <color=yellow>All the warmth and none of the coughing fits while steering into the wind. The gods must've swallowed the soot themselves!</color>",
      false
    );

    RegisterSmokelessFirepit();
  }

  private static void RegisterSmokelessFirepit()
  {
    var prefab = PrefabManager.Instance.CreateClonedPrefab(
      PrefabNames.SmokelessFirepit,
      "fire_pit"
    );

    if (prefab == null)
    {
      LoggerProvider.LogError($"[SmokelessFirepitPrefabRegistry] Failed to clone vanilla 'fire_pit' prefab.");
      return;
    }

    var piece = prefab.GetComponent<Piece>();
    if (piece == null)
    {
      piece = prefab.AddComponent<Piece>();
    }
    piece.m_name = PieceName;
    piece.m_description = PieceDescription;

    var fireplace = prefab.GetComponent<Fireplace>();
    if (fireplace != null)
    {
      fireplace.m_name = PieceName;
      if (fireplace.m_smokeSpawner != null)
      {
        var spawnerGo = fireplace.m_smokeSpawner.gameObject;
        fireplace.m_smokeSpawner = null;
        if (spawnerGo != null && spawnerGo != prefab)
        {
          spawnerGo.SetActive(false);
          Object.DestroyImmediate(spawnerGo);
        }
      }
    }

    var smokeSpawners = prefab.GetComponentsInChildren<SmokeSpawner>(true);
    foreach (var spawner in smokeSpawners)
    {
      if (spawner != null)
      {
        var go = spawner.gameObject;
        if (go != null && go != prefab)
        {
          go.SetActive(false);
          Object.DestroyImmediate(go);
        }
        else
        {
          Object.DestroyImmediate(spawner);
        }
      }
    }

    var wnt = prefab.GetComponent<WearNTear>();
    if (wnt != null)
    {
      wnt.m_noRoofWear = true;
      wnt.m_supports = true;
    }

    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);

    PrefabRegistryController.AddPiece(new CustomPiece(prefab, false,
      new PieceConfig
      {
        PieceTable = PrefabRegistryController.GetPieceTableName(),
        Name = PieceName,
        Description = PieceDescription,
        Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Misc),
        Enabled = true,
        Icon = piece.m_icon,
        Requirements = PrefabRecipeConfig.GetRequirements(PrefabNames.SmokelessFirepit)
      }));
  }
}
