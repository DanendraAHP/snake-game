using System;
using System.Collections.Generic;
using UnityEngine;

namespace SerpentRogue
{
    // Everything an upgrade can change.
    public class Stats
    {
        public float speedMul = 1, turnMul = 1, boostMul = 1, boostCostMul = 1;
        public float magnet;          // extra food attraction radius
        public float foodMul = 1;     // growth per food
        public float xpMul = 1, killXpMul = 1;
        public float regen;           // mass per second
        public int shieldMax;
        public float shieldRecharge = 30;
        public float goldenBonus;     // extra golden food spawn chance
        public float dropMul = 1;     // food dropped by your victims
        public float sizeMul = 1, zoomMul = 1;
        public int revives;
        public bool phantom, bloodrush;
    }

    public class UpgradeDef
    {
        public string id, icon, name, rarity, desc;
        public int max;
        public Action<Snake> apply;
    }

    // Roguelite power-ups. To add one: add an entry here, then use the stat in Snake.cs / GameManager.cs.
    public static class Upgrades
    {
        static UpgradeDef U(string id, string icon, string name, string rarity, int max, string desc, Action<Snake> apply)
        {
            return new UpgradeDef { id = id, icon = icon, name = name, rarity = rarity, max = max, desc = desc, apply = apply };
        }

        public static readonly List<UpgradeDef> All = new List<UpgradeDef>
        {
            U("speed", "SPD", "Swift Scales", "common", 5, "+10% movement speed.", s => s.stats.speedMul *= 1.1f),
            U("turn", "TRN", "Serpent Agility", "common", 4, "+20% turning speed.", s => s.stats.turnMul *= 1.2f),
            U("magnet", "MAG", "Food Magnet", "common", 5, "Pull food from +2.5 units further away.", s => s.stats.magnet += 2.5f),
            U("glutton", "EAT", "Glutton", "common", 5, "+30% growth from food.", s => s.stats.foodMul += 0.3f),
            U("scholar", "XP", "Fast Learner", "common", 5, "+20% XP from everything.", s => s.stats.xpMul += 0.2f),
            U("regen", "REG", "Photosynthesis", "common", 4, "Passively grow +0.5 mass per second.", s => s.stats.regen += 0.5f),
            U("hawk", "EYE", "Hawk's Eye", "common", 3, "Camera zooms out 12%. See danger sooner.", s => s.stats.zoomMul *= 1.12f),
            U("nitro", "NOS", "Nitro", "common", 3, "+15% boost speed.", s => s.stats.boostMul *= 1.15f),
            U("turbo", "TRB", "Turbo Glands", "rare", 3, "Boosting costs 30% less mass.", s => s.stats.boostCostMul *= 0.7f),
            U("shield", "SHD", "Ghost Shell", "rare", 3, "+1 shield. A shield absorbs one fatal crash and recharges every 30s.", s => { s.stats.shieldMax += 1; s.shields += 1; }),
            U("predator", "PRD", "Predator", "rare", 3, "Kills give +60% XP and you absorb 15% of the victim instantly.", s => s.stats.killXpMul += 0.6f),
            U("lucky", "GLD", "Golden Touch", "rare", 3, "Golden food (5x value) spawns much more often.", s => s.stats.goldenBonus += 0.04f),
            U("reaper", "RIP", "Death Bloom", "rare", 3, "Snakes you kill drop 50% more food.", s => s.stats.dropMul += 0.5f),
            U("feast", "+35", "Feast", "rare", 99, "Instantly grow +35 mass.", s => s.mass += 35),
            U("titan", "TTN", "Titan Blood", "epic", 2, "+15% body thickness and +25 mass.", s => { s.stats.sizeMul *= 1.15f; s.mass += 25; }),
            U("phantom", "GHO", "Phantom Dash", "epic", 1, "Starting a boost makes you pass through snakes for 1.2s (8s cooldown).", s => s.stats.phantom = true),
            U("secondwind", "2ND", "Second Wind", "epic", 2, "When you die, revive once with half your length.", s => s.stats.revives += 1),
            U("bloodrush", "BLD", "Bloodrush", "epic", 1, "After each kill, boosting is free for 4 seconds.", s => s.stats.bloodrush = true),
        };

        public static UpgradeDef ById(string id)
        {
            foreach (var u in All) if (u.id == id) return u;
            return null;
        }

        public static int Weight(string rarity)
        {
            switch (rarity) { case "common": return 60; case "rare": return 30; default: return 10; }
        }

        public static Color RarityColor(string rarity)
        {
            switch (rarity) { case "rare": return Toon.Hex("#4fa9ff"); case "epic": return Toon.Hex("#c76bff"); default: return Toon.Hex("#9aa5d6"); }
        }

        // Pick `count` different upgrades, weighted by rarity, skipping maxed ones.
        public static List<UpgradeDef> Roll(Dictionary<string, int> owned, int count = 3)
        {
            var pool = new List<UpgradeDef>();
            foreach (var u in All)
            {
                int have;
                owned.TryGetValue(u.id, out have);
                if (have < u.max) pool.Add(u);
            }
            var choices = new List<UpgradeDef>();
            while (choices.Count < count && pool.Count > 0)
            {
                int total = 0;
                foreach (var u in pool) total += Weight(u.rarity);
                float r = UnityEngine.Random.value * total;
                int idx = 0;
                while (idx < pool.Count - 1 && r > Weight(pool[idx].rarity))
                {
                    r -= Weight(pool[idx].rarity);
                    idx++;
                }
                choices.Add(pool[idx]);
                pool.RemoveAt(idx);
            }
            return choices;
        }

        public static void Apply(Snake s, UpgradeDef u)
        {
            int have;
            s.upgrades.TryGetValue(u.id, out have);
            s.upgrades[u.id] = have + 1;
            u.apply(s);
        }
    }
}
