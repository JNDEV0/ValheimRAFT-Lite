using System;
using System.Collections.Generic;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.Prefabs.Registry;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Controllers;

public static class VehicleMaterialCostController
{
  private static readonly Dictionary<string, Piece.Requirement[]> _originalRequirements = new(StringComparer.OrdinalIgnoreCase);
  private static bool _isOneWoodActive = false;

  /// <summary>
  /// Caches the original requirements of the hammer pieces before any modifications.
  /// Called when the hammer piece table is initialized and organized.
  /// </summary>
  public static void CacheOriginalRequirements(IEnumerable<GameObject>? pieces)
  {
    if (pieces == null) return;
    foreach (var go in pieces)
    {
      if (go == null) continue;
      var piece = go.GetComponent<Piece>();
      if (piece == null || piece.m_repairPiece) continue;

      string pieceName = go.name;
      if (!_originalRequirements.ContainsKey(pieceName) && piece.m_resources != null && piece.m_resources.Length > 0)
      {
        _originalRequirements[pieceName] = CloneRequirements(piece.m_resources);
      }
    }

    if (VehicleGlobalConfig.NoMaterialCost != null && VehicleGlobalConfig.NoMaterialCost.Value)
    {
      SetOneWoodCost(true);
    }
  }

  /// <summary>
  /// Sets whether all boat hammer pieces cost only 1 Wood.
  /// </summary>
  public static void SetOneWoodCost(bool enabled)
  {
    _isOneWoodActive = enabled;
    ApplyCostState();
  }

  /// <summary>
  /// Applies the current cost state (1 Wood vs Original) to all pieces in the boat hammer table.
  /// </summary>
  public static void ApplyCostState()
  {
    if (ObjectDB.instance == null) return;

    var woodPrefab = ObjectDB.instance.GetItemPrefab("Wood");
    if (woodPrefab == null) return;
    var woodItem = woodPrefab.GetComponent<ItemDrop>();
    if (woodItem == null) return;

    var table = PieceManager.Instance.GetPieceTable(VehicleHammerTableRegistry.VehicleHammerTableName)
                ?? VehicleHammerTableRegistry.VehicleHammerTable?.PieceTable;
    if (table == null || table.m_pieces == null) return;

    var oneWoodReq = new[]
    {
      new Piece.Requirement
      {
        m_resItem = woodItem,
        m_amount = 1,
        m_recover = true
      }
    };

    foreach (var go in table.m_pieces)
    {
      if (go == null) continue;
      var piece = go.GetComponent<Piece>();
      if (piece == null || piece.m_repairPiece) continue;

      string pieceName = go.name;

      if (_isOneWoodActive)
      {
        // Cache original if not already cached
        if (!_originalRequirements.ContainsKey(pieceName) && piece.m_resources != null && piece.m_resources.Length > 0)
        {
          _originalRequirements[pieceName] = CloneRequirements(piece.m_resources);
        }

        piece.m_resources = oneWoodReq;
      }
      else
      {
        // Restore original requirements
        if (_originalRequirements.TryGetValue(pieceName, out var originalReqs))
        {
          piece.m_resources = CloneRequirements(originalReqs);
        }
      }
    }

    if (Player.m_localPlayer != null)
    {
      Player.m_localPlayer.UpdateKnownRecipesList();
    }

    Logger.LogInfo($"[VehicleMaterialCost] Applied cost state: {(_isOneWoodActive ? "1 Wood (No Material Cost)" : "Standard Tiered Costs")} across {table.m_pieces.Count} boat hammer pieces.");
  }

  private static Piece.Requirement[] CloneRequirements(Piece.Requirement[] source)
  {
    if (source == null) return Array.Empty<Piece.Requirement>();
    var clone = new Piece.Requirement[source.Length];
    for (int i = 0; i < source.Length; i++)
    {
      if (source[i] == null) continue;
      clone[i] = new Piece.Requirement
      {
        m_resItem = source[i].m_resItem,
        m_amount = source[i].m_amount,
        m_extraAmountOnlyOneIngredient = source[i].m_extraAmountOnlyOneIngredient,
        m_amountPerLevel = source[i].m_amountPerLevel,
        m_upgraderResource = source[i].m_upgraderResource,
        m_recover = source[i].m_recover
      };
    }
    return clone;
  }
}
