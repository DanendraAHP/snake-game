using System.Collections.Generic;
using UnityEngine;

namespace SerpentRogue
{
    // A simple but effective bot brain:
    //  1. Choose a GOAL (food, a snake to cut off, or wander).
    //  2. Probe a few directions ahead for DANGER (bodies, the wall).
    //  3. Steer toward the goal unless it's dangerous, then pick the safest direction.
    public class BotBrain
    {
        static readonly float[] Feelers = { 0, -0.4f, 0.4f, -0.85f, 0.85f, -1.4f, 1.4f, -2.1f, 2.1f };
        static readonly List<Seg> segBuffer = new List<Seg>();
        static readonly List<Food> foodBuffer = new List<Food>();

        readonly Snake s;
        readonly GameManager g;
        readonly float skill, aggression;
        float thinkTimer, retarget, wander;
        Food goalFood;
        Snake prey;

        public BotBrain(Snake snake, GameManager game)
        {
            s = snake;
            g = game;
            skill = 0.55f + Random.value * 0.45f; // higher = reacts faster
            aggression = 0.15f + Random.value * 0.55f;
            wander = snake.angle;
        }

        public void Think(float dt)
        {
            thinkTimer -= dt;
            if (thinkTimer > 0) return;
            float interval = 0.05f + (1 - skill) * 0.15f;
            thinkTimer = interval;

            retarget -= interval;
            if (retarget <= 0)
            {
                retarget = 0.4f + Random.value * 0.6f;
                PickGoal();
            }

            float goal = GoalAngle();
            float look = 5 + s.Radius * 4;
            float target = goal;
            if (Probe(goal, look) > 0 || Probe(s.angle, look) > 0)
            {
                // Danger! Choose the safest feeler, preferring ones closer to the goal.
                float best = float.MaxValue;
                foreach (float off in Feelers)
                {
                    float a = s.angle + off;
                    float score = Probe(a, look) * 10 + Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, goal * Mathf.Rad2Deg) * Mathf.Deg2Rad) * 0.6f + Mathf.Abs(off) * 0.2f;
                    if (score < best) { best = score; target = a; }
                }
            }
            s.targetAngle = target;

            // Boost when hunting close prey or grabbing a juicy food nearby.
            bool boost = false;
            Seg h = s.Head;
            if (prey != null && prey.alive)
                boost = Dist(prey.Head.x, prey.Head.z, h.x, h.z) < 14 && s.mass > 25;
            else if (goalFood != null && goalFood.alive && goalFood.value >= 3)
                boost = Dist(goalFood.x, goalFood.z, h.x, h.z) < 10 && s.mass > 30;
            s.wantBoost = boost;
        }

        static float Dist(float ax, float az, float bx, float bz)
        {
            float dx = ax - bx, dz = az - bz;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        void PickGoal()
        {
            Seg h = s.Head;
            prey = null;
            goalFood = null;

            // Hunt: try to cut in front of a nearby snake (the player is a favorite target).
            float aggr = Mathf.Min(0.9f, aggression + (g.threat - 1) * 0.08f);
            if (Random.value < aggr)
            {
                float bestD = 20;
                foreach (Snake o in g.snakes)
                {
                    if (o == s || !o.alive) continue;
                    float d = Dist(o.Head.x, o.Head.z, h.x, h.z) - (o.isPlayer ? 6 : 0);
                    if (d < bestD && s.mass > o.mass * 0.6f) { bestD = d; prey = o; }
                }
                if (prey != null) return;
            }

            // Food: best value / distance within range.
            float bestScore = 0;
            g.food.grid.Query(h.x, h.z, 22, foodBuffer);
            foreach (Food f in foodBuffer)
            {
                if (!f.alive) continue;
                float score = f.value / (Dist(f.x, f.z, h.x, h.z) + 2);
                if (score > bestScore) { bestScore = score; goalFood = f; }
            }
            if (goalFood != null) return;

            // Wander, drifting back toward the center when far out.
            wander += (Random.value - 0.5f) * 1.6f;
            if (Mathf.Sqrt(h.x * h.x + h.z * h.z) > Config.ArenaRadius * 0.6f) wander = Mathf.Atan2(-h.z, -h.x);
        }

        float GoalAngle()
        {
            Seg h = s.Head;
            if (prey != null && prey.alive)
            {
                float lead = 6 + prey.Radius * 4;
                float tx = prey.Head.x + Mathf.Cos(prey.angle) * lead;
                float tz = prey.Head.z + Mathf.Sin(prey.angle) * lead;
                return Mathf.Atan2(tz - h.z, tx - h.x);
            }
            if (goalFood != null && goalFood.alive) return Mathf.Atan2(goalFood.z - h.z, goalFood.x - h.x);
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
                if (Mathf.Sqrt(px * px + pz * pz) > Config.ArenaRadius - s.Radius * 2 - 2)
                {
                    danger += 4 - k;
                    continue;
                }
                g.segGrid.Query(px, pz, reach, segBuffer);
                foreach (Seg seg in segBuffer)
                {
                    Snake o = seg.owner;
                    if (o == s || !o.alive || o.phase > 0) continue;
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
