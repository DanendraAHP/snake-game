# 🐍 Serpent Rogue — Unity version

The same game as the web version (3D dragon snakes, bots, roguelite power-ups, skin shop),
rebuilt in **Unity + C#**. Everything is created from code when you press Play, so there is
**nothing to set up by hand**: no prefabs, no scene objects, no Inspector wiring.

## ▶️ How to open and play

1. Install **Unity Hub**: https://unity.com/download
2. In Unity Hub → **Installs** → **Install Editor** → pick the latest **Unity 6 LTS** (6000.x).
   The free **Personal** license is fine.
3. Unity Hub → **Projects** → **Add** → **Add project from disk** → select the folder
   `unity/SerpentRogue` (the folder that contains `Assets`).
   If Hub asks which editor version to use, choose the Unity 6 you installed.
4. Open the project. The first import takes a few minutes. Unity creates the
   `Library`, `Packages` and `ProjectSettings` folders by itself.
5. Press the **▶ Play** button at the top. The menu appears and the game starts by itself
   (see `Assets/Scripts/Bootstrap.cs`).

Tip: set the **Game** view to a 16:9 resolution (e.g. *Full HD 1920x1080*) in the dropdown at
the top of the Game view.

### Make a real .exe for your PC
**File → Build Profiles** (or *Build Settings* in older versions) → **Windows** → **Build**.
You get a normal game folder you can play offline and share with friends.

### Already have a project made from a template (URP / 3D)?
That works too: copy the `Assets/Scripts` and `Assets/Resources` folders into your project's
`Assets` folder and press Play. The shaders and the input code support both the Built-in and URP
render pipelines and both the old and new Input System.

## 🎮 Controls

| Action | Mouse | Keyboard |
| --- | --- | --- |
| Steer | Point the cursor | A / D or ← / → |
| Boost (burns length) | Hold left click | Space / W / ↑ |
| Pick power-up | Click a card | 1 / 2 / 3, R = reroll |
| Pause | — | Esc / P |

## 🗺️ Where things are (`Assets/Scripts`)

Each C# file matches a file of the web version, so you can compare them side by side.

| C# file | Web file | What it does |
| --- | --- | --- |
| `Config.cs` | `config.js` | All tuning numbers. **Start experimenting here.** |
| `Bootstrap.cs` | `main.js` | Starts the game automatically when you press Play. |
| `GameManager.cs` | `game.js` | Main loop, arena, camera, eating, collisions, deaths, level-ups, coins. |
| `Snake.cs` | `snake.js` | Snake rules: steering, boosting, body following, growing. |
| `SnakeModel.cs` | `model.js` | Draws a snake. The body uses **GPU instancing** (`Graphics.DrawMeshInstanced`). |
| `HeadBuilder.cs` | `heads.js` | Builds dragon / viper / cute heads and hats from simple shapes. |
| `MeshFactory.cs` | (three.js geometries) | Makes rounded boxes, cones, fins, spheres... from code. |
| `Toon.cs` + `Resources/*.shader` | `toon.js` | Cartoon shading and black outlines. |
| `Skins.cs` | `skins.js` | Skin catalog (rarity, price), colors, saved profile (`PlayerPrefs`). |
| `BotBrain.cs` | `bot.js` | Bot AI: goals + danger "feelers". |
| `Upgrades.cs` | `upgrades.js` | Roguelite power-ups and rarity-weighted rolls. |
| `FoodField.cs` | `food.js` | Food orbs. |
| `Particles.cs` | `particles.js` | Little burst effects. |
| `SpatialHash.cs` | `grid.js` | Fast "what's near me?" lookups. |
| `GameUI.cs` | `ui.js` + HTML/CSS | All menus and the HUD, drawn with Unity's IMGUI (`OnGUI`). |
| `ShopStage.cs` | `preview.js` | 3D shop preview and card pictures (cameras rendering into textures). |
| `GameInput.cs` | `input.js` | Mouse/keyboard for both Unity input systems. |

## 🚀 Next steps that Unity makes easy

1. **Real 3D models**: make a dragon head in Blender (free) or get one from the Unity Asset Store,
   then use it instead of `HeadBuilder` (load it as a prefab and parent it like the built head).
2. **Nicer UI**: rebuild the menus with *UI Toolkit* or *uGUI* using the visual editor.
3. **Sound**: add an `AudioSource` and play clips when eating, leveling up or dying.
4. **Phone version**: switch the build target to Android and add touch steering.
5. **Particles**: replace `Particles.cs` with Unity's Particle System for sparkles and explosions.

Debug tip: while playing, select the **Serpent Rogue** object in the Hierarchy to see the scripts.
Saved data (coins, skins) lives in `PlayerPrefs`. To reset it, run
`PlayerPrefs.DeleteAll()` or use *Edit → Clear All PlayerPrefs*.
