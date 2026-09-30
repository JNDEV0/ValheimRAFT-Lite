# ValheimRAFT Lite v4.6.6

**ValheimRAFT Lite** Design and construct fully custom, navigable rafts, sailing ships, and movable bases in Valheim. Expand your vessels with building pieces, drop anchors, navigate turbulent seas, or take to the skies in flight!

> **Updated for Valheim 1.0+ (Unity 6 Engine Upgrade)**

**ValheimRAFT Lite** is a refined, streamlined, and quality-of-life focused fork of ValheimRAFT. While preserving the full freedom of building custom vessels, the **Lite** edition focuses on stability, balance, and player ergonomics:
- **📯 Horn of the Seas**: Introduces a craftable emergency travel tool that binds to your steering wheel, allowing you to recall or teleport directly back to your ship's deck from anywhere in the world (even with metals), as well as the map's initial spawn area.
- **🔨 Build Menu & Progression Overhaul**: Organizes the Boat Hammer into tiered progression tabs (**Resined**, **Nailed**, **Iron**, and **Misc**), with rebalanced material costs, logical part ordering, and consistent names.
- **🧹 Pruned Redundancy & Bloat**: Removes broken or glitchy pieces (unstable inverted hulls, redundant masts) and decouples unused terrain systems to reduce overhead.
- **⚓ Physics & Flight Stability**: Adds anchored kinematic locking (preventing unattended vessels from drifting or falling into the void), rock impact damping, realistic rudder clamp limits, simplified propulsion and balanced speeds.
- **🛠️ Diagnostics & Mechanism Controls**: A unified mechanism options menu (<kbd>Shift</kbd>+<kbd>E</kbd>) to easily toggle anchors, flight modes, damage immunity, and snow overlays without cluttering your console.

---

## ☕ Support

Enjoying the mod? You can support development on Ko-fi:

☕ **[Support JNDEV0 on Ko-fi (https://ko-fi.com/jndev0)](https://ko-fi.com/jndev0)**

---

## ⚠️ Important "As-Is" Disclaimer

- Core features have been tested mainly for SINGLE-PLAYER.
- ValheimRAFT is a massive and complex codebase containing advanced mechanics. Not all extended features or edge-case interactions have been exhaustively tested. 
- **Offered "As-Is"**: Provided freely and without warranty. Always back up your character and world saves before testing! Players and modders are warmly invited to report issues, bugs, submit suggestions or Pull Requests, and help maintain this mod at the GitHub repository: 
  👉 **[https://github.com/JNDEV0/ValheimRAFT-Lite](https://github.com/JNDEV0/ValheimRAFT-Lite)**

---

## 📜 Attribution & Open Source History

- **Original Creator**: **Sarcen** created the original ValheimRAFT mod that defined ship building in Valheim, and generously released it as open source in 2023 under the GPLv3 license.
- **Modern Rewrite & Architecture**: **Zolantris** ([zolantris/ValheimMods](https://github.com/zolantris/ValheimMods)) completely overhauled the mod's architecture, adding modular vehicle systems, convex hull boundary physics, and expansive features.

---

## 📦 Requirements & Dependencies

To use this mod, ensure you have the following required dependencies installed:
1. **[denikson-BepInExPack_Valheim-5.4.2350+](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)**
2. **[ValheimModding-Jotunn-2.30.0+](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/v/2.30.0/)**
3. **[ValheimModding-JsonDotNET-13.0.4+](https://thunderstore.io/c/valheim/p/ValheimModding/JsonDotNET/)**

---

## 📥 Installation

### Option A: Thunderstore / r2modman / Gale (Recommended)
1. Install via your mod manager of choice.
2. Dependencies are automatically resolved and installed.

### Option B: NexusMods / Vortex / Manual Installation
1. If using Vortex, install and enable the zip archive directly.
2. If installing manually, extract `ValheimRAFT` into your `Valheim/BepInEx/plugins/` directory:
   ```
   Valheim/
   └── BepInEx/
       └── plugins/
           ├── ValheimRAFT/
           │   ├── ValheimRAFT.dll
           │   ├── ValheimVehicles.dll
           │   ├── Zolantris.Shared.dll
           │   ├── ZdoWatcher.dll
           │   ├── DynamicLocations.dll
           │   ├── ServerSync.dll
           │   └── Assets/
           │       └── Translations/
           │           └── English/
           │               └── valheimraft.json
           └── Newtonsoft.Json.dll (from ValheimModding-JsonDotNET)
   ```

---

## 💬 Source Code

- **GitHub Repository**: [https://github.com/JNDEV0/ValheimRAFT-Lite](https://github.com/JNDEV0/ValheimRAFT-Lite)
- **Upstream Repository**: [https://github.com/zolantris/ValheimMods](https://github.com/zolantris/ValheimMods)
- **Original Mod**: [ValheimRAFT by Sarcen](https://www.nexusmods.com/valheim/mods/1136)
- **Support JNDEV0 on Ko-fi**: [https://ko-fi.com/jndev0](https://ko-fi.com/jndev0)

---

## 📜 Changelog (v4.6.6 - ValheimRAFT Lite Initial Release)

### 📯 Horn of the Seas (Helm Attunement & Fast Travel)
- **Craftable Boat Teleport Tool**: Added the **Horn of the Seas** (`$item_vessel_horn`), crafted by hand without needing a workbench.
- **Kept on Death**: Retained in inventory upon death, allowing you to immediately respawn and channel a return teleport to your ship to recover your gear.
- **Boat Teleport (`[Left-Click]`)**: Hold left-click for 3 seconds anywhere in the world to channel a teleport straight to your attuned boat helm (supports metal transport and heavy loads). Plays the `"Drink"`/`"Toast"` emote while channeling.
- **Steering Wheel Attunement (`[Middle-Click]`)**: Aim directly at your vessel's Vehicle Wheel within interaction range (≤ 3.5m) and hold middle-click for 3 seconds to attune the horn to the vessel.
- **Sacrificial Stones Teleport (`[Right-Click]`)**: Hold right-click for 3 seconds anywhere in the world to channel a teleport directly to the game's initial spawn area (Sacrificial Stones).
- **Disabled Boat Portals**: Portals placed on boats are refunded with an on-screen notice, preventing portal coordinate desyncs; the Horn of the Seas takes over ship fast travel.

### 🔨 Vehicle Hammer & Build Progression Overhaul
- **Categorized Build Tabs**: Reorganized vehicle pieces into clear progression categories:
  - **Resined**: Early-game wood and resin raft and hull pieces.
  - **Nailed**: Mid-game bronze nail, fine wood, and core wood construction pieces.
  - **Iron**: Late-game iron-plated hulls, iron-reinforced planking, iron guard rails, and solid iron structural beams.
  - **Misc**: Steering wheel, rudders, anchors, ladders, mechanisms, fertile soil, and utilities.
- **Sorted & Streamlined Parts**: Positioned heavy Solid Iron parts at the very end of the build menu, renamed "2x1x8" components to "Long", and grouped iron-plated deck planking logically before guard rails.
- **Pruned Unstable Pieces**: Removed redundant inverted hulls, unrigged prototype masts, and glitchy custom sails from the build menu to prevent placement errors and collider bugs, while preserving full backwards compatibility for existing placed structures in saved worlds.
- **Repairs**: Added vehicle piece repair functionality directly to the Boat Hammer.
- **Recipe Corrections**: Fixed recipes such as Fertile Soil (uses Coal instead of Charcoal) and Iron Cutwater (3 items: 24 RoundLog, 8 Iron, 4 IronNails) to avoid UI index exceptions.

### ⚓ Physics, Navigation & Flight Stability
- **Anchored Kinematic Lock**: When anchored (or auto-anchored upon leaving the vessel unattended), the vehicle Rigidbody locks to kinematic, zeroing velocities. Completely eliminates airborne ships drifting, sinking, or falling into the void (`y < -5000m`) during zone unloads.
- **Rudder Control & Turn Speeds**: Implemented authentic rudder angle clamping, tier-based turning responsiveness, and physical rudder blade alignment.
- **Dynamic Sail Furling & Speed Scaling**: Sails automatically furl and unfurl based on movement state (furled at anchor/reverse/rowing, 50% at half sail, 100% at full sail). Capped extreme storm wind speeds to prevent physics destabilization.
- **Collision Damping**: Smooth impulse damping upon impacting shoreline rocks, preventing boat shuddering and console log flooding.
- **Ocean Wave Tilt (Opt-In)**: Smoothly aligns hull pitch and roll with ocean swells for enhanced nautical immersion.
- **Instant Physical Anchors**: Integrated anchor drops and reels with runtime steering wheel controls (<kbd>Shift</kbd> toggles anchor in all modes).

### 🪜 Ergonomics, Ladders & Controls
- **Rope Ladder Fast Climbing & IK**: Ladders automatically deploy when anchored or stationary in water. Integrated full inverse kinematics (IK) for natural foot arcs, knee hints, and symmetric climbing.
- **Steering Wheel Auto-Binding & HUD**: Cleaned up helm interaction text, resolved camera/treadmill animation glitches on mounting, and added floating anchor warnings if attempting to sail while anchored.
- **Bed Spawn & Ownership**: Fixed bed respawning on moving vessels, ensuring players wake up on deck rather than falling into the ocean.

### ⚙️ Mechanism Toggle Switch Menu (<kbd>Shift</kbd>+<kbd>E</kbd>)
- **Unified Options Panel**: Press <kbd>Shift</kbd>+<kbd>E</kbd> at the helm or mechanism to configure flight modes, damage immunity toggles, snow overlay filters, and diagnostic loop logging without entering console commands.

### 🚀 Performance & "Lite" Cleanliness
- **Decoupled Bloat**: Stripped unused terrain modifications and prototype scripts; replaced bundled JSON libraries with official mod dependencies (`ValheimModding-JsonDotNET`).
- **Throttled Synchronization**: Optimized background piece sync to a ≤2ms frame slice budget, eliminating micro-stutters near large bases.
- **Save-Cleanup Protection**: Shielded vehicle ZDO registries from Ashlands+ world save purge cycles.
- **Clean Console Output**: Defaulted diagnostic spam off, registered TMP Unicode fonts, and added clear `[ValheimRAFT Lite 4.6.6]` startup logging.


