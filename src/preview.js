import * as THREE from 'three';
import { SnakeModel } from './model.js';

function makeStage(scene) {
  scene.add(new THREE.HemisphereLight(0xdcecff, 0x3a2a60, 1.3));
  const sun = new THREE.DirectionalLight(0xffffff, 2.2);
  sun.position.set(5, 10, 6);
  scene.add(sun);
}

// Coil the body into a spiral and lift the neck out toward the viewer,
// like a snake resting in a shop window. Returns the head's heading.
function coil(segs, sway) {
  const NECK = 6;
  const step = 0.62;
  for (let i = NECK; i < segs.length; i++) {
    const a = Math.PI / 2 - (i - NECK) * 0.22;
    const r = 3.3 - (i - NECK) * 0.06;
    segs[i].x = Math.cos(a) * r;
    segs[i].z = Math.sin(a) * r;
  }
  // Neck: curve from the coil's direction (-X) to facing the camera (+Z).
  let x = segs[NECK].x;
  let z = segs[NECK].z;
  let heading = Math.PI;
  for (let k = 1; k <= NECK; k++) {
    heading = Math.PI - (k / NECK) * (Math.PI / 2 - sway);
    x += Math.cos(heading) * step;
    z += Math.sin(heading) * step;
    segs[NECK - k].x = x;
    segs[NECK - k].z = z;
  }
  return heading;
}

// The big 3D view in the skin shop.
export class SkinPreview {
  constructor(canvas) {
    this.canvas = canvas;
    this.renderer = new THREE.WebGLRenderer({ canvas, antialias: true, alpha: true });
    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    this.scene = new THREE.Scene();
    this.camera = new THREE.PerspectiveCamera(38, 1, 0.1, 100);
    makeStage(this.scene);
    const floor = new THREE.Mesh(new THREE.CircleGeometry(7, 48), new THREE.MeshBasicMaterial({ color: 0x000000, transparent: true, opacity: 0.18 }));
    floor.rotation.x = -Math.PI / 2;
    this.scene.add(floor);
    this.segments = Array.from({ length: 34 }, () => ({ x: 0, z: 0 }));
    this.model = null;
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
    this.renderer.setAnimationLoop(() => this.frame());
  }

  stop() {
    this.renderer.setAnimationLoop(null);
  }

  frame() {
    const w = this.canvas.clientWidth;
    const h = this.canvas.clientHeight;
    if (!w || !h) return;
    if (this.canvas.width !== Math.floor(w * this.renderer.getPixelRatio())) {
      this.renderer.setSize(w, h, false);
      this.camera.aspect = w / h;
      this.camera.updateProjectionMatrix();
    }
    const t = this.clock.getElapsedTime();
    const angle = coil(this.segments, Math.sin(t * 1.3) * 0.35);
    this.model.group.rotation.y = Math.sin(t * 0.4) * 0.3;
    this.model.group.position.set(1.6, 0, -1.2);
    const chomp = Math.max(0, Math.sin(t * 3)) ** 4; // a little chomp now and then
    this.model.update(this.segments, 0.85, angle, t, { jaw: 0.05 + chomp * 0.8 });
    this.camera.position.set(0, 10, 15);
    this.camera.lookAt(0, 0.4, 0.6);
    this.renderer.render(this.scene, this.camera);
  }
}

// Renders small still pictures of skins for the shop cards.
let thumbRenderer = null;
const thumbCache = new Map();
export function skinThumbnail(skin) {
  const key = JSON.stringify(skin);
  if (thumbCache.has(key)) return thumbCache.get(key);
  if (!thumbRenderer) {
    thumbRenderer = {
      renderer: new THREE.WebGLRenderer({ antialias: true, alpha: true, preserveDrawingBuffer: true }),
      scene: new THREE.Scene(),
      camera: new THREE.PerspectiveCamera(30, 1.4, 0.1, 100),
      segments: Array.from({ length: 11 }, () => ({ x: 0, z: 0 })),
    };
    thumbRenderer.renderer.setSize(210, 150);
    makeStage(thumbRenderer.scene);
  }
  const T = thumbRenderer;
  // A short snake lying sideways, head on the right, looking at the camera.
  for (let i = 0; i < T.segments.length; i++) {
    T.segments[i].x = 1.6 - i * 0.62;
    T.segments[i].z = Math.sin(i * 0.6) * 0.5;
  }
  const model = new SnakeModel(skin);
  T.scene.add(model.group);
  model.update(T.segments, 0.85, 0.45, 0, { jaw: 0.55 });
  T.camera.position.set(0.3, 5.2, 10);
  T.camera.lookAt(-0.5, 0.4, 0);
  T.renderer.render(T.scene, T.camera);
  const url = T.renderer.domElement.toDataURL();
  model.dispose();
  thumbCache.set(key, url);
  return url;
}
