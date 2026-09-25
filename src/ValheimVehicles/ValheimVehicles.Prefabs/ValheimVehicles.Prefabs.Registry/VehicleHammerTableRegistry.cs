// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.Integrations;
using Zolantris.Shared;

namespace ValheimVehicles.Prefabs.Registry
{
  public class VehicleHammerTableRegistry : RegisterPrefab<VehicleHammerTableRegistry>
  {
    public static CustomPieceTable? VehicleHammerTable { get; private set; }

    public const string VehicleHammerTableName = "ValheimVehicles_HammerTable";

    /// <summary>
    /// Canonical order from config, converted to localized labels (same index order).
    /// </summary>
    private static string[] BuildLocalizedLabelsFromCanon()
    {
      var canon = PrefabConfig.GetVehicleHammerCategoryOrder();
      // Map canonicals -> localized labels in the SAME order
      return VehicleHammerTableCategories.ToLocalizedLabels(canon).ToArray();
    }

    /// <summary>
    /// Apply BOTH the canonical category list (drives grouping/index) and the localized labels (display) in lockstep.
    /// This prevents index/label drift that causes wrong items to appear under tabs.
    /// </summary>
    public static void RefreshCategoriesAndLabels()
    {
      var table = PieceManager.Instance.GetPieceTable(VehicleHammerTableName);
      if (!table) return;

      var canonicalOrder = PrefabConfig.GetVehicleHammerCategoryOrder().ToList(); // e.g., ["Tools","Hull",...]
      var localizedLabels = BuildLocalizedLabelsFromCanon().ToList(); // e.g., ["工具","船体",...]

      // 2) Set the underlying canonical categories list (index driver)
      //    Different Valheim/JVL versions used different field names; support both.
      if (!TrySetStringListField(table, "m_customCategories", canonicalOrder))
      {
        // Older/newer fallback name
        TrySetStringListField(table, "m_categories", canonicalOrder);
      }

      // 3) Set the UI labels for those categories in the SAME index order
      table.m_categoryLabels = localizedLabels;
    }

    /// <summary>
    /// Utility: tries to set a List&lt;string&gt; field on PieceTable via reflection if present.
    /// </summary>
    private static bool TrySetStringListField(PieceTable table, string fieldName, List<string> value)
    {
      var f = typeof(PieceTable).GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
      if (f == null) return false;

      // Some game builds store a List<string>, others an array of strings. Handle both.
      if (f.FieldType == typeof(List<string>))
      {
        f.SetValue(table, value);
        return true;
      }
      if (f.FieldType == typeof(string[]))
      {
        f.SetValue(table, value.ToArray());
        return true;
      }
      return false;
    }

    /// <summary>
    /// Register the custom piece table using CANONICAL categories.
    /// Labels are applied right after creation and kept in sync thereafter.
    /// </summary>
    private static void RegisterVehicleHammerTable()
    {
      // IMPORTANT: Use canonical (English) IDs as the CustomCategories (index driver)
      var canonical = PrefabConfig.GetVehicleHammerCategoryOrder().ToArray();

      var vehicleHammerTableConfig = new PieceTableConfig
      {
        CanRemovePieces = true,
        UseCategories = false,
        UseCustomCategories = true,
        CustomCategories = canonical // <-- canonical keys ONLY
      };

      VehicleHammerTable = new CustomPieceTable(VehicleHammerTableName, vehicleHammerTableConfig);

      // Keep labels & category indexes in sync on language or order changes
      Localization.OnLanguageChange += RefreshCategoriesAndLabels;
      PrefabConfig.VehicleHammerOrder.OnOrderChanged += _ => RefreshCategoriesAndLabels();
      PieceManager.OnPiecesRegistered += EnsureRepairPieceAdded;

      var success = PieceManager.Instance.AddPieceTable(VehicleHammerTable);

      // Apply localized labels now (after the table exists)
      RefreshCategoriesAndLabels();

      if (!success)
      {
        LoggerProvider.LogError(
          "VehicleHammerTable failed to be added. Falling back to original hammer table for all items. " +
          "This is a bug and could break your game. Please report this.");
        VehicleHammerTable = null;
      }
    }

    private static readonly string[] StartPieceOrder = new[]
    {
      "ValheimVehicles_WaterVehicleShip", // 1. Main Keel
      "ValheimVehicles_ShipKeel", // 112. Keel Extension 4x8
      "ValheimVehicles_ShipSteeringWheel", // 45. Ship's Helm
      "ValheimVehicles_ShipRudderBasic", // 47. Steering Oar
      "ValheimVehicles_ShipRudderAdvanced_Wood", // 51/52. Sternpost Rudder
      "MBRopeLadder", // 117. Jacob's Ladder
      "MBRaftMast", // 113. Square Rigged Sail / Raft Mast
      "MBVikingShipMast", // 114. Viking Sail
      "ValheimVehicles_DrakkalMast", // 115. Drakkar Sail
      "MBRopeAnchor", // 116. Rigging Cleat
      "ValheimVehicles_ShipAnchor_Wood", // 118. Ship Anchor
      "MBBoardingRamp", // 119. Boarding Ramp
      "MBBoardingRamp_Wide", // 120. Wide Boarding Ramp

      // Wood Hulls, Ribs, Prow, Decks, and Guard Rails
      "ValheimVehicles_Ship_Hull_Prow_Wood_2x2x4", // Resined Hull Cutwater - center
      "ValheimVehicles_Ship_Hull_Rib_Wood", // Resined Hull Midship Frame - Side
      "ValheimVehicles_Ship_Hull_Rib_Corner_Wood", // Resined Hull Stern - Corner
      "ValheimVehicles_Ship_Hull_Rib_Corner_2x2x4_Left_Wood", // Resined Hull Front Prow - Left
      "ValheimVehicles_Ship_Hull_Rib_Corner_2x2x4_Right_Wood", // Resined Hull Front Prow - Right
      "ValheimVehicles_Hull_Slab_Wood_4x4", // Resined Deck Planking
      "ValheimVehicles_Ship_Hull_Rib_Corner_Floor_2x2_Left_Wood", // Resined Deck Stern Planking - Left
      "ValheimVehicles_Ship_Hull_Rib_Corner_Floor_2x2_Right_Wood", // Resined Deck Stern Planking - Right
      "ValheimVehicles_Ship_Hull_Rib_Corner_Floor_2x4_Left_Wood", // Resined Deck Prow Planking - Left
      "ValheimVehicles_Ship_Hull_Rib_Corner_Floor_2x4_Right_Wood", // Resined Deck Prow Planking - Right
      "ValheimVehicles_hull_bow_center_wood", // Nailed Hull Cutwater - center
      "ValheimVehicles_hull_floor_keel_4x2_left_wood", // Nailed Garboard Strake
      "ValheimVehicles_hull_rib_wood", // Nailed Hull Midship Frame - Side
      "ValheimVehicles_hull_bow_tri_left_wood", // Nailed Hull Front Prow - left
      "ValheimVehicles_hull_bow_tri_right_wood", // Nailed Hull Front Prow - right
      "ValheimVehicles_hull_bow_curved_left_wood", // Nailed Hull Rear stern - left
      "ValheimVehicles_hull_bow_curved_right_wood", // Nailed Hull Rear stern - right
      "ValheimVehicles_hull_rib_aft_center_wood", // Nailed Hull Rear Counter Stern - center
      "ValheimVehicles_hull_rib_aft_left_wood", // Nailed Hull Rear Counter Stern - Left
      "ValheimVehicles_hull_rib_aft_right_wood", // Nailed Hull Rear Counter Stern - Right
      "ValheimVehicles_hull_floor_4x4_wood", // Nailed Deck Planking
      "ValheimVehicles_hull_seal_corner_left_wood", // Nailed Deck Counter Stern Planking - Left
      "ValheimVehicles_hull_seal_corner_right_wood", // Nailed Deck Counter Stern Planking - Right
      "ValheimVehicles_hull_seal_bow_left_wood", // Nailed Deck Stern Planking - Left
      "ValheimVehicles_hull_seal_bow_right_wood", // Nailed Deck Stern Planking - Right
      "ValheimVehicles_hull_seal_tri_bow_left_wood", // Nailed Deck Prow Planking - Left
      "ValheimVehicles_hull_seal_tri_bow_right_wood", // Nailed Deck Prow Planking - Right
      "ValheimVehicles_hull_rail_connector_wood", // Deck Guard-Rail - single
      "ValheimVehicles_hull_rail_straight_wood", // Deck Guard-Rail - triple
      "ValheimVehicles_hull_rail_25deg_wood", // Deck Guard-Rail - 25 deg
      "ValheimVehicles_hull_rail_45deg_wood", // Deck Guard-Rail - 45 deg
      "ValheimVehicles_hull_rail_corner_wood", // Deck Stern Guard-Rail - rounded
      "ValheimVehicles_hull_rail_prow_corner_left_wood", // Deck Prow Guard-Rail - Left
      "ValheimVehicles_hull_rail_prow_corner_right_wood" // Deck Prow Guard-Rail - Right
    };

    private static readonly string[] EndPieceOrder = new[]
    {
      "ValheimVehicles_Cannon_Fixed_Tier1", // 39. Deck Cannon - fixed
      "ValheimVehicles_Cannon_Turret_Tier1", // 40. Turret Cannon - Auto
      "ValheimVehicles_Powder_Barrel", // 42. Powder Barrel - Ammo Storage
      "ValheimVehicles_Cannon_Control_Center", // 43. Gunnery Binnacle - Cannon Control Center
      "MBDirtFloor_1x1", // 38. Ship Planter - Small
      "MBDirtFloor_2x2", // 39. Ship Planter - Large
      "MBPier_Stone", // 36. Stone Piling - Pier Support
      "MBPier_Pole", // 37. Timber Piling - Pier Support
      "ValheimVehicles_Power_Source_Eitr", // 48. Power Generator
      "ValheimVehicles_Power_Storage_Eitr", // 49. Battery / Power Storage
      "ValheimVehicles_Power_Conduit_Charge_Plate", // 50. Power Charge Plate
      "ValheimVehicles_Power_Conduit_Drain_Plate", // 50. Power Drain Plate
      "ValheimVehicles_Power_Pylon", // Power Pylon
      "ValheimVehicles_Mechanism_ToggleSwitch" // 46. Toggle Switch - Boat Settings
    };

    private static readonly HashSet<string> RedundantPieces = new(StringComparer.OrdinalIgnoreCase)
    {
      "hull_floor_keel_4x2_right_wood",
      "ValheimVehicles_hull_floor_keel_4x2_right_wood",
      "hull_slab_wood_2x2",
      "ValheimVehicles_Ship_Hull_Slab_Wood_2x2",
      "Ship_Hull_Slab_Wood_2x2",
      "valheim_vehicles_hull_slab_wood_2x2",
      "ValheimVehicles_Hull_Slab_Wood_2x2",

      // User requested removals:
      // 44. Land Vehicle
      "valheim_vehicles_land_vehicle",
      "ValheimVehicles_LandVehicle",
      "LandVehicle",

      // 56. Hull with Keel (Right) (Iron) 4x2
      "hull_floor_keel_4x2_right_iron",
      "ValheimVehicles_hull_floor_keel_4x2_right_iron",

      // 65. Hull-Rib Expander (Left) (Iron)
      "hull_rib_expander_left_iron",
      "ValheimVehicles_hull_rib_expander_left_iron",

      // 66. Hull-Rib Expander (Right) (Iron)
      "hull_rib_expander_right_iron",
      "ValheimVehicles_hull_rib_expander_right_iron",

      // 71. Hull-Expander Seal (Left) (Iron)
      "hull_seal_expander_left_iron",
      "ValheimVehicles_hull_seal_expander_left_iron",

      // 72. Hull-Expander Seal (Right) (Iron)
      "hull_seal_expander_right_iron",
      "ValheimVehicles_hull_seal_expander_right_iron",

      // 87. Porthole Window: Standalone
      "WindowPortholeStandalone",
      "WindowPortholeStandalonePrefab",
      "ValheimVehicles_Window_Porthole_Standalone",
      "valheim_vehicles_window_porthole_standalone",
      "window_porthole_standalone",

      // 93. Hull-Rib Side 2x1x2 (Iron)
      "hull_rib_iron_2x1x2",
      "hull_rib_2x1x2_iron",
      "ValheimVehicles_Ship_Hull_Rib_2x1x2_Iron",
      "Ship_Hull_Rib_2x1x2_Iron",

      // 107. Hull-Prow Seal (Iron)
      "hull_prow_seal_iron",
      "hull_rib_prow_seal_iron",
      "ValheimVehicles_Hull_Rib_Prow_Seal_iron",
      "Hull_Rib_Prow_Seal_iron",
      "ValheimVehicles_hull_prow_seal_iron"
    };

    private static string NormalizePieceName(string name)
    {
      if (string.IsNullOrEmpty(name)) return string.Empty;
      var clean = name.Replace("(Clone)", "").Trim();
      if (clean.StartsWith("ValheimVehicles_", StringComparison.OrdinalIgnoreCase))
        clean = clean.Substring("ValheimVehicles_".Length);
      else if (clean.StartsWith("valheim_vehicles_", StringComparison.OrdinalIgnoreCase))
        clean = clean.Substring("valheim_vehicles_".Length);
      return clean.Replace("_", "").ToLowerInvariant();
    }

    private static readonly Dictionary<string, string[]> PieceAliases = new(StringComparer.OrdinalIgnoreCase)
    {
      { "ValheimVehicles_ShipKeel", new[] { "valheimvehicles_ship_hull_wood", "shipkeel", "mbkeel" } },
      { "ValheimVehicles_ShipSteeringWheel", new[] { "mb_steering_wheel", "shipsteeringwheel", "steeringwheel" } },
      { "ValheimVehicles_ShipRudderBasic", new[] { "valheim_vehicles_rudder_basic", "shiprudderbasic", "rudderbasic" } },
      { "ValheimVehicles_ShipRudderAdvanced_Wood", new[] { "valheim_vehicles_rudder_advanced", "shiprudderadvancedwood", "shiprudderadvancedsinglewood" } },
      { "MBRopeLadder", new[] { "mb_rope_ladder", "ropeladder" } },
      { "MBRaftMast", new[] { "mb_raft_mast", "raftmast" } },
      { "MBVikingShipMast", new[] { "mb_vikingship_mast", "mb_viking_mast", "vikingshipmast" } },
      { "ValheimVehicles_DrakkalMast", new[] { "valheim_vehicles_drakkalship_mast", "valheim_vehicles_drakkal_mast", "drakkalmast", "drakkar_mast" } },
      { "MBRopeAnchor", new[] { "mb_rope_anchor", "ropeanchor" } },
      { "ValheimVehicles_ShipAnchor_Wood", new[] { "valheim_vehicles_ship_anchor", "shipanchorwood", "shipanchor" } },
      { "MBBoardingRamp", new[] { "mb_boarding_ramp", "boardingramp" } },
      { "MBBoardingRamp_Wide", new[] { "mb_boarding_ramp_wide", "boardingrampwide" } },
      { "ValheimVehicles_Cannon_Fixed_Tier1", new[] { "valheim_vehicles_cannon_fixed_tier1", "cannonfixedtier1" } },
      { "ValheimVehicles_Cannon_Turret_Tier1", new[] { "valheim_vehicles_cannon_turret_tier1", "cannonturrettier1" } },
      { "ValheimVehicles_Powder_Barrel", new[] { "valheim_vehicles_powder_barrel", "powderbarrel" } },
      { "ValheimVehicles_Cannon_Control_Center", new[] { "valheim_vehicles_cannon_control_center", "cannoncontrolcenter" } },
      { "MBDirtFloor_1x1", new[] { "dirtfloor1x1", "mbdirtfloor1x1" } },
      { "MBDirtFloor_2x2", new[] { "dirtfloor2x2", "mbdirtfloor2x2" } },
      { "MBPier_Stone", new[] { "pierstone", "mbpierstone" } },
      { "MBPier_Pole", new[] { "pierpole", "mbpierpole" } },
      { "ValheimVehicles_Power_Source_Eitr", new[] { "valheim_vehicles_mechanism_power_source_eitr", "powersourceeitr" } },
      { "ValheimVehicles_Power_Storage_Eitr", new[] { "valheim_vehicles_mechanism_power_storage_eitr", "powerstorageeitr" } },
      { "ValheimVehicles_Power_Conduit_Charge_Plate", new[] { "valheim_vehicles_mechanism_power_charge_plate", "valheim_vehicles_power_conduit_charge_plate", "powerconduitchargeplate" } },
      { "ValheimVehicles_Power_Conduit_Drain_Plate", new[] { "valheim_vehicles_mechanism_power_drain_plate", "valheim_vehicles_power_conduit_drain_plate", "powerconduitdrainplate" } },
      { "ValheimVehicles_Power_Pylon", new[] { "valheim_vehicles_mechanism_power_pylon", "powerpylon" } },
      { "ValheimVehicles_Mechanism_ToggleSwitch", new[] { "valheim_vehicles_mechanism_toggle_switch", "mechanismtoggleswitch", "toggleswitch" } }
    };

    private static bool MatchesPieceName(string actualName, string desiredName)
    {
      if (string.Equals(actualName, desiredName, StringComparison.OrdinalIgnoreCase))
        return true;
      var normActual = NormalizePieceName(actualName);
      var normDesired = NormalizePieceName(desiredName);
      if (string.Equals(normActual, normDesired, StringComparison.OrdinalIgnoreCase))
        return true;

      if (PieceAliases.TryGetValue(desiredName, out var aliases))
      {
        foreach (var alias in aliases)
        {
          if (string.Equals(actualName, alias, StringComparison.OrdinalIgnoreCase) ||
              string.Equals(normActual, NormalizePieceName(alias), StringComparison.OrdinalIgnoreCase))
            return true;
        }
      }

      return false;
    }

    private static bool IsEndPiece(string name)
    {
      return EndPieceOrder.Any(desired => MatchesPieceName(name, desired));
    }

    private static bool IsRedundantPiece(string name)
    {
      if (string.IsNullOrEmpty(name)) return false;
      var clean = name.Replace("(Clone)", "").Trim();
      if (RedundantPieces.Contains(clean)) return true;
      var norm = NormalizePieceName(clean);
      return RedundantPieces.Any(r => string.Equals(NormalizePieceName(r), norm, StringComparison.OrdinalIgnoreCase));
    }

    public static void EnsureRepairPieceAdded()
    {
      if (ObjectDB.instance == null) return;
      ValheimRaftLocalization.ApplyActiveLanguage();
      var table = PieceManager.Instance.GetPieceTable(VehicleHammerTableName) ?? VehicleHammerTable?.PieceTable;
      if (table == null || table.m_pieces == null) return;

      var repairPiece = table.m_pieces.FirstOrDefault(p => p != null && p.GetComponent<Piece>()?.m_repairPiece == true);
      if (repairPiece == null)
      {
        var hammerPrefab = ObjectDB.instance.GetItemPrefab("Hammer");
        if (hammerPrefab != null)
        {
          var hammerItem = hammerPrefab.GetComponent<ItemDrop>();
          var vanillaTable = hammerItem?.m_itemData?.m_shared?.m_buildPieces;
          repairPiece = vanillaTable?.m_pieces?.FirstOrDefault(p => p != null && p.GetComponent<Piece>()?.m_repairPiece == true);
        }
      }

      var currentPieces = table.m_pieces.Where(p => p != null).ToList();
      var sortedPieces = new List<GameObject>();
      var used = new HashSet<GameObject>();

      // Index 0: Repair piece
      if (repairPiece != null)
      {
        sortedPieces.Add(repairPiece);
        used.Add(repairPiece);
      }

      // Step 1: Start pieces (Main Keel, navigation, steering, rigging, masts, ramps, wood hulls, wood rails)
      foreach (var desiredName in StartPieceOrder)
      {
        var match = currentPieces.FirstOrDefault(p => !used.Contains(p) && MatchesPieceName(p.name, desiredName));
        if (match != null)
        {
          sortedPieces.Add(match);
          used.Add(match);
        }
        else
        {
          LoggerProvider.LogDebug($"[VehicleHammer] Start piece not currently in table: {desiredName}");
        }
      }

      // Step 2: Middle pieces (all Iron hulls, walls, windows, quarter floors, etc. that are not start/end/redundant)
      foreach (var remaining in currentPieces)
      {
        if (used.Contains(remaining)) continue;
        if (IsEndPiece(remaining.name)) continue;
        if (IsRedundantPiece(remaining.name))
        {
          LoggerProvider.LogDebug($"[VehicleHammer] Excluding redundant piece from table: {remaining.name}");
          continue;
        }
        sortedPieces.Add(remaining);
        used.Add(remaining);
      }

      // Step 3: End pieces (cannons, powder barrel, control center, planters, pier supports, power, toggle switch)
      foreach (var desiredName in EndPieceOrder)
      {
        var match = currentPieces.FirstOrDefault(p => !used.Contains(p) && MatchesPieceName(p.name, desiredName));
        if (match != null)
        {
          sortedPieces.Add(match);
          used.Add(match);
        }
        else
        {
          LoggerProvider.LogDebug($"[VehicleHammer] End piece not currently in table: {desiredName}");
        }
      }

      table.m_pieces = sortedPieces;
      LoggerProvider.LogInfo($"[VehicleHammer] Successfully organized {table.m_pieces.Count} pieces in {VehicleHammerTableName} with repair at index 0");
    }

    public override void OnRegister()
    {
      RegisterVehicleHammerTable();
    }
  }
}