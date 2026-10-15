using UnityEngine;

namespace SerpentRogue
{
    // All the "feel" numbers of the game live here. Tweak them and press Play!
    public static class Config
    {
        public static float ArenaRadius = 140f;  // size of the circular map (set by the current map)
        public const int BotCount = 12;          // AI snakes alive at once
        public const int FoodCount = 650;        // natural food kept on the map
        public const int FoodCapacity = 2600;    // hard limit incl. food dropped by dead snakes

        public const float BaseSpeed = 9f;       // units per second
        public const float BoostMultiplier = 1.85f;
        public const float BoostCostPerSec = 3.2f; // mass lost per second while boosting
        public const float MinBoostMass = 8f;
        public const float TurnRate = 3.4f;      // radians per second (big snakes turn slower)

        public const float StartMass = 14f;
        public const float MassPerFood = 0.6f;
        public const int MaxSegments = 600;
        public const float BaseRadius = 0.72f;

        public const float BaseMagnet = 1.2f;    // every snake slightly attracts nearby food
        public const float DeathDropRatio = 0.8f; // how much of a dead snake turns into food

        // Snake Clash rule: a snake with a HIGHER Lv eats a lower one on any touch.
        public const float EatAbsorb = 0.5f;      // the player instantly gains this much of the victim
        public const float BotEatAbsorb = 0.35f;  // bots absorb less, so they don't snowball as fast
        public const float EatDrop = 0.3f;        // spilled as food around the victim

        // Bosses
        public const float BossFirst = 90;     // seconds until the first boss
        public const float BossInterval = 150; // seconds between bosses
        public const float BossWarning = 10;   // warning this many seconds before
        public const float BossStay = 75;      // the boss leaves after this long if you survive
        public const int StartRerolls = 2;
        public const int StartCoins = 300;

        // Testing switch: true = infinite coins, every shop skin is free.
        public const bool UnlimitedCoins = true;

        // XP needed to go from `level` to `level + 1`.
        public static int XpForLevel(int level)
        {
            return Mathf.RoundToInt(5 + 3 * level + 0.8f * level * level);
        }
    }
}
