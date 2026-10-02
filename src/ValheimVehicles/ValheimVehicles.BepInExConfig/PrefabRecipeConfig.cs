using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using Jotunn.Configs;
using UnityEngine;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.Enums;
using ValheimVehicles.SharedScripts.Modules;
using Zolantris.Shared;

namespace ValheimVehicles.BepInExConfig;

public class PrefabRecipeConfig : BepInExBaseConfig<PrefabRecipeConfig>
{
  private const string BaseSectionName = "RecipeConfig";
  private const string SectionNameHullMaterial = "RecipeConfig: HullMaterial";
  public static ConfigEntry<float> HullMaterialIronRatio = null!;
  public static ConfigEntry<float> HullMaterialBronzeRatio = null!;
  public static ConfigEntry<float> HullMaterialWoodRatio = null!;
  public static ConfigEntry<float> HullMaterialYggdrasilWoodRatio = null!;
  public static ConfigEntry<float> HullMaterialNailsRatio = null!;

  public static readonly Dictionary<string, RequirementConfig[]> DefaultRequirements = new(System.StringComparer.OrdinalIgnoreCase)
  {
    {
      PrefabNames.CannonballExplosive, new[]
      {
        new RequirementConfig { Item = "BlackMetal", Amount = 1, Recover = true },
        new RequirementConfig { Item = "Coal", Amount = 1, Recover = true }
      }
    },
    {
      PrefabNames.CannonballSolid, new[]
      {
        new RequirementConfig { Item = "Bronze", Amount = 1, Recover = true }
      }
    },
    {
      PrefabNames.CannonFixedTier1, [
        new RequirementConfig
        {
          Amount = 12,
          Item = "Wood",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 12,
          Item = "Bronze",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 1,
          Item = "SurtlingCore",
          Recover = true
        }
      ]
    },
    {
      PrefabNames.CannonTurretTier1, [
        new RequirementConfig
        {
          Amount = 6,
          Item = "Wood",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 1,
          Item = "Chain",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 8,
          Item = "Iron",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 2,
          Item = "SurtlingCore",
          Recover = true
        }
      ]
    },
    {
      PrefabNames.CannonControlCenter, [
        new RequirementConfig
        {
          Amount = 6,
          Item = "FineWood",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 4,
          Item = "BronzeNails",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 1,
          Item = "SurtlingCore",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 2,
          Item = "GreydwarfEye",
          Recover = true
        }
      ]
    },
    {
      PrefabNames.CannonHandHeldItem, [
        new RequirementConfig
        {
          Amount = 4,
          Item = "Bronze",
          Recover = true,
          AmountPerLevel = 1
        },
        new RequirementConfig
        {
          Amount = 1,
          Item = "Chain",
          Recover = true
        }
      ]
    },
    {
      PrefabNames.PowderBarrel, [
        new RequirementConfig { Item = "Wood", Amount = 20, Recover = true },
        new RequirementConfig { Item = "Bronze", Amount = 2, Recover = true },
        new RequirementConfig { Item = "Coal", Amount = 20, Recover = true }
      ]
    },
    // Keel Extension (Wood / Nailed)
    {
      PrefabNames.ShipHullCenterWoodPrefabName, [
        new RequirementConfig { Item = "Wood", Amount = 20, Recover = true },
        new RequirementConfig { Item = "RoundLog", Amount = 10, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 8, Recover = true }
      ]
    },
    {
      PrefabNames.ShipKeel, [
        new RequirementConfig { Item = "Wood", Amount = 20, Recover = true },
        new RequirementConfig { Item = "RoundLog", Amount = 10, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 8, Recover = true }
      ]
    },
    // Iron-Reinforced Keel Extension
    {
      PrefabNames.ShipHullCenterIronPrefabName, [
        new RequirementConfig { Item = "RoundLog", Amount = 20, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 4, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 8, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    // Iron-Plated Deck Planking & Frame Walls
    {
      PrefabNames.GetHullSlabName(HullMaterial.Iron, PrefabNames.PrefabSizeVariant.TwoByTwo), [
        new RequirementConfig { Item = "RoundLog", Amount = 6, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 2, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 2, Recover = true }
      ]
    },
    {
      "ValheimVehicles_hull_seal_tri_bow_left_iron_reinforced", [
        new RequirementConfig { Item = "RoundLog", Amount = 8, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 2, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 2, Recover = true }
      ]
    },
    {
      "ValheimVehicles_hull_seal_tri_bow_right_iron_reinforced", [
        new RequirementConfig { Item = "RoundLog", Amount = 8, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 2, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 2, Recover = true }
      ]
    },
    {
      "ValheimVehicles_hull_floor_2x2_iron", [
        new RequirementConfig { Item = "RoundLog", Amount = 6, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 2, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 2, Recover = true }
      ]
    },
    {
      "hull_floor_2x2_iron", [
        new RequirementConfig { Item = "RoundLog", Amount = 6, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 2, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 2, Recover = true }
      ]
    },
    {
      PrefabNames.GetHullSlabName(HullMaterial.Iron, PrefabNames.PrefabSizeVariant.FourByFour), [
        new RequirementConfig { Item = "RoundLog", Amount = 24, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 4, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    {
      PrefabNames.GetHullWallName(HullMaterial.Iron, PrefabNames.PrefabSizeVariant.TwoByTwo), [
        new RequirementConfig { Item = "Wood", Amount = 10, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 4, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    {
      PrefabNames.GetHullWallName(HullMaterial.Iron, PrefabNames.PrefabSizeVariant.FourByFour), [
        new RequirementConfig { Item = "Wood", Amount = 20, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 8, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 8, Recover = true }
      ]
    },
    // Iron-Plated Portholes
    {
      PrefabNames.WindowWallPorthole2x2Prefab, [
        new RequirementConfig { Item = "Iron", Amount = 1, Recover = true },
        new RequirementConfig { Item = "RoundLog", Amount = 4, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 2, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 2, Recover = true }
      ]
    },
    {
      PrefabNames.WindowWallPorthole4x4Prefab, [
        new RequirementConfig { Item = "Iron", Amount = 2, Recover = true },
        new RequirementConfig { Item = "RoundLog", Amount = 8, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 4, Recover = true }
      ]
    },
    {
      PrefabNames.WindowWallPorthole8x4Prefab, [
        new RequirementConfig { Item = "Iron", Amount = 8, Recover = true },
        new RequirementConfig { Item = "FineWood", Amount = 8, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 8, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 8, Recover = true }
      ]
    },
    {
      PrefabNames.WindowFloorPorthole4x4Prefab, [
        new RequirementConfig { Item = "Iron", Amount = 2, Recover = true },
        new RequirementConfig { Item = "RoundLog", Amount = 8, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 4, Recover = true }
      ]
    },
    // Nailed Portholes
    {
      "ValheimVehicles_ShipWindow_Wall_Porthole_Wood_2x2", [
        new RequirementConfig { Item = "Wood", Amount = 4, Recover = true },
        new RequirementConfig { Item = "Bronze", Amount = 1, Recover = true },
        new RequirementConfig { Item = "BronzeNails", Amount = 2, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 2, Recover = true }
      ]
    },
    {
      "ValheimVehicles_ShipWindow_Wall_Porthole_Wood_4x4", [
        new RequirementConfig { Item = "Wood", Amount = 8, Recover = true },
        new RequirementConfig { Item = "Bronze", Amount = 2, Recover = true },
        new RequirementConfig { Item = "BronzeNails", Amount = 4, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 4, Recover = true }
      ]
    },
    {
      "ValheimVehicles_ShipWindow_Wall_Porthole_Wood_8x4", [
        new RequirementConfig { Item = "Wood", Amount = 16, Recover = true },
        new RequirementConfig { Item = "Bronze", Amount = 4, Recover = true },
        new RequirementConfig { Item = "BronzeNails", Amount = 8, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 8, Recover = true }
      ]
    },
    {
      "ValheimVehicles_ShipWindow_Floor_Porthole_Wood_4x4", [
        new RequirementConfig { Item = "Wood", Amount = 8, Recover = true },
        new RequirementConfig { Item = "Bronze", Amount = 2, Recover = true },
        new RequirementConfig { Item = "BronzeNails", Amount = 4, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 4, Recover = true }
      ]
    },
    // Resined Hulls (Dandelion removed)
    {
      "ValheimVehicles_Ship_Hull_Prow_Wood_2x2x4", [
        new RequirementConfig { Item = "Wood", Amount = 24, Recover = true },
        new RequirementConfig { Item = "RoundLog", Amount = 4, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 4, Recover = true }
      ]
    },
    {
      "ValheimVehicles_Ship_Hull_Rib_Wood", [
        new RequirementConfig { Item = "Wood", Amount = 18, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 4, Recover = true }
      ]
    },
    {
      "ValheimVehicles_Ship_Hull_Rib_Corner_Wood", [
        new RequirementConfig { Item = "Wood", Amount = 12, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 4, Recover = true }
      ]
    },
    {
      "ValheimVehicles_Ship_Hull_Rib_Corner_2x2x4_Left_Wood", [
        new RequirementConfig { Item = "Wood", Amount = 12, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 4, Recover = true }
      ]
    },
    {
      "ValheimVehicles_Ship_Hull_Rib_Corner_2x2x4_Right_Wood", [
        new RequirementConfig { Item = "Wood", Amount = 12, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 4, Recover = true }
      ]
    },
    // Resined Decks (Wood only: Dandelion and Resin removed)
    {
      "ValheimVehicles_Hull_Slab_Wood_4x4", [
        new RequirementConfig { Item = "Wood", Amount = 10, Recover = true }
      ]
    },
    {
      "ValheimVehicles_Hull_Slab_Wood_2x2", [
        new RequirementConfig { Item = "Wood", Amount = 5, Recover = true }
      ]
    },
    {
      "ValheimVehicles_Ship_Hull_Rib_Corner_Floor_2x2_left_Wood", [
        new RequirementConfig { Item = "Wood", Amount = 10, Recover = true }
      ]
    },
    {
      "ValheimVehicles_Ship_Hull_Rib_Corner_Floor_2x2_right_Wood", [
        new RequirementConfig { Item = "Wood", Amount = 10, Recover = true }
      ]
    },
    {
      "ValheimVehicles_Ship_Hull_Rib_Corner_Floor_2x4_left_Wood", [
        new RequirementConfig { Item = "Wood", Amount = 10, Recover = true }
      ]
    },
    {
      "ValheimVehicles_Ship_Hull_Rib_Corner_Floor_2x4_right_Wood", [
        new RequirementConfig { Item = "Wood", Amount = 10, Recover = true }
      ]
    },
    {
      "hull_rib_corner_floor_wood_left_2x2", [
        new RequirementConfig { Item = "Wood", Amount = 10, Recover = true }
      ]
    },
    {
      "hull_rib_corner_floor_wood_right_2x2", [
        new RequirementConfig { Item = "Wood", Amount = 10, Recover = true }
      ]
    },
    {
      "hull_rib_corner_floor_wood_left_2x4", [
        new RequirementConfig { Item = "Wood", Amount = 10, Recover = true }
      ]
    },
    {
      "hull_rib_corner_floor_wood_right_2x4", [
        new RequirementConfig { Item = "Wood", Amount = 10, Recover = true }
      ]
    },

    // Nailed Hulls & Rails (v4)
    {
      "hull_bow_center_wood", [
        new RequirementConfig { Item = "Wood", Amount = 24, Recover = true },
        new RequirementConfig { Item = "RoundLog", Amount = 8, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 4, Recover = true },
        new RequirementConfig { Item = "BronzeNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_floor_keel_4x2_left_wood", [
        new RequirementConfig { Item = "Wood", Amount = 11, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 2, Recover = true },
        new RequirementConfig { Item = "BronzeNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_rib_wood", [
        new RequirementConfig { Item = "Wood", Amount = 24, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 4, Recover = true },
        new RequirementConfig { Item = "BronzeNails", Amount = 4, Recover = true }
      ]
    },
    {
      "ValheimVehicles_hull_rib_wood", [
        new RequirementConfig { Item = "Wood", Amount = 24, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 4, Recover = true },
        new RequirementConfig { Item = "BronzeNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_bow_tri_left_wood", [
        new RequirementConfig { Item = "Wood", Amount = 16, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 4, Recover = true },
        new RequirementConfig { Item = "BronzeNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_bow_tri_right_wood", [
        new RequirementConfig { Item = "Wood", Amount = 16, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 4, Recover = true },
        new RequirementConfig { Item = "BronzeNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_bow_curved_left_wood", [
        new RequirementConfig { Item = "Wood", Amount = 16, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 4, Recover = true },
        new RequirementConfig { Item = "BronzeNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_bow_curved_right_wood", [
        new RequirementConfig { Item = "Wood", Amount = 16, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 4, Recover = true },
        new RequirementConfig { Item = "BronzeNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_rib_aft_center_wood", [
        new RequirementConfig { Item = "Wood", Amount = 24, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 4, Recover = true },
        new RequirementConfig { Item = "BronzeNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_rib_aft_left_wood", [
        new RequirementConfig { Item = "Wood", Amount = 24, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 4, Recover = true },
        new RequirementConfig { Item = "BronzeNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_rib_aft_right_wood", [
        new RequirementConfig { Item = "Wood", Amount = 24, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 4, Recover = true },
        new RequirementConfig { Item = "BronzeNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_floor_4x4_wood", [
        new RequirementConfig { Item = "Wood", Amount = 12, Recover = true },
        new RequirementConfig { Item = "BronzeNails", Amount = 4, Recover = true }
      ]
    },
    {
      "ValheimVehicles_hull_floor_4x4_wood", [
        new RequirementConfig { Item = "Wood", Amount = 12, Recover = true },
        new RequirementConfig { Item = "BronzeNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_floor_2x2_wood", [
        new RequirementConfig { Item = "Wood", Amount = 6, Recover = true },
        new RequirementConfig { Item = "BronzeNails", Amount = 2, Recover = true }
      ]
    },
    {
      "ValheimVehicles_hull_floor_2x2_wood", [
        new RequirementConfig { Item = "Wood", Amount = 6, Recover = true },
        new RequirementConfig { Item = "BronzeNails", Amount = 2, Recover = true }
      ]
    },
    {
      "hull_seal_corner_left_wood", [
        new RequirementConfig { Item = "Wood", Amount = 12, Recover = true },
        new RequirementConfig { Item = "BronzeNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_seal_corner_right_wood", [
        new RequirementConfig { Item = "Wood", Amount = 12, Recover = true },
        new RequirementConfig { Item = "BronzeNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_seal_bow_left_wood", [
        new RequirementConfig { Item = "Wood", Amount = 12, Recover = true },
        new RequirementConfig { Item = "BronzeNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_seal_bow_right_wood", [
        new RequirementConfig { Item = "Wood", Amount = 12, Recover = true },
        new RequirementConfig { Item = "BronzeNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_seal_tri_bow_left_wood", [
        new RequirementConfig { Item = "Wood", Amount = 12, Recover = true },
        new RequirementConfig { Item = "BronzeNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_seal_tri_bow_right_wood", [
        new RequirementConfig { Item = "Wood", Amount = 12, Recover = true },
        new RequirementConfig { Item = "BronzeNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_rail_connector_wood", [
        new RequirementConfig { Item = "Wood", Amount = 8, Recover = true }
      ]
    },
    {
      "hull_rail_straight_wood", [
        new RequirementConfig { Item = "Wood", Amount = 24, Recover = true }
      ]
    },
    {
      "hull_rail_25deg_wood", [
        new RequirementConfig { Item = "Wood", Amount = 16, Recover = true }
      ]
    },
    {
      "hull_rail_45deg_wood", [
        new RequirementConfig { Item = "Wood", Amount = 16, Recover = true }
      ]
    },
    {
      "hull_rail_corner_wood", [
        new RequirementConfig { Item = "Wood", Amount = 24, Recover = true }
      ]
    },
    {
      "hull_rail_prow_corner_left_wood", [
        new RequirementConfig { Item = "Wood", Amount = 24, Recover = true }
      ]
    },
    {
      "hull_rail_prow_corner_right_wood", [
        new RequirementConfig { Item = "Wood", Amount = 24, Recover = true }
      ]
    },
    // Iron-Plated Hulls & Rails (v4)
    {
      "hull_floor_4x4_iron", [
        new RequirementConfig { Item = "RoundLog", Amount = 24, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 4, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_floor_keel_4x2_left_iron", [
        new RequirementConfig { Item = "RoundLog", Amount = 11, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 2, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 2, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_rib_iron", [
        new RequirementConfig { Item = "RoundLog", Amount = 24, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 4, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 4, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    {
      "ValheimVehicles_hull_rib_iron", [
        new RequirementConfig { Item = "RoundLog", Amount = 24, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 4, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 4, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_bow_center_iron", [
        new RequirementConfig { Item = "RoundLog", Amount = 24, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 8, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    {
      "ValheimVehicles_hull_bow_center_iron", [
        new RequirementConfig { Item = "RoundLog", Amount = 24, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 8, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_bow_tri_left_iron", [
        new RequirementConfig { Item = "RoundLog", Amount = 16, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 4, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 4, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_bow_tri_right_iron", [
        new RequirementConfig { Item = "RoundLog", Amount = 16, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 4, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 4, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_bow_curved_left_iron", [
        new RequirementConfig { Item = "RoundLog", Amount = 16, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 4, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 4, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_bow_curved_right_iron", [
        new RequirementConfig { Item = "RoundLog", Amount = 16, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 4, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 4, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_rib_aft_center_iron", [
        new RequirementConfig { Item = "RoundLog", Amount = 16, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 4, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 4, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    {
      "ValheimVehicles_hull_rib_aft_center_iron", [
        new RequirementConfig { Item = "RoundLog", Amount = 16, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 4, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 4, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_rib_aft_left_iron", [
        new RequirementConfig { Item = "RoundLog", Amount = 24, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 4, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 4, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_rib_aft_right_iron", [
        new RequirementConfig { Item = "RoundLog", Amount = 24, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 4, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 4, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_seal_bow_left_iron", [
        new RequirementConfig { Item = "RoundLog", Amount = 6, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 2, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_seal_bow_right_iron", [
        new RequirementConfig { Item = "RoundLog", Amount = 6, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 2, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_seal_corner_left_iron", [
        new RequirementConfig { Item = "RoundLog", Amount = 6, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 2, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_seal_corner_right_iron", [
        new RequirementConfig { Item = "RoundLog", Amount = 6, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 2, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_seal_tri_bow_left_iron", [
        new RequirementConfig { Item = "RoundLog", Amount = 6, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 2, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_seal_tri_bow_right_iron", [
        new RequirementConfig { Item = "RoundLog", Amount = 6, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 2, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_rail_connector_iron", [
        new RequirementConfig { Item = "Iron", Amount = 2, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 2, Recover = true }
      ]
    },
    {
      "hull_rail_straight_iron", [
        new RequirementConfig { Item = "Iron", Amount = 6, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 6, Recover = true }
      ]
    },
    {
      "hull_rail_25deg_iron", [
        new RequirementConfig { Item = "Iron", Amount = 4, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_rail_45deg_iron", [
        new RequirementConfig { Item = "Iron", Amount = 4, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_rail_corner_iron", [
        new RequirementConfig { Item = "Iron", Amount = 6, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 6, Recover = true }
      ]
    },
    {
      "hull_rail_prow_corner_left_iron", [
        new RequirementConfig { Item = "Iron", Amount = 6, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 6, Recover = true }
      ]
    },
    {
      "hull_rail_prow_corner_right_iron", [
        new RequirementConfig { Item = "Iron", Amount = 6, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 6, Recover = true }
      ]
    },
    // Solid Iron Hulls
    {
      "ValheimVehicles_Ship_Hull_Rib_2x1x8_Iron", [
        new RequirementConfig { Item = "Iron", Amount = 12, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 4, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 8, Recover = true }
      ]
    },
    {
      "ValheimVehicles_Ship_Hull_Rib_Corner_Iron", [
        new RequirementConfig { Item = "Iron", Amount = 6, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 4, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    {
      "ValheimVehicles_Ship_Hull_Rib_Corner_2x2x4_Left_Iron", [
        new RequirementConfig { Item = "Iron", Amount = 8, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 4, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    {
      "ValheimVehicles_Ship_Hull_Rib_Corner_2x2x4_Right_Iron", [
        new RequirementConfig { Item = "Iron", Amount = 8, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 4, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    {
      "ValheimVehicles_Ship_Hull_Rib_Corner_2x1x8_Left_Iron", [
        new RequirementConfig { Item = "Iron", Amount = 10, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 4, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 6, Recover = true }
      ]
    },
    {
      "ValheimVehicles_Ship_Hull_Rib_Corner_2x1x8_Right_Iron", [
        new RequirementConfig { Item = "Iron", Amount = 10, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 4, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 6, Recover = true }
      ]
    },
    // Iron-Reinforced Deck Planking (2x2)
    {
      "hull_slab_iron_2x2", [
        new RequirementConfig { Item = "RoundLog", Amount = 6, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 2, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 2, Recover = true }
      ]
    },
    // Iron-Reinforced Corner Floors
    {
      "ValheimVehicles_Ship_Hull_Rib_Corner_Floor_2x2_left_Iron", [
        new RequirementConfig { Item = "RoundLog", Amount = 6, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 2, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 2, Recover = true }
      ]
    },
    {
      "ValheimVehicles_Ship_Hull_Rib_Corner_Floor_2x2_right_Iron", [
        new RequirementConfig { Item = "RoundLog", Amount = 6, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 2, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 2, Recover = true }
      ]
    },
    {
      "ValheimVehicles_Ship_Hull_Rib_Corner_Floor_2x4_left_Iron", [
        new RequirementConfig { Item = "RoundLog", Amount = 8, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 4, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    {
      "ValheimVehicles_Ship_Hull_Rib_Corner_Floor_2x4_right_Iron", [
        new RequirementConfig { Item = "RoundLog", Amount = 8, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 4, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    {
      "ValheimVehicles_Ship_Hull_Rib_Corner_Floor_2x8_left_Iron", [
        new RequirementConfig { Item = "RoundLog", Amount = 12, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 4, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    {
      "ValheimVehicles_Ship_Hull_Rib_Corner_Floor_2x8_right_Iron", [
        new RequirementConfig { Item = "RoundLog", Amount = 12, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 4, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    {
      "ValheimVehicles_Ship_Hull_Rib_Corner_Floor_2x8_Left_Iron", [
        new RequirementConfig { Item = "RoundLog", Amount = 12, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 4, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    {
      "ValheimVehicles_Ship_Hull_Rib_Corner_Floor_2x8_Right_Iron", [
        new RequirementConfig { Item = "RoundLog", Amount = 12, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 4, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_rib_corner_floor_iron_left_2x2", [
        new RequirementConfig { Item = "RoundLog", Amount = 6, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 2, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 2, Recover = true }
      ]
    },
    {
      "hull_rib_corner_floor_iron_right_2x2", [
        new RequirementConfig { Item = "RoundLog", Amount = 6, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 2, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 2, Recover = true }
      ]
    },
    {
      "hull_rib_corner_floor_iron_left_2x4", [
        new RequirementConfig { Item = "RoundLog", Amount = 8, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 4, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_rib_corner_floor_iron_right_2x4", [
        new RequirementConfig { Item = "RoundLog", Amount = 8, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 4, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_rib_corner_floor_iron_left_2x8", [
        new RequirementConfig { Item = "RoundLog", Amount = 12, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 6, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_rib_corner_floor_iron_right_2x8", [
        new RequirementConfig { Item = "RoundLog", Amount = 12, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 6, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    // Iron-Reinforced Hull Set
    {
      "ValheimVehicles_hull_bow_center_iron_reinforced", [
        new RequirementConfig { Item = "Wood", Amount = 10, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 2, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 2, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 2, Recover = true }
      ]
    },
    {
      "ValheimVehicles_hull_rib_iron_reinforced", [
        new RequirementConfig { Item = "Wood", Amount = 10, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 2, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 2, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 2, Recover = true }
      ]
    },
    {
      "ValheimVehicles_hull_bow_tri_left_iron_reinforced", [
        new RequirementConfig { Item = "Wood", Amount = 10, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 2, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 2, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 2, Recover = true }
      ]
    },
    {
      "ValheimVehicles_hull_bow_tri_right_iron_reinforced", [
        new RequirementConfig { Item = "Wood", Amount = 10, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 2, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 2, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 2, Recover = true }
      ]
    },
    {
      "ValheimVehicles_hull_bow_curved_left_iron_reinforced", [
        new RequirementConfig { Item = "Wood", Amount = 10, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 2, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 2, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 2, Recover = true }
      ]
    },
    {
      "ValheimVehicles_hull_bow_curved_right_iron_reinforced", [
        new RequirementConfig { Item = "Wood", Amount = 10, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 2, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 2, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 2, Recover = true }
      ]
    },
    {
      "ValheimVehicles_hull_rib_aft_center_iron_reinforced", [
        new RequirementConfig { Item = "Wood", Amount = 10, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 2, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 2, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 2, Recover = true }
      ]
    },
    {
      "ValheimVehicles_hull_rib_aft_left_iron_reinforced", [
        new RequirementConfig { Item = "Wood", Amount = 10, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 2, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 2, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 2, Recover = true }
      ]
    },
    {
      "ValheimVehicles_hull_rib_aft_right_iron_reinforced", [
        new RequirementConfig { Item = "Wood", Amount = 10, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 2, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 2, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 2, Recover = true }
      ]
    },

    // Sails
    {
      "MBRaftMast", [
        new RequirementConfig { Item = "Wood", Amount = 20, Recover = true },
        new RequirementConfig { Item = "DeerHide", Amount = 6, Recover = true },
        new RequirementConfig { Item = "Dandelion", Amount = 12, Recover = true }
      ]
    },
    {
      "mb_raft_mast", [
        new RequirementConfig { Item = "Wood", Amount = 20, Recover = true },
        new RequirementConfig { Item = "DeerHide", Amount = 6, Recover = true },
        new RequirementConfig { Item = "Dandelion", Amount = 12, Recover = true }
      ]
    },
    {
      "MBKarveMast", [
        new RequirementConfig { Item = "Wood", Amount = 20, Recover = true },
        new RequirementConfig { Item = "RoundLog", Amount = 4, Recover = true },
        new RequirementConfig { Item = "TrollHide", Amount = 6, Recover = true },
        new RequirementConfig { Item = "Dandelion", Amount = 12, Recover = true }
      ]
    },
    {
      "mb_karve_mast", [
        new RequirementConfig { Item = "Wood", Amount = 20, Recover = true },
        new RequirementConfig { Item = "RoundLog", Amount = 4, Recover = true },
        new RequirementConfig { Item = "TrollHide", Amount = 6, Recover = true },
        new RequirementConfig { Item = "Dandelion", Amount = 12, Recover = true }
      ]
    },
    {
      "MBVikingShipMast", [
        new RequirementConfig { Item = "RoundLog", Amount = 10, Recover = true },
        new RequirementConfig { Item = "WolfPelt", Amount = 6, Recover = true }
      ]
    },
    {
      "mb_vikingship_mast", [
        new RequirementConfig { Item = "RoundLog", Amount = 10, Recover = true },
        new RequirementConfig { Item = "WolfPelt", Amount = 6, Recover = true }
      ]
    },
    {
      "ValheimVehicles_DrakkalMast", [
        new RequirementConfig { Item = "ElderBark", Amount = 10, Recover = true },
        new RequirementConfig { Item = "LinenThread", Amount = 20, Recover = true }
      ]
    },
    {
      "valheim_vehicles_drakkalship_mast", [
        new RequirementConfig { Item = "ElderBark", Amount = 10, Recover = true },
        new RequirementConfig { Item = "LinenThread", Amount = 20, Recover = true }
      ]
    },
    // Iron-Reinforced Frame Wall 4x4
    {
      "ValheimVehicles_Hull_Wall_Iron_4x4", [
        new RequirementConfig { Item = "Wood", Amount = 20, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 4, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_wall_iron_4x4", [
        new RequirementConfig { Item = "Wood", Amount = 20, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 4, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    // Iron-Reinforced Deck Planking 2x2
    {
      "ValheimVehicles_Hull_Slab_Iron_2x2", [
        new RequirementConfig { Item = "RoundLog", Amount = 5, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 1, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    {
      "hull_slab_iron_2x2", [
        new RequirementConfig { Item = "RoundLog", Amount = 5, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 1, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 4, Recover = true }
      ]
    },
    // Iron-Reinforced Deck Planking 4x4
    {
      "ValheimVehicles_Hull_Slab_Iron_4x4", [
        new RequirementConfig { Item = "RoundLog", Amount = 10, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 2, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 6, Recover = true }
      ]
    },
    {
      "hull_slab_iron_4x4", [
        new RequirementConfig { Item = "RoundLog", Amount = 10, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 2, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 6, Recover = true }
      ]
    },
    // Iron-Reinforced Deck Stern Planking 2x2 Left / Right (both cases)
    {
      "ValheimVehicles_Ship_Hull_Rib_Corner_Floor_2x2_Left_Iron", [
        new RequirementConfig { Item = "RoundLog", Amount = 6, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 2, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 2, Recover = true }
      ]
    },
    {
      "ValheimVehicles_Ship_Hull_Rib_Corner_Floor_2x2_Right_Iron", [
        new RequirementConfig { Item = "RoundLog", Amount = 6, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 2, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 2, Recover = true }
      ]
    },
    // Rigging Cleat
    {
      "MBRopeAnchor", [
        new RequirementConfig { Item = "Iron", Amount = 1, Recover = true },
        new RequirementConfig { Item = "BronzeNails", Amount = 4, Recover = true },
        new RequirementConfig { Item = "Dandelion", Amount = 2, Recover = true }
      ]
    },
    {
      "mb_rope_anchor", [
        new RequirementConfig { Item = "Iron", Amount = 1, Recover = true },
        new RequirementConfig { Item = "BronzeNails", Amount = 4, Recover = true },
        new RequirementConfig { Item = "Dandelion", Amount = 2, Recover = true }
      ]
    },
    // Ship Anchor
    {
      "ValheimVehicles_ShipAnchor_Wood", [
        new RequirementConfig { Item = "RoundLog", Amount = 10, Recover = true },
        new RequirementConfig { Item = "Stone", Amount = 20, Recover = true },
        new RequirementConfig { Item = "Dandelion", Amount = 20, Recover = true }
      ]
    },
    // Boarding Ramps
    {
      "BoardingRamp", [
        new RequirementConfig { Item = "Wood", Amount = 20, Recover = true },
        new RequirementConfig { Item = "BronzeNails", Amount = 4, Recover = true },
        new RequirementConfig { Item = "Dandelion", Amount = 10, Recover = true }
      ]
    },
    {
      "mb_boarding_ramp", [
        new RequirementConfig { Item = "Wood", Amount = 20, Recover = true },
        new RequirementConfig { Item = "BronzeNails", Amount = 4, Recover = true },
        new RequirementConfig { Item = "Dandelion", Amount = 10, Recover = true }
      ]
    },
    {
      "BoardingRampWide", [
        new RequirementConfig { Item = "Wood", Amount = 40, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 8, Recover = true },
        new RequirementConfig { Item = "Dandelion", Amount = 20, Recover = true }
      ]
    },
    {
      "mb_boarding_ramp_wide", [
        new RequirementConfig { Item = "Wood", Amount = 40, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 8, Recover = true },
        new RequirementConfig { Item = "Dandelion", Amount = 20, Recover = true }
      ]
    },
    // Ship Planters
    {
      "MBDirtFloor_1x1", [
        new RequirementConfig { Item = "Wood", Amount = 10, Recover = true },
        new RequirementConfig { Item = "Coal", Amount = 5, Recover = true },
        new RequirementConfig { Item = "BoneFragments", Amount = 2, Recover = true }
      ]
    },
    {
      "MBDirtFloor_2x2", [
        new RequirementConfig { Item = "Wood", Amount = 20, Recover = true },
        new RequirementConfig { Item = "Coal", Amount = 10, Recover = true },
        new RequirementConfig { Item = "BoneFragments", Amount = 4, Recover = true }
      ]
    },
    // Pier Support
    {
      "MBPier_Pole", [
        new RequirementConfig { Item = "RoundLog", Amount = 4, Recover = true }
      ]
    },
    // Iron-Plated Keel Extension
    {
      "ValheimVehicles_Ship_Hull_Iron_Plated", [
        new RequirementConfig { Item = "RoundLog", Amount = 10, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 8, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 8, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 8, Recover = true }
      ]
    },
    {
      "hull_center_iron_plated", [
        new RequirementConfig { Item = "RoundLog", Amount = 10, Recover = true },
        new RequirementConfig { Item = "Iron", Amount = 8, Recover = true },
        new RequirementConfig { Item = "Resin", Amount = 8, Recover = true },
        new RequirementConfig { Item = "IronNails", Amount = 8, Recover = true }
      ]
    },
    // hull materials
    {
      GetHullMaterialRecipe(HullMaterial.Iron), [
        new RequirementConfig
        {
          Amount = 1,
          Item = "Iron",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 1,
          Item = "Resin",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 2, Item = "IronNails", Recover = true
        },
        new RequirementConfig
          { Amount = 1, Item = "RoundLog", Recover = true }
      ]
    },
    {
      GetHullMaterialRecipe(HullMaterial.Wood), [
        new RequirementConfig
          { Amount = 2, Item = "Wood", Recover = true }
      ]
    }
    // end <hull-materials>
  };

  public static string GetHullMaterialRecipe(string hullMaterialVariant)
  {
    return $"hull_base_recipe_{hullMaterialVariant}";
  }

  public static int GetHullMaterialAmountByItem(string ItemId, int materialCount)
  {
    return ItemId switch
    {
      "Iron" => Mathf.RoundToInt(Mathf.Clamp(materialCount * HullMaterialIronRatio.Value, 0, 100)),
      "Bronze" => Mathf.RoundToInt(Mathf.Clamp(materialCount * HullMaterialBronzeRatio.Value, 0, 100)),
      "BronzeNails" => Mathf.RoundToInt(Mathf.Clamp(materialCount * HullMaterialNailsRatio.Value, 0, 100)),
      "Wood" => Mathf.RoundToInt(Mathf.Clamp(materialCount * HullMaterialWoodRatio.Value, 0, 100)),
      _ => 1
    };
  }

  public static RequirementConfig[] GetHullMaterialRecipeConfig(string hullMaterialVariant, int materialCount)
  {
    var materialRecipeName = GetHullMaterialRecipe(hullMaterialVariant);
    var baseRequirements = GetRequirements(materialRecipeName);
    if (baseRequirements.Length == 0)
    {
      LoggerProvider.LogError($"No base requirements set for hull material {materialRecipeName} of variant <{hullMaterialVariant}>");
      return baseRequirements;
    }

    return baseRequirements.Select(r => new RequirementConfig
    {
      Item = r.Item,
      Amount = GetHullMaterialAmountByItem(r.Item, materialCount),
      Recover = r.Recover,
      AmountPerLevel = r.AmountPerLevel
    }).ToArray();
  }

  // Map: prefabName => config entry
  public static Dictionary<string, ConfigEntry<string>> RecipeRequirementConfigs { get; } = new(System.StringComparer.OrdinalIgnoreCase);
  private const string ratioDescription = "For configuring hull size ratio. EG materialValue 2x2=4 but ratio 1/4 would get 1 of <itemName>. (rounds to lowets 0 or nearest int). This is meant for the default recipe. Customize the base recipe if you want to override things.";
  private const string ratioDescriptionShort = "For configuring hull size ratio";

  public void AddHullRecipeModifierConfig(ConfigFile config)
  {

    HullMaterialIronRatio = config.Bind(
      SectionNameHullMaterial,
      "IronRatio",
      0.25f,
      ratioDescription
    );
    HullMaterialBronzeRatio = config.Bind(
      SectionNameHullMaterial,
      "BronzeRatio",
      0.25f,
      ratioDescriptionShort
    );
    HullMaterialWoodRatio = config.Bind(
      SectionNameHullMaterial,
      "WoodRatio",
      2f,
      ratioDescriptionShort
    );
    HullMaterialYggdrasilWoodRatio = config.Bind(
      SectionNameHullMaterial,
      "YggdrasilWoodRatio",
      1f,
      ratioDescriptionShort
    );

    HullMaterialNailsRatio = config.Bind(
      SectionNameHullMaterial,
      "NailsRatio",
      1f,
      ratioDescriptionShort
    );
  }

  public override void OnBindConfig(ConfigFile config)
  {
    AddHullRecipeModifierConfig(config);

    var isFirst = true;
    foreach (var kvp in DefaultRequirements)
    {
      var prefabName = kvp.Key;
      var defaultArray = kvp.Value;
      var defaultString = ToConfigString(defaultArray);

      var description = isFirst
        ? $"Recipe requirements for {prefabName}.\n" +
          "Format: ItemName,Amount[,Recover][,AmountPerLevel]|... (e.g., BlackPowder,2,true|Bronze,1,true)\n" +
          "Recover is optional (defaults true). AmountPerLevel is optional (defaults 0). Amount is clamped between 0 and 100. No decimals are allowed."
        : $"Recipe requirements for {prefabName}.";

      RecipeRequirementConfigs[prefabName] = config.Bind(
        BaseSectionName,
        prefabName,
        defaultString,
        description
      );
      isFirst = false;
    }
  }

  public static RequirementConfig[] GetRequirements(string prefabName)
  {
    if (RecipeRequirementConfigs.TryGetValue(prefabName, out var entry))
    {
      var val = entry.Value?.Trim();
      if (!string.IsNullOrEmpty(val))
      {
        var parsed = ParseRequirements(val);
        if (parsed.Length > 0)
          return parsed;
      }
    }

    var stripped = prefabName.StartsWith("ValheimVehicles_") ? prefabName.Substring("ValheimVehicles_".Length) : prefabName;
    if (RecipeRequirementConfigs.TryGetValue(stripped, out var entryStripped))
    {
      var val = entryStripped.Value?.Trim();
      if (!string.IsNullOrEmpty(val))
      {
        var parsed = ParseRequirements(val);
        if (parsed.Length > 0)
          return parsed;
      }
    }

    // Fallback to in-memory default array
    if (DefaultRequirements.TryGetValue(prefabName, out var def)) return def;
    if (DefaultRequirements.TryGetValue(stripped, out var defStripped)) return defStripped;
    return [];
  }

  // Parser supporting: Item,Amount[,Recover][,AmountPerLevel]
  public static RequirementConfig?[] ParseRequirements(string configValue)
  {
    if (string.IsNullOrEmpty(configValue))
      return [];

    return configValue.Split('|')
      .Where(req =>
      {
        var parts = req.Split(',');
        if (parts.Length < 2)
        {
          LoggerProvider.LogWarning($"Invalid requirement entry: \"{req}\". Format should be Item,Amount[,Recover][,AmountPerLevel]");
          return false;
        }
        return true;
      })
      .Select(req =>
      {
        var parts = req.Split(',');
        return new RequirementConfig
        {
          Item = parts[0].Trim(),
          Amount = MathX.Clamp(int.TryParse(parts[1], out var amt) ? amt : 1, 0, 100),
          Recover = parts.Length <= 2 || !bool.TryParse(parts[2], out var recover) || recover,
          AmountPerLevel = parts.Length > 3 && int.TryParse(parts[3], out var perLvl) ? perLvl : 0
        };
      })
      .ToArray();
  }

  // Serializes RequirementConfig[] to a config string
  private static string ToConfigString(RequirementConfig[] reqs)
  {
    return string.Join("|", reqs.Select(r =>
      $"{r.Item},{r.Amount},{r.Recover.ToString().ToLower()}{(r.AmountPerLevel > 0 ? $",{r.AmountPerLevel}" : "")}"));
  }
}