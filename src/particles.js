import * as THREE from 'three';

const MAX = 800;
const m4 = new THREE.Matrix4();

// Tiny "juice" system: little cubes that burst out and fall.
export class Particles {
  constructor(scene) {
    this.list = [];
    this.mesh = new THREE.InstancedMesh(new THREE.BoxGeometry(0.25, 0.25, 0.25), new THREE.MeshBasicMaterial(), MAX);
    this.mesh.frustumCulled = false;
    this.mesh.count = 0;
    scene.add(this.mesh);
  }

  burst(x, z, color, count = 20, speed = 8, y = 1) {
    const c = new THREE.Color(color);
    for (let i = 0; i < count && this.list.length < MAX; i++) {
      const a = Math.random() * Math.PI * 2;
      const v = speed * (0.4 + Math.random() * 0.6);
      this.list.push({
        x, y, z,
        vx: Math.cos(a) * v, vy: 4 + Math.random() * 6, vz: Math.sin(a) * v,
        life: 0.6 + Math.random() * 0.6, max: 1.2, color: c,
      });
    }
  }

  clear() {
    this.list.length = 0;
  }

  update(dt) {
    let w = 0;
    for (const p of this.list) {
      p.life -= dt;
      if (p.life <= 0) continue;
      p.vy -= 18 * dt;
      p.x += p.vx * dt;
      p.y = Math.max(0.1, p.y + p.vy * dt);
      p.z += p.vz * dt;
      const s = Math.min(1, p.life / 0.4) * 1.3;
      m4.makeScale(s, s, s);
      m4.setPosition(p.x, p.y, p.z);
      this.mesh.setMatrixAt(w, m4);
      this.mesh.setColorAt(w, p.color);
      this.list[w++] = p;
    }
    this.list.length = w;
    this.mesh.count = w;
    this.mesh.instanceMatrix.needsUpdate = true;
    if (this.mesh.instanceColor) this.mesh.instanceColor.needsUpdate = true;
  }
}
