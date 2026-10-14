import * as THREE from 'three';
import { CONFIG } from './config.js';
import { SpatialHash } from './grid.js';

const GEO = new THREE.IcosahedronGeometry(0.32, 0);
const PALETTE = ['#ff5d73', '#ffd25d', '#5dff9b', '#5dd6ff', '#b45dff', '#ff8f3d', '#ffffff'].map((c) => new THREE.Color(c));
const GOLD = new THREE.Color('#ffcc33');
const m4 = new THREE.Matrix4();
const q = new THREE.Quaternion();
const up = new THREE.Vector3(0, 1, 0);
const pos = new THREE.Vector3();
const scl = new THREE.Vector3();

// All food is drawn with ONE InstancedMesh. Items are plain objects.
export class FoodField {
  constructor(scene) {
    this.items = [];
    this.natural = 0; // how many naturally spawned food are on the map
    this.grid = new SpatialHash(4);
    this.mesh = new THREE.InstancedMesh(GEO, new THREE.MeshBasicMaterial({ color: 0xffffff }), CONFIG.foodCapacity);
    this.mesh.instanceMatrix.setUsage(THREE.DynamicDrawUsage);
    this.mesh.frustumCulled = false;
    this.mesh.count = 0;
    scene.add(this.mesh);
  }

  clear() {
    this.items.length = 0;
    this.natural = 0;
  }

  spawn(x, z, value, { natural = false, golden = false, color = null } = {}) {
    if (this.items.length >= CONFIG.foodCapacity) return null;
    const f = {
      x, z, value, natural, golden, alive: true,
      color: golden ? GOLD : color ? new THREE.Color(color) : PALETTE[Math.floor(Math.random() * PALETTE.length)],
      phase: Math.random() * Math.PI * 2,
    };
    this.items.push(f);
    if (natural) this.natural++;
    return f;
  }

  spawnRandom(goldenChance) {
    const r = CONFIG.arenaRadius * 0.97 * Math.sqrt(Math.random());
    const a = Math.random() * Math.PI * 2;
    const golden = Math.random() < goldenChance;
    const value = golden ? 5 : Math.random() < 0.15 ? 2 : 1;
    this.spawn(Math.cos(a) * r, Math.sin(a) * r, value, { natural: true, golden });
  }

  // Keep the map stocked with food.
  maintain(goldenChance, perFrame = 25) {
    for (let i = 0; i < perFrame && this.natural < CONFIG.foodCount; i++) this.spawnRandom(goldenChance);
  }

  rebuildGrid() {
    this.grid.clear();
    for (const f of this.items) if (f.alive) this.grid.insert(f.x, f.z, f);
  }

  // Remove eaten food (done once per frame so indices stay stable during the frame).
  compact() {
    let w = 0;
    for (let i = 0; i < this.items.length; i++) {
      const f = this.items[i];
      if (f.alive) this.items[w++] = f;
      else if (f.natural) this.natural--;
    }
    this.items.length = w;
  }

  render(time) {
    for (let i = 0; i < this.items.length; i++) {
      const f = this.items[i];
      const s = (0.7 + Math.sqrt(f.value) * 0.45) * (f.golden ? 1.4 : 1);
      q.setFromAxisAngle(up, time * 1.5 + f.phase);
      pos.set(f.x, 0.55 + Math.sin(time * 2.5 + f.phase) * 0.15, f.z);
      scl.set(s, s, s);
      m4.compose(pos, q, scl);
      this.mesh.setMatrixAt(i, m4);
      this.mesh.setColorAt(i, f.color);
    }
    this.mesh.count = this.items.length;
    this.mesh.instanceMatrix.needsUpdate = true;
    if (this.mesh.instanceColor) this.mesh.instanceColor.needsUpdate = true;
  }
}
