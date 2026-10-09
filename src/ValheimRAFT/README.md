# ValheimRAFT Lite v5.1.7

**ValheimRAFT Lite** — Design and construct fully custom, navigable rafts, sailing ships, and movable floating bases in Valheim. Expand your vessels with building pieces, drop anchors, navigate turbulent seas, recruit Greydwarf deckhands, or take to the skies in flight!

> **Updated for Valheim 1.0+ (Unity 6 Engine Upgrade)**

**ValheimRAFT Lite** is a refined, streamlined, and quality-of-life focused edition of ValheimRAFT. **Lite** focuses on stability, balanced survival progression, nautical immersion, and adding content i felt was missing from the original:
- **Build Menu & Nautical Progression**: Reorganized the Boat Hammer into clean, tiered progression tabs (**Resined**, **Nailed**, **Iron**, and **Misc**), with authentic seafaring terminology, balanced material costs.
- **Greydwarf Sailors**: Hire friendly Greydwarf crewmembers that will throw rocks to defend your vessel against hostiles.
- **Dynamic Water Wake & Waterline Controls**: Water wake foam and trailing rudder spray track the water surface, customizable float height controls (**Max**, **Base**, and **Min** sliders) and updated toggles.
- **Persistent ZDOID**: Migrated vehicle piece tracking to persistent ZDOIDs, fixing disappearing parts, sector transition desyncs, and save issues.
- **Physics & Flight Stability**: automatic Anchoring on leaving the ship, kinematic locking (preventing vessels from drifting or falling offmap), rock impact damping physics(no bulldozing terrain with the boat), simplified sail/rowing speed scaling, responsive flight controls, improved water physics.
- **Ergonomics & Quality of Life**: Bed respawn, portal and map pin tracking that accurately follow moving vessels, locked helm hand IK, automatic retractable and extending rope ladders, and various other fixes.
- **Helm & Rudder Safety**: Sailing now requires a rudder, with reduced turn angles. If the steering wheel or rudder is destroyed while underway, the vessel automatically halts safely instead of sailing away.
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