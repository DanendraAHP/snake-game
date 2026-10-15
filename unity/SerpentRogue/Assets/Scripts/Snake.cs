using System.Collections.Generic;
using UnityEngine;

namespace SerpentRogue
{
    // Snake = game rules (movement, growth, timers). Drawing is done by SnakeModel.
    public class Snake
    {
        public readonly GameManager game;
        public SkinData skin;
        public string name;
        public bool isPlayer;
        public float angle, targetAngle;
        public float mass, maxMass;
        public bool alive = true, handled;
        public Snake killer;
        public int kills;

        public Stats stats = new Stats();
        public readonly Dictionary<string, int> upgrades = new Dictionary<string, int>();
        public int shields;
        float shieldTimer;
        public float invuln, phase, phantomCooldown, freeBoost, chomp, biteCooldown;

        public bool wantBoost, boosting;
        float dropAccum;

        // Player-only roguelite data.
        public int level = 1, rerolls;
        public float xp, xpNext;

        public bool isBoss, noGrow; // bosses keep their size
        public bool eaten;

        public BotBrain brain;
        public readonly List<Seg> segments = new List<Seg>();
        public readonly SnakeModel model;

        public Snake(GameManager game, SkinData skin, string name, float x, float z, float angle, float mass, bool isPlayer)
        {
            this.game = game;
            this.skin = skin;
            this.name = name;
            this.angle = targetAngle = angle;
            this.mass = maxMass = mass;
            this.isPlayer = isPlayer;
            int n = TargetSegments();
            for (int i = 0; i < n; i++)
                AddSegment(x - Mathf.Cos(angle) * i * Spacing, z - Mathf.Sin(angle) * i * Spacing);
            model = new SnakeModel(skin);
        }

        public float Radius { get { return Config.BaseRadius * (1 + Mathf.Sqrt(Mathf.Max(0, mass)) / 12f) * stats.sizeMul; } }
        public float Spacing { get { return Radius * 0.75f; } }
        public Seg Head { get { return segments[0]; } }
        // The "Lv" shown above every snake. Higher Lv eats lower Lv on touch.
        public int Lv { get { return Mathf.FloorToInt(mass); } }

        int TargetSegments() { return Mathf.Min(Config.MaxSegments, Mathf.FloorToInt(6 + mass * 0.5f)); }

        void AddSegment(float x, float z)
        {
            segments.Add(new Seg { x = x, z = z, owner = this, idx = segments.Count });
        }

        public void Grow(float amount)
        {
            if (amount > 0 && chomp <= 0) chomp = 0.22f;
            if (noGrow && amount > 0) return;
            mass += amount;
            maxMass = Mathf.Max(maxMass, mass);
        }

        public void Update(float dt)
        {
            // --- Timers ---
            invuln = Mathf.Max(0, invuln - dt);
            phase = Mathf.Max(0, phase - dt);
            phantomCooldown = Mathf.Max(0, phantomCooldown - dt);
            freeBoost = Mathf.Max(0, freeBoost - dt);
            chomp = Mathf.Max(0, chomp - dt);
            biteCooldown = Mathf.Max(0, biteCooldown - dt);
            if (stats.regen > 0) mass += stats.regen * dt;
            if (shields < stats.shieldMax)
            {
                shieldTimer += dt;
                if (shieldTimer >= stats.shieldRecharge) { shields++; shieldTimer = 0; }
            }

            // --- Steering: turn toward targetAngle, limited by turn rate ---
            MapDef map = game.map;
            float turnRate = Config.TurnRate * stats.turnMul * map.turnMul / (1 + (Radius - Config.BaseRadius) * 0.35f);
            float diff = Mathf.DeltaAngle(angle * Mathf.Rad2Deg, targetAngle * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            float maxTurn = turnRate * dt;
            angle += Mathf.Clamp(diff, -maxTurn, maxTurn);

            // --- Boost: faster, but you lose mass which drops behind you as food ---
            bool wasBoosting = boosting;
            boosting = wantBoost && (mass > Config.MinBoostMass || freeBoost > 0);
            if (boosting && !wasBoosting && stats.phantom && phantomCooldown <= 0)
            {
                phase = 1.2f;
                phantomCooldown = 8;
            }
            float speed = Config.BaseSpeed * stats.speedMul * map.speedMul;
            if (boosting)
            {
                speed *= Config.BoostMultiplier * stats.boostMul;
                if (freeBoost <= 0)
                {
                    float lost = Config.BoostCostPerSec * stats.boostCostMul * dt;
                    mass -= lost;
                    dropAccum += lost;
                    if (dropAccum >= 1)
                    {
                        Seg tail = segments[segments.Count - 1];
                        game.food.Spawn(tail.x, tail.z, dropAccum * 0.7f, skin.A, false, false);
                        dropAccum = 0;
                    }
                }
            }

            // --- Move the head, then every segment follows the one in front ---
            Seg h = segments[0];
            h.x += Mathf.Cos(angle) * speed * dt;
            h.z += Mathf.Sin(angle) * speed * dt;
            float sp = Spacing;
            for (int i = 1; i < segments.Count; i++)
            {
                Seg prev = segments[i - 1], s = segments[i];
                float dx = s.x - prev.x, dz = s.z - prev.z;
                float d = Mathf.Sqrt(dx * dx + dz * dz);
                if (d > 0.0001f)
                {
                    s.x = prev.x + dx / d * sp;
                    s.z = prev.z + dz / d * sp;
                }
            }

            // --- Grow / shrink the body to match mass ---
            int target = TargetSegments();
            while (segments.Count < target)
            {
                Seg last = segments[segments.Count - 1];
                AddSegment(last.x, last.z);
            }
            while (segments.Count > target && segments.Count > 4) segments.RemoveAt(segments.Count - 1);
        }

        public void Draw(float time)
        {
            float c = chomp > 0 ? Mathf.Sin(chomp / 0.22f * Mathf.PI) : 0;
            model.Draw(segments, Radius, angle, time, new SnakeFx
            {
                jaw = Mathf.Max(c, boosting ? 0.45f : 0.05f),
                boosting = boosting,
                phase = phase > 0,
                invuln = invuln > 0,
            });
        }

        // Yellow = you. Red = bigger than you (it can eat you!). White = you can eat it.
        public Color LabelColor()
        {
            if (isPlayer) return Toon.Hex("#ffe066");
            if (isBoss) return Toon.Hex("#ff3b3b");
            Snake p = game.player;
            if (p != null && p.alive && Lv >= p.Lv) return Toon.Hex("#ff7a7a");
            return Color.white;
        }

        public void Destroy() { model.Destroy(); }
    }
}
