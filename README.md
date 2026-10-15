# 🐍 Serpent Rogue

An offline 3D snake game (like slither.io / Snake Clash) with **collectible dragon skins**, **AI bots**, and
**roguelite power-ups**: every time you level up by eating, you pick 1 of 3 upgrades.

- 🐉 Cartoon 3D snakes: dragon / viper / cute heads with chomping jaws and teeth, scale plates,
  spikes or flame fins, black outlines and toon shading.
- 🛒 Skin shop with **Common / Rare / Epic / Legend** skins (Fire Dragon, Anubis, Void Leviathan...),
  bought with coins you earn at the end of each run, plus a **Custom** tab to design your own.

Built with plain JavaScript + [Three.js](https://threejs.org/) (3D) + [Vite](https://vitejs.dev/) (dev server).
No internet needed once installed.

## 🧩 Two versions

| Version | Folder | How to run |
| --- | --- | --- |
| **Web** (JavaScript + Three.js) | `src/`, `index.html` | `npm install` then `npm start` (below) |
| **Unity** (C#) | `unity/SerpentRogue/` | Open it in Unity Hub and press Play. See [`unity/README.md`](unity/README.md) |

Both versions have the same features, and every C# file matches a JavaScript file, so you can
compare how the same idea looks in each language.

## ▶️ Run it on your computer

1. Install **Node.js** (LTS) from https://nodejs.org
2. In a terminal, inside this folder:

```bash
npm install     # once (downloads three.js + vite into node_modules)
npm start       # opens the game in your browser at http://localhost:5173
```

After `npm install` everything is local, so it works offline.
To make a final optimized build: `npm run build`, then `npm run preview`.

## 🎮 Controls

| Action | Mouse | Keyboard |
| --- | --- | --- |
| Steer | Point the cursor | A / D or ← / → |
| Boost (burns length) | Hold left click | Space / W / ↑ |
| Pick power-up | Click a card | 1 / 2 / 3, R = reroll |
| Pause | — | Esc / P |

## 🗺️ How the code is organized

Each file does one job. Read them in this order:

| File | What it does |
| --- | --- |
| `src/config.js` | All tuning numbers (speed, arena size, bot count, XP curve). **Start experimenting here.** |
| `src/main.js` | Entry point: creates the game, wires up menu buttons and the skin editor. |
| `src/game.js` | The heart: the 3D scene and the **game loop** (input → AI → move → eat → collide → die → draw). |
| `src/snake.js` | Snake rules: steering, boosting, following body segments, growing. |
| `src/model.js` | Draws a snake: body plates + back spikes as `InstancedMesh`es (hundreds of copies in one draw call), head, "Lv" label. |
| `src/skins.js` | Skin options, the shop **catalog** (rarity + price), body color patterns, your saved profile (coins, owned skins). |
| `src/heads.js` | Builds the 3D heads (dragon, viper, cute) and hats out of simple shapes, with a jaw that opens. |
| `src/toon.js` | The cartoon look: toon materials + "inverted hull" outlines + merging parts into few meshes. |
| `src/bot.js` | Bot AI: pick a goal (food / hunt / wander), probe ahead for danger, steer. |
| `src/upgrades.js` | The roguelite power-up list and weighted random picking by rarity. |
| `src/food.js` | Food orbs: spawning, eating, drawing. |
| `src/grid.js` | Spatial hash: only test collisions with *nearby* things (keeps it fast). |
| `src/ui.js` | HUD, leaderboard, minimap, level-up cards, game over (plain HTML/CSS). |
| `src/preview.js` | The coiled 3D snake in the skin shop, and the little card pictures. |

### Key ideas you'll learn here

- **Game loop**: every frame, `Game.update(dt)` moves everything by `speed * dt` (dt = seconds since
  the last frame) so the game runs the same at 30 or 144 FPS.
- **Follow-the-leader body**: the head moves; each segment is pulled to stay a fixed distance behind the
  one in front of it (`snake.js`). That's the whole "snake" trick.
- **Collision**: a head dies if it overlaps a segment of *another* snake (circle vs circle distance).
- **Spatial hashing**: put objects in grid cells so each check only looks at nearby cells.
- **Simple AI**: "feelers" (like whiskers) check a few directions ahead for danger.
- **Roguelite loop**: XP → level up → pause → random weighted choices → stats change → runs feel different.
- **Separating logic and visuals**: `Snake` (rules) vs `SnakeModel` (drawing). The skin editor reuses
  `SnakeModel` without any game rules.

## 🧪 Ideas to try next (good practice!)

1. **Easy**: change `CONFIG.botCount`, `baseSpeed`, or the XP curve in `config.js`.
2. **Easy**: add a new skin to `CATALOG` in `skins.js` (pick head, body, spikes, colors, price).
3. **Medium**: add a new upgrade in `upgrades.js` (e.g. "Thorns: snakes that hit you lose 20% mass").
4. **Medium**: add a new head type or hat in `heads.js` using Three.js shapes (Cone, Torus, RoundedBox...).
5. **Medium**: add sound effects with the Web Audio API when eating / leveling / dying.
6. **Hard**: spend coins on permanent bonuses too (e.g. start every run with +10 length).
7. **Hard**: give bots their own random upgrades as they grow, or add boss snakes.
8. **Hard**: package it as a desktop app with [Electron](https://www.electronjs.org/) or [Tauri](https://tauri.app/).

Debug tip: the game is exposed as `window.game`. Open the browser console (F12) and try
`game.player.mass += 200`, `game.gainXp(100)` or `profile.coins = 99999` (then reopen the shop).

### Want to go further with a game engine?

This project teaches the fundamentals in code. When you want a visual editor, physics, and exporting to
Windows/Android, the same design maps directly onto **Godot** (free, GDScript is easy) or **Unity** (C#):
the snake becomes a scene/prefab, upgrades become Resources/ScriptableObjects, and the bot brain becomes a
script on the bot snake.
