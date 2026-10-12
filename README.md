# ValheimRAFT Lite SP v5.4.5

**ValheimRAFT Lite (Single Player)** — Fixed various issues and expanded features. Design and construct custom navigable vessels and movable bases in Valheim. Ship bed, Portal, improved physics, drop anchor, recruit Greydwarf sailors, and take to the skies in flight!

![ValheimRAFT Lite](https://raw.githubusercontent.com/JNDEV0/ValheimRAFT-Lite/main/src/ValheimRAFT/Thunderstore/icon.png)

> ⚠️ **Single Player Focus**: For multiplayer support, please use the original [zolantris/ValheimMods](https://github.com/zolantris/ValheimMods). This version features reworked material costs to build boat parts. To build freely without cost, enable "Hammer Mode" in the world modifiers settings on the world/save selection screen, or enable the "No Material Cost (1 Wood)" setting by building a "Toggle Switch - Boat settings" part to open the options menu.

---

- **ValheimRAFT Lite** is a streamlined single-player edition of ValheimRAFT designed to improve progression, expand missing features, enhance simulation stability, and deepen nautical immersion.

### 🔨 Nautical Progression & Construction
- **Reorganized Boat Hammer**: Categorized into clean progression tabs (**Resined**, **Nailed**, **Iron**, and **Misc**) with authentic seafaring terminology and balanced material costs. *(Tip: find a Greydwarf nest in the Black Forest for resin, and Meadows for dandelions).*
- **Focused Piece Palette**: Pruned cluttered land-vehicle parts, bloated third-party plugins, and redundant inverted pieces. Build a sturdy keel, hull, and deck first, then construct your vessel freely on top.
- **Ship Planters & Maritime Agriculture**: Small and Large ship planters let you cultivate crops aboard your vessel—plant seeds directly from your actionbar (slots 1–8) or using a Cultivator tool.

### ⚓ Helm, Rudder & Propulsion
- **Helm & Rudder Control**: Navigating under sail requires a rudder (steering oar or sternpost rudder) and a steering wheel facing forward with clamped turn angles. If the helm or rudder is destroyed underway, the ship halts safely to avoid running adrift.
- **Straightforward Propulsion**: Rudders and oars govern rowing speed, while sails govern sailing velocity. No arbitrary weight penalties—sail or fly even an immense stone castle!
- **Dynamic Wake & Waterline Settings**: Trailing spray and foam realistically track the water surface. Adjust your waterline with customizable float height sliders (**Max**, **Base**, and **Min**) in the Boat Settings menu.

### 🪜 Rope Ladders & Climbing
- **Realistic Climbing Animations**: Smooth climbing motion with biomechanical hand and foot placement aligned precisely to Jacob's / rope ladder rungs.
- **Smooth Top-Deck Release**: Early dismount detection snaps your character cleanly forward onto the deck planks rather than dropping you in front of the ladder or climbing into empty air.
- **Automatic Deployment & Retraction**: Rope ladders and anchors automatically deploy when disembarking and retract when getting underway.

### 🧭 Moving Bases: Beds, Portals & Map Pins
- **Onboard Bed Respawns**: Claim a bed on your ship as your active respawn point—it dynamically moves and tracks with your vessel anywhere across the realm.
- **Mobile Shipboard Portals**: Portals built on your deck travel seamlessly with your ship, letting you teleport directly to and from your vessel while exploring.
- **Accurate Map Pin Tracking**: Real-time map pins follow your ship's coordinates, anchor state, and custom vessel name without producing ghost duplicate markers.

### 💨 Physics, Flight & Ground Handling
- **Anchoring & Kinematic Locking**: Automatic anchoring when stepping away from the helm or boarding, with kinematic locking to prevent vessels from drifting away or falling off the edge of the world.
- **Smooth Flight Controls**: Take to the skies in Flight Mode with dedicated altitude controls, responsive maneuvering, and rock impact damping.
- **Gentle Ground Landings & Anti-Fallthrough**: Safely land on terrain or beaches in Float Mode without getting catapulted into the air; anti-fallthrough guards keep the hull and rudder resting securely on land.
- **Zero Jitter & Visual Polishing**: Eliminated camera and vessel movement jitter. Snow overlays on boat pieces are disabled by default, preventing visual blinding whiteouts when flying through mountain altitudes.

### 🧙 Greydwarf Sailors (Optional Feature)
- **Hirable Crew**: Hire friendly Greydwarf sailors (10 coins, 1 resin upkeep) through the "Toggle Switch - Boat settings" menu or via the `vehicle hiresailor` console command.
- **Ship Defense & Logistics**: Sailors throw stones to defend your ship against hostiles, respect closed doors to stay within designated quarters, collect loose dropped cargo into chests, and feature 19 custom nautical hats!

### 💾 Architecture & Stability
- **Persistent ZDOID Architecture**: Migrated all vehicle piece tracking to persistent ZDOIDs, eliminating disappearing parts, sector transition desyncs, infinite loading screens, and save corruption.
- **In-Game Boat Settings Menu**: Build a "Toggle Switch - Boat settings" piece to access real-time settings: toggle boat damage (god mode), adjust ladder climb speeds, fine-tune waterlines, or enable Creative Free-Build Mode (1 Wood per piece).

---

## ⚓ Recommended Mods

These mods complement **ValheimRAFT Lite** particularly well for an expansive nautical and exploration playthrough:

- **[Monstrum](https://thunderstore.io/c/valheim/p/Therzie/Monstrum/)** by Therzie: Adds arious land animals and monsters throughout the world.
- **[SeaAnimals](https://thunderstore.io/c/valheim/p/Marlthon/SeaAnimals/)** by Marlthon: Brings the ocean biome to life with dolphins, sharks, whales, and other sea creatures to encounter on your voyages.
- **[Dive_In](https://thunderstore.io/c/valheim/p/sighsorry/Dive_In/)** by sighsorry: Adds underwater plants, allows sea creatures to dive as well as the player, underwater combat, more to explore while sailing, and you can retrieve materials that sink underwater.
- **[AirAnimals](https://thunderstore.io/c/valheim/p/Marlthon/AirAnimals/)** by Marlthon: Adds more flying animals to encounter. Land Ho!
- **[Warfare](https://thunderstore.io/c/valheim/p/Therzie/Warfare/) & [Armory](https://thunderstore.io/c/valheim/p/Therzie/Armory/) & [Wizardry](https://thunderstore.io/c/valheim/p/Therzie/Wizardry/)** by Therzie: Expands weapons and armor crafted from materials dropped by Monstrum creatures, wizardry goes well with the eitr generator and battery unlocked later in game (kept from zolantris-valheimraft).
- **[RecyclePlus](https://thunderstore.io/c/valheim/p/TastyChickenLegs/RecyclePlus/)** by TastyChickenLegs: Allows you to recycle or destroy items. better than leaving stuff you dont want on the floor.
- **[OdinArchitect](https://thunderstore.io/c/valheim/p/OdinPlus/OdinArchitect/)** by OdinPlus: Adds elevators, working drawbridges, 205+ architectural pieces that work great on large vessels and docks.
- **[OdinsKingdom](https://thunderstore.io/c/valheim/p/OdinPlus/OdinsKingdom/)** by OdinPlus: Castle walls and fortress parts. Ideal for constructing a floating or flying sky fortress.
- **[More_World_Locations_AIO](https://thunderstore.io/c/valheim/p/warpalicious/More_World_Locations_AIO/)** by warpalicious: Adds dozens of hand-crafted dungeons, camps, and coastal points of interest to discover while sailing.
- **[ImmersiveTrader](https://thunderstore.io/c/valheim/p/Hypnogoogic/ImmersiveTrader/)** by Hypnogoogic: Take on shipping and transport contracts between traders across the map for profit and adventure.
- **[SeedBed](https://thunderstore.io/c/valheim/p/blacks7ar/SeedBed/)** by blacks7ar: Adds a compact raised bed to plant seeds onto. Works well on a boat where soil space is limited. requires ymir's flesh bought from haldor trader.
- **[ODINFLIGHT](https://thunderstore.io/c/valheim/p/Pfhoenix/ODINFLIGHT/)** by Pfhoenix: Adds glider wings to skydive and glide down safely when leaping from a flying vessel in flight mode.

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