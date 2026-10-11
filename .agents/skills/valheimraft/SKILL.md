---
name: valheimraft-lite
description: Comprehensive knowledge base, architecture cheatsheet, build/release workflows, and changelog history for developing and maintaining the ValheimRAFT Lite mod.
---

# ValheimRAFT Development & Maintenance Guide

This skill provides some contextual memory, architectural patterns starting reference files, build/packaging procedures, and historical changelogs for the **ValheimRAFT** project.

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
   - Flat root containing `manifest.json`, `icon.png`, `README.md`, `LICENSE`, all DLLs/PDBs, and `Assets/Translations/English/valheimraft.json` *(English ONLY)*.
   - **For local testing**: Import this `.zip` file directly into **r2modman** (`Settings` -> `Import local mod`). Never manually copy binaries into the game's `BepInEx/plugins` folder.
2. **NexusMods Zip** (`ValheimRAFT-<version>-NexusMods.zip`):
   - Contains a root `ValheimRAFT/` folder with all DLLs, PDBs, ServerSync, English translation json ONLY, and README.
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
