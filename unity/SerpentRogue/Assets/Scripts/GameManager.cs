using System.Collections.Generic;
using UnityEngine;

namespace SerpentRogue
{
    public enum GameState { Attract, Playing, Over }

    // The heart of the game. Every frame:
    //   input -> AI -> movement -> eating -> collisions -> deaths -> draw
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public Profile profile;
        public Camera cam;
        public GameUI ui;
        public ShopStage shop;

        public readonly List<Snake> snakes = new List<Snake>();
        public Snake player;
        public FoodField food;
        public Particles particles;
        public readonly SpatialHash<Seg> segGrid = new SpatialHash<Seg>(3);
        public float maxSegRadius = 1;

        public GameState state = GameState.Attract;
        public bool paused, choosing;
        public float time, runTime, threat = 1;
        public int pendingLevels;
        readonly List<float> respawnQueue = new List<float>();
        readonly List<Seg> segBuffer = new List<Seg>();
        readonly List<Food> foodBuffer = new List<Food>();
        Vector3 camTarget;

        void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 120;
            profile = Profile.Load();
            Toon.SetLight(new Vector3(0.45f, 1f, -0.35f));

            cam = Camera.main;
            if (cam == null)
            {
                cam = new GameObject("Main Camera").AddComponent<Camera>();
                cam.tag = "MainCamera";
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Toon.Hex("#1a1d26");
            cam.fieldOfView = 55;
            cam.nearClipPlane = 0.5f;
            cam.farClipPlane = 700;
            cam.transform.position = new Vector3(0, 80, -80);

            BuildArena();
            food = new FoodField();
            particles = new Particles();
            ui = gameObject.AddComponent<GameUI>();
            shop = gameObject.AddComponent<ShopStage>();
            StartAttract();
        }

        // ------------------------------------------------------------------ world
        void BuildArena()
        {
            float R = Config.ArenaRadius;
            var arena = new GameObject("Arena").transform;

            // Dark floor with little "+" marks, like Snake Clash.
            var tex = new Texture2D(64, 64) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            Color bg = Toon.Hex("#262a35"), mark = Toon.Hex("#323746");
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    bool plus = (Mathf.Abs(x - 32) <= 1 && Mathf.Abs(y - 32) <= 5) || (Mathf.Abs(y - 32) <= 1 && Mathf.Abs(x - 32) <= 5);
                    tex.SetPixel(x, y, plus ? mark : bg);
                }
            tex.Apply();
            var floorMat = Toon.Make(Color.white, 1);
            floorMat.SetTexture("_MainTex", tex);
            Spawn("Floor", MeshFactory.Disc(R + 60, 128, 1 / 6f), floorMat, arena);

            Spawn("Outside", MeshFactory.Ring(R, R + 60, 128), Toon.Make(Toon.Hex("#3a0d1c"), 1), arena).transform.position = Vector3.up * 0.03f;
            var rim = Spawn("Rim", MeshFactory.Torus(R, 0.35f, 220, 8), Toon.Make(Toon.Hex("#ff6b8a"), 1), arena);
            rim.transform.rotation = Quaternion.Euler(90, 0, 0);
            rim.transform.position = Vector3.up * 0.4f;

            // Decorative glowing crystals outside the arena.
            Mesh crystal = MeshFactory.Cone(1, 2, 4);
            for (int i = 0; i < 50; i++)
            {
                float a = Random.value * Mathf.PI * 2, d = R + 8 + Random.value * 40, s = 1.5f + Random.value * 4;
                var c = Spawn("Crystal", crystal, Toon.Make(Color.HSVToRGB(0.5f + Random.value * 0.4f, 0.7f, 1f), 0.4f), arena);
                c.transform.position = new Vector3(Mathf.Cos(a) * d, s * 0.8f, Mathf.Sin(a) * d);
                c.transform.localScale = new Vector3(s * 0.6f, s * 0.8f, s * 0.6f);
                c.transform.rotation = Quaternion.Euler(0, Random.value * 360, 0);
            }
        }

        GameObject Spawn(string name, Mesh mesh, Material mat, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        void ClearWorld()
        {
            foreach (var s in snakes) s.Destroy();
            snakes.Clear();
            player = null;
            food.Clear();
            particles.Clear();
            for (int i = 0; i < Config.FoodCount; i++) food.SpawnRandom(0.01f);
            respawnQueue.Clear();
            runTime = 0;
            threat = 1;
            paused = false;
            choosing = false;
            pendingLevels = 0;
        }

        public void StartAttract()
        {
            ClearWorld();
            state = GameState.Attract;
            for (int i = 0; i < Config.BotCount; i++) SpawnBot();
        }

        public void StartRun()
        {
            ClearWorld();
            state = GameState.Playing;
            var p = new Snake(this, profile.EquippedSkin(), profile.name, 0, 0, Random.value * Mathf.PI * 2, Config.StartMass, true);
            p.xpNext = Config.XpForLevel(1);
            p.rerolls = Config.StartRerolls;
            p.invuln = 2;
            player = p;
            snakes.Add(p);
            for (int i = 0; i < Config.BotCount; i++) SpawnBot();
            ui.Show(UIScreen.Hud);
        }

        void SpawnBot()
        {
            float R = Config.ArenaRadius, x = 0, z = 0;
            for (int tries = 0; tries < 30; tries++)
            {
                float r = R * 0.85f * Mathf.Sqrt(Random.value), a = Random.value * Mathf.PI * 2;
                x = Mathf.Cos(a) * r;
                z = Mathf.Sin(a) * r;
                if (player == null || !player.alive || Vector2.Distance(new Vector2(x, z), new Vector2(player.Head.x, player.Head.z)) > 35) break;
            }
            // Bots spawn bigger as the run goes on: the "threat" ramps up.
            float mass = Config.StartMass + Random.value * 25 * threat + (threat - 1) * 30;
            var bot = new Snake(this, Skins.RandomBotSkin(), Skins.RandomBotName(), x, z, Random.value * Mathf.PI * 2, mass, false);
            bot.brain = new BotBrain(bot, this);
            bot.invuln = 1;
            snakes.Add(bot);
        }

        // ------------------------------------------------------------------ loop
        void Update()
        {
            GameInput.Tick();
            if (GameInput.Pressed(GameKey.Pause)) TogglePause();
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            if (!paused) Step(dt);

            // Instanced drawing must be repeated every frame, even when paused.
            food.Draw(time);
            foreach (var s in snakes) s.Draw(time);
            particles.Draw();
            UpdateCamera(dt);
        }

        void Step(float dt)
        {
            time += dt;
            if (state == GameState.Playing)
            {
                runTime += dt;
                threat = 1 + runTime / 90;
            }
            if (state == GameState.Playing && player != null && player.alive) ApplyInput(player);

            foreach (var s in snakes) if (s.brain != null && s.alive) s.brain.Think(dt);
            foreach (var s in snakes) if (s.alive) s.Update(dt);

            // Rebuild spatial grids for this frame.
            segGrid.Clear();
            maxSegRadius = 0;
            foreach (var s in snakes)
            {
                if (!s.alive) continue;
                maxSegRadius = Mathf.Max(maxSegRadius, s.Radius);
                foreach (var seg in s.segments) segGrid.Insert(seg.x, seg.z, seg);
            }
            food.RebuildGrid();

            foreach (var s in snakes) if (s.alive) Eat(s, dt);
            foreach (var s in snakes) if (s.alive) Collide(s);

            // Handle deaths, then remove dead snakes.
            for (int i = 0; i < snakes.Count; i++) if (!snakes[i].alive && !snakes[i].handled) OnDeath(snakes[i]);
            snakes.RemoveAll(s => !s.alive);

            // Respawn bots after a short delay.
            for (int i = respawnQueue.Count - 1; i >= 0; i--)
            {
                respawnQueue[i] -= dt;
                if (respawnQueue[i] <= 0)
                {
                    respawnQueue.RemoveAt(i);
                    SpawnBot();
                }
            }

            float goldenChance = 0.01f + (player != null && player.alive ? player.stats.goldenBonus : 0);
            food.Compact();
            food.Maintain(goldenChance);
            particles.Update(dt);

            // Roguelite level-up: pause and offer a choice.
            if (state == GameState.Playing && pendingLevels > 0 && !choosing) OpenLevelUp();
        }

        void ApplyInput(Snake p)
        {
            float turn = (GameInput.Held(GameKey.Left) ? 1 : 0) - (GameInput.Held(GameKey.Right) ? 1 : 0);
            if (turn != 0)
            {
                p.targetAngle = p.angle + turn; // keep turning at full speed while held
            }
            else
            {
                Ray ray = cam.ScreenPointToRay(GameInput.MousePosition);
                float enter;
                if (new Plane(Vector3.up, 0).Raycast(ray, out enter))
                {
                    Vector3 m = ray.GetPoint(enter);
                    float dx = m.x - p.Head.x, dz = m.z - p.Head.z;
                    if (dx * dx + dz * dz > 1) p.targetAngle = Mathf.Atan2(dz, dx);
                }
            }
            p.wantBoost = GameInput.MouseHeld || GameInput.Held(GameKey.Boost);
        }

        void Eat(Snake s, float dt)
        {
            Seg h = s.Head;
            float pickup = s.Radius + 0.7f;
            float magnet = pickup + Config.BaseMagnet + s.stats.magnet;
            float pull = (14 + s.stats.magnet * 2) * dt;
            food.grid.Query(h.x, h.z, magnet, foodBuffer);
            foreach (Food f in foodBuffer)
            {
                if (!f.alive) continue;
                float dx = f.x - h.x, dz = f.z - h.z, d2 = dx * dx + dz * dz;
                if (d2 < pickup * pickup)
                {
                    f.alive = false;
                    s.Grow(f.value * Config.MassPerFood * s.stats.foodMul);
                    if (s.isPlayer)
                    {
                        GainXp(f.value);
                        if (f.golden) particles.Burst(f.x, f.z, Toon.Hex("#ffcc33"), 10, 5);
                    }
                }
                else if (d2 < magnet * magnet)
                {
                    float d = Mathf.Sqrt(d2), step = Mathf.Min(d, pull);
                    f.x -= dx / d * step;
                    f.z -= dz / d * step;
                }
            }
        }

        void Collide(Snake s)
        {
            // The 3D head sticks out in front of the first segment, so test a bit ahead.
            Seg seg0 = s.Head;
            float hx = seg0.x + Mathf.Cos(s.angle) * s.Radius * 0.9f;
            float hz = seg0.z + Mathf.Sin(s.angle) * s.Radius * 0.9f;
            float hr = s.Radius * 0.9f, R = Config.ArenaRadius;
            bool safe = s.invuln > 0 || s.phase > 0;

            // The wall.
            float dc = Mathf.Sqrt(hx * hx + hz * hz);
            if (dc + hr > R)
            {
                if (safe)
                {
                    float k = (R - hr - s.Radius - 0.1f) / dc;
                    seg0.x *= k;
                    seg0.z *= k;
                    s.angle = s.targetAngle = Mathf.Atan2(-seg0.z, -seg0.x);
                }
                else Hit(s, null);
                return;
            }
            if (safe) return;

            // Other snakes' bodies (your own body is safe, like slither.io).
            segGrid.Query(hx, hz, hr + maxSegRadius, segBuffer);
            foreach (Seg seg in segBuffer)
            {
                Snake o = seg.owner;
                if (o == s || !o.alive || o.phase > 0) continue;
                float rr = hr + o.Radius * 0.85f;
                float dx = seg.x - hx, dz = seg.z - hz;
                if (dx * dx + dz * dz < rr * rr)
                {
                    if (seg.idx == 0 && s.mass > o.mass) continue; // head-on: the bigger snake wins
                    Hit(s, o);
                    return;
                }
            }
        }

        void Hit(Snake s, Snake killer)
        {
            if (s.shields > 0)
            {
                s.shields--;
                s.invuln = 2;
                s.angle = s.targetAngle = s.angle + Mathf.PI;
                particles.Burst(s.Head.x, s.Head.z, Toon.Hex("#7fe8ff"), 30, 10);
                if (s.isPlayer) ui.Toast("Shield broken!");
                return;
            }
            if (s.stats.revives > 0)
            {
                s.stats.revives--;
                s.mass = Mathf.Max(Config.StartMass, s.mass * 0.5f);
                s.invuln = 3;
                s.angle = s.targetAngle = s.angle + Mathf.PI;
                particles.Burst(s.Head.x, s.Head.z, Toon.Hex("#ff7b39"), 50, 12);
                if (s.isPlayer) ui.Toast("Second Wind! You revived!");
                return;
            }
            s.alive = false;
            s.killer = killer;
        }

        void OnDeath(Snake s)
        {
            s.handled = true;
            Snake killer = s.killer;

            // The body turns into food, colored like the dead snake.
            int n = s.segments.Count;
            int drops = Mathf.CeilToInt(n / 2f);
            float total = s.mass * Config.DeathDropRatio * (killer != null ? killer.stats.dropMul : 1);
            float per = Mathf.Max(0.5f, total / drops);
            Color a = s.skin.A, b = s.skin.B;
            for (int i = 0; i < n; i += 2)
            {
                Seg seg = s.segments[i];
                food.Spawn(seg.x + (Random.value - 0.5f) * 1.5f, seg.z + (Random.value - 0.5f) * 1.5f, per, Skins.SegmentColor(s.skin, a, b, i), false, false);
            }
            particles.Burst(s.Head.x, s.Head.z, a, 40, 10);

            if (killer != null && killer.alive)
            {
                killer.kills++;
                if (killer.stats.bloodrush) killer.freeBoost = 4;
                if (killer.isPlayer)
                {
                    GainXp((15 + s.mass * 0.25f) * killer.stats.killXpMul);
                    if (killer.upgrades.ContainsKey("predator")) killer.Grow(s.mass * 0.15f);
                    ui.Toast("You defeated " + s.name + "!");
                }
            }

            s.Destroy();
            if (s.isPlayer) GameOver();
            else respawnQueue.Add(2 + Random.value * 2);
        }

        public void GainXp(float amount)
        {
            Snake p = player;
            if (p == null || !p.alive) return;
            p.xp += amount * p.stats.xpMul;
            while (p.xp >= p.xpNext)
            {
                p.xp -= p.xpNext;
                p.level++;
                p.xpNext = Config.XpForLevel(p.level);
                if (p.level % 5 == 0) p.rerolls++;
                pendingLevels++;
                particles.Burst(p.Head.x, p.Head.z, Toon.Hex("#ffe066"), 30, 8);
            }
        }

        void OpenLevelUp()
        {
            choosing = true;
            paused = true;
            ui.OpenLevelUp(player.level - pendingLevels + 1, Upgrades.Roll(player.upgrades));
        }

        public List<UpgradeDef> Reroll()
        {
            if (player.rerolls <= 0) return null;
            player.rerolls--;
            return Upgrades.Roll(player.upgrades);
        }

        public void PickUpgrade(UpgradeDef u)
        {
            Upgrades.Apply(player, u);
            pendingLevels--;
            choosing = false;
            paused = false;
            player.invuln = Mathf.Max(player.invuln, 0.8f); // a moment to react after the pause
            ui.Show(UIScreen.Hud);
        }

        public void TogglePause()
        {
            if (state != GameState.Playing || choosing) return;
            paused = !paused;
            ui.Show(paused ? UIScreen.Pause : UIScreen.Hud);
        }

        public RunStats lastRun;

        void GameOver()
        {
            Snake p = player;
            state = GameState.Over;
            var stats = new RunStats
            {
                level = p.level,
                length = Mathf.FloorToInt(p.maxMass),
                kills = p.kills,
                time = runTime,
                upgrades = new Dictionary<string, int>(p.upgrades),
            };
            // Coins for the skin shop: bigger, longer, deadlier runs earn more.
            stats.coins = Mathf.RoundToInt(p.maxMass / 4 + p.kills * 15 + p.level * 10 + runTime / 6);
            stats.newBestLevel = stats.level > profile.bestLevel;
            stats.newBestLength = stats.length > profile.bestLength;
            stats.newBestKills = stats.kills > profile.bestKills;
            stats.newBestTime = stats.time > profile.bestTime;
            profile.bestLevel = Mathf.Max(profile.bestLevel, stats.level);
            profile.bestLength = Mathf.Max(profile.bestLength, stats.length);
            profile.bestKills = Mathf.Max(profile.bestKills, stats.kills);
            profile.bestTime = Mathf.Max(profile.bestTime, stats.time);
            profile.coins += stats.coins;
            profile.Save();
            lastRun = stats;
            ui.ShowGameOverSoon();
        }

        public void GoMenu()
        {
            StartAttract();
            ui.Show(UIScreen.Menu);
        }

        // ------------------------------------------------------------------ camera
        void UpdateCamera(float dt)
        {
            float k = 1 - Mathf.Exp(-dt * 5);
            Vector3 desired;
            if (state == GameState.Playing && player != null && player.alive)
            {
                camTarget = Vector3.Lerp(camTarget, new Vector3(player.Head.x, 0, player.Head.z), Mathf.Min(1, k * 2));
                float height = (15 + player.Radius * 12) * player.stats.zoomMul;
                desired = new Vector3(camTarget.x, height, camTarget.z - height * 0.62f);
                cam.transform.position = Vector3.Lerp(cam.transform.position, desired, Mathf.Min(1, k * 2));
            }
            else
            {
                // Slow orbit for the menu / game over screen.
                float a = time * 0.05f;
                camTarget = Vector3.Lerp(camTarget, Vector3.zero, k * 0.3f);
                desired = new Vector3(Mathf.Cos(a) * 110, 85, Mathf.Sin(a) * 110);
                cam.transform.position = Vector3.Lerp(cam.transform.position, desired, k * 0.3f);
            }
            cam.transform.LookAt(camTarget);
        }
    }

    public class RunStats
    {
        public int level, length, kills, coins;
        public float time;
        public Dictionary<string, int> upgrades;
        public bool newBestLevel, newBestLength, newBestKills, newBestTime;
    }
}
