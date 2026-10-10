using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Prefabs.Registry;

namespace ValheimVehicles.Patches
{
  [HarmonyPatch(typeof(ByUsagePieceList))]
  public static class ByUsagePieceList_Patch
  {
    private static bool IsVehicleHammer(PieceTable? pieceTable)
    {
      if (pieceTable != null && pieceTable.name.IndexOf(VehicleHammerTableRegistry.VehicleHammerTableName, StringComparison.OrdinalIgnoreCase) >= 0)
      {
        return true;
      }
      var playerTable = Player.m_localPlayer?.m_buildPieces;
      return playerTable != null && playerTable.name.IndexOf(VehicleHammerTableRegistry.VehicleHammerTableName, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    [HarmonyPatch(nameof(ByUsagePieceList.TagCount), MethodType.Getter)]
    [HarmonyPrefix]
    public static bool TagCount_Prefix(ByUsagePieceList __instance, ref int __result)
    {
      var pieceTable = Player.m_localPlayer?.m_buildPieces;
      if (!IsVehicleHammer(pieceTable)) return true;

      __result = 4;
      return false;
    }

    [HarmonyPatch(nameof(ByUsagePieceList.UpdateAvailableTags))]
    [HarmonyPrefix]
    public static bool UpdateAvailableTags_Prefix(ByUsagePieceList __instance, PieceTable pieceTable)
    {
      var table = pieceTable ?? Player.m_localPlayer?.m_buildPieces;
      if (!IsVehicleHammer(table)) return true;

      __instance.m_availableTags.Clear();
      __instance.m_availableTags.Add(0); // Resined
      __instance.m_availableTags.Add(1); // Nailed
      __instance.m_availableTags.Add(2); // Iron
      __instance.m_availableTags.Add(3); // Misc
      return false;
    }

    [HarmonyPatch(nameof(ByUsagePieceList.GetTagIdByIndex))]
    [HarmonyPrefix]
    public static bool GetTagIdByIndex_Prefix(ByUsagePieceList __instance, int index, ref int __result)
    {
      var pieceTable = Player.m_localPlayer?.m_buildPieces;
      if (!IsVehicleHammer(pieceTable)) return true;

      __result = Mathf.Clamp(index, 0, 3);
      return false;
    }

    [HarmonyPatch(nameof(ByUsagePieceList.GetTagDisplayName))]
    [HarmonyPrefix]
    public static bool GetTagDisplayName_Prefix(ByUsagePieceList __instance, int index, ref string __result)
    {
      var pieceTable = Player.m_localPlayer?.m_buildPieces;
      if (!IsVehicleHammer(pieceTable)) return true;

      var tagId = index >= 0 && index < __instance.m_availableTags.Count
        ? __instance.GetTagIdByIndex(index)
        : index;

      __result = tagId switch
      {
        0 => VehicleHammerTableCategories.ToLocalizedLabel(VehicleHammerTableCategories.Resined),
        1 => VehicleHammerTableCategories.ToLocalizedLabel(VehicleHammerTableCategories.Nailed),
        2 => VehicleHammerTableCategories.ToLocalizedLabel(VehicleHammerTableCategories.Iron),
        _ => VehicleHammerTableCategories.ToLocalizedLabel(VehicleHammerTableCategories.Misc)
      };
      return false;
    }

    [HarmonyPatch(nameof(ByUsagePieceList.GetAvailablePiecesWithTag))]
    [HarmonyPrefix]
    public static bool GetAvailablePiecesWithTag_Prefix(
      ByUsagePieceList __instance,
      int tagId,
      PieceTable pieceTable,
      IList<Piece> resultOut)
    {
      var table = pieceTable ?? Player.m_localPlayer?.m_buildPieces;
      if (!IsVehicleHammer(table)) return true;

      resultOut.Clear();
      if (table.m_pieces == null) return false;

      foreach (var go in table.m_pieces)
      {
        if (go == null) continue;
        var piece = go.GetComponent<Piece>();
        if (piece == null) continue;

        var isSpecial = piece.m_repairPiece || piece.m_removePiece;
        if (tagId == -1 || isSpecial)
        {
          if (isSpecial || table.m_availablePieces.Contains(piece))
          {
            resultOut.Add(piece);
          }
          continue;
        }

        if (!table.m_availablePieces.Contains(piece)) continue;

        var category = VehicleHammerTableCategories.GetCategoryForPiece(go.name);
        var matches = tagId switch
        {
          0 => category == VehicleHammerTableCategories.Resined,
          1 => category == VehicleHammerTableCategories.Nailed,
          2 => category == VehicleHammerTableCategories.Iron,
          3 => category == VehicleHammerTableCategories.Misc,
          _ => false
        };

        if (matches)
        {
          resultOut.Add(piece);
        }
      }

      return false;
    }
  }
}
