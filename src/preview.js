import * as THREE from 'three';
import { SnakeModel } from './model.js';

// A small separate 3D view used by the skin editor.
export class SkinPreview {
  constructor(canvas) {
    this.canvas = canvas;
    this.renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    this.scene = new THREE.Scene();
    this.scene.background = new THREE.Color(0x121936);
    this.camera = new THREE.PerspectiveCamera(40, 1, 0.1, 100);
    this.scene.add(new THREE.HemisphereLight(0xbfdcff, 0x2a1a40, 1.2));
    const sun = new THREE.DirectionalLight(0xffffff, 2);
    sun.position.set(5, 10, 6);
    this.scene.add(sun);

    const floor = new THREE.Mesh(new THREE.CircleGeometry(9, 48), new THREE.MeshStandardMaterial({ color: 0x1b2450 }));
    floor.rotation.x = -Math.PI / 2;
    this.scene.add(floor);

    this.segments = Array.from({ length: 34 }, () => ({ x: 0, z: 0 }));
    this.model = null;
    this.running = false;
    this.clock = new THREE.Clock();
  }

  setSkin(skin) {
    if (this.model) this.model.setSkin(skin);
    else {
      this.model = new SnakeModel(skin);
      this.scene.add(this.model.group);
    }
  }

  start() {
    this.running = true;
    this.renderer.setAnimationLoop(() => this.frame());
  }

  stop() {
    this.running = false;
    this.renderer.setAnimationLoop(null);
  }

  frame() {
    const w = this.canvas.clientWidth;
    const h = this.canvas.clientHeight;
    if (this.canvas.width !== Math.floor(w * this.renderer.getPixelRatio())) {
      this.renderer.setSize(w, h, false);
      this.camera.aspect = w / h;
      this.camera.updateProjectionMatrix();
    }
    const t = this.clock.getElapsedTime();
    // Coil the snake into a slowly turning spiral so every side of the skin shows.
    const n = this.segments.length;
    for (let i = 0; i < n; i++) {
      const a = t * 0.8 - i * 0.22;
      const r = 4.8 - i * 0.1; // head on the outside so it's easy to see
      this.segments[i].x = Math.cos(a) * r;
      this.segments[i].z = Math.sin(a) * r;
    }
    const angle = t * 0.8 + Math.PI / 2;
    this.model.update(this.segments, 0.8, angle, t);
    this.camera.position.set(0, 12, 14);
    this.camera.lookAt(0, 0.5, 0);
    this.renderer.render(this.scene, this.camera);
  }
}
