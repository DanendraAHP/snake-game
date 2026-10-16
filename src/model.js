import * as THREE from 'three';
import { RoundedBoxGeometry } from 'three/addons/geometries/RoundedBoxGeometry.js';
import { CONFIG } from './config.js';
import { buildHead, disposeHead } from './heads.js';
import { segmentColor } from './skins.js';
import { toon, OUTLINE_MAT, fatten, PartBuilder } from './toon.js';

// SnakeModel only knows how to DRAW a snake (no game rules).
// Body parts are InstancedMeshes: hundreds of copies in one draw call.
// Each body segment is drawn in its own local space: +X = forward, +Y = up.

const OUTLINE = 0.07;

function bodyGeometry(style) {
  if (style === 'smooth') return new THREE.SphereGeometry(1, 14, 10);
  if (style === 'blocky') return new RoundedBoxGeometry(1.35, 1.45, 1.75, 1, 0.14);
  if (style === 'cube') return new THREE.BoxGeometry(1.4, 1.4, 1.7);
  if (style === 'bone') return new RoundedBoxGeometry(1.15, 1.35, 1.6, 2, 0.3);
  return new RoundedBoxGeometry(1.45, 1.55, 1.85, 2, 0.42); // scales
}

function finShape(w, h) {
  const s = new THREE.Shape();
  s.moveTo(w * 0.45, 0);
  s.lineTo(-w * 0.55, 0);
  s.lineTo(-w * 0.9, h);
  s.quadraticCurveTo(-w * 0.1, h * 0.55, w * 0.45, 0);
  const g = new THREE.ExtrudeGeometry(s, { depth: 0.1, bevelEnabled: false });
  g.translate(0, 0, -0.05);
  return g;
}

// What grows on the back of every segment. Returns { main, outline } or null.
function spikeGeometry(style) {
  const b = new PartBuilder(0.04);
  if (style === 'spikes') {
    b.add('main', new THREE.ConeGeometry(0.26, 0.9, 6), { p: [-0.15, 1.05, 0], r: [0, 0, 0.6] });
  } else if (style === 'fins') {
    b.add('main', finShape(1, 1.25), { p: [0, 0.55, 0] });
    for (const side of [1, -1]) b.add('main', finShape(0.8, 0.85), { p: [0, 0.2, side * 0.75], r: [side * 1.1, 0, 0] });
  } else if (style === 'plates') {
    for (const side of [1, -1]) b.add('main', new THREE.BoxGeometry(0.42, 0.6, 0.14), { p: [0, 0.8, side * 0.5], r: [side * 0.55, 0, 0] });
    b.add('main', new THREE.BoxGeometry(0.5, 0.18, 1.2), { p: [0, 0.62, 0] });
  } else if (style === 'ribs') {
    // Long bone spikes sticking out sideways (Zabimaru).
    for (const side of [1, -1]) b.add('main', new THREE.ConeGeometry(0.11, 2.0, 6), { p: [-0.35, 1.0, side * 1.05], r: [side * 0.95, 0, 0.55] });
  } else if (style === 'cubes') {
    b.add('main', new THREE.BoxGeometry(0.38, 0.5, 0.34), { p: [0, 0.9, 0] });
  } else {
    return null;
  }
  return b.build();
}

// Put spikes on every Nth segment only (long bone spikes would be too crowded).
const SPIKE_EVERY = { ribs: 3, cubes: 2 };

// A wider, flatter plate under each segment in the second color (Shenron's belly).
function bellyGeometry() {
  const g = new RoundedBoxGeometry(1.3, 0.6, 2.15, 2, 0.25);
  g.translate(0, -0.42, 0);
  return g;
}

// Geometries are shared between all snakes with the same style.
const geoCache = new Map();
function cached(key, make) {
  if (!geoCache.has(key)) geoCache.set(key, make());
  return geoCache.get(key);
}

function instanced(geo, mat, shadow) {
  const m = new THREE.InstancedMesh(geo, mat, CONFIG.maxSegments);
  m.instanceMatrix.setUsage(THREE.DynamicDrawUsage);
  m.frustumCulled = false; // the body moves every frame, skip bounds checks
  m.castShadow = shadow;
  m.count = 0;
  return m;
}

const m4 = new THREE.Matrix4();
const tilt = new THREE.Matrix4();
const scaleV = new THREE.Vector3();
const col = new THREE.Color();
const HIDDEN = new THREE.Matrix4().makeScale(0, 0, 0);

export class SnakeModel {
  constructor(skin, { label = false } = {}) {
    this.group = new THREE.Group();
    this.parts = [];
    this.withLabel = label;
    this.setSkin(skin);
  }

  setSkin(skin) {
    this.clearParts();
    this.skin = skin;
    const glow = skin.glow ? { emissive: 0xffffff, emissiveIntensity: 0 } : {};

    // Body plates (colored per instance).
    const geo = cached('body:' + skin.body, () => bodyGeometry(skin.body));
    const outlineGeo = cached('bodyOutline:' + skin.body, () => fatten(geo, OUTLINE));
    this.bodyMat = toon(0xffffff, { emissive: skin.colorA, emissiveIntensity: skin.glow ? 0.3 : 0 });
    this.glowBase = skin.glow ? 0.3 : 0;
    this.body = instanced(geo, this.bodyMat, true);
    this.bodyOutline = instanced(outlineGeo, OUTLINE_MAT, false);
    this.group.add(this.bodyOutline, this.body);

    // Spikes / fins / plates along the back.
    const sg = cached('spikes:' + skin.spikes, () => spikeGeometry(skin.spikes));
    this.spikes = this.spikesOutline = null;
    if (sg) {
      const sc = skin.spikeColor || skin.colorC;
      this.spikeMat = toon(sc, glow.emissive ? { emissive: sc, emissiveIntensity: 0.4 } : {});
      this.spikes = instanced(sg.main, this.spikeMat, true);
      this.spikesOutline = instanced(sg.outline, OUTLINE_MAT, false);
      this.group.add(this.spikesOutline, this.spikes);
    }

    // Optional belly in the second color.
    this.belly = this.bellyOutline = null;
    if (skin.belly) {
      const bg = cached('belly', bellyGeometry);
      this.bellyMat = toon(skin.colorB);
      this.belly = instanced(bg, this.bellyMat, false);
      this.bellyOutline = instanced(cached('bellyOutline', () => fatten(bg, OUTLINE)), OUTLINE_MAT, false);
      this.group.add(this.bellyOutline, this.belly);
    }
    this.spikeEvery = SPIKE_EVERY[skin.spikes] || 1;

    const head = buildHead(skin);
    this.head = head.group;
    this.hinge = head.hinge;
    this.group.add(this.head);

    this.yOffset = skin.body === 'smooth' ? 1 : 0.8;
    tilt.makeRotationZ(skin.body === 'smooth' ? 0 : 0.14); // front of each plate tilts up -> overlapping scales
    this.coloredCount = -1;
    this.opacity = 1;
    this.labelText = '';
    if (this.withLabel && !this.label) this.label = makeLabel();
    if (this.label) this.group.add(this.label);
  }

  clearParts() {
    if (!this.body) return;
    this.group.clear();
    this.bodyMat.dispose();
    this.spikeMat?.dispose();
    this.bellyMat?.dispose();
    this.spikeMat = this.bellyMat = null;
    for (const m of [this.body, this.bodyOutline, this.spikes, this.spikesOutline, this.belly, this.bellyOutline]) m?.dispose();
    disposeHead(this.head);
  }

  setOpacity(alpha) {
    if (alpha === this.opacity) return;
    this.opacity = alpha;
    this.group.traverse((o) => {
      if (!o.isMesh || o.material === OUTLINE_MAT) return;
      o.material.transparent = alpha < 1;
      o.material.opacity = alpha;
      o.material.needsUpdate = true;
    });
    for (const m of [this.bodyOutline, this.spikesOutline, this.bellyOutline]) if (m) m.visible = alpha === 1;
  }

  // segs: [{x, z}, ...] head first. fx: visual effects + label.
  update(segs, radius, angle, time, fx = {}) {
    const n = Math.min(segs.length, CONFIG.maxSegments + 1);
    const bodyN = n - 1;

    // Recolor only when the length changes (patterns depend on index).
    if (bodyN !== this.coloredCount) {
      for (let j = 0; j < bodyN; j++) {
        segmentColor(this.skin, j + 1, n, col);
        this.body.setColorAt(j, col);
      }
      if (this.body.instanceColor) this.body.instanceColor.needsUpdate = true;
      this.coloredCount = bodyN;
    }

    const taperStart = bodyN * 0.65;
    const spikeEnd = bodyN * 0.9;
    for (let j = 0; j < bodyN; j++) {
      const s = segs[j + 1];
      const front = segs[j];
      let k = 1;
      if (j > taperStart) k = 1 - (0.6 * (j - taperStart)) / (bodyN - taperStart);
      const r = radius * k;
      // Face the segment toward the one in front of it.
      const yaw = Math.atan2(front.z - s.z, front.x - s.x);
      m4.makeRotationY(-yaw).multiply(tilt).scale(scaleV.set(r, r, r));
      m4.setPosition(s.x, r * this.yOffset, s.z);
      this.body.setMatrixAt(j, m4);
      this.bodyOutline.setMatrixAt(j, m4);
      if (this.spikes) {
        const mat = j < spikeEnd && j % this.spikeEvery === 0 ? m4 : HIDDEN;
        this.spikes.setMatrixAt(j, mat);
        this.spikesOutline.setMatrixAt(j, mat);
      }
      if (this.belly) {
        this.belly.setMatrixAt(j, m4);
        this.bellyOutline.setMatrixAt(j, m4);
      }
    }
    for (const m of [this.body, this.bodyOutline, this.spikes, this.spikesOutline, this.belly, this.bellyOutline]) {
      if (!m) continue;
      m.count = bodyN;
      m.instanceMatrix.needsUpdate = true;
    }

    const h = segs[0];
    const headScale = radius * 1.4; // big cartoon heads
    this.head.position.set(h.x, radius * this.yOffset, h.z);
    this.head.rotation.y = -angle;
    this.head.scale.setScalar(headScale);
    // Jaw: chomp when eating, open wide while boosting.
    const open = fx.jaw ?? 0.05;
    this.hinge.rotation.z = open * 0.38; // the upper head lifts up

    // Effects: brighter while boosting, see-through while phased, blink while invulnerable.
    this.bodyMat.emissiveIntensity = fx.boosting ? 0.45 + Math.sin(time * 20) * 0.15 : this.glowBase;
    this.setOpacity(fx.phase ? 0.35 : 1);
    this.group.visible = fx.invuln ? Math.sin(time * 28) > -0.3 : true;

    if (this.label) {
      this.label.visible = !!fx.label;
      if (fx.label) {
        const key = fx.label + fx.labelColor; // redraw only when text or color changes
        if (key !== this.labelText) {
          this.labelText = key;
          drawLabel(this.label, fx.label, fx.labelColor);
        }
        const s = 1.7 + radius * 1.2;
        this.label.scale.set(s * 2.4, s, 1);
        this.label.position.set(h.x, radius * 3 + s * 0.5, h.z - radius * 2.2); // above the head on screen
      }
    }
  }

  dispose() {
    this.clearParts();
    this.group.removeFromParent();
    if (this.label) {
      this.label.material.map.dispose();
      this.label.material.dispose();
    }
  }
}

// ---------------------------------------------------------------------------
// Floating "Lv 182 / Name" label (a sprite always facing the camera).
// ---------------------------------------------------------------------------
function makeLabel() {
  const canvas = document.createElement('canvas');
  canvas.width = 384;
  canvas.height = 160;
  const tex = new THREE.CanvasTexture(canvas);
  tex.colorSpace = THREE.SRGBColorSpace;
  const sprite = new THREE.Sprite(new THREE.SpriteMaterial({ map: tex, depthTest: false, transparent: true }));
  sprite.renderOrder = 10;
  sprite.userData.canvas = canvas;
  return sprite;
}

function drawLabel(sprite, text, color = '#ffffff') {
  const canvas = sprite.userData.canvas;
  const g = canvas.getContext('2d');
  g.clearRect(0, 0, canvas.width, canvas.height);
  g.textAlign = 'center';
  g.lineJoin = 'round';
  const [top, bottom] = text.split('\n');
  const line = (t, y, size) => {
    g.font = `900 ${size}px "Trebuchet MS", "Segoe UI", sans-serif`;
    g.lineWidth = size * 0.22;
    g.strokeStyle = '#0a0a12';
    g.strokeText(t, canvas.width / 2, y);
    g.fillStyle = color;
    g.fillText(t, canvas.width / 2, y);
  };
  line(top, 66, 58);
  if (bottom) line(bottom, 138, 54);
  sprite.material.map.needsUpdate = true;
}
