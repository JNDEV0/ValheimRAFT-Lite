using ValheimVehicles.Controllers;
using BepInEx.Configuration;
using ValheimVehicles.Components;
using ValheimVehicles.Helpers;
using ValheimVehicles.SharedScripts.Validation;
using Zolantris.Shared;
namespace ValheimVehicles.BepInExConfig;

public class VehicleGlobalConfig : BepInExBaseConfig<VehicleGlobalConfig>
{
  // sounds for VehicleShip Effects
  public static ConfigEntry<bool> EnableShipWakeSounds = null!;
  public static ConfigEntry<bool> EnableShipInWaterSounds = null!;
  public static ConfigEntry<bool> EnableShipSailSounds = null!;

  // updaters
  public static ConfigEntry<float> ServerRaftUpdateZoneInterval = null!;
    public static ConfigEntry<bool> FastMultiplayerSync = null!;
  public static ConfigEntry<bool> ForceShipOwnerUpdatePerFrame { get; set; }

  // localization
  public static ConfigEntry<string> ModLanguage = null!;
  private const string VehicleLocalizationKey = $"{VehicleGlobalBaseKey}:Localization";

  // Horn of Loki
  public static ConfigEntry<float> HornTeleportCooldownSeconds = null!;
  public static ConfigEntry<float> HornChannelDurationSeconds = null!;
  private const string VehicleHornKey = $"{VehicleGlobalBaseKey}:VesselHorn";

  // section keys
  private const string VehicleGlobalBaseKey = "VehicleGlobal";
  private const string VehicleSoundKey = $"{VehicleGlobalBaseKey}:Sound";
  private const string VehicleGlobalUpdateKey = $"{VehicleGlobalBaseKey}:Updates";
  private const string VehicleDamageKey = $"{VehicleGlobalBaseKey}:Damage";
  private const string VehicleSnowKey = $"{VehicleGlobalBaseKey}:Snow";
  private const string VehicleMaterialKey = $"{VehicleGlobalBaseKey}:Materials";

  // no material cost toggle (false by default: regular material costs)
  public static ConfigEntry<bool> NoMaterialCost = null!;

  // boat damage toggles (all false by default: no damage to boat parts)
  public static ConfigEntry<bool> BoatDamageEnv = null!;
  public static ConfigEntry<bool> BoatDamageMobs = null!;
  public static ConfigEntry<bool> BoatDamagePlayer = null!;

  // snow overlay toggle (false by default)
  public static ConfigEntry<bool> BoatSnowOverlay = null!;

  // greydwarf sailors toggle (false by default: optional easter egg)
  public static ConfigEntry<bool> EnableGreydwarfSailors = null!;
  private const string VehicleSailorsKey = $"{VehicleGlobalBaseKey}:EasterEggs";

  public override void OnBindConfig(ConfigFile config)
  {
    CreateSoundConfig(config);
    CreateVehicleUpdaterConfig(config);
    CreateLocalizationConfig(config);
    CreateHornConfig(config);
    CreateDamageConfig(config);
    CreateSnowConfig(config);
    CreateMaterialConfig(config);
    CreateSailorsConfig(config);
  }

  private static void CreateHornConfig(ConfigFile config)
  {
    HornTeleportCooldownSeconds = config.BindUnique(VehicleHornKey,
      "HornTeleportCooldownSeconds", 10.0f,
      ConfigHelpers.CreateConfigDescription(
        "Cooldown in seconds applied after successfully teleporting to a vessel using the Horn of Loki.",
        false, false, new AcceptableValueRange<float>(0f, 60f)));

    HornChannelDurationSeconds = config.BindUnique(VehicleHornKey,
      "HornChannelDurationSeconds", 3.0f,
      ConfigHelpers.CreateConfigDescription(
        "Channel duration in seconds required to activate the Horn of Loki.",
        false, false, new AcceptableValueRange<float>(1.0f, 10f)));
  }

  private static void CreateLocalizationConfig(ConfigFile config)
  {
    ModLanguage = config.BindUnique(VehicleLocalizationKey,
      "Language",
      "Auto",
      ConfigHelpers.CreateConfigDescription(
        "Language used by ValheimRAFT. 'Auto' follows the game's selected language if an optional translation is installed, else defaults to English.",
        false, false));
  }

  private static void CreateVehicleUpdaterConfig(ConfigFile config)
  {
    ForceShipOwnerUpdatePerFrame = config.BindUnique("Rendering",
      "Force Ship Owner Piece Update Per Frame", false,
      ConfigHelpers.CreateConfigDescription(
        "Forces an update during the Update sync of unity meaning it fires every frame for the Ship owner who also owns Physics. This will possibly make updates better for non-boat owners. Noting that the boat owner is determined by the first person on the boat, otherwise the game owns it.",
        true, true));


    FastMultiplayerSync = config.BindUnique(VehicleGlobalUpdateKey,
      "FastMultiplayerSync",
      false,
      ConfigHelpers.CreateConfigDescription(
        "When enabled on multiplayer servers, tightens piece sync interval to 1.0s for reduced sync latency at higher network load. When disabled, uses ServerRaftUpdateZoneInterval (default 3.0s).",
        true, true));

    ServerRaftUpdateZoneInterval = config.BindUnique(VehicleGlobalUpdateKey,
      "ServerRaftUpdateZoneInterval",
      3f,
      ConfigHelpers.CreateConfigDescription(
        "Allows Server Admin control over the update tick for the RAFT location. Larger Rafts will take much longer and lag out players, but making this ticket longer will make the raft turn into a box from a long distance away.",
        true, true, new AcceptableValueRange<float>(1, 30f)));
  }

  private static void CreateSoundConfig(ConfigFile config)
  {
    EnableShipSailSounds = config.BindUnique(VehicleSoundKey, "Ship Sailing Sounds", true,
      "Toggles the ship sail sounds.");
    EnableShipWakeSounds = config.BindUnique(VehicleSoundKey, "Ship Wake Sounds", true,
      "Toggles Ship Wake sounds. Can be pretty loud");
    EnableShipInWaterSounds = config.BindUnique(VehicleSoundKey, "Ship In-Water Sounds",
      true,
      "Toggles ShipInWater Sounds, the sound of the hull hitting water");

    EnableShipSailSounds.SettingChanged += VehicleManager.UpdateAllShipSounds;
    EnableShipWakeSounds.SettingChanged += VehicleManager.UpdateAllShipSounds;
    EnableShipInWaterSounds.SettingChanged += VehicleManager.UpdateAllShipSounds;
  }

  private static void CreateDamageConfig(ConfigFile config)
  {
    BoatDamageEnv = config.BindUnique(VehicleDamageKey,
      "BoatDamageEnv", false,
      ConfigHelpers.CreateConfigDescription(
        "Allow environmental damage (rocks, water wear, impacts) to boat parts. Disabled by default.",
        false, false));

    BoatDamageMobs = config.BindUnique(VehicleDamageKey,
      "BoatDamageMobs", false,
      ConfigHelpers.CreateConfigDescription(
        "Allow enemy/mob attack damage to boat parts. Disabled by default.",
        false, false));

    BoatDamagePlayer = config.BindUnique(VehicleDamageKey,
      "BoatDamagePlayer", false,
      ConfigHelpers.CreateConfigDescription(
        "Allow player attack/weapon damage to boat parts. Disabled by default. Does not affect deconstruction via hammer.",
        false, false));
  }

  private static void CreateSnowConfig(ConfigFile config)
  {
    BoatSnowOverlay = config.BindUnique(VehicleSnowKey,
      "BoatSnowOverlay", false,
      ConfigHelpers.CreateConfigDescription(
        "Allow snow overlay on boat parts when at high altitude / mountain biomes. Disabled by default to prevent visual flickering during flight/movement.",
        false, false));
  }
  private static void CreateMaterialConfig(ConfigFile config)
  {
    NoMaterialCost = config.BindUnique(VehicleMaterialKey,
      "NoMaterialCost", false,
      ConfigHelpers.CreateConfigDescription(
        "When enabled, all boat hammer pieces cost only 1 Wood. When disabled, standard tiered materials are required.",
        false, false));
    NoMaterialCost.SettingChanged += (_, _) =>
    {
      VehicleMaterialCostController.SetOneWoodCost(NoMaterialCost.Value);
    };
  }

  private static void CreateSailorsConfig(ConfigFile config)
  {
    EnableGreydwarfSailors = config.BindUnique(VehicleSailorsKey,
      "EnableGreydwarfSailors", false,
      ConfigHelpers.CreateConfigDescription(
        "Enable Greydwarf Sailors (Easter Egg). When disabled, Greydwarfs cannot be hired as sailors and any active shipboard sailors become wild. Disabled by default.",
        false, false));
    EnableGreydwarfSailors.SettingChanged += (_, _) =>
    {
      if (!EnableGreydwarfSailors.Value)
      {
        RaftGreydwarfSailorComponent.DismissAllSailors();
      }
    };
  }
}
