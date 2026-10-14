import * as THREE from 'three';
import { mergeGeometries } from 'three/addons/utils/BufferGeometryUtils.js';

// Cartoon look = toon shading (a few flat light bands) + black outlines.

// 3-step light ramp used by every MeshToonMaterial.
const ramp = new THREE.DataTexture(new Uint8Array([90, 175, 255]), 3, 1, THREE.RedFormat);
ramp.minFilter = ramp.magFilter = THREE.NearestFilter;
ramp.needsUpdate = true;

export function toon(color, { emissive = null, emissiveIntensity = 0 } = {}) {
  return new THREE.MeshToonMaterial({
    color,
    gradientMap: ramp,
    emissive: emissive ?? 0x000000,
    emissiveIntensity,
  });
}

// Outline trick ("inverted hull"): draw a slightly fatter copy of the mesh
// showing only its back faces, in black. It peeks out around the edges.
export const OUTLINE_MAT = new THREE.MeshBasicMaterial({ color: 0x0a0a12, side: THREE.BackSide });

// Grow a geometry by `t` units on every side, around its own center.
export function fatten(geo, t) {
  const g = geo.clone();
  g.computeBoundingBox();
  const box = g.boundingBox;
  const c = box.getCenter(new THREE.Vector3());
  const size = box.getSize(new THREE.Vector3());
  g.translate(-c.x, -c.y, -c.z);
  g.scale(1 + (2 * t) / Math.max(size.x, 1e-3), 1 + (2 * t) / Math.max(size.y, 1e-3), 1 + (2 * t) / Math.max(size.z, 1e-3));
  g.translate(c.x, c.y, c.z);
  return g;
}

const tmpM = new THREE.Matrix4();
const tmpQ = new THREE.Quaternion();
const tmpE = new THREE.Euler();

// Collects many small shapes into a few merged geometries (one per material
// "slot") so a whole head is only a handful of draw calls.
export class PartBuilder {
  constructor(outline = 0.05) {
    this.slots = {};
    this.outline = outline;
  }

  add(slot, geo, { p = [0, 0, 0], r = [0, 0, 0], s = [1, 1, 1] } = {}, outline = true) {
    const g = geo.index ? geo.toNonIndexed() : geo.clone();
    tmpM.compose(new THREE.Vector3(...p), tmpQ.setFromEuler(tmpE.set(...r)), new THREE.Vector3(...s));
    g.applyMatrix4(tmpM);
    (this.slots[slot] ||= []).push(g);
    if (outline && this.outline > 0) (this.slots.outline ||= []).push(fatten(g, this.outline));
    return this;
  }

  // -> { slotName: BufferGeometry }
  build() {
    const out = {};
    for (const [slot, list] of Object.entries(this.slots)) {
      for (const g of list) {
        // Merging needs identical attribute sets.
        for (const name of Object.keys(g.attributes)) if (!['position', 'normal', 'uv'].includes(name)) g.deleteAttribute(name);
        if (!g.attributes.uv) g.setAttribute('uv', new THREE.Float32BufferAttribute(new Float32Array((g.attributes.position.count) * 2), 2));
      }
      out[slot] = mergeGeometries(list);
    }
    return out;
  }
}
