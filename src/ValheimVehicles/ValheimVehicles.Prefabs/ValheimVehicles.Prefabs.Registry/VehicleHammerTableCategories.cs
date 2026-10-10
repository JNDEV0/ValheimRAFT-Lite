// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ValheimVehicles.Prefabs.Registry
{
  /// <summary>
  /// Stable category identifiers (English) + mapping to localization keys.
  /// Keep ALL game logic/config using the English canonical names.
  /// Only the UI layer turns these into localized labels.
  /// </summary>
  public static class VehicleHammerTableCategories
  {
    // Canonical (stable) category IDs used everywhere in config & code
    public const string Resined = "Resined";
    public const string Nailed = "Nailed";
    public const string Iron = "Iron";
    public const string Misc = "Misc";
    public const string Deprecated = "Deprecated";

    // Backwards-compatible aliases for existing code references
    public const string Vehicles = Misc;
    public const string Tools = Misc;
    public const string Propulsion = Misc;
    public const string Power = Misc;
    public const string Structure = Misc;
    public const string Hull = Iron;

    /// <summary>
    /// Default order of canonical IDs (used to append any missing that user omitted).
    /// </summary>
    public static readonly List<string> AllVehicleHammerCategoriesFallbackNames =
      new() { Resined, Nailed, Iron, Misc };

    /// <summary>
    /// English canonical -> localization key ($key) mapping.
    /// If a canonical is missing here, we fall back to English at runtime.
    /// </summary>
    private static readonly Dictionary<string, string> EnglishToLocKey =
      new(StringComparer.Ordinal)
      {
        { Resined, "$valheim_vehicles_build_hammer_category_resined" },
        { Nailed, "$valheim_vehicles_build_hammer_category_nailed" },
        { Iron, "$valheim_vehicles_build_hammer_category_iron" },
        { Misc, "$valheim_vehicles_build_hammer_category_misc" },
        { Deprecated, "$valheim_vehicles_build_hammer_category_deprecated" },
        // Legacy mappings
        { "Tools", "$valheim_vehicles_build_hammer_category_misc" },
        { "Vehicles", "$valheim_vehicles_build_hammer_category_misc" },
        { "Propulsion", "$valheim_vehicles_build_hammer_category_misc" },
        { "Power", "$valheim_vehicles_build_hammer_category_misc" },
        { "Structure", "$valheim_vehicles_build_hammer_category_misc" },
        { "Hull", "$valheim_vehicles_build_hammer_category_iron" }
      };

    /// <summary>
    /// Categorizes a piece prefab into Resined, Nailed, Iron, or Misc.
    /// </summary>
    public static string GetCategoryForPiece(string name)
    {
      if (string.IsNullOrEmpty(name)) return Misc;
      var clean = name.Replace("(Clone)", "").Trim();

      // 1. Check Nailed Wood Portholes
      if (clean.IndexOf("porthole", StringComparison.OrdinalIgnoreCase) >= 0 &&
          clean.IndexOf("wood", StringComparison.OrdinalIgnoreCase) >= 0)
      {
        return Nailed;
      }

      // 2. Check Iron
      if (clean.IndexOf("iron", StringComparison.OrdinalIgnoreCase) >= 0 ||
          clean.IndexOf("porthole", StringComparison.OrdinalIgnoreCase) >= 0)
      {
        return Iron;
      }

      // 2. Check Resined (specific legacy wood rib/prow pieces)
      if (clean.StartsWith("ValheimVehicles_Ship_Hull_Prow_Wood", StringComparison.OrdinalIgnoreCase) ||
          clean.StartsWith("ValheimVehicles_Ship_Hull_Rib_Wood", StringComparison.OrdinalIgnoreCase) ||
          clean.StartsWith("ValheimVehicles_Ship_Hull_Rib_Corner_Wood", StringComparison.OrdinalIgnoreCase) ||
          clean.StartsWith("ValheimVehicles_Ship_Hull_Rib_Corner_2x2x4", StringComparison.OrdinalIgnoreCase) ||
          clean.StartsWith("ValheimVehicles_Hull_Slab_Wood_4x4", StringComparison.OrdinalIgnoreCase) ||
          clean.StartsWith("ValheimVehicles_Hull_Slab_Wood_2x2", StringComparison.OrdinalIgnoreCase) ||
          clean.StartsWith("ValheimVehicles_Ship_Hull_Rib_Corner_Floor", StringComparison.OrdinalIgnoreCase))
      {
        return Resined;
      }

      // 3. Check Nailed (v4 hulls, keel extensions, deck rails)
      if (clean.IndexOf("hull", StringComparison.OrdinalIgnoreCase) >= 0 ||
          clean.IndexOf("keel", StringComparison.OrdinalIgnoreCase) >= 0 ||
          clean.IndexOf("rail", StringComparison.OrdinalIgnoreCase) >= 0)
      {
        return Nailed;
      }

      // 4. Everything else is Misc
      return Misc;
    }

    /// <summary>
    /// Normalizes any category string (including legacy ones) to one of the 4 canonical categories:
    /// Resined, Nailed, Iron, or Misc.
    /// </summary>
    public static string NormalizeCategory(string? val)
    {
      if (string.IsNullOrWhiteSpace(val)) return Misc;
      var clean = val.Trim();
      if (string.Equals(clean, Resined, StringComparison.OrdinalIgnoreCase)) return Resined;
      if (string.Equals(clean, Nailed, StringComparison.OrdinalIgnoreCase)) return Nailed;
      if (string.Equals(clean, Iron, StringComparison.OrdinalIgnoreCase) ||
          string.Equals(clean, "Hull", StringComparison.OrdinalIgnoreCase)) return Iron;
      return Misc;
    }

    /// <summary>
    /// Returns true if the provided string is a valid canonical category ID (English).
    /// </summary>
    public static bool IsHammerTableCategory(string val)
    {
      return AllVehicleHammerCategoriesFallbackNames.Contains(val) ||
             string.Equals(val, "Tools", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(val, "Hull", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(val, "Structure", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(val, "Power", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(val, "Propulsion", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(val, "Vehicles", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Map a canonical category (English) to a localized label for display.
    /// Falls back to the canonical if no Localization instance or key is missing.
    /// </summary>
    public static string ToLocalizedLabel(string canonical)
    {
      // Defensive guard: if we don't know this canonical, just show what we got.
      if (string.IsNullOrEmpty(canonical))
        return canonical ?? string.Empty;

      // No localization system loaded yet -> show English canonical
      if (Localization.instance == null)
        return canonical;

      // If we have a key mapping, localize that. Otherwise, try a "$"-prefixed guess, then fall back.
      if (EnglishToLocKey.TryGetValue(canonical, out var key))
      {
        var localized = Localization.instance.Localize(key);
        return string.IsNullOrEmpty(localized) ? canonical : localized;
      }

      // Optional heuristic: allow "$<canonical>" if you ever decide to migrate canonicals to keys.
      var probe = "$" + canonical;
      var maybe = Localization.instance.Localize(probe);
      return string.IsNullOrEmpty(maybe) ? canonical : maybe;
    }

    /// <summary>
    /// Utility: map an entire list of canonicals to localized labels.
    /// </summary>
    public static List<string> ToLocalizedLabels(IEnumerable<string> canonicals)
    {
      return canonicals.Select(ToLocalizedLabel).ToList();
    }

    /// <summary>
    /// Expose a copy of the mapping (read-only) if needed by diagnostics.
    /// </summary>
    public static IReadOnlyDictionary<string, string> GetCanonicalToKeyMap()
    {
      return EnglishToLocKey;
    }
  }
}