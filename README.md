# ValheimRAFT Lite SP v5.3.1

**ValheimRAFT Lite (Single Player)** — Fixed various issues and expanded features. Design and construct custom navigable vessels and movable bases in Valheim. ship bed, improved physics, drop anchor, recruit Greydwarf deckhands, and take to the skies in flight!

![ValheimRAFT Lite](https://raw.githubusercontent.com/JNDEV0/ValheimRAFT-Lite/main/src/ValheimRAFT/Thunderstore/icon.png)

> ⚠️ **Single Player Focus**: Not tested for multiplayer. For multiplayer support, please use the original [zolantris/ValheimMods](https://github.com/zolantris/ValheimMods). This version has reworked material costs to build parts, to build freely without cost enable "hammer mode" in world modifiers setting on world/save selection screen, or enable "no material cost (1 wood)" setting by building a mechanism toggle part to open the menu.

**ValheimRAFT Lite** is a streamlined edition of ValheimRAFT aiming to improve progression, expand on missing features, enhance simulation stability and nautical immersion. new material costs to build parts can be toggled in the mechanism toggle options menu.
- **Build Menu & Nautical Progression**: Reorganized the Boat Hammer into clean, tiered progression tabs (**Resined**, **Nailed**, **Iron**, and **Misc**), with authentic seafaring terminology and balanced material costs. Removed land vehicle pieces, unrelated additional plugins packaged into the original version. removed inverted parts.
- **Helm & Rudder**: Sailing now requires a rudder, with authentic clamped turn angles. If the steering wheel or rudder is destroyed while underway, the vessel automatically halts safely instead of sailing away.
- **Simple Propulsion**: Rudder determines rowing speed, sails determine sail speed. No weight calculations, you can fly a stone castle now if you like.
- **Water Wake & Waterline Controls**: Water wake foam and trailing rudder spray dynamically track the water surface, with customizable float height controls (**Max**, **Base**, and **Min** sliders) in the mechanism toggle menu.
- **Physics & Flight Stability**: Automatic anchoring when leaving the ship, kinematic locking (preventing vessels from drifting or falling off the map), rock impact damping physics (no bulldozing terrain with the boat), responsive flight controls, and improved ship water physics.
- **Quality of Life**: Working bed, portal and map pin tracking that accurately follow moving vessels, locked wheel hand IK and activation responsiveness, automatically deploying and retracting rope ladders and anchor, enable/disable boat damage in the mechanism toggle menu to fit your playstyle, various other fixes.
- **No Jittering on boat Movement**: Moving the boat no longer causes jittering back and forth of the boat, snow overlay on parts are off by default (to fix the visual glitch when flying the boat/airship).
- **Greydwarf Sailors (optional, enable in mechanism toggle menu)**: Hire friendly Greydwarf crewmembers (10 coins, 1 resin upkeep) that throw rocks to defend your vessel against hostiles. sailors respect closed doors so you can keep them to areas you want them otherwise will walk around the boat. sailors consume resin from any container every few minutes, and will also pickup items dropped on the boat and place them into chests for you. sailors will eventually mutiny if there is not enough resin to consume. 19 hats to choose from for your sailors.
- **Persistent ZDOID Architecture**: Migrated vehicle piece tracking to persistent ZDOIDs, fixing disappearing parts, sector transition desyncs, and save issues. many other fixes of issues like incorrect vessel piece tracking, collapsing/dissapearing parts, infinite loading screens, bed desync etc.



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
1. Install via your mod manager of choice. r2modman recommended, you should delete the entire bepinex folder from your game directory and let r2modman handle all the mods to avoid conflicts.
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
- **Upstream (zolantris version) Repository**: [https://github.com/zolantris/ValheimMods](https://github.com/zolantris/ValheimMods)
- **Original Deprecated Mod**: [ValheimRAFT by Sarcen](https://www.nexusmods.com/valheim/mods/1136)

---