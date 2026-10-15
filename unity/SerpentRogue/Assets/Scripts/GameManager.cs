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

        // Current map / biome
        public MapDef map;
        public readonly List<LavaPool> pools = new List<LavaPool>();
        GameObject mapRoot;
        Material lavaMat;

        // Bosses
        public Snake boss;
        public float bossTimer, bossLeft, bossTimerMax;
        public int bossCount, bossKills;
        bool bossWarned;

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

            food = new FoodField();
            food.blocked = (x, z) => InLava(x, z, 2);
            SetMap(Maps.Get(profile.map));
            particles = new Particles();
            ui = gameObject.AddComponent<GameUI>();
            shop = gameObject.AddComponent<ShopStage>();
            StartAttract();
        }

        // ------------------------------------------------------------------ world
        // Switch to a map/biome (see Maps.cs): rebuilds floor, wall, decorations and lava.
        public void SetMap(MapDef m)
        {
            if (mapRoot != null) DestroyMap(mapRoot);
            map = m;
            Config.ArenaRadius = m.radius;
            mapRoot = Maps.Build(m, pools, out lavaMat);
            cam.backgroundColor = Toon.Hex(m.bg);
        }

        // Frees the meshes, materials and textures made for the old map.
        static void DestroyMap(GameObject root)
        {
            var done = new HashSet<Object>();
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>())
                if (done.Add(mf.sharedMesh)) Destroy(mf.sharedMesh);
            foreach (var mr in root.GetComponentsInChildren<MeshRenderer>())
            {
                Material mat = mr.sharedMaterial;
                if (mat == null || !done.Add(mat)) continue;
                Texture tex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                if (tex != null && done.Add(tex)) Destroy(tex);
                Destroy(mat);
            }
            root.SetActive(false);
            Destroy(root);
        }

        // Is the circle (x, z, margin) touching a lava pool?
        public bool InLava(float x, float z, float margin = 0)
        {
            foreach (var p in pools)
            {
                float dx = x - p.x, dz = z - p.z, r = p.r + margin;
                if (dx * dx + dz * dz < r * r) return true;
            }
            return false;
        }

        void ClearWorld()
        {
            foreach (var s in snakes) s.Destroy();
            snakes.Clear();
            player = null;
            food.Clear();
            particles.Clear();
            for (int i = 0; i < Config.FoodCount * map.foodMul; i++) food.SpawnRandom(0.01f);
            boss = null;
            bossCount = bossKills = 0;
            bossTimer = bossTimerMax = Config.BossFirst;
            bossWarned = false;
            respawnQueue.Clear();
            runTime = 0;
            threat = 1;
            paused = false;
            choosing = false;
            pendingLevels = 0;
        }

        public void StartAttract(bool newMap = false)
        {
            if (newMap) SetMap(Maps.Get(profile.map));
            ClearWorld();
            state = GameState.Attract;
            for (int i = 0; i < Config.BotCount; i++) SpawnBot();
        }

        public void StartRun()
        {
            SetMap(Maps.Get(profile.map));
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
            ui.Toast(map.name + ": " + map.desc);
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
            // Bots spawn around YOUR size (some smaller = food, some bigger = danger),
            // and a bit bigger as the run goes on: the "threat" ramps up.
            float baseMass = player != null && player.alive ? player.mass : 30;
            float mass = Mathf.Max(8, baseMass * (0.35f + Random.value * 1.0f)) * (1 + (threat - 1) * 0.15f);
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

            if (state == GameState.Playing) UpdateBoss(dt);

            float goldenChance = 0.01f + map.goldenBonus + (player != null && player.alive ? player.stats.goldenBonus : 0);
            food.Compact();
            food.Maintain(goldenChance, Config.FoodCount * map.foodMul);
            if (lavaMat != null) lavaMat.SetColor("_Color", Color.HSVToRGB(0.06f + Mathf.Sin(time * 2) * 0.015f, 1, 1));
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

            // The wall and lava pools.
            float dc = Mathf.Sqrt(hx * hx + hz * hz);
            bool inLava = InLava(hx, hz, -0.3f);
            if (dc + hr > R || inLava)
            {
                if (safe)
                {
                    if (inLava) s.angle = s.targetAngle = s.angle + Mathf.PI;
                    else
                    {
                        float k = (R - hr - s.Radius - 0.1f) / dc;
                        seg0.x *= k;
                        seg0.z *= k;
                        s.angle = s.targetAngle = Mathf.Atan2(-seg0.z, -seg0.x);
                    }
                }
                else
                {
                    if (inLava && s.isPlayer) ui.Toast("Burned in lava!");
                    Hit(s, null);
                }
                return;
            }
            if (s.phase > 0) return; // phased snakes pass through everything

            // Touching another snake: the HIGHER Lv bites the lower one.
            // Find the touched piece closest to our head.
            Seg touched = null;
            float bestD = float.MaxValue;
            segGrid.Query(hx, hz, hr + maxSegRadius, segBuffer);
            foreach (Seg seg in segBuffer)
            {
                Snake o = seg.owner;
                if (o == s || !o.alive || o.phase > 0 || seg.cut) continue;
                float rr = hr + o.Radius * 0.85f;
                float dx = seg.x - hx, dz = seg.z - hz, d2 = dx * dx + dz * dz;
                if (d2 < rr * rr && d2 < bestD) { bestD = d2; touched = seg; }
            }
            if (touched == null) return;
            Snake other = touched.owner;
            if (s.Lv > other.Lv)
            {
                if (other.invuln <= 0) Bite(s, other, touched.idx); // chomp!
            }
            else if (s.Lv < other.Lv)
            {
                // We ran our head into a bigger snake: our head gets eaten.
                if (!safe) Hit(s, other, true);
            }
            else
            {
                // Same Lv: bounce off each other.
                s.angle = s.targetAngle = s.angle + Mathf.PI;
                s.invuln = Mathf.Max(s.invuln, 0.4f);
            }
        }

        // `eater` bites `victim` at body piece number `idx`.
        // Bitten in the head (or too small afterwards) = eaten whole.
        // Bitten in the body = everything from there to the tail is bitten off.
        void Bite(Snake eater, Snake victim, int idx)
        {
            if (eater.biteCooldown > 0) return;
            eater.biteCooldown = Config.BiteCooldown;
            var segs = victim.segments;
            int n = segs.Count;
            float lost = Mathf.Max(1, victim.mass * (n - idx) / n);
            if (idx <= 2 || victim.mass - lost < Config.MinMass)
            {
                Hit(victim, eater, true);
                return;
            }
            var cut = segs.GetRange(idx, n - idx);
            segs.RemoveRange(idx, n - idx);
            foreach (var seg in cut) seg.cut = true; // ignore them for the rest of this frame
            victim.mass -= lost;

            eater.Grow(lost * (eater.isPlayer ? Config.EatAbsorb : Config.BotEatAbsorb));
            eater.chomp = 0.22f;
            // Some of the bitten part spills out as orbs.
            float spill = lost * Config.EatDrop;
            int drops = Mathf.Max(1, Mathf.CeilToInt(cut.Count / 3f));
            Color a = victim.skin.A, b = victim.skin.B;
            for (int i = 0; i < cut.Count; i += 3)
                food.Spawn(cut[i].x + Random.value - 0.5f, cut[i].z + Random.value - 0.5f, Mathf.Max(0.5f, spill / drops), Skins.SegmentColor(victim.skin, a, b, idx + i), false, false);
            particles.Burst(cut[0].x, cut[0].z, a, 14, 7);

            if (eater.isPlayer) GainXp(lost * 0.5f);
            if (victim.isPlayer)
            {
                ui.Toast(eater.name + " bit you! -" + Mathf.RoundToInt(lost) + " Lv");
                ui.Flash();
            }
        }

        // Something fatal happened to `s`. Shields and Second Wind can save it.
        void Hit(Snake s, Snake killer, bool eaten = false)
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
            s.eaten = eaten;
        }

        void OnDeath(Snake s)
        {
            s.handled = true;
            Snake killer = s.killer;

            // Eaten: the eater swallows a big part of it right away.
            if (s.eaten && killer != null && killer.alive)
            {
                killer.Grow(s.mass * (killer.isPlayer ? Config.EatAbsorb : Config.BotEatAbsorb));
                killer.chomp = 0.22f;
                particles.Burst(s.Head.x, s.Head.z, Color.white, 25, 9);
            }

            // The (rest of the) body turns into food, colored like the dead snake.
            int n = s.segments.Count;
            int drops = Mathf.CeilToInt(n / 2f);
            float ratio = s.eaten ? Config.EatDrop : Config.DeathDropRatio;
            float total = s.mass * ratio * (killer != null ? killer.stats.dropMul : 1);
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
                    if (!s.isBoss) ui.Toast("You ate " + s.name + "!");
                }
            }

            s.Destroy();
            if (s.isBoss) OnBossDefeated(s, killer);
            else if (s.isPlayer) GameOver();
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
            stats.bossKills = bossKills;
            stats.mapName = map.name;
            // Coins for the skin shop: bigger, longer, deadlier runs earn more.
            stats.coins = Mathf.RoundToInt(p.maxMass / 4 + p.kills * 15 + p.level * 10 + runTime / 6 + bossKills * 150);
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
            StartAttract(true);
            ui.Show(UIScreen.Menu);
        }

        // ------------------------------------------------------------------ bosses
        void UpdateBoss(float dt)
        {
            Snake p = player;
            if (p == null || !p.alive) return;
            if (boss == null)
            {
                bossTimer -= dt;
                if (!bossWarned && bossTimer <= Config.BossWarning)
                {
                    bossWarned = true;
                    ui.Toast("A BOSS is coming! Grow fast or stay away!");
                }
                if (bossTimer <= 0) SpawnBoss();
                return;
            }
            bossLeft -= dt;
            if (bossLeft <= 0)
            {
                // You survived: the boss gives up and leaves.
                Snake b = boss;
                particles.Burst(b.Head.x, b.Head.z, b.skin.A, 60, 14);
                b.alive = false;
                b.handled = true;
                b.Destroy();
                snakes.Remove(b);
                boss = null;
                bossTimer = bossTimerMax = Config.BossInterval;
                bossWarned = false;
                GainXp(25 + bossCount * 10);
                ui.Toast("You survived " + b.name + "! Bonus XP!");
            }
        }

        void SpawnBoss()
        {
            Snake p = player;
            float R = Config.ArenaRadius;
            // Come in from the far side of the map.
            float a = Mathf.Atan2(-p.Head.z, -p.Head.x) + (Random.value - 0.5f);
            float x = Mathf.Cos(a) * R * 0.75f, z = Mathf.Sin(a) * R * 0.75f;
            SkinData skin = Skins.BossSkins[bossCount % Skins.BossSkins.Count];
            // Always much bigger than you, and each boss bigger than the last.
            float mass = Mathf.Max(180, p.mass * 1.8f) * (1 + bossCount * 0.25f);
            var b = new Snake(this, skin, skin.label, x, z, a + Mathf.PI, mass, false);
            b.isBoss = true;
            b.noGrow = true;
            b.stats.sizeMul = 1.2f;
            b.stats.speedMul = 0.92f;
            b.invuln = 2;
            b.brain = new BotBrain(b, this, true);
            snakes.Add(b);
            boss = b;
            bossCount++;
            bossLeft = Config.BossStay;
            particles.Burst(x, z, skin.A, 80, 16);
            ui.Toast("BOSS: " + skin.label + " (Lv" + b.Lv + ") appeared!");
        }

        void OnBossDefeated(Snake b, Snake killer)
        {
            boss = null;
            bossTimer = bossTimerMax = Config.BossInterval;
            bossWarned = false;
            if (killer != null && killer.isPlayer)
            {
                bossKills++;
                GainXp(80 + b.Lv * 0.2f);
                ui.Toast("You ate the boss " + b.name + "! +150 coins at the end!");
            }
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
        public int bossKills;
        public string mapName;
    }
}
