---
name: valheimraft
description: Comprehensive knowledge base, architecture cheatsheet, troubleshooting guide, build/release workflows, and changelog history for developing and maintaining the ValheimRAFT mod for Valheim 1.0.12 (Unity 6).
---

# ValheimRAFT Development & Maintenance Guide

This skill provides full contextual memory, architectural patterns, critical bug resolutions, build/packaging procedures, and historical changelogs for the **ValheimRAFT** project (specifically the Valheim 1.0.12 / Unity 6 overhaul).

---

## 1. Project Overview & Repository Layout

- **Git Repository**: `D:\SteamLibrary\steamapps\common\Valheim\ValheimMods_Repo`
- **Active Game Plugin Directory**: `d:\SteamLibrary\steamapps\common\Valheim\BepInEx\plugins\ValheimRAFT`
- **Solution File**: `D:\SteamLibrary\steamapps\common\Valheim\ValheimMods_Repo\ValheimMods.sln`
- **Desktop Release Folder**: `C:\Users\User\Desktop\ValheimRAFT <version> for Valheim 1.0.12\`

### Key Sub-Projects
| Project | Path | Role |
| :--- | :--- | :--- |
| **ValheimRAFT** | `src/ValheimRAFT/` | Entry point mod plugin (`ValheimRaftPlugin.cs`), ship boarding, dock components. |
| **ValheimVehicles** | `src/ValheimVehicles/` | Vehicle controllers (`VehicleManager`, `VehiclePiecesController`, `VehicleMovementController`), helm steering, propulsion, portals, ropes. |
| **ZdoWatcher** | `src/ZdoWatcher/` | ZDO lifecycle hooks, persistent ID lookups, server-client sync (`ZdoWatchController`, `ZdoPatch`). |
| **DynamicLocations** | `src/DynamicLocations/` | Dynamic location management for moving vessels. |
| **Shared** | `src/Shared/` | `Zolantris.Shared` utilities, RPC wrappers, math helpers. |

---

## 2. Core Architectural Discoveries & Critical Gotchas

### A. The Ashlands+ Save-Cleanup Wipeout
- **Mechanism**: In Valheim Ashlands+, world saves call `ZDOMan.GetSaveClonePerChunk()`, which creates temporary clone ZDOs, writes them to disk, and then invokes `ZDOMan.SaveCleanup()`. `SaveCleanup` calls `zdo.Reset()` to recycle save clones.
- **The Fix**: In both `Zdo_Patch.ZDO_Reset` and `VehiclePiecesController.RemoveZDO`, verify whether the live ZDO is still active in `ZDOMan`:
  ```csharp
  if (ZDOMan.instance != null && ZDOMan.instance.GetZDO(zdo.m_uid) != null)
  {
      return; // Live ZDO is still active in ZDOMan; do NOT remove or unregister!
  }
  ```

### B. Valheim Sector Migration Limitation & Missing Pieces
- **Mechanism**: Valheim's internal `ZDO.SetSector(SectorIndex)` only runs `AddToSector` and `RemoveFromSector` for portal prefabs. Non-portal pieces (beds, chests, crafting benches, walls) are never migrated across sectors by vanilla Valheim code when moved!
- **The Fix**:
  - In `VehiclePiecesController.SetPrefabWorldPosition` and `MigratePortalSectorInZdoMan`, detect when `oldSector != newSector`.
  - For **all** pieces, call `ZDOMan.instance.RemoveFromSector(zdo, oldSector)` and `ZDOMan.instance.AddToSector(zdo, newSector)`.
  - For **portals**, migrate `ZDOMan.instance.m_portalObjects` dictionary and call `ZDOMan.instance.SetDirtyPortals()`.
  - In `ForceUpdateAllPiecePositions`, compute `pieceWorldPos` using `VehicleZdoVars.MBPositionHash` even if the GameObject is unloaded (`nv == null`), ensuring bed spawn points and map icons follow the moving ship.

### C. ServerSync MissingFieldException Patch (Mono.Cecil)
- **Problem**: In Valheim 1.0.12 (Unity 6), `ZRoutedRpc.Everybody` changed from a `public static readonly long` field to a compile-time literal / const `0L`. Calling code referencing `ldsfld int64 ZRoutedRpc::Everybody` throws `System.MissingFieldException: Field not found: .ZRoutedRpc.Everybody Due to: Using static instructions with literal field`.
- **The Permanent Fix**: `ServerSync.dll` across the repository dependencies and builds is binary patched using Mono.Cecil to replace `ldsfld int64 ZRoutedRpc::Everybody` instruction sequences with `ldc.i8 0L`. Always ensure the patched binary (SHA256 `EB93074622090E0B27FFACE68FEB2A8B746BF963398532A3A5DDF74A8CA53F8D`) is deployed.

### D. Greydwarf Sailors (Easter Egg Mechanism Toggle)
- **Easter Egg Toggle**: Gated behind `VehicleGlobalConfig.EnableGreydwarfSailors` (`false` by default) in the mechanism menu ("Greydwarf Sailors (Easter Egg)").
- **When Disabled**:
  - Hover text on Greydwarfs to hire them with coins is completely suppressed (`""`).
  - Player interaction and item use (`Interact` and `UseItem`) on Greydwarfs are rejected.
  - Active shipboard sailors immediately revert to wild monsters (`Dismiss(null)`): they drop carried items, remove hats, revert faction to `ForestMonsters`, unparent from the vessel, launch into the water, and alert monster AI.
  - Console command `vehicle hiresailor` and remote crew restore checks are blocked.
- **When Enabled**:
  - Hire with 10+ coins in the action bar within 250m of a ship.
  - 19 curated working hats cyclable via <kbd>Ctrl</kbd>+<kbd>E</kbd> or giving helmet item.
  - Tuned hat offset defaults: `Pos (0, 0.003, 0)`, `Rot (-90, 0, -180)`, `Scale 0.025`.
  - Predictive velocity leading and ballistic drop compensation for thrown rocks; melee claw swings disabled for shipboard defenders.
  - Double-tap dismissal confirmation: <kbd>Shift</kbd>+<kbd>E</kbd> requires repeat press within 3s.
  - Passive resin upkeep (1 resin from any vessel chest per 5 minutes) and loot gathering.

### E. Pruning Stale Rowing Seats
- Rowing benches (`GreydwarfRowingSeatComponent` and `GreydwarfRowingSeatPrefab`) have been permanently removed. Sailors roam the decks freely and do not occupy stationary rowing seats. Rowing speed is determined by rudders (`GetRowingSpeed()`).

### F. Localization Architecture & Build Packaging Guardrails
- **3-Tier English Safety Net**: `English/valheimraft.json` is embedded into `ValheimRAFT.dll` as an assembly `<EmbeddedResource>` (`LogicalName="English.valheimraft.json"`), ensuring the mod never displays raw variable tags even with zero loose files on disk.
- **CRITICAL Build Guardrail - Do NOT Include Optional Translations in Distribution Builds**:
  - Standard distribution packages (NexusMods and Thunderstore zips) must contain **ONLY** `Assets/Translations/English/valheimraft.json`.
  - Additional language files (`Chinese`, `Russian`, `Spanish`, `Portuguese_Brazilian`, `French`, `German`, `Polish`, `Japanese`, `Korean`) are maintained **exclusively** in the repository's top-level `optional_translations/` folder.
  - **NEVER bundle `optional_translations/` or extra language folders into release zips**; keeping them separate prevents folder clutter, suspicion, and mod bloat.
  - International users download their specific language folder from the GitHub repository and place it into `BepInEx/plugins/ValheimRAFT/Assets/Translations/<Language>/valheimraft.json`.

---

## 3. Build & Release Workflow

### Step 1: Version Bumping
Calculate version: count commits since version `5.1.7` (`0.0.1` per commit, rolling over every 10 commits by `0.1.0`).
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

### Step 3: Deploy to Local Game Plugin Directory
Copy compiled binaries from `Build/bin/` to `d:\SteamLibrary\steamapps\common\Valheim\BepInEx\plugins\ValheimRAFT\`:
- `ValheimRAFT.dll` & `.pdb`
- `ValheimVehicles.dll` & `.pdb`
- `ZdoWatcher.dll` & `.pdb`
- `DynamicLocations.dll` & `.pdb`
- `Zolantris.Shared.dll` & `.pdb`
- `ServerSync.dll`
- `Assets/Translations/English/valheimraft.json` *(English ONLY)*

### Step 4: Desktop Packaging
Create directory `C:\Users\User\Desktop\ValheimRAFT <version> for Valheim 1.0.12`:
1. **NexusMods Zip** (`ValheimRAFT-<version>-NexusMods.zip`):
   - Contains a root `ValheimRAFT/` folder with all DLLs, PDBs, ServerSync, English translation json ONLY, and README.
2. **Thunderstore Zip** (`ValheimRAFT-<version>-Thunderstore.zip`):
   - Flat root containing `manifest.json`, `icon.png`, `README.md`, `LICENSE`, all DLLs/PDBs, and `Assets/Translations/English/valheimraft.json` *(English ONLY)*.
3. Unpacked `NexusMods/` and `Thunderstore/` folders alongside the zips for manual review.

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
