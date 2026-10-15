using System.Collections.Generic;
using UnityEngine;

namespace SerpentRogue
{
    // A simple but effective bot brain. Rule of the game: a HIGHER Lv snake eats
    // a lower Lv snake on any touch. So bots:
    //  1. RUN from bigger snakes that get close.
    //  2. CHASE smaller snakes (any part of their body is food!).
    //  3. Otherwise eat orbs, or wander.
    // Every move is checked with "feelers" for danger (bigger snakes, wall, lava).
    public class BotBrain
    {
        static readonly float[] Feelers = { 0, -0.4f, 0.4f, -0.85f, 0.85f, -1.4f, 1.4f, -2.1f, 2.1f };
        static readonly List<Seg> segBuffer = new List<Seg>();
        static readonly List<Food> foodBuffer = new List<Food>();

        enum Mode { Wander, Food, Hunt, Flee }

        readonly Snake s;
        readonly GameManager g;
        readonly bool boss;
        readonly float skill, aggression;
        float thinkTimer, retarget, wander, dashTimer = 3;
        Mode mode = Mode.Wander;
        bool hasGoal;
        Vector2 goal;
        Food goalFood;

        public BotBrain(Snake snake, GameManager game, bool isBoss = false)
        {
            s = snake;
            g = game;
            boss = isBoss;
            skill = boss ? 1 : 0.55f + Random.value * 0.45f; // higher = reacts faster
            aggression = boss ? 1 : 0.35f + Random.value * 0.55f;
            wander = snake.angle;
        }

        // Can `o` eat me? (bigger or equal Lv is dangerous)
        bool Dangerous(Snake o) { return o.Lv >= s.Lv && o.phase <= 0; }

        static float Dist(float ax, float az, float bx, float bz)
        {
            float dx = ax - bx, dz = az - bz;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        public void Think(float dt)
        {
            if (boss)
            {
                // Bosses dash every few seconds without losing length.
                dashTimer -= dt;
                if (dashTimer <= 0)
                {
                    s.freeBoost = 1.6f;
                    dashTimer = 4 + Random.value * 3;
                }
            }

            thinkTimer -= dt;
            if (thinkTimer > 0) return;
            float interval = 0.05f + (1 - skill) * 0.15f;
            thinkTimer = interval;

            retarget -= interval;
            if (retarget <= 0 || mode == Mode.Flee || mode == Mode.Hunt)
            {
                retarget = 0.35f + Random.value * 0.4f;
                PickGoal();
            }

            float goalA = GoalAngle();
            float look = 5 + s.Radius * 4;
            float target = goalA;
            if (Probe(goalA, look) > 0 || Probe(s.angle, look) > 0)
            {
                // Danger! Choose the safest feeler, preferring ones closer to the goal.
                float best = float.MaxValue;
                foreach (float off in Feelers)
                {
                    float a = s.angle + off;
                    float score = Probe(a, look) * 10 + Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, goalA * Mathf.Rad2Deg) * Mathf.Deg2Rad) * 0.6f + Mathf.Abs(off) * 0.2f;
                    if (score < best) { best = score; target = a; }
                }
            }
            s.targetAngle = target;

            Seg h = s.Head;
            float d = hasGoal ? Dist(goal.x, goal.y, h.x, h.z) : float.MaxValue;
            s.wantBoost = s.freeBoost > 0
                || (mode == Mode.Flee && s.mass > 20)
                || (mode == Mode.Hunt && d < 12 && s.mass > 25)
                || (mode == Mode.Food && goalFood != null && goalFood.value >= 3 && d < 10 && s.mass > 30);
        }

        void PickGoal()
        {
            Seg h = s.Head;
            hasGoal = false;
            goalFood = null;

            // 1. Flee: a bigger snake's head is close.
            Snake threat = null;
            float threatD = 10 + s.Radius * 4;
            foreach (Snake o in g.snakes)
            {
                if (o == s || !o.alive || !Dangerous(o)) continue;
                float d = Dist(o.Head.x, o.Head.z, h.x, h.z) - o.Radius * 2;
                if (d < threatD) { threatD = d; threat = o; }
            }
            if (threat != null && !boss)
            {
                float away = Mathf.Atan2(h.z - threat.Head.z, h.x - threat.Head.x);
                mode = Mode.Flee;
                SetGoal(h.x + Mathf.Cos(away) * 20, h.z + Mathf.Sin(away) * 20);
                return;
            }

            // 2. Hunt: the closest piece of any smaller snake (the player is a favorite).
            float aggr = Mathf.Min(1, aggression + (g.threat - 1) * 0.08f);
            if (Random.value < aggr)
            {
                Seg best = null;
                float bestD = boss ? 200 : 26;
                g.segGrid.Query(h.x, h.z, boss ? 60 : 26, segBuffer);
                foreach (Seg seg in segBuffer)
                {
                    Snake o = seg.owner;
                    if (o == s || !o.alive || o.Lv >= s.Lv || o.invuln > 0 || o.phase > 0) continue;
                    float d = Dist(seg.x, seg.z, h.x, h.z) - (o.isPlayer ? 8 : 0);
                    if (d < bestD) { bestD = d; best = seg; }
                }
                // Bosses always know where the player is.
                Snake p = g.player;
                if (boss && best == null && p != null && p.alive && p.Lv < s.Lv) best = p.Head;
                if (best != null)
                {
                    mode = Mode.Hunt;
                    SetGoal(best.x, best.z);
                    return;
                }
            }

            // 3. Food: best value / distance within range.
            float bestScore = 0;
            g.food.grid.Query(h.x, h.z, 22, foodBuffer);
            foreach (Food f in foodBuffer)
            {
                if (!f.alive) continue;
                float score = f.value / (Dist(f.x, f.z, h.x, h.z) + 2);
                if (score > bestScore) { bestScore = score; goalFood = f; }
            }
            if (goalFood != null)
            {
                mode = Mode.Food;
                SetGoal(goalFood.x, goalFood.z);
                return;
            }

            // 4. Wander, drifting back toward the center when far out.
            mode = Mode.Wander;
            wander += (Random.value - 0.5f) * 1.6f;
            if (Mathf.Sqrt(h.x * h.x + h.z * h.z) > Config.ArenaRadius * 0.6f) wander = Mathf.Atan2(-h.z, -h.x);
        }

        void SetGoal(float x, float z)
        {
            hasGoal = true;
            goal = new Vector2(x, z);
        }

        float GoalAngle()
        {
            Seg h = s.Head;
            if (mode == Mode.Food && goalFood != null && goalFood.alive) return Mathf.Atan2(goalFood.z - h.z, goalFood.x - h.x);
            if (hasGoal && mode != Mode.Food) return Mathf.Atan2(goal.y - h.z, goal.x - h.x);
            return wander;
        }

        // Danger score for moving in direction `a` (0 = safe).
        float Probe(float a, float look)
        {
            Seg h = s.Head;
            float cx = Mathf.Cos(a), cz = Mathf.Sin(a);
            float reach = s.Radius + g.maxSegRadius + 0.8f;
            float danger = 0;
            for (int k = 1; k <= 3; k++)
            {
                float d = look * k / 3;
                float px = h.x + cx * d, pz = h.z + cz * d;
                if (Mathf.Sqrt(px * px + pz * pz) > Config.ArenaRadius - s.Radius * 2 - 2 || g.InLava(px, pz, s.Radius + 1.5f))
                {
                    danger += 4 - k;
                    continue;
                }
                g.segGrid.Query(px, pz, reach, segBuffer);
                foreach (Seg seg in segBuffer)
                {
                    Snake o = seg.owner;
                    if (o == s || !o.alive || !Dangerous(o)) continue;
                    float rr = s.Radius + o.Radius + 0.5f;
                    float dx = seg.x - px, dz = seg.z - pz;
                    if (dx * dx + dz * dz < rr * rr)
                    {
                        danger += 4 - k; // closer danger counts more
                        break;
                    }
                }
            }
            return danger;
        }
    }
}
