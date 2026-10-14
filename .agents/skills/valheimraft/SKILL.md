---
name: valheimraft-lite
description: Comprehensive knowledge base, architecture cheatsheet, build/release workflows, and changelog history for developing and maintaining the ValheimRAFT Lite mod.
---

# ValheimRAFT Development & Maintenance Guide

This skill provides some contextual memory, architectural patterns starting reference files, build/packaging procedures, and historical changelogs for the **ValheimRAFT** project.

---

## Low Token Limit Workflow: "low token limit, add to future prompts"
When the user states "low token limit, add to future prompts" (or similar):
- **DO NOT** edit code, run builds, update versions, or expand `SKILL.md`.
- **ONLY** append the reported bug/issue/complaint into [`futureprompts.md`](file:///d:/SteamLibrary/steamapps/common/Valheim/ValheimMods_Repo/futureprompts.md) in the repository root, separating each issue with `---`.
- Keep the response short, acknowledge the issue was added, and await future prompt.

---

## 1. Project Overview & Repository Layout

- **Git Repository**: `D:\SteamLibrary\steamapps\common\Valheim\ValheimMods_Repo`
- **Test / Mod Manager**: r2modman (via local Thunderstore `.zip` import, replacing manual plugin folder copies)
- **Solution File**: `D:\SteamLibrary\steamapps\common\Valheim\ValheimMods_Repo\ValheimMods.sln`
- **Desktop Release Folder**: `C:\Users\User\Desktop\ValheimRAFT <version>\`

| **ValheimRAFT** | `src/ValheimRAFT/` | Entry point mod plugin (`ValheimRaftPlugin.cs`), ship boarding, dock components. |
| **ValheimVehicles** | `src/ValheimVehicles/` | Vehicle controllers (`VehicleManager`, `VehiclePiecesController`, `VehicleMovementController`), helm steering, propulsion, portals, ropes. |
| **ZdoWatcher** | `src/ZdoWatcher/` | ZDO lifecycle hooks, persistent ID lookups, server-client sync (`ZdoWatchController`, `ZdoPatch`). |
| **DynamicLocations** | `src/DynamicLocations/` | Dynamic location management for moving vessels. |
| **Shared** | `src/Shared/` | `Zolantris.Shared` utilities, RPC wrappers, math helpers. |

---

## 2. Build & Release Workflow

### Step 1: Version Bumping
Calculate version: count commits since version `5.3.1` (`7f91460`): `+0.0.1` per commit, `+0.1.0` every 10 commits, and `+1.0.0` every 100 commits (e.g. 4 commits since `5.3.1` -> `5.3.5`, 14 commits since `5.3.1` -> `5.4.5`).
Update version across:
1. `build/valheimraft_version.props`: `<Version>5.3.X</Version>`
2. `src/ValheimRAFT/Thunderstore/manifest.json`: `"version_number": "5.3.X"`
3. `src/ValheimRAFT/ThunderstoreBeta/manifest.json`: `"version_number": "5.3.X"`
4. `README.md` & `src/ValheimRAFT/README.md`: Update title and notes.

### Step 2: Compiling via MSBuild
Run MSBuild with `SolutionDir` defined:
```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" "D:\SteamLibrary\steamapps\common\Valheim\ValheimMods_Repo\src\ValheimRAFT\ValheimRAFT.csproj" -p:Configuration=Release -p:SolutionDir="D:\SteamLibrary\steamapps\common\Valheim\ValheimMods_Repo\\" -v:m
```

### Step 3: Desktop Packaging & r2modman Testing
Create directory `C:\Users\User\Desktop\ValheimRAFT <version>`:
1. **Thunderstore Zip** (`ValheimRAFT-<version>-Thunderstore.zip`):
   - Flat root containing `manifest.json`, `icon.png`, `README.md`, `CHANGELOG.md`, `LICENSE`, all DLLs/PDBs, `Assets/valheim-ladderclimb`, and `Assets/Translations/English/valheimraft.json` *(English ONLY)*.
   - **For local testing**: Import this `.zip` file directly into **r2modman** (`Settings` -> `Import local mod`). Never manually copy binaries into the game's `BepInEx/plugins` folder.
2. **NexusMods Zip** (`ValheimRAFT-<version>-NexusMods.zip`):
   - Contains a root `ValheimRAFT/` folder with all DLLs, PDBs, ServerSync, `Assets/valheim-ladderclimb`, English translation json ONLY, `CHANGELOG.md`, and `README.md`.
3. Unpacked `NexusMods/` and `Thunderstore/` folders alongside the zips for manual inspection.

### Step 4: Git Commit & Push

prepend(newer versions towards the top, older towards the bottom) this skill.md's "4. release history highlights" section with the latest changes when there is a version update.

### Step 5: Git Commit & Push
```powershell
git add -A
git commit -m "ValheimRAFT <version> release: <summary>"
git push origin dev
git checkout main
git merge dev
git push origin main
```

---

## 4. Release History Highlights

### v5.4.8
- **Prompt Repairs & In-Game Wake Sliders**: Added middle-click demolish confirmation safety and preserved inventory on death for the Boat Hammer. Suppressed `m_wet` snow overlay across all vessel pieces at altitude (>80m) and in flight mode. Fixed rope ladder upward climb character collision jitter. Fixed anchor mesh/rope visual desync on sector reload. Dynamically projected wake foam and spray particles to the undulating ocean surface. Added 4 live in-game sliders to the Boat Settings menu for wake foam and water spray rate and lifespan. Fixed steering wheel `[E]` release input bleed and instant re-grab. Updated sail descriptions with authentic speed values (+5.0, +7.5, +10.0, +12.0).

### v5.4.7
- **Tuned Rudder Rowing & Sail Speed Balancing**: Reduced Steering Oar row speed to 3 m/s (from 4 m/s) and Sternpost Rudder row speed to 5 m/s (from 7.5 m/s). Applied `Vector3.SmoothDamp` throttle acceleration (2.0s) to rudder linear force so rowing ramps up smoothly rather than snapping instantly. Adjusted sailing throttle transition time from 3.5s to 2.0s for responsive helm control. Rebalanced all sail tiers upward so the Raft sail immediately matches full rowing speed (Raft: 5.0, Karve: 7.5, Longship: 10.0, Drakkar: 12.0 m/s). Fixed HUD formatting on steering wheel to prevent integer ceiling-rounding.

### v5.4.6
- **Vanilla-Aligned Sailing Physics & Balanced Progression**: Decoupled sailing speed from rowing speed (removed `minProp` from half/full sail calculations). Rebalanced individual sail tier defaults (Raft: 3.0, Karve: 5.0, Longship: 7.5, Drakkar: 10.0) matching authentic vanilla ship cruising speeds. Clamped storm wind intensity scaling (max 0.55) to prevent runaway speed surges. Halved `MaxSailSpeed` ceiling from 30.0 to 15.0 m/s. Added wooden hull acceleration inertia (3.5s throttle smoothing), wave chop pitch resistance, and rudder turn drag.

### v5.4.5
- **Fixed Rope Ladder Sector Activation NullReferenceException**: Fixed a `NullReferenceException` thrown in `RopeLadderComponent.UpdateSteps()` during early sector loading and piece initialization (`VehiclePiecesController.AddPieceDataForComponents`). Added lazy-initialization and fallback guards for `m_attachPoint`, `m_stepObject`, and `m_collider`, wrapped step calculations in try-catch handlers, and deferred early initialization step updates until vehicle piece activation completes.

### v5.4.4
- **Fixed Rope Ladder Sector Reload & Extension**: Fixed rope ladders stuck at 1 rung upon sector unload/reload. Replaced restrictive world Euler angle checks with vertical deviation angles (supporting rolling vessels and sloping beach shorelines), filtered out mounting surface hits within 0.4m, and hooked automatic ladder extension upon piece addition and sector piece activation completion (`UpdateAllLadders`).
- **Fixed Ship Planter Crops Detaching & Floating**: Fixed crops (carrots, turnips, etc.) detaching and floating in space after growing or upon sector unload/reload. Captured vehicle parenting and relative transform offsets before sapling growth so mature pickables remain registered with `VehiclePiecesController`, maintain persistent `MBParentId` / `MBPositionHash`, and move with the vessel.
- **Existing Floating Crop Recovery**: Enhanced `CultivatableComponent.InitPiece` on game load and sector reload to automatically detect any detached or floating crops previously linked to a ship planter, reattaching them directly to their parent planter surface with persistent vehicle ZDO tracking.

### v5.4.3
- **Categorized Feature Documentation**: Reorganized mod documentation into distinct categories (Nautical Progression, Helm & Propulsion, Rope Ladders, Mobile Bases, Physics & Flight, Greydwarf Sailors, Architecture & Stability) for clarity.
- **Terminology Standardization**: Standardized all references to "Greydwarf Sailors" across manifest descriptions, in-game UI, and documentation.
- **Dedicated Changelog Tab Distribution**: Included `CHANGELOG.md` directly into the Thunderstore package root to properly populate Thunderstore and r2modman Changelog tabs.

### v5.4.2
- **Fixed Float Mode Ground Impact Violent Y-Kickup**: Fixed a critical calculation bug in `VehicleMovementController.UpdateColliderPositions` where touching the ground or rudder while in Float Mode over land was adding the full world ground elevation (`_lastHighestGroundPoint`, ~45–55m) directly to `position.y`, violently catapulting the vessel 50–55m into the sky and causing an infinite falling/kickup loop. Corrected the anti-fallthrough penetration logic, capped maximum upward displacement strictly to 2.0m, zeroed downward linear velocity upon ground impact to prevent bounces, and ensured land terrain elevation does not pollute water ballast `TargetHeight`.
- **Pruned Ghost Unloaded Ships & Filtered Non-Vehicle ZDOs**: Updated `VehicleRecallController.DoesVehicleExist` and `VehicleRecallController.GetVehicleLocation` to strictly validate vehicle prefabs (`WaterVehicleShip` and `LandVehicle`) and verify non-zero piece counts (`VehiclePiecesController.EnsurePiecesForVehicle(id).Count > 0`). Filtered `MapPinSync.UpdateVehiclePins` active vehicle lookups to ignore non-vehicle persistent IDs (such as toggle switches, rope ladder anchors, carrots, and dock components), completely eliminating ghost unloaded ship logs and cleaning up phantom map pins.

### v5.4.1
- **Top Release Clearance Tuning Slider**: Added "Top Release Clearance" slider (`LadderTopReleaseClearance`, range -1.2m to +0.8m, default -0.6m) to the Boat Settings toggle menu (`VehicleGui.cs`), eliminating character overshooting above the deck and enabling precision fine-tuning so feet land directly on deck planks upon top release.
- **Hardset Rung Alignment & Early Dismount**: Hardcoded ideal tested values for Jacob's / rope ladders: `ClimbOffset = -0.5f` (perfect rung alignment) and `TopReleaseOffset = -1.0f` (immediate top release right as hands crest the deck edge), removing both redundant sliders from the menu.
- **Coupled Speed Cadence & Streamlined Menu**: Hardcoded biomechanical speed ratio `AnimationSpeed = ClimbSpeed + 0.7f`, dynamically synchronizing climbing animation cadence with vertical movement speed while removing the standalone animation speed slider. Default climb speed updated to `1.5m/s` (animation speed `2.2`).

### v5.4.0
- **Embedded Ladder Climb Animation Asset**: Embedded `valheim-ladderclimb` asset bundle directly into `ValheimVehicles.dll` as a manifest resource with automatic memory stream fallback loading, guaranteeing the ladder climbing animation always loads even if external asset files are omitted during packaging or manual installs.
- **Top Release Deck Snapping & Forward Landing**: Fixed early dismount (`TopReleaseOffset`) to snap the player up to the boat deck exit surface (`m_exitPoint.position + forward * 0.5f + up * 0.25f`) with forward velocity, preventing the character from falling straight down into the sea/ground in front of the ladder when early release is triggered.
- **Unaltered Climb Offset Vertical Alignment**: Verified that "Climbing Offset" shifts the character's physical attachment vertically relative to the rungs without altering animation playback, phase scrubbing, or speed.
- **Immediate Mecanim Climb State Activation**: Forced immediate animator state activation (`anim.Play("attach_mast", 0, phase); anim.Update(0f);`) upon ladder attachment, eliminating transition freezes on frame 0.

### v5.3.9
- **Ship Planter Direct Hotbar Seed Planting**: Implemented `Interactable` and `Hoverable` on `CultivatableComponent` (`Ship Planter - Small` and `Large`), allowing players to select seeds on the hotbar (slots 1–8) and plant them directly into the soil bed without receiving "Use on what?" errors.
- **Dynamic Crop Resolution & Spacing Protection**: Added dynamic lookup from the Cultivator piece table (with comprehensive vanilla and modded crop fallbacks), normal-aligned placement rotation on rolling vessels, and 0.35m anti-stacking spacing protection to prevent accidental overlapping plantings.
- **Cultivator Tool Compatibility**: Enhanced `Plantable_Patch` and `Player_Patch` to validate both `NeedCultivated` and `Invalid` placement states on `CultivatableComponent` pieces via `GetComponentInParent`, ensuring smooth placement with both Cultivator tools and actionbar items.
- **Recommended Mods Updated**: Added blacks7ar's `SeedBed` (compact raised beds for confined boat decks) and Pfhoenix's `ODINFLIGHT` (gliders and wings to skydive from flight mode) to the recommended mods list in `README.md`.

### v5.3.8
- **Ladder Alignment & Early Dismount Tuning Sliders**: Added "Climbing Offset" (`LadderClimbOffset`) and "Top Release Offset" (`LadderTopReleaseOffset`) sliders to the Boat Settings toggle switch menu (`VehicleGui.cs`) backed by persistent BepInEx configurations.
- **Rung Alignment Tuning**: "Climbing Offset" slider allows fine-tuning the character's vertical offset (-0.5m to +0.5m) to visually align hands and feet with ladder rungs.
- **Top Release Early Dismount**: "Top Release Offset" slider allows tuning down the release height threshold (-1.0m to +0.8m) so climbing completes early and pops the character forward onto the boat surface without climbing unrealistically into thin air.

### v5.3.7
- **Uniform Ladder Climb Speed**: Removed all sprint / Shift speed multipliers during ladder climbing to enforce a consistent, uniform vertical velocity ascending and descending without stamina sprinting.
- **Fixed Inverted Animation Direction**: Fixed the inverted scrub phase calculation so climbing up plays the upward climbing motion and climbing down plays downward motion.
- **In-Game Speed Tuning Sliders**: Added two real-time sliders ("Ladder Climb Speed" and "Ladder Animation Speed") to the Boat Settings toggle switch menu (`VehicleGui.cs`) backed by persistent BepInEx configs (`LadderClimbSpeed` and `LadderAnimationSpeed` in `VehicleGlobalConfig.cs`), enabling real-time fine-tuning of movement speed versus animation cadence.
- **Decoupled Movement & Animation Cadence**: Decoupled spatial translation from animation scrub phase, preserving continuous rung alignment and seamless directional reversals during any speed configuration.

### v5.3.6
- **Jacob's / Rope Ladder Biomechanical IK & Gait**: Extracted standalone `valheim-ladderclimb` animation clip and implemented deterministic scrubbing gait (`LadderGait.cs`). Integrated analytical 2-bone arm IK (`Bend`) and rung snap targeting on Jacob's ladder / rope ladders (`RopeLadderComponent.cs`). Suppressed vanilla foot IK interference in `CharacterAnimEvent_Patch.cs` to prevent feet snapping downwards to terrain/water colliders.
- **Standalone Asset Separation**: Isolated `LadderClimb` into pure standalone asset bundle without embedding third-party shaders.

### v5.3.5
- **Map Pin Bed Target Guard**: `BoatBedSpawnController.VehicleHasOnboardBed` ensures the map icon and `[Spawn]` label never target boats that lack an onboard bed piece, resolving an issue where claiming a land bed near a newly built boat produced duplicate bed map pins.
- **Nautical Recipe Rebalances**:
  - Ship's Helm: Removed resin cost.
  - Steering Oar: Halved resin cost (6 -> 3).
  - Sternpost Rudder: Doubled round logs (12 -> 24), resin (6 -> 12), neck tails (2 -> 4), and added 2 bronze.
  - Sails (Raft & Karve): Halved dandelion cost (12 -> 6).
  - Ship Anchor: Halved dandelion cost (20 -> 10).
  - Rope Ladder / Jacob's Ladder: Halved dandelion cost (12 -> 6).
  - Deck Planking: Halved nail costs across nailed, iron-plated, and iron-reinforced planking pieces.
  - Nailed Keel Extension: Added 4 bronze nail requirement.
  - Portholes: Doubled wood requirement across standalone and wall/floor porthole variants.
  - Gunnery Binnacle / Cannon Control Center: Removed surtling core requirement.
- **Unity Project Cleanup**: Removed 73.9 MB of orphaned 3D models (naval cannon, nautilus, extra engines) and tank test scenes from Unity project.
- **Documentation Sync**: Synchronized root `README.md` with `src/ValheimRAFT/README.md` to ensure Thunderstore details page and GitHub display the latest guide and recommended mod list.
- **Rudder Forward Orientation Requirement**: Rudders must now face forward relative to the vessel heading, ensuring proper rudder alignment.

### v5.3.1
- **Greydwarf Sailors (Easter Egg)**: Added `VehicleGlobalConfig.EnableGreydwarfSailors` toggle (off by default). Turning off suppresses taming, hides coin hover text, blocks commands, and dismisses active shipboard sailors to the wild.
- **Curated Sailor Hats**: 19 curated working hats with `Ctrl`+`E` cycle next hat and fine-tuned position, rotation, and scale defaults.
- **Combat Defense Aim**: Rock-throwing trajectory predictive velocity leading and ballistic drop compensation.
- **Double-Tap Dismissal**: <kbd>Shift</kbd>+<kbd>E</kbd> on sailors requires confirmation within 3s.
- **Pruned Rowing Seats**: Completely removed obsolete rowing seat code and prefabs.
- **ServerSync ZRoutedRpc Patch**: Mono.Cecil binary patch to prevent `MissingFieldException` on settings changes in Valheim 1.0.12.
- **Unbreakable Boat Hammer**: Zero durability loss on boat hammer.
- **Dynamic Water Wake Controls**: Rudder water wake foam and waterline height sliders (<kbd>Shift</kbd>+<kbd>E</kbd>).
- **Persistent ZDOID Tracking**: Migrated vehicle piece tracking to persistent ZDOIDs, preventing detached parts on sector traversal.
