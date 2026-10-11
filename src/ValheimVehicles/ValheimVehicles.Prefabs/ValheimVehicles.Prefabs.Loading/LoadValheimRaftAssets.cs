using System;
using System.IO;
using BepInEx;
using UnityEngine;
using Zolantris.Shared;

namespace ValheimVehicles.Prefabs;

public class LoadValheimRaftAssets : ILoadAssets
{
  public static readonly LoadValheimRaftAssets Instance = new();

  public static GameObject boardingRampAsset;
  public static GameObject ropeLadder;
  public static GameObject dirtFloor;
  public static Material sailMat;
  public static Texture sailTextureNormal;
  public static Texture sailTexture;
  public static GameObject editPanel;
  public static GameObject editTexturePanel;

  public static GameObject? anchor_rope;
  public static AnimationClip? ladderClimb;

  public void Init(AssetBundle assetBundle)
  {
    editPanel = assetBundle.LoadAsset<GameObject>("edit_sail_panel");
    editTexturePanel = assetBundle.LoadAsset<GameObject>("edit_texture_panel");
    sailTextureNormal = assetBundle.LoadAsset<Texture>("sail_normal.png");
    sailTexture = assetBundle.LoadAsset<Texture>("sail.png");
    anchor_rope =
      assetBundle.LoadAsset<GameObject>("anchor_rope.prefab");
    boardingRampAsset =
      assetBundle.LoadAsset<GameObject>("boarding_ramp.prefab");
    ropeLadder =
      assetBundle.LoadAsset<GameObject>("rope_ladder.prefab");
    sailMat = assetBundle.LoadAsset<Material>("SailMat.mat");
    dirtFloor = assetBundle.LoadAsset<GameObject>("dirt_floor.prefab");
    LoadLadderClimb();
  }

  public static void LoadLadderClimb()
  {
    if (ladderClimb != null) return;
    try
    {
      string assemblyDir = Path.GetDirectoryName(typeof(LoadValheimRaftAssets).Assembly.Location) ?? "";
      string[] candidatePaths = new[]
      {
        Path.Combine(assemblyDir, "Assets", "valheim-ladderclimb"),
        Path.Combine(assemblyDir, "valheim-ladderclimb"),
        Path.Combine(Paths.PluginPath, "ValheimRAFT", "Assets", "valheim-ladderclimb"),
        Path.Combine(Paths.PluginPath, "ValheimRAFT", "valheim-ladderclimb")
      };

      foreach (var path in candidatePaths)
      {
        if (File.Exists(path))
        {
          var bundle = AssetBundle.LoadFromFile(path);
          if (bundle != null)
          {
            ladderClimb = bundle.LoadAsset<AnimationClip>("LadderClimb");
            if (ladderClimb != null)
            {
              LoggerProvider.LogInfo($"Loaded ladder climb animation from {path}");
              break;
            }
          }
        }
      }
    }
    catch (Exception ex)
    {
      LoggerProvider.LogWarning($"Could not load ladder climb animation: {ex.Message}");
    }
  }
}