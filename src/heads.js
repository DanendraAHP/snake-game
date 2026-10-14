import * as THREE from 'three';
import { RoundedBoxGeometry } from 'three/addons/geometries/RoundedBoxGeometry.js';
import { PartBuilder, toon, OUTLINE_MAT } from './toon.js';

// 3D heads built from simple shapes. A head is ~1 unit "radius", faces +X,
// and has a separate lower JAW group that can swing open (chomp!).

const RB = (w, h, d, r = 0.25, seg = 2) => new RoundedBoxGeometry(w, h, d, seg, r);
const SPHERE = new THREE.SphereGeometry(1, 16, 12);
const TOOTH = new THREE.ConeGeometry(0.085, 0.3, 5);
const FANG = new THREE.ConeGeometry(0.11, 0.45, 6);
const HORN = new THREE.ConeGeometry(0.17, 1.0, 7);
const SMALL_SPIKE = new THREE.ConeGeometry(0.1, 0.35, 5);
const DOWN = [Math.PI, 0, 0];

function eyes(b, x, y, z, size) {
  for (const side of [1, -1]) {
    b.add('eye', SPHERE, { p: [x, y, side * z], s: [size, size, size * 0.7] });
    // Slit pupil, like a reptile.
    b.add('dark', SPHERE, { p: [x + size * 0.15, y, side * (z + size * 0.55)], s: [size * 0.28, size * 0.85, size * 0.25] }, false);
    b.add('white', SPHERE, { p: [x + size * 0.35, y + size * 0.35, side * (z + size * 0.5)], s: [size * 0.15, size * 0.15, size * 0.12] }, false);
  }
}

function dragon(upper, jaw, viper) {
  if (viper) {
    upper.add('skin', RB(1.45, 0.78, 1.7, 0.3), { p: [0.05, 0.08, 0] });
    upper.add('skin', RB(1.05, 0.5, 1.1, 0.22), { p: [0.95, 0.02, 0] });
    eyes(upper, 0.4, 0.4, 0.66, 0.28);
    for (const side of [1, -1]) {
      upper.add('skin', RB(0.45, 0.12, 0.26, 0.05), { p: [0.3, 0.68, side * 0.55], r: [side * 0.35, 0, -0.15] });
      upper.add('white', FANG, { p: [1.15, -0.32, side * 0.3], r: DOWN }, false);
      upper.add('dark', SPHERE, { p: [1.45, 0.18, side * 0.22], s: [0.06, 0.06, 0.06] }, false);
    }
    jaw.add('belly', RB(1.45, 0.26, 1.2, 0.1), { p: [0.85, -0.13, 0] });
    jaw.add('mouth', new THREE.BoxGeometry(1.3, 0.05, 0.95), { p: [0.85, 0.02, 0] }, false);
    return 0.5; // height of the top of the head (for hats)
  }

  // Dragon: big skull, long snout, teeth, horns.
  upper.add('skin', RB(1.5, 1.05, 1.45, 0.35), { p: [0, 0.15, 0] });
  upper.add('skin', RB(1.4, 0.6, 1.15, 0.25), { p: [1.05, 0.05, 0] });
  upper.add('skin', RB(0.9, 0.22, 0.55, 0.1), { p: [1.0, 0.38, 0] });
  eyes(upper, 0.5, 0.5, 0.6, 0.27);
  for (const side of [1, -1]) {
    upper.add('skin', RB(0.45, 0.14, 0.28, 0.06), { p: [0.4, 0.84, side * 0.5], r: [side * 0.35, 0, -0.2] });
    upper.add('accent', HORN, { p: [-0.5, 0.85, side * 0.42], r: [side * 0.35, 0, 1.15] });
    upper.add('dark', SPHERE, { p: [1.7, 0.28, side * 0.24], s: [0.07, 0.07, 0.07] }, false);
    for (const x of [0.55, 0.9, 1.25]) upper.add('white', TOOTH, { p: [x, -0.3, side * 0.48], r: DOWN }, false);
    upper.add('white', FANG, { p: [1.6, -0.33, side * 0.38], r: DOWN }, false);
  }
  for (const x of [-0.6, -0.2, 0.2]) upper.add('accent', SMALL_SPIKE, { p: [x, 0.78, 0], r: [0, 0, 0.6] });

  jaw.add('belly', RB(1.8, 0.32, 1.05, 0.12), { p: [0.95, -0.15, 0] });
  jaw.add('mouth', new THREE.BoxGeometry(1.55, 0.05, 0.85), { p: [0.95, 0.02, 0] }, false);
  for (const side of [1, -1]) {
    for (const x of [0.7, 1.05, 1.4]) jaw.add('white', TOOTH, { p: [x, 0.12, side * 0.4] }, false);
  }
  return 0.75;
}

function round(upper, eyeStyle) {
  upper.add('skin', SPHERE, { p: [0, 0.05, 0], s: [1.25, 0.95, 1.1] });
  const ball = (x, y, z, size, shine) => {
    upper.add('white', SPHERE, { p: [x, y, z], s: [size, size, size] });
    upper.add('dark', SPHERE, { p: [x + size * 0.6, y, z], s: [size * 0.55, size * 0.55, size * 0.55] }, false);
    if (shine) upper.add('white', SPHERE, { p: [x + size * 0.85, y + size * 0.3, z + size * 0.15], s: [size * 0.2, size * 0.2, size * 0.2] }, false);
  };
  if (eyeStyle === 'cyclops') ball(0.65, 0.5, 0, 0.5, true);
  else {
    const big = eyeStyle === 'cute';
    for (const side of [1, -1]) ball(0.55, 0.45, side * (big ? 0.5 : 0.48), big ? 0.42 : 0.32, big);
    if (eyeStyle === 'angry') {
      for (const side of [1, -1]) upper.add('dark', new THREE.BoxGeometry(0.12, 0.09, 0.42), { p: [0.72, 0.8, side * 0.45], r: [side * 0.45, 0, 0] }, false);
    }
  }
  return 0.95;
}

function hat(b, kind, top) {
  switch (kind) {
    case 'crown':
      b.add('gold', new THREE.CylinderGeometry(0.5, 0.56, 0.34, 18, 1, true), { p: [-0.05, top + 0.15, 0] }, false);
      for (let k = 0; k < 6; k++) {
        const a = (k / 6) * Math.PI * 2;
        b.add('gold', new THREE.ConeGeometry(0.12, 0.34, 6), { p: [-0.05 + Math.cos(a) * 0.48, top + 0.47, Math.sin(a) * 0.48] });
      }
      break;
    case 'horns':
      for (const side of [1, -1]) b.add('ivory', HORN, { p: [-0.1, top + 0.35, side * 0.5], r: [side * 0.55, 0, 0.35], s: [1.1, 1.1, 1.1] });
      break;
    case 'ears':
      // Anubis-style tall ears.
      for (const side of [1, -1]) {
        b.add('skin', new THREE.ConeGeometry(0.3, 1.3, 4), { p: [-0.35, top + 0.55, side * 0.42], r: [side * 0.2, 0, 0.25], s: [1, 1, 0.45] });
        b.add('gold', new THREE.ConeGeometry(0.16, 0.85, 4), { p: [-0.3, top + 0.5, side * 0.48], r: [side * 0.2, 0, 0.25], s: [1, 1, 0.3] }, false);
      }
      break;
    case 'halo':
      b.add('halo', new THREE.TorusGeometry(0.55, 0.07, 8, 32), { p: [-0.1, top + 0.65, 0], r: [Math.PI / 2, 0, 0] }, false);
      break;
    case 'tophat':
      b.add('black', new THREE.CylinderGeometry(0.75, 0.75, 0.06, 24), { p: [-0.05, top + 0.05, 0] });
      b.add('black', new THREE.CylinderGeometry(0.42, 0.45, 0.75, 20), { p: [-0.05, top + 0.45, 0] });
      b.add('red', new THREE.CylinderGeometry(0.46, 0.46, 0.14, 20), { p: [-0.05, top + 0.17, 0] }, false);
      break;
  }
}

// Geometry doesn't depend on colors, so cache it by shape options.
const cache = new Map();
function headGeometry(skin) {
  const key = `${skin.head}|${skin.hat}|${skin.eyes}`;
  if (!cache.has(key)) {
    const upper = new PartBuilder(0.05);
    const jaw = new PartBuilder(0.04);
    const top = skin.head === 'round' ? round(upper, skin.eyes) : dragon(upper, jaw, skin.head === 'viper');
    hat(upper, skin.hat, top);
    cache.set(key, { upper: upper.build(), jaw: jaw.build() });
  }
  return cache.get(key);
}

function materialsFor(skin) {
  const glow = skin.glow ? { emissive: skin.colorA, emissiveIntensity: 0.35 } : {};
  return {
    skin: toon(skin.colorA, glow),
    belly: toon(new THREE.Color(skin.colorB).lerp(new THREE.Color(0xffffff), 0.25)),
    accent: toon(skin.colorC),
    eye: toon(skin.eyeColor || '#ffd23d'),
    white: toon(0xffffff),
    dark: new THREE.MeshBasicMaterial({ color: 0x111111 }),
    mouth: toon(0xc8102e),
    gold: toon(0xffc83d),
    ivory: toon(0xf2e6c9),
    halo: new THREE.MeshBasicMaterial({ color: 0xfff27a }),
    black: toon(0x1b1b22),
    red: toon(0xc8283c),
    outline: OUTLINE_MAT,
  };
}

function meshes(group, geos, mats) {
  for (const [slot, geo] of Object.entries(geos)) {
    const mesh = new THREE.Mesh(geo, mats[slot]);
    mesh.castShadow = slot !== 'outline';
    group.add(mesh);
  }
}

// Returns { group, jaw }. Rotate jaw.rotation.z negative to open the mouth.
export function buildHead(skin) {
  const geos = headGeometry(skin);
  const mats = materialsFor(skin);
  const group = new THREE.Group();
  meshes(group, geos.upper, mats);
  const jaw = new THREE.Group();
  jaw.position.set(-0.2, -0.15, 0);
  meshes(jaw, geos.jaw, mats);
  group.add(jaw);
  group.userData.materials = mats;
  return { group, jaw };
}

export function disposeHead(group) {
  // Geometry is cached/shared; only dispose this head's materials.
  for (const [slot, m] of Object.entries(group.userData.materials || {})) if (slot !== 'outline') m.dispose();
}
