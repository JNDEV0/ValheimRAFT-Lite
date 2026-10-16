# Changelog

All notable changes to ValheimRAFT are documented here.
For full commit history see
the [GitHub Releases](https://github.com/zolantris/ValheimMods/releases) page.

## [5.4.9] - 2026-10-15

### Added

- **Procedural 3D Anchor Chain Links**: Replaced the flat billboard line renderer with high-fidelity 3D interlocking iron chain links (`chain_link.prefab`):
  - Retains authentic heavy anchor chain thickness (~0.2m) with dark pitted iron material matching the anchor body.
  - Dynamically extends when dropping anchor and contracts when reeling in, smoothly deactivating drop links when retrieved in the cradle.
  - Features an efficient object pool managing up to 600 links across both the deck winch-to-roller chute run and the vertical drop to the seabed.
  - Zero-jitter synchronization updated in `LateUpdate` to ensure the chain stays locked to the anchor and vessel during ocean wave motion.

## [5.4.8] - 2026-10-14

### Added

- **Smokeless Campfire for Boat Hammer**: Added a seaworthy smokeless campfire piece to the Boat Hammer menu (conveniently organized right before the Ship Planters in the build palette), cloned from the vanilla campfire (`fire_pit`) with all smoke spawners stripped to prevent blinding smoke plumes while the ship is in motion. Balanced crafting cost (8 Stone, 2 Core Wood, 4 Thistle with `Recover = true`), full weather/cooking station support, and authentic sailor lore description.
- **Boat Hammer Middle-Click Demolish Confirmation**: Intercepts middle-click dismantle on ValheimRAFT pieces. Displays a HUD confirmation prompt (`"Dismantle [PieceName]? Click again to confirm"`) with a 2.5s window before dismantling, preventing accidental destruction of boat hull and deck pieces.
- **Boat Hammer Preserved on Death**: Added quest item protection (`m_questItem = true`) to the Boat Hammer so it remains safely in inventory on death and cannot be lost in a tombstone.

### Changed

- **Water Wake Effects Tuning & Simplified UI**: Baked optimal water wake parameters into global configuration and emitter fallbacks:
  - *Wake Foam Rate*: 2.0x
  - *Wake Foam Lifespan*: 3.0x
  - *Wake Water Spray Rate*: 1.0x
  - *Wake Water Spray Lifespan*: 1.0x
  - *Wake Water Spray Distance*: 1.0x
  - *Wake Water Spray Arc*: 3.0x
  - Removed the 6 individual tuning sliders from the Boat Settings UI menu in favor of a clean, dedicated on/off toggle (`ShipWaterWakeEnabled`) that live-updates emitters immediately.
- **Cleaned Up Boat Settings Sliders**: Removed the Top Release Clearance slider (`LadderTopReleaseClearance`) from the UI while keeping the optimal default clearance (-0.6m) preserved.

### Fixed

- **Universal Boat Snow Overlay & Movement Recast Freeze**: Fixed snow overlay toggle being ignored by vanilla wood pieces (`woodwall`, `wood_stepladder`, `wood_beam`), floors, and OdinArchitect parts:
  - Extended vehicle piece identification to all 138+ vehicle pieces using comprehensive persistent/parent IDs and vessel bounds (`VehicleOwnership_Patches.IsVehiclePiece`).
  - Resolved `WearNTear.UpdateCover` recasting raycasts on movement: froze roof evaluations upon initial initialization for all vehicle pieces so rocking and pitching vessels never repeatedly recast or flip roof states.
  - Added full shader and keyword management across piece renderers (`_ADDSNOW_ON` keyword toggle, `_SnowLevel`, `_AddSnow`, and `MaterialMan` persistent value binding) ensuring all building piece types cleanly show or hide snow upon toggle without flickering or lingering.
- **Water Wake Live Updates & Rudder Waterline Contact**: Fixed water wake and spray sliders not applying in real time. Connected dynamic parameter updates directly to rudder pieces in `VehicleGui` and `LateUpdate`. Replaced restrictive height offsets with rudder collider vertical bounds (`waterY >= minY`) to eliminate wave crest stutter when submerged, and positioned spray emission precisely at the waterline contact height along the rudder blade.
- **Fireplace Heightmap Out-of-Range Guard**: Added prefix guard on `Fireplace.CheckUnderTerrain` to skip terrain burial checks for shipboard fireplaces, preventing `ArgumentOutOfRangeException: Index was out of range` when sailing deep oceans or flying.
- **Suppressed Altitude & Flight Snow Overlay**: Fixed boat pieces turning blindingly white with snow when ascending near mountain altitudes or flying. When altitude exceeds 80m or flight mode is active, `m_wet` snow accumulation is automatically suppressed across all vehicle pieces and materials are refreshed to clear lingering snow buildup.
- **Rope Ladder Upward Climb Camera Jitter**: Fixed camera stutter and character-rung collision jitter when climbing rope ladders upward from lower deck sections, water, or midair. Dynamically disables player capsule collisions against all ladder step and frame colliders (`Physics.IgnoreCollision`) while climbing, cleanly restoring collisions upon dismount.
- **Anchor Visual Desync on Sector Reload**: Fixed visual desync where walking away (sector unload) and returning (sector reload) caused the physical anchor mesh and chain to appear dropped on the seabed even though the helm HUD reported `[Anchor Retrieved]`. Synchronized visual anchor state upon sector piece activation completion (`VehiclePiecesActivator.OnActivationComplete`) and ensured anchor transforms, rigidbodies, and rope line renderers are forcibly synchronized with the vessel's anchor state on start and sector re-entry.
- **Dynamic Water Surface Tracking for Wake Foam & Spray**: Fixed flat wake foam and spray particles floating detached in midair or submerging when sailing across ocean waves. Replaced static Y=0 calculations with dynamic water surface sampling (`WaterVolume.GetWaterSurface` and `Floating.GetLiquidLevel`), adjusting living wake and foam particle heights every frame to undulate naturally with wave crests and troughs.
- **Steering Wheel Release `[E]` Input Bleed & Re-grab Prevention**: Fixed input bleed where pressing `[E]` (`Use`) to release the steering wheel / helm simultaneously triggered an interaction with whatever interactable was under the crosshair (causing instant wheel re-grabbing, accidental station/chest opening, or ladder latching). Added an active wheel detachment input consumption buffer (0.35s cooldown) and prefix guards on `Player.Interact` and `Player.UpdateHover` to exclusively consume the release keypress and prevent instant secondary interactions.
- **Updated Sail Descriptions with Authentic Speeds**: Updated English localization descriptions (`valheimraft.json`) for all 4 sail tiers to reflect their upgraded propulsion values: Raft Sail (`+5.0`), Karve Sail (`+7.5`), Longship Sail (`+10.0`), and Drakkar Sail (`+12.0`).

## [5.4.7] - 2026-10-12

### Changed

- **Tuned Rudder Rowing Speeds**: Reduced Steering Oar row speed to 3 m/s (from 4 m/s) and Sternpost Rudder row speed to 5 m/s (from 7.5 m/s) for tighter harbor maneuvering and authentic rowing pacing.
- **Rudder Linear Force SmoothDamp Acceleration**: Added smooth throttle acceleration (`Vector3.SmoothDamp` over 2.0s) to rowing linear force, eliminating sudden jerk when engaging row or reverse gear.
- **Responsive Sailing Acceleration**: Tightened sailing throttle interpolation `SmoothDamp` transition time from 3.5s to 2.0s for responsive yet weighted helm control.
- **Upgraded Sail Progression Speeds**: Rebalanced all sail tiers upward so the Raft sail immediately matches full rowing speed, and advanced sails provide superior velocity:
  - Raft Sail: 5.0 m/s (up from 3.0 m/s, matching former Karve tier).
  - Karve Sail: 7.5 m/s (up from 5.0 m/s, matching former Longship tier).
  - Longship Sail: 10.0 m/s (up from 7.5 m/s, matching former Drakkar tier).
  - Drakkar Sail: 12.0 m/s (up from 10.0 m/s).
- **Steering Wheel HUD Formatting**: Fixed rowing and sailing speed display formatting (`{val:0.#}`) so values are never ceiling-rounded to the next whole integer.

## [5.4.6] - 2026-10-12

### Changed

- **Decoupled Sailing from Rowing**: Removed baseline rowing speed (`minProp`) from half and full sail propulsion calculations. Sailing speed is now purely governed by your vessel's sails and wind angle.
- **Rebalanced Sail Tier Progression**: Raised individual sail propulsion defaults (Raft: 3.0, Karve: 5.0, Longship: 7.5, Drakkar: 10.0) so each sail tier provides a distinct, noticeable speed increase matching authentic vanilla ship tiers.
- **Controlled Storm Wind Surge**: Narrowed interpolated storm wind intensity scaling (`Mathf.Lerp(0.38f, 0.55f, windIntensity)`), giving an authentic ~20% storm speed boost without runaway double-speed surges.
- **Halved Max Sailing Speed**: Reduced `MaxSailSpeed` cap from 30.0 m/s to 15.0 m/s, preventing vessels from ramping uncontrollably off wave crests.
- **Vessel Inertia & Throttle Smoothing**: Increased sail force interpolation smoothing time from 1.0s to 3.5s, giving ships heavy wooden hull momentum rather than instant arcade acceleration.
- **Wave Chop & Pitch Resistance**: Added incline swell resistance where climbing steep ocean waves drains forward momentum slightly, restoring the sensation of plowing into heavy seas.
- **Rudder Turn Scrub**: Hard rudder deflection now naturally scrubs up to 25% forward sailing momentum, mimicking realistic underwater rudder drag.

## [5.4.5] - 2026-10-12

### Fixed

- **Rope Ladder Sector Activation NullReferenceException**: Fixed a `NullReferenceException` thrown in `RopeLadderComponent.UpdateSteps()` during early sector loading and piece initialization (`VehiclePiecesController.AddPieceDataForComponents`). Added lazy-initialization and fallback guards for `m_attachPoint`, `m_stepObject`, and `m_collider`, wrapped step calculations in try-catch handlers, and deferred early initialization step updates until vehicle piece activation completes.

## [5.4.4] - 2026-10-12

### Fixed

- **Rope Ladder Sector Reload & Anchor Deployment**: Fixed rope ladders failing to extend or remaining stuck at 1 rung upon sector unload/reload. Replaced restrictive world Euler angle checks with vertical deviation angles (allowing ladders to dangle and extend even when vessels roll or sit beached on sloping shorelines), filtered out mounting surface hits within 0.4m, and hooked automatic ladder extension upon piece addition and sector piece activation completion (`UpdateAllLadders`).
- **Ship Planter Crops Detachment & Floating**: Fixed crops (carrots, turnips, onions, etc.) detaching from `Ship Planter - Small` and `Large` and floating in space after growing or upon sector unload/reload. Captured vehicle parenting and relative transform offsets before sapling growth so mature pickables remain registered with `VehiclePiecesController`, maintain persistent `MBParentId` / `MBPositionHash`, and move with the vessel.
- **Existing Floating Crop Recovery**: Enhanced `CultivatableComponent.InitPiece` on game load and sector reload to automatically detect any detached or floating crops previously linked to a ship planter, reattaching them directly to their parent planter surface with persistent vehicle ZDO tracking.

## [5.4.3] - 2026-10-11

### Changed

- **Documentation Organization**: Completely restructured the README into clean, categorized sections covering Nautical Progression & Construction, Helm & Propulsion, Rope Ladders & Climbing, Mobile Base Systems (Beds, Portals, Map Pins), Flight & Physics, Greydwarf Sailors, and Core Stability.
- **Terminology Alignment**: Standardized terminology across manifest descriptions, documentation, and in-game UI to consistently reference "Greydwarf Sailors".
- **Package Distribution**: Packaged dedicated `CHANGELOG.md` directly into the Thunderstore archive root to populate the Changelog tab in Thunderstore and mod managers (r2modman).

## [5.4.2] - 2026-10-11

### Fixed

- **Float Mode Ground Impact Kickup**: Fixed violent +50-55m Y-axis launch when touching ground/rudder in Float Mode over land. Corrected the anti-fallthrough penetration math, capped maximum lift strictly to 2.0m, zeroed downward linear velocity upon ground impact to prevent bounces, and ensured land terrain elevation does not pollute water ballast `TargetHeight`.
- **Ghost Unloaded Ships Pruning**: Strictly validate vehicle prefabs (`WaterVehicleShip` and `LandVehicle`) and verify non-zero piece counts (`EnsurePiecesForVehicle(id).Count > 0`). Filtered `MapPinSync` active vehicle lookups to ignore non-vehicle persistent IDs (toggle switches, rope ladder anchors, carrots, and dock components), completely eliminating ghost unloaded ship logs and cleaning up phantom map pins.

## [5.4.1] - 2026-10-11

### Added

- **Top Release Clearance Tuning Slider**: Added "Top Release Clearance" slider (`LadderTopReleaseClearance`, range -1.2m to +0.8m, default -0.6m) to the Boat Settings toggle menu (`VehicleGui.cs`), eliminating character overshooting above the deck and enabling precision fine-tuning so feet land directly on deck planks upon top release.

### Changed

- **Hardset Rung Alignment & Early Dismount**: Hardcoded ideal tested values for Jacob's / rope ladders: `ClimbOffset = -0.5f` (perfect rung alignment) and `TopReleaseOffset = -1.0f` (immediate top release right as hands crest the deck edge), removing both redundant sliders from the menu.
- **Coupled Speed Cadence & Streamlined Menu**: Hardcoded biomechanical speed ratio `AnimationSpeed = ClimbSpeed + 0.7f`, dynamically synchronizing climbing animation cadence with vertical movement speed while removing the standalone animation speed slider. Default climb speed updated to `1.5m/s` (animation speed `2.2`).

## [5.4.0] - 2026-10-11

### Added

- **Embedded Ladder Climb Animation Asset**: Embedded `valheim-ladderclimb` asset bundle directly into `ValheimVehicles.dll` as a manifest resource with automatic memory stream fallback loading, guaranteeing the ladder climbing animation always loads even if external asset files are omitted during packaging or manual installs.

### Fixed

- **Top Release Deck Snapping & Forward Landing**: Fixed early dismount (`TopReleaseOffset`) to snap the player up to the boat deck exit surface (`m_exitPoint.position + forward * 0.5f + up * 0.25f`) with forward velocity, preventing the character from falling straight down into the sea/ground in front of the ladder when early release is triggered.
- **Immediate Mecanim Climb State Activation**: Forced immediate animator state activation (`anim.Play("attach_mast", 0, phase); anim.Update(0f);`) upon ladder attachment, eliminating transition freezes on frame 0.

## [5.3.9] - 2026-10-11

### Added

- **Ship Planter Direct Hotbar Seed Planting**: Implemented `Interactable` and `Hoverable` on `CultivatableComponent` (`Ship Planter - Small` and `Large`), allowing players to select seeds on the hotbar (slots 1–8) and plant them directly into the soil bed without receiving "Use on what?" errors.
- **Dynamic Crop Resolution & Spacing Protection**: Added dynamic lookup from the Cultivator piece table (with comprehensive vanilla and modded crop fallbacks), normal-aligned placement rotation on rolling vessels, and 0.35m anti-stacking spacing protection to prevent accidental overlapping plantings.

### Fixed

- **Cultivator Tool Compatibility**: Enhanced `Plantable_Patch` and `Player_Patch` to validate both `NeedCultivated` and `Invalid` placement states on `CultivatableComponent` pieces via `GetComponentInParent`, ensuring smooth placement with both Cultivator tools and actionbar items.

## [5.3.8] - 2026-10-11

### Added

- **Ladder Alignment & Early Dismount Tuning Sliders**: Added "Climbing Offset" (`LadderClimbOffset`) and "Top Release Offset" (`LadderTopReleaseOffset`) sliders to the Boat Settings toggle switch menu (`VehicleGui.cs`) backed by persistent BepInEx configurations.

## [5.3.7] - 2026-10-11

### Added

- **In-Game Speed Tuning Sliders**: Added two real-time sliders ("Ladder Climb Speed" and "Ladder Animation Speed") to the Boat Settings toggle switch menu.

### Changed

- **Uniform Ladder Climb Speed**: Removed all sprint / Shift speed multipliers during ladder climbing to enforce a consistent, uniform vertical velocity ascending and descending.
- **Fixed Inverted Animation Direction**: Fixed the inverted scrub phase calculation so climbing up plays the upward climbing motion and climbing down plays downward motion.

## [5.3.6] - 2026-10-11

### Added

- **Jacob's / Rope Ladder Biomechanical IK & Gait**: Extracted standalone `valheim-ladderclimb` animation clip and implemented deterministic scrubbing gait (`LadderGait.cs`). Integrated analytical 2-bone arm IK (`Bend`) and rung snap targeting on Jacob's ladder / rope ladders (`RopeLadderComponent.cs`). Suppressed vanilla foot IK interference in `CharacterAnimEvent_Patch.cs` to prevent feet snapping downwards to terrain/water colliders.

## [5.3.5] - 2026-10-10

### Fixed

- **Map Pin Bed Target Guard**: `BoatBedSpawnController.VehicleHasOnboardBed` ensures the map icon and `[Spawn]` label never target boats that lack an onboard bed piece, resolving an issue where claiming a land bed near a newly built boat produced duplicate bed map pins.

### Changed

- **Nautical Recipe Rebalances**: Rebalanced material costs across Boat Hammer categories (Ship's Helm, Steering Oar, Sternpost Rudder, Sails, Ship Anchor, Rope Ladder, Deck Planking, Keel Extensions, Portholes).

## [5.3.1] - 2026-10-09

### Added

- **Greydwarf Sailors (Easter Egg)**: Added `VehicleGlobalConfig.EnableGreydwarfSailors` toggle. Hire friendly sailors (10 coins, 1 resin upkeep) with 19 unique hats to defend your ship with rocks and haul loose items.
- **Dynamic Water Wake Controls**: Rudder water wake foam and waterline height sliders (<kbd>Shift</kbd>+<kbd>E</kbd>).
- **Persistent ZDOID Architecture**: Migrated vehicle piece tracking to persistent ZDOIDs, fixing disappearing parts, sector transition desyncs, and save issues.

---

## [4.2.2] - 2026-03-14

### Fixed

- fixes land vehicles. They now work again.
- Warning: Still a bit of sway when idle/braked on uneven terrain.

## [4.2.2] - 2026-03-14

### Fixed

- ensure that sails request a sync to other peers
- ensure peers can send a message that sail has been updated.
- ensure sail data is not corrupted
- ensure a failed claim of a sail can be recalled to request ownership.

## [4.2.1] - 2026-03-14

### Fixed

- fixes a constant stutter issue with valheim raft on non-dedicated servers and
  likely fixes dedicated server stuttering. Issue introduced in 4.2.0 with
  server sync fixes
- ensure that syncs only happen on vehicles for servers within 250 unit
  radius of an active player. This ensures only active vehicles are
  force synced to these positions.

## [4.2.0] — 2026-03-13

### Added

- Vehicle Name field — set a custom name per-vehicle, saved to ZDO and shown on
  the minimap pin as `V:<name>`.
- Custom minimap pin icons for all vehicle types (water, land, air) sourced from
  the asset bundle.
- `AddTextInputRow` UI helper — reusable TMP input field that scales to its
  container, with `onChanged` and `onEndEdit` callbacks.
- Documentation overhaul — new tutorials for Basics, Cannons, Power System, and
  a Changelog.

### Fixed

- Map pin sync stability improvements and NRE guards on server reconnect.
- Vehicle config panel input text rendering white-on-white (was inheriting label
  colour instead of input text colour).
- Server sync and GUI inconsistencies across clients.

---

## [4.1.3] — 2025-12-04

### Fixed

- Archive step in the release build pipeline.

---

## [4.1.2] — 2025-12-03

### Fixed

- Optimised `HumanoidOnAttackTrigger` patch to better support external mods that
  hook the same method.
- Versioning chores and dependency bump to Jotunn 2.27.

---

## [4.1.1] — 2025-10-20

### Fixed

- Sail normals rendering incorrectly on some mesh variants.
- Flight stability issues when sails were attached to a flying vehicle.

---

## [4.1.0-beta.1] — 2025-10-14

### Added

- Vehicle chunk boundary limits — constrain the physics bounds of a vehicle to a
  defined area using Boundary Chunk Addition prefabs.

---

## [4.0.4] / [4.0.3] — 2025-10-12

### Fixed

- Removed the 4×4 Dirt Planter prefab which was accidentally included in the
  release build.

---

## [4.0.2] — 2025-10-12

### Added

- Vehicle boundary safety — prevents hull collision bounds from expanding
  dangerously when pieces are placed at extreme offsets.

---

## [4.0.1] — 2025-10-03

### Fixed

- Unity 6 compatibility for Water Vehicle ships and Air Vehicles.

---

## [4.0.0] — 2025-08-25

### Added

- Full Unity 6 compatibility pass across all vehicle types.
- Power System: Eitr Power Generator, Eitr Power Storage (battery), Power
  Pylons (BETA), and Drain/Charge Activator Plates.
- Swivels — rotate or move any placed prefab using Mechanism Toggle triggers.
  Requires power by default.
- Mechanism Toggle (mechanism switch) — configure swivels, open the vehicle
  debug panel, and trigger docking sequences.

### Changed

- Swivels now require a connected power network and charge to activate (
  configurable).

---

## [3.7.2] — 2025-08-24

### Added

- Prefab naming snapshots — prefab names are now stable across saves.
- Various quality-of-life improvements to piece placement and snapping.

---

## [3.7.1-beta] — 2025-08-16

### Added

- New hull pieces and updated existing hull geometry.

---

## [3.6.6] — 2025-07-31

### Fixed

- Steering wheel not detecting the player when standing at certain positions.

---

## [3.6.5] — 2025-07-30

### Fixed

- Null reference exception on equip patch affecting some item interactions.

---

## [3.6.4] — 2025-07-26

### Added

- Cannonballs (solid bronze, explosive blackmetal) added to the items section.

### Fixed

- Multiple stability fixes across the 3.6.x cannon release series.

---

## [3.6.3-beta] / [3.6.1-beta.2] / [3.6.0-beta.1] — 2025-07-21

### Added

- Fixed Cannons — mount to any vehicle or base. Controlled from the Steering
  Wheel on a vehicle or from the Cannon Control Center off-vehicle.
- Cannon Control Center (telescope) — fire-control station for fixed cannons.
  Manages all cannons within its discovery radius (default 15 units). Supports
  four directional groups (forward / port / starboard / aft).
- Handheld Cannon — mobile cannon with auto-aim mechanics. Fully multiplayer
  compatible.
- Cannonball ammunition — solid (piercing) and explosive (AoE blackmetal)
  variants.

---

## [3.5.4] — 2025-07-03

### Fixed

- Compile error in swivel stability changes.

---

## [3.5.3] — 2025-06-24

### Fixed

- Missing prefabs added back to the build.
- Scale of corner floor hull pieces corrected.

---

## [3.5.2] — 2025-06-23

### Added

- Improved vehicle docking — child vehicles now align anchor-to-anchor with lerp
  smoothing.
- Mast ZDO syncing across clients.

### Fixed

- Sail mesh and rotation sync issues.

---

## [3.4.3] — 2025-05-30

### Fixed

- Eitr Drain Conduit multiplayer sync issues.
- Swivel multiplayer sync reliability.
- Power network logic sync across clients.

---

## [3.4.0-beta.1] — 2025-05-10

### Added

- Swivel GUI improvements — configure rotation axes, speed, and power
  requirements from a dedicated panel.
- Swivel rotators with visible connection lines.

---

## [3.3.0] — 2025-04-29

### Added

- Custom masts with crow's nest — tier 1–3 mast variants with a climbable crow's
  nest platform.
- Vehicle Save & Spawn system — save a vehicle blueprint to disk and spawn it
  later anywhere.
  See [Vehicle Storage Tutorial](./docs/tutorials/Tutorial_VehicleStorage.md).

---

## [3.2.1] — 2025-04-28

### Added

- Russian and Chinese Simplified translations.

---

## [3.2.0] — 2025-04-28

### Added

- Vehicle Hammer — dedicated build tool replacing the vanilla hammer for all
  ValheimRAFT pieces. All vehicle prefabs now live under its tabbed build menu.
- Custom floatation height per vehicle — set a fixed height or use the
  Floatation Prefab for per-vehicle control.
- Toggle switch persistence — mechanism switch state is now saved to ZDO.

### Changed

- Removed V1 raft support. Use `vehicle recover` to migrate old V1 rafts.
- `Custom` floatation mode is now locked to local vehicles only to prevent sync
  bugs on shared ships.

---

## [3.1.0] — 2025-04-04

### Added

- Collision object parenting optimisations — large ships with many pieces load
  faster.

---

## [3.0.8] / [3.0.7] / [3.0.5] / [3.0.4] — 2025-04-01

### Fixed

- NRE in collection rebuild bounds on ship load.
- Convex hull optimisation — large ships compute collision bounds significantly
  faster.
- Valheim game update regressions.
- Land vehicle stability and tread rotation logic.

---

## [3.0.0-beta] — 2025-02-01

### Added

- Land Vehicle (tank) — wheeled/tread ground vehicle. Drive it overland or load
  it onto a water vehicle for transport.
- Tank tread rotation logic — treads animate based on vehicle velocity and
  turning.
- Boundary alignment tools for large vehicle builds.

---

## [2.5.3] — 2025-01-20

### Added

- Anchor HUD — shows anchor state on screen when near your vehicle.

### Fixed

- Stability tweaks for anchor physics and multiplayer sync.

---

## [2.5.0-beta.1] — 2025-01-12

### Added

- Anchors — physically anchor the ship when the anchor touches the seabed or
  terrain. Synced across clients. Requires swamp-tier materials.
- Window Porthole walls and floors (2×2, 4×4, 6×4, 8×4 iron variants) — see
  through hull sections with half the health of solid iron pieces.
- New hull geometry: corner floors (2×2, 2×4, 2×8 left/right wood and iron).

---

## [2.4.3] — 2024-11-30

### Fixed

- Player control responsiveness tweaks while helming a vehicle.
- Ship lean cosmetics towards wind direction.
- Boat ownership transfer stability.

---

## [2.4.2] — 2024-11-24

### Fixed

- Water damping minimum and maximum values clamped correctly — ships no longer
  oscillate uncontrollably in rough weather.

---

## [2.4.1] — 2024-11-11

### Fixed

- Auto-ascend/descend logic for flying ships.
- Target height offset when flying causing incorrect vertical positioning.

---

## [2.4.0] — 2024-11-10

### Added

- Water ballast mass system — hull pieces affect buoyancy dynamically.
- Flight mode — vehicles can be made to fly using ballast controls.
- Vehicle map pins — vehicles now show on the minimap.
- Translocations (teleport vehicle to player position, admin only).
- Rope Ladder — climbable ladder that extends to the ground or seafloor
  automatically.

---

## [2.4.0-beta.1] — 2024-11-09

### Added

- Water mesh masking (water opacity bucket) — hide water inside or around a
  sealed hull to create submarine-like interiors.
- Dynamic location system for world-space vehicle placement.

---

## [2.2.4] / [2.2.3] / [2.2.2] — 2024-08-25

### Fixed

- Ship floatation logic regressions.
- Flying vehicle height and ballast stability.
- Sail asset and ladder tweaks.
- Hull piece sizing corrections.

---

## [2.2.0] — 2024-07-06

### Added

- Fully sealable hull — new rib, corner, prow, and seal pieces allow building a
  watertight ship.
- Yggdrassil iron reinforced prefab variants for hull walls and floors.

### Fixed

- Rudder turning speed optimisations.
- Sail rotation, physics, and anchor behaviour tweaks.
- Many hull wall and floor collision bug fixes.

---

## [2.1.1] / [2.1.0] — 2024-05-22

### Fixed

- Sail 3-point mesh not rendering correctly.
- `raftcreative` rotation fix — no longer rotates the vehicle unexpectedly.
- Multiplayer RPC stability improvements.

---

## [2.0.3] / [2.0.2] / [2.0.1] / [2.0.0] — 2024-05-17

### Added

- V2 WaterVehicle — completely rewritten vehicle system with proper physics, ZDO
  syncing, and hull floatation.
- `vehicle recover` command — migrates V1 raft pieces to the new V2 system.
- Boarding Ramp — extendable ramp (1–50+ units) for boarding ships from the
  water or shore.
- Dirt Floor (1×1, 2×2) — grow vanilla Valheim crops on your ship.

### Changed

- Plugin GUID changed from the original ValheimRaft GUID to
  `zolantris.ValheimRAFT`. Existing V1 ships must be recovered with
  `vehicle recover`.

### Fixed

- ZDO logging spam on rope anchors removed.
- Multiplayer ship sync regressions from 2.0.x patch series.

---

## [1.6.14] — 2024-01-21

### Fixed

- Local server (LAN host) ship rendering on connecting clients.

---

## [1.6.13] — 2024-01-20

### Fixed

- Rendering regression introduced in 1.6.12 for locally hosted servers.

---

## [1.6.12] — 2024-01-07

### Fixed

- Nexus/Vortex folder structure install path.

---

## [1.6.11] — 2023-12-30

### Added

- Vulkan graphics API support (Windows and Linux).

### Fixed

- Collider placement moved below the lowest deck to stop clutter items flipping
  the boat.
- Config options added for collider sensitivity.

---

## [1.6.9] / [1.6.10] — 2023-12-30

### Fixed

- Anchor now correctly stops a flying raft.
- Sail component NRE guards to prevent log spam.

---

## [1.6.7] / [1.6.8] — 2023-12-29

### Added

- Boat size-based sailing calculations — larger ships are proportionally faster.
- Admin-only build mode config option.

### Fixed

- Thunderstore folder structure for manual installs.

---

## [1.6.5] / [1.6.6] — 2023-12-27

### Fixed

- Sail mesh generation errors.
- Rope anchor duplicate WearNTear NRE spam.

---

## [1.6.4] — 2023-12-26

### Added

- Sail area calculations — sail speed is now based on the cubic volume of each
  sail.

---

## [1.6.3] — 2023-12-24

### Fixed

- Stable dedicated server and client raft rendering.

---

## [1.6.2] — 2023-12-21

### Fixed

- Server stability for zone-based raft updates.
- Plugin patch compatibility fixes for mods hooking ValheimRaft classes.

