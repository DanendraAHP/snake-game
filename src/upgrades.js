// Roguelite power-ups. Each one modifies the snake's `stats` when picked.
// To add a new upgrade: add an entry here, then use the stat in snake.js/game.js.

export function baseStats() {
  return {
    speedMul: 1,
    turnMul: 1,
    boostMul: 1,
    boostCostMul: 1,
    magnet: 0, // extra food attraction radius
    foodMul: 1, // growth per food
    xpMul: 1,
    killXpMul: 1,
    regen: 0, // mass per second
    shieldMax: 0,
    shieldRecharge: 30, // seconds per shield charge
    goldenBonus: 0, // extra golden food spawn chance
    dropMul: 1, // food dropped by your victims
    sizeMul: 1,
    zoomMul: 1,
    revives: 0,
    phantom: false,
    bloodrush: false,
  };
}

export const RARITY = {
  common: { weight: 60, label: 'Common' },
  rare: { weight: 30, label: 'Rare' },
  epic: { weight: 10, label: 'Epic' },
};

export const UPGRADES = [
  { id: 'speed', icon: '⚡', name: 'Swift Scales', rarity: 'common', max: 5,
    desc: '+10% movement speed.', apply: (s) => { s.stats.speedMul *= 1.1; } },
  { id: 'turn', icon: '🌀', name: 'Serpent Agility', rarity: 'common', max: 4,
    desc: '+20% turning speed.', apply: (s) => { s.stats.turnMul *= 1.2; } },
  { id: 'magnet', icon: '🧲', name: 'Food Magnet', rarity: 'common', max: 5,
    desc: 'Pull food from +2.5 units further away.', apply: (s) => { s.stats.magnet += 2.5; } },
  { id: 'glutton', icon: '🍖', name: 'Glutton', rarity: 'common', max: 5,
    desc: '+30% growth from food.', apply: (s) => { s.stats.foodMul += 0.3; } },
  { id: 'scholar', icon: '📘', name: 'Fast Learner', rarity: 'common', max: 5,
    desc: '+20% XP from everything.', apply: (s) => { s.stats.xpMul += 0.2; } },
  { id: 'regen', icon: '🌿', name: 'Photosynthesis', rarity: 'common', max: 4,
    desc: 'Passively grow +0.5 mass per second.', apply: (s) => { s.stats.regen += 0.5; } },
  { id: 'hawk', icon: '🦅', name: "Hawk's Eye", rarity: 'common', max: 3,
    desc: 'Camera zooms out 12%. See danger sooner.', apply: (s) => { s.stats.zoomMul *= 1.12; } },
  { id: 'nitro', icon: '🚀', name: 'Nitro', rarity: 'common', max: 3,
    desc: '+15% boost speed.', apply: (s) => { s.stats.boostMul *= 1.15; } },
  { id: 'turbo', icon: '🔥', name: 'Turbo Glands', rarity: 'rare', max: 3,
    desc: 'Boosting costs 30% less mass.', apply: (s) => { s.stats.boostCostMul *= 0.7; } },
  { id: 'shield', icon: '🛡️', name: 'Ghost Shell', rarity: 'rare', max: 3,
    desc: '+1 shield. A shield absorbs one fatal crash and recharges every 30s.',
    apply: (s) => { s.stats.shieldMax += 1; s.shields += 1; } },
  { id: 'predator', icon: '🩸', name: 'Predator', rarity: 'rare', max: 3,
    desc: 'Kills give +60% XP and you absorb 15% of the victim instantly.',
    apply: (s) => { s.stats.killXpMul += 0.6; } },
  { id: 'lucky', icon: '✨', name: 'Golden Touch', rarity: 'rare', max: 3,
    desc: 'Golden food (5x value) spawns much more often.', apply: (s) => { s.stats.goldenBonus += 0.04; } },
  { id: 'reaper', icon: '💀', name: 'Death Bloom', rarity: 'rare', max: 3,
    desc: 'Snakes you kill drop 50% more food.', apply: (s) => { s.stats.dropMul += 0.5; } },
  { id: 'feast', icon: '🍰', name: 'Feast', rarity: 'rare', max: 99,
    desc: 'Instantly grow +35 mass.', apply: (s) => { s.mass += 35; } },
  { id: 'titan', icon: '🗿', name: 'Titan Blood', rarity: 'epic', max: 2,
    desc: '+15% body thickness and +25 mass.', apply: (s) => { s.stats.sizeMul *= 1.15; s.mass += 25; } },
  { id: 'phantom', icon: '👻', name: 'Phantom Dash', rarity: 'epic', max: 1,
    desc: 'Starting a boost makes you pass through snakes for 1.2s (8s cooldown).',
    apply: (s) => { s.stats.phantom = true; } },
  { id: 'secondwind', icon: '❤️‍🔥', name: 'Second Wind', rarity: 'epic', max: 2,
    desc: 'When you die, revive once with half your length.', apply: (s) => { s.stats.revives += 1; } },
  { id: 'bloodrush', icon: '😈', name: 'Bloodrush', rarity: 'epic', max: 1,
    desc: 'After each kill, boosting is free for 4 seconds.', apply: (s) => { s.stats.bloodrush = true; } },
];

export const UPGRADES_BY_ID = Object.fromEntries(UPGRADES.map((u) => [u.id, u]));

// Pick `count` different upgrades, weighted by rarity, skipping maxed ones.
export function rollChoices(owned, count = 3) {
  const pool = UPGRADES.filter((u) => (owned[u.id] || 0) < u.max);
  const choices = [];
  while (choices.length < count && pool.length) {
    const total = pool.reduce((sum, u) => sum + RARITY[u.rarity].weight, 0);
    let r = Math.random() * total;
    let idx = 0;
    while (r > RARITY[pool[idx].rarity].weight) {
      r -= RARITY[pool[idx].rarity].weight;
      idx++;
    }
    choices.push(pool.splice(idx, 1)[0]);
  }
  return choices;
}

export function applyUpgrade(snake, upgrade) {
  snake.upgrades[upgrade.id] = (snake.upgrades[upgrade.id] || 0) + 1;
  upgrade.apply(snake);
}
