using UnityEngine;

namespace SerpentRogue
{
    // All the "feel" numbers of the game live here. Tweak them and press Play!
    public static class Config
    {
        public const float ArenaRadius = 140f;   // size of the circular map
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
