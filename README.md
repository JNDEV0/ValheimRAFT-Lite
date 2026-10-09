# ValheimRAFT Lite v4.11.7

**ValheimRAFT Lite** — Design and construct fully custom, navigable rafts, sailing ships, and movable floating bases in Valheim. Expand your vessels with building pieces, drop anchors, navigate turbulent seas, recruit Greydwarf deckhands, or take to the skies in flight!

> **Updated for Valheim 1.0+ (Unity 6 Engine Upgrade)**

**ValheimRAFT Lite** is a refined, streamlined, and quality-of-life focused edition of ValheimRAFT. While preserving full creative freedom to build custom modular vessels, **Lite** focuses on rock-solid stability, balanced survival progression, nautical immersion, and player ergonomics:
- **Build Menu & Nautical Progression**: Reorganized the Boat Hammer into clean, tiered progression tabs (**Resined**, **Nailed**, **Iron**, and **Misc**), with authentic seafaring terminology, balanced material costs, and cleaned up unstable piece prototypes.
- **Greydwarf Sailors**: Hire and command friendly Greydwarf crewmembers with sailor hats to man rowing benches, defend your vessel against hostiles, and provide shipboard labor.
- **Dynamic Water Wake & Waterline Controls**: Water wake foam and trailing rudder spray dynamically track the water surface and hug ocean waves, complete with customizable float height controls (**Max**, **Base**, and **Min** sliders) and wake toggles.
- **Persistent ZDOID Architecture**: Migrated vehicle piece tracking to persistent ZDOIDs, permanently fixing disappearing parts, sector transition desyncs, and long-voyage save issues.
- **Physics & Flight Stability**: Anchored kinematic locking (preventing vessels from drifting or falling into the void), rock impact damping, balanced sail speed scaling, and responsive flight controls.
- **Ergonomics & Quality of Life**: Bed respawn and map pin tracking that accurately follow moving vessels, locked helm hand IK, retractable rope ladders, on-deck farming, and a unified mechanism toggle menu (<kbd>Shift</kbd>+<kbd>E</kbd>).

---

## ☕ Support

Enjoying the mod? You can support development on Ko-fi:

👉 **[Support JNDEV0 on Ko-fi (https://ko-fi.com/jndev0)](https://ko-fi.com/jndev0)**

---

## ⚠️ Important "As-Is" Disclaimer

- Core features have been tested primarily for SINGLE-PLAYER.
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

## 🔧 Installation

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
           └── ValheimRAFT/
               ├── ValheimRAFT.dll
               ├── ValheimVehicles.dll
               ├── Zolantris.Shared.dll
               ├── ZdoWatcher.dll
               ├── DynamicLocations.dll
               ├── ServerSync.dll
               └── Assets/
                   └── Translations/
                       └── English/
                           └── valheimraft.json
   ```

---

## 💻 Source Code

- **GitHub Repository**: [https://github.com/JNDEV0/ValheimRAFT-Lite](https://github.com/JNDEV0/ValheimRAFT-Lite)
- **Upstream Repository**: [https://github.com/zolantris/ValheimMods](https://github.com/zolantris/ValheimMods)
- **Original Mod**: [ValheimRAFT by Sarcen](https://www.nexusmods.com/valheim/mods/1136)
- **Support JNDEV0 on Ko-fi**: [https://ko-fi.com/jndev0](https://ko-fi.com/jndev0)

---

## 🚀 Feature Updates & Differentiators (v4.11.7)

ValheimRAFT Lite diverges from upstream Zolantris-ValheimRAFT with a strong focus on gameplay stability, authentic nautical progression, performance optimization, and quality-of-life additions:

### 🌲 Greydwarf Sailors & Shipboard Labor
- **Crew Recruitment & Hiring**: Hire tamed Greydwarfs as loyal shipboard deckhands when carrying 10+ coins in your action bar, complete with cosmetic Viking sailor caps.
- **Resin Upkeep & Crew Management**: Sailors are maintained with passive resin offerings (5-minute cycle); easily dismiss or manage crew using <kbd>Shift</kbd>+<kbd>E</kbd> at the helm or interact prompt.
- **Shipboard Stationing & Fall Protection**: Stationed sailors stay securely rooted to the deck while underway, equipped with automatic hull-edge guards to prevent them from falling overboard into choppy seas.
- **Rowing Benches & Propulsion**: Craftable rowing seats provide designated crew stations that actively scale manual rowing propulsion.
- **Vessel Defense**: Stationed deckhands provide ranged rock-throwing defense against alerted hostile threats targeting your ship.

### 🔨 Reworked Build Progression & Cleaned-Up Ship Parts
- **Streamlined Boat Build Menu**: Cleaned up the Boat Hammer menu by eliminating redundant, inverted, and unstable piece prototypes that caused placement and collider conflicts, while preserving full backwards compatibility for existing structures.
- **Authentic Seafaring Terminology**: Piece names and descriptions have been overhauled with proper nautical terminology (keels, cutwaters, strakes, gunwales, deck prow planking, portholes, and rudder assemblies).
- **Tiered Survival Progression**: Building costs rebalanced across clear survival tiers:
  - **Resined (Primitive)**: Early-game wood and resin raft components.
  - **Nailed (Mid-Game)**: Bronze nails, fine wood, and core wood construction for sturdy karves and longships.
  - **Iron-Plated (Late-Game)**: Heavy iron-reinforced hulls, structural keels, and iron-banded deck planking for ocean voyaging.
- **Visual & Texture Polish**: Fixed UV mapping, parallax tiling, and iron reinforcement textures across keels, hulls, and deck prow pieces.

### ⛵ Simplified Propulsion & Sail Dynamics
- **Dynamic Ship Masts & Rigging**: Fully restored functional ship masts (Raft, Karve, Viking, and Drakkar sails) with tuned sailing power bonuses (1.5x up to 3.0x).
- **Adaptive Sail Furling**: Sails automatically furl and unfurl based on movement state (fully open under sail, reefed at half-sail, and automatically furled at anchor, in reverse, or when rowing).
- **Responsive Flight Mechanics**: Removed artificial takeoff ascent delays, delivering immediate, smooth vertical lift and predictable hover control.

### 🌊 Dynamic Water Wake & Waterline Height Controls
- **Interactive Water Level Controls**: Added in-game sliders in the mechanism toggle menu (<kbd>Shift</kbd>+<kbd>E</kbd>) for **Max Float Height** (prevents boat from hovering above water), **Base Float Height** (default resting waterline), and **Min Float Height** (prevents sinking), with automatic height decay on key release.
- **Dynamic Rudder Water Wake**: Water wake foam and trailing particle spray dynamically track the rudder waterline, projecting flat foam decals that realistically hug undulating ocean waves.
- **Balanced Wake Particle Scale**: Trailing particle spray maintained at full scale (1.0x) for extended rudder reach, with flat foam decals trimmed (0.25x emission rate and duration) for realistic, non-distracting nautical trails.
- **Water Wake Toggle**: Dedicated toggle in the mechanism options panel to turn ship water wake effects on or off on demand.
- **Visual Clutter Elimination**: Permanently eliminated angled water-cutting splash clutter, ocean-spanning sail reflection probe artifacts, and legacy square underwater shadow projectors.

### 🛡️ Persistent ZDOID Architecture & Sector Migration
- **Persistent ZDOID Tracking**: Migrated vehicle piece tracking from volatile ZDO objects to persistent ZDOIDs, permanently preventing ship pieces from disappearing or detaching upon zone unloads or game reloads.
- **Proactive Sector Migration**: Synchronizes all attached vehicle pieces in batch when traversing sector borders, eliminating desynchronization on long ocean voyages.
- **Reload & Teleport Hardening**: Fixed piece duplication, teleport clipping, and sector transition errors.
- **Graceful Scene Teardown**: Comprehensive null guards prevent game quit and logout crashes.

### ⚓ Helmsmanship, Ergonomics & Vessel Living
- **Steering Wheel HUD & Feedback**: Clear overhead text feedback and interaction gating at the helm ensure immediate, responsive wheel control.
- **Locked Hand Inverse Kinematics (IK)**: Helmsman hands stay locked securely to the wheel across all vessel speeds without animation jitter or running interference.
- **Accurate Bed Tracking**: Beds placed on moving vessels reliably retain player spawn points and map pin tracking, ensuring players respawn on deck rather than at sea.
- **On-Deck Agriculture**: Cultivated dirt floors and ship planter boxes allow growing and harvesting crops directly on moving vessels.
- **Retractable Rope Ladders**: Ladders automatically deploy when anchored or stationary, and retract upon underway movement.
- **Toggleable Snow Overlay**: Toggle snow coverage on flight-capable components and vehicle pieces directly from the mechanism options panel (<kbd>Shift</kbd>+<kbd>E</kbd>).
