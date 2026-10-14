// All the "feel" numbers of the game live here. Tweak them and reload!
export const CONFIG = {
  arenaRadius: 140, // size of the circular map
  botCount: 12, // how many AI snakes are alive at once
  foodCount: 650, // natural food kept on the map
  foodCapacity: 2600, // hard limit incl. food dropped by dead snakes

  baseSpeed: 9, // units per second
  boostMultiplier: 1.85, // speed while boosting
  boostCostPerSec: 3.2, // mass lost per second while boosting
  minBoostMass: 8, // can't boost when smaller than this
  turnRate: 3.4, // radians per second (bigger snakes turn a bit slower)

  startMass: 14,
  massPerFood: 0.6, // how much a food of value 1 makes you grow
  maxSegments: 600,
  baseRadius: 0.72,

  baseMagnet: 1.2, // every snake slightly attracts nearby food
  deathDropRatio: 0.8, // how much of a dead snake's mass turns into food

  startRerolls: 2,
};

// XP needed to go from `level` to `level + 1`.
export function xpForLevel(level) {
  return Math.round(5 + 3 * level + 0.8 * level * level);
}
