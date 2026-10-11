# ValheimRAFT Lite SP v5.3.7

**ValheimRAFT Lite (Single Player)** — Fixed various issues and expanded features. Design and construct custom navigable vessels and movable bases in Valheim. Ship bed, Portal, improved physics, drop anchor, recruit Greydwarf sailors, and take to the skies in flight!

![ValheimRAFT Lite](https://raw.githubusercontent.com/JNDEV0/ValheimRAFT-Lite/main/src/ValheimRAFT/Thunderstore/icon.png)

> ⚠️ **Single Player Focus**: For multiplayer support, please use the original [zolantris/ValheimMods](https://github.com/zolantris/ValheimMods). This version features reworked material costs to build boat parts. To build freely without cost, enable "Hammer Mode" in the world modifiers settings on the world/save selection screen, or enable the "No Material Cost (1 Wood)" setting by building a "Toggle Switch - Boat settings" part to open the options menu.

---

- **ValheimRAFT Lite** is a streamlined edition of ValheimRAFT aiming to improve progression, expand on missing features, enhance simulation stability, and nautical immersion. New material costs(tip: find a greydwarf nest in the black forest biome to farm resin, dandelions in meadows biome).
- **Build Menu & Nautical Progression**: Reorganized the Boat Hammer into clean, tiered progression tabs (**Resined**, **Nailed**, **Iron**, and **Misc**), with authentic seafaring terminology and balanced material costs. Removed land vehicle pieces, unrelated extra plugins packaged into the original version, and redundant inverted parts. make a hull and deck first, then build with other parts on top.
- **Helm & Rudder**: Sailing now requires a rudder, with clamped turn angles. the wheel and rudder must face forward. If the steering wheel or rudder is destroyed while underway, the vessel automatically halts safely instead of sailing away.
- **Simple Propulsion**: Oar/Rudder determines rowing speed, sails determine sailing speed. No weight calculations. You can fly a stone castle now!
- **Water Wake & Waterline Controls**: Water wake foam and trailing water spray dynamically track the water surface, with customizable float height controls (**Max**, **Base**, and **Min** sliders) in the "Toggle Switch - Boat settings" part menu.
- **Physics & Flight Stability**: Automatic anchoring when leaving the ship, kinematic locking (preventing vessels from drifting or falling off the map), rock impact damping physics (no bulldozing terrain with the boat), responsive flight controls, and improved ship water physics.
- **Quality of Life**: Working bed, portals and map pins that accurately track moving vessels, realistic ladder climbing animation with biomechanical hand and foot placement on Jacob's/rope ladder rungs, locked wheel hand IK and activation responsiveness, automatically deploying and retracting rope ladders and anchors, and the ability to toggle boat damage on/off in the "Toggle Switch - Boat settings" part menu to fit your playstyle.
- **No Jittering on Boat Movement**: Moving the boat no longer causes vessel jitter, and the snow overlay on vehicle parts is disabled by default (fixing the visual glitch when flying at mountain altitude).
- **Greydwarf Sailors (Optional — Enable in "Toggle Switch - Boat settings" part menu)**: Hire friendly Greydwarf sailors (10 coins, 1 resin upkeep) that throw rocks to defend your vessel against hostiles. Sailors respect closed doors so you can keep them contained in designated areas; otherwise, they wander freely around the boat. Sailors consume resin from nearby onboard containers every few minutes and automatically pick up loose items dropped on the boat, storing them into chests for you. Sailors will eventually mutiny if their resin upkeep runs out. Choose from 19 unique hats to customize your crew! (you can spawn one typing "vehicle hiresailor" in F5 console, after enabling)
- **Persistent ZDOID Architecture**: Migrated vehicle piece tracking to persistent ZDOIDs, fixing disappearing parts, sector transition desyncs, and save issues. Includes numerous fixes for issues like incorrect vessel piece tracking, collapsing/disappearing parts, infinite loading screens, and bed desync.

---

## ⚓ Recommended Mods

These mods complement **ValheimRAFT Lite** particularly well for an expansive nautical and exploration playthrough:

- **[Monstrum](https://thunderstore.io/c/valheim/p/Therzie/Monstrum/)** by Therzie: Adds diverse land animals, monsters across the world.
- **[SeaAnimals](https://thunderstore.io/c/valheim/p/Marlthon/SeaAnimals/)** by Marlthon: Brings the ocean biome to life with dolphins, sharks, whales, and more sea creatures to encounter on your voyages.
- **[Warfare](https://thunderstore.io/c/valheim/p/Therzie/Warfare/) & [Armory](https://thunderstore.io/c/valheim/p/Therzie/Armory/) & [Wizardry](https://thunderstore.io/c/valheim/p/Therzie/Wizardry/)** by Therzie: Expands weapons and armor crafted from materials dropped by Monstrum creatures, wizardry goes well with the eitr generator and battery unlocked later in game (kept from zolantris-valheimraft).
- **[RecyclePlus](https://thunderstore.io/c/valheim/p/TastyChickenLegs/RecyclePlus/)** by TastyChickenLegs: Allows you to recycle or destroy items. better than leaving stuff you dont want on the floor.
- **[OdinArchitect](https://thunderstore.io/c/valheim/p/OdinPlus/OdinArchitect/)** by OdinPlus: Adds elevators, working drawbridges, 205+ architectural pieces that work great on large vessels and docks.
- **[OdinsKingdom](https://thunderstore.io/c/valheim/p/OdinPlus/OdinsKingdom/)** by OdinPlus: Castle walls, turrets, and fortress parts—ideal for constructing a floating or flying sky fortress.
- **[More_World_Locations_AIO](https://thunderstore.io/c/valheim/p/warpalicious/More_World_Locations_AIO/)** by warpalicious: Adds dozens of hand-crafted dungeons, camps, and coastal points of interest to discover while sailing.
- **[ImmersiveTrader](https://thunderstore.io/c/valheim/p/Hypnogoogic/ImmersiveTrader/)** by Hypnogoogic: Take on shipping and transport contracts between traders across the map for profit and adventure.

---

## 🔧 Installation

### Option A: Thunderstore / r2modman / Gale (Recommended)
1. Install via your mod manager of choice. r2modman is recommended. If switching from a previously manually installed collection, delete the old `BepInEx` folder from your game directory first and let r2modman manage your mods to avoid stale file conflicts.
2. Dependencies are automatically resolved and installed. 

### Option B: NexusMods / Vortex / Manual Installation
1. If installing manually, extract `ValheimRAFT` into your `Valheim/BepInEx/plugins/` directory:
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

## ⚠️ Important "As-Is" Disclaimer

- Core features have been tested primarily for SINGLE-PLAYER.
- ValheimRAFT is a massive and complex codebase containing advanced mechanics. Not all extended features or edge-case interactions have been exhaustively tested.
- **Offered "As-Is"**: Provided freely and without warranty. Always back up your character and world saves before testing! Players and modders are invited to report issues, bugs, submit suggestions or Pull Requests, and help maintain this mod at the GitHub repository: 
  👉 **[https://github.com/JNDEV0/ValheimRAFT-Lite](https://github.com/JNDEV0/ValheimRAFT-Lite)**

---

## 📜 Attribution & Open Source History

- **Original Creator**: **Sarcen** created the original ValheimRAFT mod that defined ship building in Valheim, and generously released it as open source in 2023 under the GPLv3 license.
- **Modern Rewrite & Architecture**: **Zolantris** ([zolantris/ValheimMods](https://github.com/zolantris/ValheimMods)) completely overhauled the mod's architecture, adding modular vehicle systems, convex hull boundary physics, and expansive features.

---

## 📦 Requirements & Dependencies

To use this mod, ensure you have the following required dependencies installed (resolved automatically with r2modman or other mod managers, only need to download these separately if manually installing):
1. **[denikson-BepInExPack_Valheim-5.4.2350+](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)**
2. **[ValheimModding-Jotunn-2.30.0+](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/v/2.30.0/)**
3. **[ValheimModding-JsonDotNET-13.0.4+](https://thunderstore.io/c/valheim/p/ValheimModding/JsonDotNET/)**

---

## 💻 Source Code

- **GitHub Repository**: [https://github.com/JNDEV0/ValheimRAFT-Lite](https://github.com/JNDEV0/ValheimRAFT-Lite)
- **Upstream (Zolantris Version) Repository**: [https://github.com/zolantris/ValheimMods](https://github.com/zolantris/ValheimMods)
- **Original Deprecated Mod**: [ValheimRAFT by Sarcen](https://www.nexusmods.com/valheim/mods/1136)

---

## ☕ Support

Enjoying the mod? You can support development on Ko-fi:

👉 **[Support JNDEV0 on Ko-fi (https://ko-fi.com/jndev0)](https://ko-fi.com/jndev0)**

---