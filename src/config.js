// All the "feel" numbers of the game live here. Tweak them and reload!
export const CONFIG = {
  arenaRadius: 140, // size of the circular map (each map in maps.js sets its own)
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

  // Snake Clash rule: a snake with a HIGHER Lv eats a lower one on any touch.
  eatAbsorb: 0.5, // the eater instantly gains this much of the victim's mass
  botEatAbsorb: 0.35, // bots absorb less, so they don't snowball as fast as you
  eatDrop: 0.3, // and this much is spilled as food around the victim
  // Bites: touching a smaller snake's BODY bites off everything from there to the tail.
  biteCooldown: 0.3, // seconds between two bites of the same snake
  minMass: 6, // a snake bitten smaller than this dies

  // Bosses
  bossFirst: 90, // seconds until the first boss
  bossInterval: 150, // seconds between bosses
  bossWarning: 10, // warning shown this many seconds before
  bossStay: 75, // the boss leaves after this many seconds if you survive

  startRerolls: 2,

  // Testing switch: true = infinite coins, every shop skin is free.
  // Set to false to go back to earning coins by playing.
  unlimitedCoins: true,
};

// XP needed to go from `level` to `level + 1`.
export function xpForLevel(level) {
  return Math.round(5 + 3 * level + 0.8 * level * level);
}
