import * as THREE from 'three';

// ---------------------------------------------------------------------------
// Skin data: a skin is just a small plain object, easy to save as JSON.
// ---------------------------------------------------------------------------

export const PATTERNS = [
  { id: 'solid', label: 'Solid' },
  { id: 'stripes', label: 'Stripes' },
  { id: 'bands', label: 'Bands' },
  { id: 'spots', label: 'Spots' },
  { id: 'gradient', label: 'Gradient' },
  { id: 'rainbow', label: 'Rainbow' },
];

export const EYES = [
  { id: 'normal', label: 'Normal' },
  { id: 'cute', label: 'Cute' },
  { id: 'angry', label: 'Angry' },
  { id: 'cyclops', label: 'Cyclops' },
];

export const HATS = [
  { id: 'none', label: 'None' },
  { id: 'crown', label: 'Crown' },
  { id: 'horns', label: 'Horns' },
  { id: 'halo', label: 'Halo' },
  { id: 'tophat', label: 'Top Hat' },
];

export const DEFAULT_SKIN = {
  name: 'You',
  pattern: 'stripes',
  colorA: '#3be37a',
  colorB: '#16794a',
  eyes: 'normal',
  hat: 'none',
  glow: false,
};

const STORAGE_KEY = 'serpentRogue.skin';

export function loadSkin() {
  try {
    const saved = JSON.parse(localStorage.getItem(STORAGE_KEY));
    if (saved) return { ...DEFAULT_SKIN, ...saved };
  } catch {
    /* storage unavailable: fall back to default */
  }
  return { ...DEFAULT_SKIN };
}

export function saveSkin(skin) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(skin));
  } catch {
    /* ignore */
  }
}

const pick = (arr) => arr[Math.floor(Math.random() * arr.length)];

export function randomSkin() {
  const h = Math.random();
  const a = new THREE.Color().setHSL(h, 0.75, 0.55);
  const b = new THREE.Color().setHSL((h + 0.08 + Math.random() * 0.5) % 1, 0.7, 0.35 + Math.random() * 0.3);
  return {
    name: randomBotName(),
    pattern: pick(PATTERNS).id,
    colorA: '#' + a.getHexString(),
    colorB: '#' + b.getHexString(),
    eyes: pick(EYES).id,
    hat: Math.random() < 0.45 ? pick(HATS).id : 'none',
    glow: Math.random() < 0.2,
  };
}

const NAMES = [
  'Noodle', 'Slinky', 'Viper', 'Zigzag', 'Mamba', 'Wiggles', 'Boa', 'Sssam', 'Kaa', 'Fang',
  'Cobra', 'Hissy', 'Twisty', 'Python', 'Nibbles', 'Sidewinder', 'Coily', 'Basilisk', 'Rattle', 'Spaghetti',
];
export function randomBotName() {
  return pick(NAMES) + (Math.random() < 0.4 ? Math.floor(Math.random() * 99) : '');
}

// ---------------------------------------------------------------------------
// Turning a skin into colors and 3D meshes
// ---------------------------------------------------------------------------

const ca = new THREE.Color();
const cb = new THREE.Color();

// Color of body segment i (0 = head) for a snake with n segments.
export function segmentColor(skin, i, n, out) {
  ca.set(skin.colorA);
  cb.set(skin.colorB);
  switch (skin.pattern) {
    case 'stripes':
      return out.copy(Math.floor(i / 2) % 2 ? cb : ca);
    case 'bands':
      return out.copy(Math.floor(i / 6) % 2 ? cb : ca);
    case 'spots':
      return out.copy(i % 5 === 2 ? cb : ca);
    case 'gradient':
      return out.copy(ca).lerp(cb, n > 1 ? i / (n - 1) : 0);
    case 'rainbow': {
      const hsl = {};
      ca.getHSL(hsl);
      return out.setHSL((hsl.h + i * 0.025) % 1, 0.85, 0.55);
    }
    default:
      return out.copy(ca);
  }
}

// Shared geometries (created once, reused by every snake).
const GEO = {
  head: new THREE.SphereGeometry(1, 24, 16),
  eye: new THREE.SphereGeometry(1, 14, 10),
  brow: new THREE.BoxGeometry(0.12, 0.09, 0.42),
  crownBand: new THREE.CylinderGeometry(0.5, 0.55, 0.32, 18, 1, true),
  crownSpike: new THREE.ConeGeometry(0.11, 0.3, 8),
  horn: new THREE.ConeGeometry(0.16, 0.75, 12),
  halo: new THREE.TorusGeometry(0.55, 0.07, 8, 32),
  hatTop: new THREE.CylinderGeometry(0.42, 0.45, 0.75, 20),
  hatBrim: new THREE.CylinderGeometry(0.75, 0.75, 0.06, 24),
  hatBand: new THREE.CylinderGeometry(0.46, 0.46, 0.14, 20),
};

function eye(group, x, y, z, size, mats, highlight) {
  const white = new THREE.Mesh(GEO.eye, mats.white);
  white.scale.setScalar(size);
  white.position.set(x, y, z);
  const pupil = new THREE.Mesh(GEO.eye, mats.pupil);
  pupil.scale.setScalar(size * 0.55);
  pupil.position.set(x + size * 0.6, y, z);
  group.add(white, pupil);
  if (highlight) {
    const spark = new THREE.Mesh(GEO.eye, mats.white);
    spark.scale.setScalar(size * 0.2);
    spark.position.set(x + size * 0.85, y + size * 0.3, z + size * 0.15);
    group.add(spark);
  }
}

// Builds a head of radius ~1 facing +X. The snake scales/rotates it.
export function buildHead(skin) {
  const group = new THREE.Group();
  const mats = {
    skin: new THREE.MeshStandardMaterial({
      color: skin.colorA,
      roughness: 0.4,
      metalness: 0.15,
      emissive: skin.glow ? skin.colorA : 0x000000,
      emissiveIntensity: skin.glow ? 0.35 : 0,
    }),
    white: new THREE.MeshStandardMaterial({ color: 0xffffff, roughness: 0.2 }),
    pupil: new THREE.MeshStandardMaterial({ color: 0x111111, roughness: 0.1 }),
  };

  const head = new THREE.Mesh(GEO.head, mats.skin);
  head.scale.set(1.25, 0.95, 1.1);
  head.castShadow = true;
  group.add(head);

  switch (skin.eyes) {
    case 'cute':
      eye(group, 0.55, 0.45, 0.5, 0.42, mats, true);
      eye(group, 0.55, 0.45, -0.5, 0.42, mats, true);
      break;
    case 'cyclops':
      eye(group, 0.65, 0.5, 0, 0.5, mats, true);
      break;
    case 'angry': {
      eye(group, 0.6, 0.42, 0.48, 0.3, mats, false);
      eye(group, 0.6, 0.42, -0.48, 0.3, mats, false);
      for (const side of [1, -1]) {
        const brow = new THREE.Mesh(GEO.brow, mats.pupil);
        brow.position.set(0.72, 0.78, side * 0.45);
        brow.rotation.x = side * 0.45; // inner end lower = angry look
        group.add(brow);
      }
      break;
    }
    default:
      eye(group, 0.6, 0.45, 0.48, 0.32, mats, false);
      eye(group, 0.6, 0.45, -0.48, 0.32, mats, false);
  }

  switch (skin.hat) {
    case 'crown': {
      const gold = new THREE.MeshStandardMaterial({ color: 0xffc83d, metalness: 0.8, roughness: 0.25, side: THREE.DoubleSide });
      const band = new THREE.Mesh(GEO.crownBand, gold);
      band.position.y = 0.98;
      group.add(band);
      for (let k = 0; k < 6; k++) {
        const a = (k / 6) * Math.PI * 2;
        const spike = new THREE.Mesh(GEO.crownSpike, gold);
        spike.position.set(Math.cos(a) * 0.48, 1.28, Math.sin(a) * 0.48);
        group.add(spike);
      }
      break;
    }
    case 'horns': {
      const ivory = new THREE.MeshStandardMaterial({ color: 0xf2e6c9, roughness: 0.5 });
      for (const side of [1, -1]) {
        const horn = new THREE.Mesh(GEO.horn, ivory);
        horn.position.set(-0.1, 1.05, side * 0.45);
        horn.rotation.x = side * 0.5;
        horn.rotation.z = 0.3;
        group.add(horn);
      }
      break;
    }
    case 'halo': {
      const halo = new THREE.Mesh(GEO.halo, new THREE.MeshBasicMaterial({ color: 0xfff27a }));
      halo.rotation.x = Math.PI / 2;
      halo.position.y = 1.55;
      group.add(halo);
      break;
    }
    case 'tophat': {
      const black = new THREE.MeshStandardMaterial({ color: 0x1b1b22, roughness: 0.6 });
      const red = new THREE.MeshStandardMaterial({ color: 0xc8283c, roughness: 0.5 });
      const brim = new THREE.Mesh(GEO.hatBrim, black);
      brim.position.y = 0.9;
      const top = new THREE.Mesh(GEO.hatTop, black);
      top.position.y = 1.3;
      const band = new THREE.Mesh(GEO.hatBand, red);
      band.position.y = 1.02;
      group.add(brim, top, band);
      break;
    }
  }
  return group;
}

export function disposeHead(group) {
  // Geometries are shared, only materials belong to this head.
  group.traverse((o) => {
    if (o.isMesh) o.material.dispose();
  });
}
