import * as THREE from 'three';

// ---------------------------------------------------------------------------
// A skin is a small plain object (easy to save as JSON):
//   head:    dragon | viper | round
//   body:    scales | blocky | smooth
//   spikes:  none | spikes | fins | plates      (what grows on the back)
//   pattern: how colorA / colorB are spread along the body
//   colorA (main), colorB (second / belly), colorC (accent: horns, spikes)
//   eyeColor, eyes (round head only), hat, glow
//   optional: belly (bool, a 2nd-color underside), spikeColor, maneColor
// ---------------------------------------------------------------------------

export const OPTIONS = {
  head: [
    { id: 'dragon', label: 'Dragon' },
    { id: 'viper', label: 'Viper' },
    { id: 'round', label: 'Cute' },
    { id: 'bone', label: 'Bone Skull' },
    { id: 'block', label: 'Blocky' },
    { id: 'eastern', label: 'Eastern Dragon' },
  ],
  body: [
    { id: 'scales', label: 'Scales' },
    { id: 'blocky', label: 'Blocky' },
    { id: 'smooth', label: 'Smooth' },
    { id: 'bone', label: 'Bone' },
    { id: 'cube', label: 'Cube' },
  ],
  spikes: [
    { id: 'none', label: 'None' },
    { id: 'spikes', label: 'Spikes' },
    { id: 'fins', label: 'Flame Fins' },
    { id: 'plates', label: 'Plates' },
    { id: 'ribs', label: 'Bone Spikes' },
    { id: 'cubes', label: 'Cubes' },
  ],
  pattern: [
    { id: 'solid', label: 'Solid' },
    { id: 'stripes', label: 'Stripes' },
    { id: 'bands', label: 'Bands' },
    { id: 'spots', label: 'Spots' },
    { id: 'gradient', label: 'Gradient' },
    { id: 'rainbow', label: 'Rainbow' },
  ],
  eyes: [
    { id: 'normal', label: 'Normal' },
    { id: 'cute', label: 'Cute' },
    { id: 'angry', label: 'Angry' },
    { id: 'cyclops', label: 'Cyclops' },
  ],
  hat: [
    { id: 'none', label: 'None' },
    { id: 'crown', label: 'Crown' },
    { id: 'horns', label: 'Horns' },
    { id: 'ears', label: 'Jackal Ears' },
    { id: 'halo', label: 'Halo' },
    { id: 'tophat', label: 'Top Hat' },
    { id: 'mane', label: 'Wild Mane' },
  ],
};

const base = { head: 'dragon', body: 'scales', spikes: 'spikes', pattern: 'solid', eyes: 'normal', hat: 'none', glow: false, eyeColor: '#ffd23d' };
const S = (id, label, rarity, price, o) => ({ ...base, ...o, id, label, rarity, price });

// The skin shop. Prices are in coins, earned at the end of every run.
export const CATALOG = [
  S('jungle', 'Jungle Viper', 'common', 0, { head: 'viper', spikes: 'none', pattern: 'stripes', colorA: '#3bd16f', colorB: '#17703f', colorC: '#d8ff6b' }),
  S('oriole', 'Violet Spots', 'common', 150, { head: 'viper', spikes: 'none', pattern: 'spots', colorA: '#8a3fd1', colorB: '#d6a8ff', colorC: '#ffffff' }),
  S('coral', 'Coral Snake', 'common', 200, { head: 'viper', body: 'smooth', spikes: 'none', pattern: 'bands', colorA: '#e8323c', colorB: '#1b1b22', colorC: '#ffd23d' }),
  S('bubble', 'Bubblegum', 'common', 250, { head: 'round', body: 'smooth', spikes: 'none', pattern: 'gradient', eyes: 'cute', colorA: '#ff7ac8', colorB: '#7ad7ff', colorC: '#ffffff' }),
  S('ice', 'Ice Wyrm', 'rare', 600, { pattern: 'gradient', colorA: '#9ff0ff', colorB: '#2d6cdf', colorC: '#ffffff', eyeColor: '#7fe8ff' }),
  S('toxic', 'Toxic Drake', 'rare', 650, { pattern: 'stripes', colorA: '#9be52f', colorB: '#2f6b1c', colorC: '#ffe93d' }),
  S('sand', 'Sandstorm', 'rare', 700, { head: 'viper', body: 'blocky', spikes: 'plates', pattern: 'bands', colorA: '#e3b874', colorB: '#8a5a2b', colorC: '#5a3a1a' }),
  S('python', 'Royal Python', 'rare', 900, { head: 'viper', pattern: 'spots', spikes: 'none', hat: 'crown', colorA: '#ffc83d', colorB: '#2a1d10', colorC: '#ffffff' }),
  S('fire', 'Fire Dragon', 'epic', 1800, { spikes: 'fins', pattern: 'gradient', glow: true, colorA: '#ff3b1f', colorB: '#ffb52e', colorC: '#ffd23d', eyeColor: '#ffe066' }),
  S('storm', 'Storm Serpent', 'epic', 1800, { pattern: 'stripes', glow: true, colorA: '#2f8bff', colorB: '#123a8f', colorC: '#e8f4ff', eyeColor: '#ffe93d' }),
  S('rainbow', 'Prism Drake', 'epic', 2200, { pattern: 'rainbow', colorA: '#ff4d4d', colorB: '#ffffff', colorC: '#ffffff' }),
  S('sakura', 'Sakura', 'epic', 2000, { spikes: 'fins', pattern: 'gradient', colorA: '#ff9ccf', colorB: '#fff0f6', colorC: '#ffc83d', eyeColor: '#ff4d8d' }),
  S('anubis', 'Anubis', 'legend', 5000, { body: 'blocky', spikes: 'plates', hat: 'ears', colorA: '#2a2a33', colorB: '#3a3a46', colorC: '#ffc83d', eyeColor: '#ffae00' }),
  S('inferno', 'Inferno King', 'legend', 5500, { spikes: 'fins', pattern: 'gradient', hat: 'crown', glow: true, colorA: '#ffd23d', colorB: '#ff2a00', colorC: '#ff6a00', eyeColor: '#ff2a00' }),
  // Mythic: inspired by famous dragons.
  S('zabimaru', 'Zabimaru', 'mythic', 9000, { head: 'bone', body: 'bone', spikes: 'ribs', hat: 'mane', pattern: 'stripes', colorA: '#efe6cf', colorB: '#cdb88f', colorC: '#efe6cf', spikeColor: '#e8dcc0', maneColor: '#d2232a', eyeColor: '#ffd400' }),
  S('ender', 'Ender Dragon', 'mythic', 9500, { head: 'block', body: 'cube', spikes: 'cubes', pattern: 'stripes', colorA: '#1d1d24', colorB: '#2b2b35', colorC: '#5d5d68', spikeColor: '#70707c', eyeColor: '#e04dff' }),
  S('shenron', 'Shenron', 'mythic', 10000, { head: 'eastern', body: 'scales', spikes: 'spikes', belly: true, colorA: '#2f9e44', colorB: '#f0a040', colorC: '#d1a868', spikeColor: '#1f6b2e', maneColor: '#1f6b2e', eyeColor: '#ff2a2a' }),
  S('void', 'Void Leviathan', 'legend', 6000, { spikes: 'fins', pattern: 'gradient', glow: true, colorA: '#8a2be2', colorB: '#140a26', colorC: '#ff3bd4', eyeColor: '#ff3bd4' }),
];
export const CATALOG_BY_ID = Object.fromEntries(CATALOG.map((s) => [s.id, s]));

export const RARITIES = [
  { id: 'common', label: 'Common' },
  { id: 'rare', label: 'Rare' },
  { id: 'epic', label: 'Epic' },
  { id: 'legend', label: 'Legend' },
  { id: 'mythic', label: 'Mythic' },
];

// Boss-only skins (not sold in the shop).
export const BOSS_SKINS = [
  S('boss_wyrm', 'Crimson Wyrm', 'boss', 0, { spikes: 'fins', pattern: 'gradient', hat: 'crown', glow: true, colorA: '#c4001d', colorB: '#2a0008', colorC: '#ffd23d', eyeColor: '#ffe600' }),
  S('boss_tyrant', 'Bone Tyrant', 'boss', 0, { head: 'bone', body: 'bone', spikes: 'ribs', hat: 'horns', pattern: 'bands', colorA: '#3a3a3a', colorB: '#e8dcc0', colorC: '#e8dcc0', spikeColor: '#e8dcc0', eyeColor: '#ff2a00' }),
  S('boss_leviathan', 'Abyss Leviathan', 'boss', 0, { head: 'eastern', spikes: 'fins', belly: true, glow: true, pattern: 'gradient', colorA: '#102a8f', colorB: '#00e5ff', colorC: '#7af7ff', spikeColor: '#00e5ff', maneColor: '#00e5ff', eyeColor: '#ffffff' }),
];

export const DEFAULT_CUSTOM = { ...CATALOG_BY_ID.jungle, id: 'custom', label: 'My Custom', rarity: 'custom', price: 0 };

// ---------------------------------------------------------------------------
// Profile: name, coins, owned skins, equipped skin. Saved in the browser.
// ---------------------------------------------------------------------------
const PROFILE_KEY = 'serpentRogue.profile';

export function loadProfile() {
  let p = null;
  try {
    p = JSON.parse(localStorage.getItem(PROFILE_KEY));
  } catch {
    /* storage unavailable */
  }
  return {
    name: 'Player',
    coins: 300,
    owned: ['jungle'],
    equipped: 'jungle',
    ...p,
    custom: { ...DEFAULT_CUSTOM, ...(p?.custom || {}) },
  };
}

export function saveProfile(profile) {
  try {
    localStorage.setItem(PROFILE_KEY, JSON.stringify(profile));
  } catch {
    /* ignore */
  }
}

export function equippedSkin(profile) {
  if (profile.equipped === 'custom') return profile.custom;
  return CATALOG_BY_ID[profile.equipped] || CATALOG[0];
}

// ---------------------------------------------------------------------------
// Bots
// ---------------------------------------------------------------------------
const pick = (arr) => arr[Math.floor(Math.random() * arr.length)];
const BOT_WEIGHT = { common: 6, rare: 4, epic: 2, legend: 1, mythic: 1 };

export function randomBotSkin() {
  if (Math.random() < 0.3) return randomCustomSkin();
  const total = CATALOG.reduce((s, k) => s + BOT_WEIGHT[k.rarity], 0);
  let r = Math.random() * total;
  for (const k of CATALOG) {
    r -= BOT_WEIGHT[k.rarity];
    if (r <= 0) return k;
  }
  return CATALOG[0];
}

export function randomCustomSkin() {
  const h = Math.random();
  const col = (hh, s, l) => '#' + new THREE.Color().setHSL(hh % 1, s, l).getHexString();
  return {
    ...base,
    id: 'random',
    head: pick(OPTIONS.head).id,
    body: pick(OPTIONS.body).id,
    spikes: pick(OPTIONS.spikes).id,
    pattern: pick(OPTIONS.pattern).id,
    eyes: pick(OPTIONS.eyes).id,
    hat: Math.random() < 0.35 ? pick(OPTIONS.hat).id : 'none',
    glow: Math.random() < 0.2,
    colorA: col(h, 0.75, 0.55),
    colorB: col(h + 0.08 + Math.random() * 0.5, 0.7, 0.35 + Math.random() * 0.3),
    colorC: col(h + 0.5, 0.8, 0.7),
    eyeColor: col(0.1 + Math.random() * 0.1, 1, 0.6),
  };
}

const NAMES = [
  'Noodle', 'Slinky', 'Viper', 'Zigzag', 'Mamba', 'Wiggles', 'Boa', 'Sssam', 'Kaa', 'Fang',
  'Cobra', 'Hissy', 'Twisty', 'Python', 'Nibbles', 'Sidewinder', 'Coily', 'Basilisk', 'Rattle', 'Spaghetti',
  'Rachel', 'Josh', 'Lin', 'Biggus', 'Snoops', 'Speedy', 'Gomes', 'Jinx',
];
export function randomBotName() {
  return pick(NAMES) + (Math.random() < 0.4 ? Math.floor(Math.random() * 99) : '');
}

// ---------------------------------------------------------------------------
// Colors along the body
// ---------------------------------------------------------------------------
const ca = new THREE.Color();
const cb = new THREE.Color();
const hsl = {};

// Color of body segment i (0 = head) for a snake with n segments.
export function segmentColor(skin, i, n, out) {
  ca.set(skin.colorA);
  cb.set(skin.colorB);
  switch (skin.pattern) {
    case 'stripes':
      return out.copy(Math.floor(i / 2) % 2 ? cb : ca);
    case 'bands':
      return out.copy(Math.floor(i / 5) % 2 ? cb : ca);
    case 'spots':
      return out.copy(i % 4 === 2 ? cb : ca);
    case 'gradient':
      // Ping-pong so long snakes keep showing both colors.
      return out.copy(ca).lerp(cb, 0.5 - 0.5 * Math.cos((i / 24) * Math.PI));
    case 'rainbow':
      ca.getHSL(hsl);
      return out.setHSL((hsl.h + i * 0.03) % 1, 0.85, 0.58);
    default:
      return out.copy(ca);
  }
}
