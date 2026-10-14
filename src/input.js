import * as THREE from 'three';

// Mouse: the snake turns toward the cursor, hold left button to boost.
// Keyboard: A/D or arrows to turn, Space/W/Up to boost, Esc to pause.
export class Input {
  constructor(canvas) {
    this.ndc = new THREE.Vector2(); // mouse in normalized device coords (-1..1)
    this.mouseActive = false;
    this.mouseDown = false;
    this.keys = new Set();
    this.onPause = null;

    window.addEventListener('mousemove', (e) => {
      this.ndc.set((e.clientX / window.innerWidth) * 2 - 1, -(e.clientY / window.innerHeight) * 2 + 1);
      this.mouseActive = true;
    });
    canvas.addEventListener('mousedown', (e) => {
      if (e.button === 0) this.mouseDown = true;
    });
    window.addEventListener('mouseup', (e) => {
      if (e.button === 0) this.mouseDown = false;
    });
    window.addEventListener('keydown', (e) => {
      if (e.code === 'Space' && e.target === document.body) e.preventDefault();
      if (e.code === 'Escape' || e.code === 'KeyP') this.onPause?.();
      this.keys.add(e.code);
    });
    window.addEventListener('keyup', (e) => this.keys.delete(e.code));
    window.addEventListener('blur', () => {
      this.keys.clear();
      this.mouseDown = false;
    });
  }

  get turn() {
    const left = this.keys.has('KeyA') || this.keys.has('ArrowLeft');
    const right = this.keys.has('KeyD') || this.keys.has('ArrowRight');
    return (right ? 1 : 0) - (left ? 1 : 0);
  }

  get boost() {
    return this.mouseDown || this.keys.has('Space') || this.keys.has('KeyW') || this.keys.has('ArrowUp');
  }
}
