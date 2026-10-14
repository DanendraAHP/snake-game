import * as THREE from 'three';
import { CONFIG } from './config.js';
import { buildHead, disposeHead, segmentColor } from './skins.js';

// SnakeModel only knows how to DRAW a snake (no game rules).
// The body is one InstancedMesh: hundreds of spheres in a single draw call.
const SEGMENT_GEO = new THREE.SphereGeometry(1, 14, 10);
const m4 = new THREE.Matrix4();
const col = new THREE.Color();

export class SnakeModel {
  constructor(skin) {
    this.group = new THREE.Group();
    this.bodyMat = new THREE.MeshStandardMaterial({ color: 0xffffff, roughness: 0.4, metalness: 0.15 });
    this.body = new THREE.InstancedMesh(SEGMENT_GEO, this.bodyMat, CONFIG.maxSegments);
    this.body.instanceMatrix.setUsage(THREE.DynamicDrawUsage);
    this.body.castShadow = true;
    this.body.receiveShadow = true;
    this.body.frustumCulled = false; // the body moves every frame, skip bounds checks
    this.body.count = 0;
    this.group.add(this.body);
    this.head = null;
    this.opacity = 1;
    this.setSkin(skin);
  }

  setSkin(skin) {
    this.skin = skin;
    if (this.head) {
      this.group.remove(this.head);
      disposeHead(this.head);
    }
    this.head = buildHead(skin);
    this.group.add(this.head);
    this.coloredCount = -1; // force recolor
    this.glowBase = skin.glow ? 0.35 : 0;
    this.bodyMat.emissive.set(skin.colorA);
    this.bodyMat.emissiveIntensity = this.glowBase;
    this.opacity = 1;
  }

  setOpacity(alpha) {
    if (alpha === this.opacity) return;
    this.opacity = alpha;
    const apply = (m) => {
      m.transparent = alpha < 1;
      m.opacity = alpha;
      m.needsUpdate = true;
    };
    apply(this.bodyMat);
    this.head.traverse((o) => o.isMesh && apply(o.material));
  }

  // segs: [{x, z}, ...] head first. fx: visual effect flags.
  update(segs, radius, angle, time, fx = {}) {
    const n = Math.min(segs.length, CONFIG.maxSegments + 1);
    const bodyN = n - 1;

    // Recolor only when the length changes (patterns depend on index/length).
    if (bodyN !== this.coloredCount) {
      for (let j = 0; j < bodyN; j++) {
        segmentColor(this.skin, j + 1, n, col);
        this.body.setColorAt(j, col);
      }
      if (this.body.instanceColor) this.body.instanceColor.needsUpdate = true;
      this.coloredCount = bodyN;
    }

    // Body spheres, tapering toward the tail.
    const taperStart = bodyN * 0.7;
    for (let j = 0; j < bodyN; j++) {
      const s = segs[j + 1];
      let k = 1;
      if (j > taperStart) k = 1 - (0.55 * (j - taperStart)) / (bodyN - taperStart);
      const r = radius * k;
      m4.makeScale(r, r, r);
      m4.setPosition(s.x, r, s.z);
      this.body.setMatrixAt(j, m4);
    }
    this.body.count = bodyN;
    this.body.instanceMatrix.needsUpdate = true;

    const h = segs[0];
    this.head.position.set(h.x, radius, h.z);
    this.head.rotation.y = -angle;
    this.head.scale.setScalar(radius * 1.05);

    // Effects: brighter while boosting, see-through while phased, blink while invulnerable.
    this.bodyMat.emissiveIntensity = fx.boosting ? 0.55 + Math.sin(time * 20) * 0.15 : this.glowBase;
    this.setOpacity(fx.phase ? 0.35 : 1);
    this.group.visible = fx.invuln ? Math.sin(time * 28) > -0.3 : true;
  }

  dispose() {
    this.group.removeFromParent();
    this.bodyMat.dispose();
    this.body.dispose();
    disposeHead(this.head);
  }
}
