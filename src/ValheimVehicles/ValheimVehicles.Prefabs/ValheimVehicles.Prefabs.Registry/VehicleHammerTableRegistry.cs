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
      "ValheimVehicles_ShipKeel", // Nailed Keel Extension 4x8 (Wood)
      "ValheimVehicles_Ship_Hull_Wood", // Nailed Keel Extension (Wood)
      "ValheimVehicles_Ship_Hull_Iron", // Iron-Reinforced Keel Extension (Iron)
      "ValheimVehicles_Ship_Hull_Iron_Plated", // Iron-Plated Keel Extension
      "ValheimVehicles_ShipSteeringWheel", // 45. Ship's Helm
      "ValheimVehicles_ShipRudderBasic", // 47. Steering Oar
      "ValheimVehicles_ShipRudderAdvanced_Wood", // 51/52. Sternpost Rudder
      "MBRopeLadder", // 117. Jacob's Ladder
      "MBRaftMast", // 113. Square Rigged Sail / Raft Mast
      "MBKarveMast", // Karve Sail / Mast (between square rigged sail and rigging cleat)
      "ValheimVehicles_Greydwarf_Rowing_Seat", // Greydwarf Rowing Seat (next to the sails)
      "MBRopeAnchor", // 116. Rigging Cleat
      "ValheimVehicles_ShipAnchor_Wood", // 118. Ship Anchor
      "MBBoardingRamp", // 119. Boarding Ramp
      "MBBoardingRamp_Wide", // 120. Wide Boarding Ramp

      // Cannons, Turrets & Gunnery Control (moved up per user request)
      "ValheimVehicles_Cannon_Fixed_Tier1", // Deck Cannon - fixed
      "ValheimVehicles_Cannon_Turret_Tier1", // Turret Cannon - Auto
      "ValheimVehicles_Powder_Barrel", // Powder Barrel - Ammo Storage
      "ValheimVehicles_Cannon_Control_Center", // Gunnery Binnacle - Cannon Control Center

      // Planters & Pier Supports (moved up per user request)
      "MBDirtFloor_1x1", // Ship Planter - Small
      "MBDirtFloor_2x2", // Ship Planter - Large
      "MBPier_Stone", // Stone Piling - Pier Support
      "MBPier_Pole", // Timber Piling - Pier Support

      // Eitr Power System & Controls (moved up per user request)
      "ValheimVehicles_Power_Source_Eitr", // Power Generator
      "ValheimVehicles_Power_Storage_Eitr", // Battery / Power Storage
      "ValheimVehicles_Power_Conduit_Charge_Plate", // Power Charge Plate
      "ValheimVehicles_Power_Conduit_Drain_Plate", // Power Drain Plate
      "ValheimVehicles_Mechanism_ToggleSwitch", // Toggle Switch - Boat Settings

      // Wood Hulls, Ribs, Prow, Decks, and Guard Rails
      "ValheimVehicles_Ship_Hull_Prow_Wood_2x2x4", // Resined Hull Cutwater - center
      "ValheimVehicles_Ship_Hull_Rib_Wood", // Resined Hull Midship Frame - Side
      "ValheimVehicles_Ship_Hull_Rib_Corner_Wood", // Resined Hull Stern - Corner
      "ValheimVehicles_Ship_Hull_Rib_Corner_2x2x4_Left_Wood", // Resined Hull Front Prow - Left
      "ValheimVehicles_Ship_Hull_Rib_Corner_2x2x4_Right_Wood", // Resined Hull Front Prow - Right
      "ValheimVehicles_Hull_Slab_Wood_2x2", // Resined Deck Planking - Small
      "ValheimVehicles_Hull_Slab_Wood_4x4", // Resined Deck Planking
      "ValheimVehicles_Ship_Hull_Rib_Corner_Floor_2x2_Left_Wood", // Resined Deck Stern Planking - Left
      "ValheimVehicles_Ship_Hull_Rib_Corner_Floor_2x2_Right_Wood", // Resined Deck Stern Planking - Right
      "ValheimVehicles_Ship_Hull_Rib_Corner_Floor_2x4_Left_Wood", // Resined Deck Prow Planking - Left
      "ValheimVehicles_Ship_Hull_Rib_Corner_Floor_2x4_Right_Wood", // Resined Deck Prow Planking - Right
      "ValheimVehicles_hull_bow_center_wood", // Nailed Hull Cutwater - center
      "ValheimVehicles_hull_rib_wood", // Nailed Hull Midship Frame - Side
      "ValheimVehicles_hull_bow_tri_left_wood", // Nailed Hull Front Prow - left
      "ValheimVehicles_hull_bow_tri_right_wood", // Nailed Hull Front Prow - right
      "ValheimVehicles_hull_bow_curved_left_wood", // Nailed Hull Rear stern - left
      "ValheimVehicles_hull_bow_curved_right_wood", // Nailed Hull Rear stern - right
      "ValheimVehicles_hull_rib_aft_center_wood", // Nailed Hull Rear Counter Stern - center
      "ValheimVehicles_hull_rib_aft_left_wood", // Nailed Hull Rear Counter Stern - Left
      "ValheimVehicles_hull_rib_aft_right_wood", // Nailed Hull Rear Counter Stern - Right
      "ValheimVehicles_hull_floor_2x2_wood", // Nailed Deck Planking - Small
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
      "ValheimVehicles_hull_rail_prow_corner_right_wood", // Deck Prow Guard-Rail - Right

      // Nailed Portholes
      "ValheimVehicles_ShipWindow_Wall_Porthole_Wood_2x2", // Nailed Porthole Frame - 2x2
      "ValheimVehicles_ShipWindow_Wall_Porthole_Wood_4x4", // Nailed Porthole Frame - 4x4
      "ValheimVehicles_ShipWindow_Wall_Porthole_Wood_8x4", // Nailed Porthole Frame - Large
      "ValheimVehicles_ShipWindow_Floor_Porthole_Wood_4x4", // Nailed Floor Hatch Porthole - 4x4

      // Iron-Plated Hulls, Ribs, Decks, and Guard Rails
      "ValheimVehicles_hull_bow_center_iron", // Iron-Plated Hull Cutwater - center
      "ValheimVehicles_hull_rib_iron", // Iron-Plated Hull Midship Frame - Side
      "ValheimVehicles_hull_bow_tri_left_iron", // Iron-Plated Hull Front Prow - left
      "ValheimVehicles_hull_bow_tri_right_iron", // Iron-Plated Hull Front Prow - right
      "ValheimVehicles_hull_bow_curved_left_iron", // Iron-Plated Hull Rear stern - left
      "ValheimVehicles_hull_bow_curved_right_iron", // Iron-Plated Hull Rear stern - right
      "ValheimVehicles_hull_rib_aft_center_iron", // Iron-Plated Hull Rear Counter Stern - center
      "ValheimVehicles_hull_rib_aft_left_iron", // Iron-Plated Hull Rear Counter Stern - Left
      "ValheimVehicles_hull_rib_aft_right_iron", // Iron-Plated Hull Rear Counter Stern - Right
      "ValheimVehicles_hull_floor_2x2_iron", // Iron-Plated Deck Planking - Small
      "ValheimVehicles_hull_floor_4x4_iron", // Iron-Plated Deck Planking (moved before iron guard-rails)
      "ValheimVehicles_hull_seal_corner_left_iron", // Iron-Plated Deck Counter Stern Planking - Left
      "ValheimVehicles_hull_seal_corner_right_iron", // Iron-Plated Deck Counter Stern Planking - Right
      "ValheimVehicles_hull_seal_bow_left_iron", // Iron-Plated Deck Stern Planking - Left
      "ValheimVehicles_hull_seal_bow_right_iron", // Iron-Plated Deck Stern Planking - Right
      "ValheimVehicles_hull_seal_tri_bow_left_iron", // Iron-Plated Deck Prow Planking - Left
      "ValheimVehicles_hull_seal_tri_bow_right_iron", // Iron-Plated Deck Prow Planking - Right
      "ValheimVehicles_hull_rail_connector_iron", // Iron Guard-Rail - single
      "ValheimVehicles_hull_rail_straight_iron", // Iron Guard-Rail - triple
      "ValheimVehicles_hull_rail_25deg_iron", // Iron Guard-Rail - 25 deg
      "ValheimVehicles_hull_rail_45deg_iron", // Iron Guard-Rail - 45 deg
      "ValheimVehicles_hull_rail_corner_iron", // Iron Stern Guard-Rail - rounded
      "ValheimVehicles_hull_rail_prow_corner_left_iron", // Iron Prow Guard-Rail - Left
      "ValheimVehicles_hull_rail_prow_corner_right_iron", // Iron Prow Guard-Rail - Right

      // Iron-Plated Portholes
      "ValheimVehicles_ShipWindow_Wall_Porthole_2x2", // Iron-Plated Porthole Frame - Small
      "ValheimVehicles_ShipWindow_Wall_Porthole_4x4", // Iron-Plated Porthole Frame
      "ValheimVehicles_ShipWindow_Wall_Porthole_8x4", // Iron-Plated Porthole Frame - Large
      "ValheimVehicles_ShipWindow_Floor_Porthole_4x4" // Iron-Plated Porthole Floor
    };

    private static readonly string[] EndPieceOrder = new[]
    {
      // Solid Iron Parts Set (moved to end of build menu per user request)
      "ValheimVehicles_Ship_Hull_Rib_2x1x8_Iron", // Solid Iron Hull Midship Frame - Side - Long
      "ValheimVehicles_Ship_Hull_Rib_Corner_Iron", // Solid Iron Hull Stern - Corner
      "ValheimVehicles_Ship_Hull_Rib_Corner_2x2x4_Left_Iron", // Solid Iron Hull Front Prow - Left
      "ValheimVehicles_Ship_Hull_Rib_Corner_2x2x4_Right_Iron", // Solid Iron Hull Front Prow - Right
      "ValheimVehicles_Ship_Hull_Rib_Corner_2x1x8_Left_Iron", // Solid Iron Hull Stern Corner - Left - Long
      "ValheimVehicles_Ship_Hull_Rib_Corner_2x1x8_Right_Iron", // Solid Iron Hull Stern Corner - Right - Long

      // Iron-Reinforced Hull Set (9 pieces)
      "ValheimVehicles_hull_bow_center_iron_reinforced",
      "ValheimVehicles_hull_rib_iron_reinforced",
      "ValheimVehicles_hull_bow_tri_left_iron_reinforced",
      "ValheimVehicles_hull_bow_tri_right_iron_reinforced",
      "ValheimVehicles_hull_bow_curved_left_iron_reinforced",
      "ValheimVehicles_hull_bow_curved_right_iron_reinforced",
      "ValheimVehicles_hull_rib_aft_center_iron_reinforced",
      "ValheimVehicles_hull_rib_aft_left_iron_reinforced",
      "ValheimVehicles_hull_rib_aft_right_iron_reinforced",

      // Iron-Reinforced Deck Planking (2x2 and 4x4 ahead of stern planking pieces per user request)
      "ValheimVehicles_Hull_Slab_Iron_2x2", // Iron-Reinforced Deck Planking - Small
      "ValheimVehicles_Hull_Slab_Iron_4x4", // Iron-Reinforced Deck Planking

      // Iron-Reinforced Additional Deck Seal Parts
      "ValheimVehicles_Ship_Hull_Rib_Corner_Floor_2x2_Left_Iron", // Iron-Reinforced Deck Stern Planking - Small - Left
      "ValheimVehicles_Ship_Hull_Rib_Corner_Floor_2x2_Right_Iron", // Iron-Reinforced Deck Stern Planking - Small - Right
      "ValheimVehicles_Ship_Hull_Rib_Corner_Floor_2x4_Left_Iron", // Iron-Reinforced Deck Stern Planking - Left
      "ValheimVehicles_Ship_Hull_Rib_Corner_Floor_2x4_Right_Iron", // Iron-Reinforced Deck Stern Planking - Right
      "ValheimVehicles_Ship_Hull_Rib_Corner_Floor_2x8_Left_Iron", // Iron-Reinforced Deck Stern Planking - Long - Left
      "ValheimVehicles_Ship_Hull_Rib_Corner_Floor_2x8_Right_Iron", // Iron-Reinforced Deck Stern Planking - Long - Right

      // Iron-Reinforced Frame Walls
      "ValheimVehicles_Hull_Wall_Iron_2x2", // Iron-Reinforced Frame Wall - Small
      "ValheimVehicles_Hull_Wall_Iron_4x4"  // Iron-Reinforced Frame Wall
    };

    private static readonly HashSet<string> RedundantPieces = new(StringComparer.OrdinalIgnoreCase)
    {
      // Garboard strakes (removed per user request)
      "hull_floor_keel_4x2_left_wood",
      "ValheimVehicles_hull_floor_keel_4x2_left_wood",
      "hull_floor_keel_4x2_right_wood",
      "ValheimVehicles_hull_floor_keel_4x2_right_wood",
      "hull_floor_keel_4x2_left_iron",
      "ValheimVehicles_hull_floor_keel_4x2_left_iron",

      // Rigged sails (temporarily hidden from build menu per user request)
      "MBVikingShipMast",
      "mb_vikingship_mast",
      "ValheimVehicles_DrakkalMast",
      "valheim_vehicles_drakkalship_mast",

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
      "ValheimVehicles_hull_prow_seal_iron",

      // Porthole Window 6x4 (Iron)
      "WindowWallPorthole6x4",
      "WindowWallPorthole6x4Prefab",
      "ValheimVehicles_ShipWindow_Wall_Porthole_6x4",
      "valheim_vehicles_shipwindow_wall_porthole_6x4",
      "ShipWindow_Wall_Porthole_6x4",
      "hull_wall_window_porthole_iron_6x4",

      // Power Pylon (disabled per user request)
      "ValheimVehicles_Power_Pylon",
      "valheim_vehicles_mechanism_power_pylon",
      "power_pylon",
      "powerpylon",

      // Hull-Rib Side 2x2x2 (Iron) - do not match v4 hull_rib_iron!
      "hull_rib_iron_2x2x2",
      "ValheimVehicles_Ship_Hull_Rib_Iron",
      "Ship_Hull_Rib_Iron",

      // Hull-Rib Prow (Iron)
      "hull_prow_iron_2x2x4",
      "ValheimVehicles_Ship_Hull_Prow_Iron_2x2x4",
      "Ship_Hull_Prow_Iron_2x2x4",

      // Hull-Rib Prow Sleek (Iron) 2x2x8 (Left & Right)
      "ValheimVehicles_Ship_Hull_Prow_Rib_sleek_2x2x8_Iron_left",
      "ValheimVehicles_Ship_Hull_Prow_Rib_sleek_2x2x8_Iron_right",
      "Ship_Hull_Prow_Rib_sleek_2x2x8_Iron_left",
      "Ship_Hull_Prow_Rib_sleek_2x2x8_Iron_right",
      "hull_prow_rib_sleek_2x2x8_iron_left",
      "hull_prow_rib_sleek_2x2x8_iron_right",

      // Hull-Rib Prow Cutter (Iron) 2x2x8 (Left & Right)
      "ValheimVehicles_Ship_Hull_Prow_Rib_cutter_2x2x8_Iron_left",
      "ValheimVehicles_Ship_Hull_Prow_Rib_cutter_2x2x8_Iron_right",
      "Ship_Hull_Prow_Rib_cutter_2x2x8_Iron_left",
      "Ship_Hull_Prow_Rib_cutter_2x2x8_Iron_right",
      "hull_prow_rib_cutter_2x2x8_iron_left",
      "hull_prow_rib_cutter_2x2x8_iron_right"
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
      { "ValheimVehicles_ShipKeel", new[] { "valheimvehicles_ship_hull_wood", "shipkeel", "mbkeel", "valheim_vehicles_hull_center_wood", "hull_center_wood", "ship_hull_wood" } },
      { "ValheimVehicles_Ship_Hull_Iron", new[] { "valheimvehicles_ship_hull_iron", "shiphullcenteriron", "ship_hull_iron", "valheim_vehicles_hull_center_iron", "hull_center_iron" } },
      { "ValheimVehicles_Ship_Hull_Iron_Plated", new[] { "valheimvehicles_ship_hull_iron_plated", "shiphullcenterironplated", "ship_hull_iron_plated", "valheim_vehicles_hull_center_iron_plated", "hull_center_iron_plated" } },
      { "ValheimVehicles_Hull_Slab_Wood_2x2", new[] { "hull_slab_wood_2x2", "valheimvehicles_hull_slab_wood_2x2", "valheim_vehicles_hull_slab_wood_2x2" } },
      { "ValheimVehicles_hull_floor_2x2_wood", new[] { "hull_floor_2x2_wood", "valheimvehicles_hull_floor_2x2_wood", "valheim_vehicles_hull_floor_2x2_wood" } },
      { "ValheimVehicles_hull_floor_2x2_iron", new[] { "hull_floor_2x2_iron", "valheimvehicles_hull_floor_2x2_iron", "valheim_vehicles_hull_floor_2x2_iron" } },
      { "ValheimVehicles_ShipWindow_Wall_Porthole_Wood_2x2", new[] { "shipwindow_wall_porthole_wood_2x2", "valheimvehicles_shipwindow_wall_porthole_wood_2x2", "windowwallportholewood2x2" } },
      { "ValheimVehicles_ShipWindow_Wall_Porthole_Wood_4x4", new[] { "shipwindow_wall_porthole_wood_4x4", "valheimvehicles_shipwindow_wall_porthole_wood_4x4", "windowwallportholewood4x4" } },
      { "ValheimVehicles_ShipWindow_Wall_Porthole_Wood_8x4", new[] { "shipwindow_wall_porthole_wood_8x4", "valheimvehicles_shipwindow_wall_porthole_wood_8x4", "windowwallportholewood8x4" } },
      { "ValheimVehicles_ShipWindow_Floor_Porthole_Wood_4x4", new[] { "shipwindow_floor_porthole_wood_4x4", "valheimvehicles_shipwindow_floor_porthole_wood_4x4", "windowfloorportholewood4x4" } },
      { "ValheimVehicles_ShipSteeringWheel", new[] { "mb_steering_wheel", "shipsteeringwheel", "steeringwheel" } },
      { "ValheimVehicles_ShipRudderBasic", new[] { "valheim_vehicles_rudder_basic", "shiprudderbasic", "rudderbasic" } },
      { "ValheimVehicles_ShipRudderAdvanced_Wood", new[] { "valheim_vehicles_rudder_advanced", "shiprudderadvancedwood", "shiprudderadvancedsinglewood" } },
      { "MBRopeLadder", new[] { "mb_rope_ladder", "ropeladder" } },
      { "MBRaftMast", new[] { "mb_raft_mast", "raftmast" } },
      { "MBKarveMast", new[] { "mb_karve_mast", "karvemast", "karve_mast", "karve_sail", "valheimvehicles_karvesail", "karvesail" } },
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
      { "ValheimVehicles_hull_bow_center_iron", new[] { "hull_bow_center_iron", "valheimvehicles_hull_bow_center_iron", "valheim_vehicles_hull_bow_center_iron" } },
      { "ValheimVehicles_Hull_Slab_Iron_2x2", new[] { "hull_slab_iron_2x2", "valheimvehicles_hull_slab_iron_2x2", "valheim_vehicles_hull_slab_iron_2x2" } },
      { "ValheimVehicles_Hull_Slab_Iron_4x4", new[] { "hull_slab_iron_4x4", "valheimvehicles_hull_slab_iron_4x4", "valheim_vehicles_hull_slab_iron_4x4", "ValheimVehicles_Hull_Slab_Iron_4x4" } },
      { "ValheimVehicles_hull_floor_4x4_iron", new[] { "hull_floor_4x4_iron", "valheimvehicles_hull_floor_4x4_iron", "valheim_vehicles_hull_floor_4x4_iron" } },
      { "ValheimVehicles_Hull_Wall_Iron_2x2", new[] { "hull_wall_iron_2x2", "valheimvehicles_hull_wall_iron_2x2", "valheim_vehicles_hull_wall_iron_2x2" } },
      { "ValheimVehicles_Hull_Wall_Iron_4x4", new[] { "hull_wall_iron_4x4", "valheimvehicles_hull_wall_iron_4x4", "valheim_vehicles_hull_wall_iron_4x4" } },
      { "ValheimVehicles_ShipWindow_Wall_Porthole_2x2", new[] { "shipwindow_wall_porthole_2x2", "valheimvehicles_shipwindow_wall_porthole_2x2", "windowwallporthole2x2", "hull_wall_window_porthole_iron_2x2" } },
      { "ValheimVehicles_ShipWindow_Wall_Porthole_4x4", new[] { "shipwindow_wall_porthole_4x4", "valheimvehicles_shipwindow_wall_porthole_4x4", "windowwallporthole4x4", "hull_wall_window_porthole_iron_4x4" } },
      { "ValheimVehicles_ShipWindow_Wall_Porthole_8x4", new[] { "shipwindow_wall_porthole_8x4", "valheimvehicles_shipwindow_wall_porthole_8x4", "windowwallporthole8x4", "hull_wall_window_porthole_iron_8x4" } },
      { "ValheimVehicles_ShipWindow_Floor_Porthole_4x4", new[] { "shipwindow_floor_porthole_4x4", "valheimvehicles_shipwindow_floor_porthole_4x4", "windowfloorporthole4x4", "hull_floor_window_porthole_iron_4x4" } },
      { "ValheimVehicles_Mechanism_ToggleSwitch", new[] { "valheim_vehicles_mechanism_toggle_switch", "mechanismtoggleswitch", "toggleswitch" } },
      { "ValheimVehicles_Ship_Hull_Rib_2x1x8_Iron", new[] { "hull_rib_side_iron_2x1x8", "valheim_vehicles_hull_rib_iron_2x1x8", "hull_rib_iron_2x1x8", "ship_hull_rib_2x1x8_iron" } },
      { "ValheimVehicles_Ship_Hull_Rib_Corner_Iron", new[] { "hull_rib_corner_iron_2x2x2", "valheim_vehicles_hull_rib_corner_iron_2x2x2", "ship_hull_rib_corner_iron" } },
      { "ValheimVehicles_Ship_Hull_Rib_Corner_2x2x4_Left_Iron", new[] { "hull_rib_corner_iron_2x2x4_left", "valheim_vehicles_hull_rib_corner_iron_left_2x2x4", "ship_hull_rib_corner_2x2x4_left_iron" } },
      { "ValheimVehicles_Ship_Hull_Rib_Corner_2x2x4_Right_Iron", new[] { "hull_rib_corner_iron_2x2x4_right", "valheim_vehicles_hull_rib_corner_iron_right_2x2x4", "ship_hull_rib_corner_2x2x4_right_iron" } },
      { "ValheimVehicles_Ship_Hull_Rib_Corner_2x1x8_Left_Iron", new[] { "hull_rib_corner_iron_2x1x8_left", "valheim_vehicles_hull_rib_corner_iron_left_2x1x8", "ship_hull_rib_corner_2x1x8_left_iron" } },
      { "ValheimVehicles_Ship_Hull_Rib_Corner_2x1x8_Right_Iron", new[] { "hull_rib_corner_iron_2x1x8_right", "valheim_vehicles_hull_rib_corner_iron_right_2x1x8", "ship_hull_rib_corner_2x1x8_right_iron" } },
      { "ValheimVehicles_Ship_Hull_Rib_Corner_Floor_2x2_Left_Iron", new[] { "hull_rib_corner_floor_iron_2x2_left", "valheim_vehicles_hull_rib_corner_floor_iron_left_2x2" } },
      { "ValheimVehicles_Ship_Hull_Rib_Corner_Floor_2x2_Right_Iron", new[] { "hull_rib_corner_floor_iron_2x2_right", "valheim_vehicles_hull_rib_corner_floor_iron_right_2x2" } },
      { "ValheimVehicles_Ship_Hull_Rib_Corner_Floor_2x4_Left_Iron", new[] { "hull_rib_corner_floor_iron_2x4_left", "valheim_vehicles_hull_rib_corner_floor_iron_left_2x4" } },
      { "ValheimVehicles_Ship_Hull_Rib_Corner_Floor_2x4_Right_Iron", new[] { "hull_rib_corner_floor_iron_2x4_right", "valheim_vehicles_hull_rib_corner_floor_iron_right_2x4" } },
      { "ValheimVehicles_Ship_Hull_Rib_Corner_Floor_2x8_Left_Iron", new[] { "hull_rib_corner_floor_iron_2x8_left", "valheim_vehicles_hull_rib_corner_floor_iron_left_2x8" } },
      { "ValheimVehicles_Ship_Hull_Rib_Corner_Floor_2x8_Right_Iron", new[] { "hull_rib_corner_floor_iron_2x8_right", "valheim_vehicles_hull_rib_corner_floor_iron_right_2x8" } },
      { "ValheimVehicles_Greydwarf_Rowing_Seat", new[] { "greydwarf_rowing_seat", "valheimvehicles_greydwarf_rowing_seat", "rowingseat" } }
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