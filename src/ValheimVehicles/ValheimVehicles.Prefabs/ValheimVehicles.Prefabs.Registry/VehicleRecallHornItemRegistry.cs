using System;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.Components;
using ValheimVehicles.SharedScripts;
using Zolantris.Shared;

namespace ValheimVehicles.Prefabs.Registry;

public class VehicleRecallHornItemRegistry : RegisterPrefab<VehicleRecallHornItemRegistry>
{
  public static void RegisterVehicleHorn()
  {
    // Horn of the Seas disabled
    return;
    GameObject? hornPrefab = null;

    if (PrefabManager.Instance.GetPrefab("Tankard_Odin") != null)
    {
      hornPrefab = PrefabManager.Instance.CreateClonedPrefab(PrefabNames.VesselHorn, "Tankard_Odin");
    }
    else if (PrefabManager.Instance.GetPrefab("TankardAnniversary") != null)
    {
      hornPrefab = PrefabManager.Instance.CreateClonedPrefab(PrefabNames.VesselHorn, "TankardAnniversary");
    }
    else if (PrefabManager.Instance.GetPrefab("Tankard") != null)
    {
      hornPrefab = PrefabManager.Instance.CreateClonedPrefab(PrefabNames.VesselHorn, "Tankard");
    }
    else
    {
      LoggerProvider.LogError("All horn base prefabs (Tankard_Odin, TankardAnniversary, Tankard) were not found.");
    }

    if (!hornPrefab)
    {
      LoggerProvider.LogError("Failed to create cloned prefab for VesselHorn!");
      return;
    }

    // Register localization tokens directly with Jotunn so they never appear as [raw_token]
    LocalizationManager.Instance.AddToken("item_vessel_horn", "Horn of the Seas", false);
    LocalizationManager.Instance.AddToken("item_vessel_horn_desc", "[Left-Click] Teleport to boat | [Middle-Click] Bind to boat | [Right-Click] Teleport to Sacrificial Stones. <color=yellow>Blow this when ye've lost yer vessel, or when the mead makes ye forget where ye parked it.</color>", false);
    LocalizationManager.Instance.AddToken("valheim_vehicles_portal_not_supported", "Portals on boats are not supported. Use the Horn of the Seas to teleport to/from the boat.", false);

    var nv = PrefabRegistryHelpers.AddNetViewWithPersistence(hornPrefab);
    var zSyncTransform = hornPrefab.GetComponent<ZSyncTransform>() ?? hornPrefab.AddComponent<ZSyncTransform>();
    zSyncTransform.m_syncBodyVelocity = false;
    zSyncTransform.m_syncRotation = true;
    zSyncTransform.m_syncPosition = true;

    var itemDrop = hornPrefab.GetComponent<ItemDrop>();
    if (itemDrop == null)
    {
      itemDrop = hornPrefab.AddComponent<ItemDrop>();
    }
    if (itemDrop.m_nview == null)
    {
      itemDrop.m_nview = nv;
    }

    itemDrop.m_itemData.m_shared.m_name = "$item_vessel_horn";
    itemDrop.m_itemData.m_shared.m_description = "$item_vessel_horn_desc";
    itemDrop.m_itemData.m_shared.m_itemType = ItemDrop.ItemData.ItemType.Tool;
    itemDrop.m_itemData.m_shared.m_animationState = ItemDrop.ItemData.AnimationState.OneHanded;
    itemDrop.m_itemData.m_shared.m_equipDuration = 0;
    itemDrop.m_itemData.m_shared.m_maxStackSize = 1;
    itemDrop.m_itemData.m_shared.m_weight = 1.0f;
    itemDrop.m_itemData.m_shared.m_useDurability = false;
    itemDrop.m_itemData.m_shared.m_maxDurability = 100f;
    itemDrop.m_itemData.m_shared.m_consumeStatusEffect = null;
    itemDrop.m_itemData.m_shared.m_attack = null;
    itemDrop.m_itemData.m_shared.m_secondaryAttack = null;
    itemDrop.m_itemData.m_shared.m_blockPower = 0f;

    var itemConfig = new ItemConfig
    {
      Name = "$item_vessel_horn",
      Description = "$item_vessel_horn_desc",
      CraftingStation = "", // Hand-crafted, no workbench required
      MinStationLevel = 0,
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 5,
          Item = "Wood"
        },
        new RequirementConfig
        {
          Amount = 4,
          Item = "GreydwarfEye"
        },
        new RequirementConfig
        {
          Amount = 5,
          Item = "Resin"
        },
        new RequirementConfig
        {
          Amount = 10,
          Item = "Coins"
        }
      ]
    };

    var customItem = new CustomItem(hornPrefab, true, itemConfig);
    var success = ItemManager.Instance.AddItem(customItem);
    if (!success)
    {
      LoggerProvider.LogError($"Error occurred while registering {PrefabNames.VesselHorn}");
    }
    else
    {
      LoggerProvider.LogMessage($"Registered custom item {PrefabNames.VesselHorn} (Horn of Loki)");
    }
  }

  public override void OnRegister()
  {
    // Horn of the Seas disabled
    return;
  }
}