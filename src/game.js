import * as THREE from 'three';
import { CONFIG, xpForLevel } from './config.js';
import { Snake } from './snake.js';
import { BotBrain } from './bot.js';
import { FoodField } from './food.js';
import { SpatialHash } from './grid.js';
import { Particles } from './particles.js';
import { randomBotSkin, randomBotName, segmentColor } from './skins.js';
import { rollChoices, applyUpgrade } from './upgrades.js';

const tmpColor = new THREE.Color();
const tmpVec = new THREE.Vector3();
const camDesired = new THREE.Vector3();

// The Game owns the 3D scene and runs the main loop:
//   input -> AI -> movement -> eating -> collisions -> deaths -> render
export class Game {
  constructor(canvas, input, ui) {
    this.input = input;
    this.ui = ui;

    this.renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    this.renderer.shadowMap.enabled = true;
    this.renderer.shadowMap.type = THREE.PCFSoftShadowMap;

    this.scene = new THREE.Scene();
    this.scene.background = new THREE.Color(0x0b1020);
    this.scene.fog = new THREE.Fog(0x0b1020, 90, 260);

    this.camera = new THREE.PerspectiveCamera(55, 1, 0.5, 700);
    this.camera.position.set(0, 80, 80);
    this.camTarget = new THREE.Vector3();

    this.scene.add(new THREE.HemisphereLight(0xa8d0ff, 0x24163a, 1.1));
    this.sun = new THREE.DirectionalLight(0xffffff, 1.8);
    this.sun.castShadow = true;
    this.sun.shadow.mapSize.set(2048, 2048);
    const sc = this.sun.shadow.camera;
    sc.left = sc.bottom = -45;
    sc.right = sc.top = 45;
    sc.far = 200;
    this.scene.add(this.sun, this.sun.target);

    this.buildArena();
    this.food = new FoodField(this.scene);
    this.particles = new Particles(this.scene);
    this.segGrid = new SpatialHash(3);
    this.maxSegRadius = 1;

    this.raycaster = new THREE.Raycaster();
    this.ground = new THREE.Plane(new THREE.Vector3(0, 1, 0), 0);
    this.mouseWorld = new THREE.Vector3();

    this.snakes = [];
    this.player = null;
    this.state = 'attract'; // 'attract' (menu background) | 'playing' | 'over'
    this.paused = false;
    this.time = 0;
    this.runTime = 0;
    this.threat = 1;
    this.hudTimer = 0;

    window.addEventListener('resize', () => this.resize());
    this.resize();
    this.clock = new THREE.Clock();
    this.renderer.setAnimationLoop(() => this.frame());
  }

  // ------------------------------------------------------------------ world
  buildArena() {
    const R = CONFIG.arenaRadius;

    // Hex-grid floor texture drawn on a 2D canvas.
    const c = document.createElement('canvas');
    c.width = 256;
    c.height = 222;
    const g = c.getContext('2d');
    g.fillStyle = '#141a33';
    g.fillRect(0, 0, c.width, c.height);
    g.strokeStyle = '#26305a';
    g.lineWidth = 4;
    const hex = (cx, cy, r) => {
      g.beginPath();
      for (let i = 0; i < 6; i++) {
        const a = (Math.PI / 3) * i;
        g.lineTo(cx + Math.cos(a) * r, cy + Math.sin(a) * r);
      }
      g.closePath();
      g.stroke();
    };
    const r = 64;
    const h = Math.sqrt(3) * r;
    for (let col = -1; col < 4; col++) {
      for (let row = -1; row < 3; row++) {
        hex(col * r * 1.5, row * h + (col % 2 ? h / 2 : 0), r);
      }
    }
    const tex = new THREE.CanvasTexture(c);
    tex.wrapS = tex.wrapT = THREE.RepeatWrapping;
    tex.repeat.set((R * 2) / 14, (R * 2) / 14);
    tex.colorSpace = THREE.SRGBColorSpace;
    tex.anisotropy = 8;

    const floor = new THREE.Mesh(
      new THREE.CircleGeometry(R + 60, 96),
      new THREE.MeshStandardMaterial({ map: tex, roughness: 0.9 }),
    );
    floor.rotation.x = -Math.PI / 2;
    floor.receiveShadow = true;
    this.scene.add(floor);

    // Danger zone outside the arena.
    const outside = new THREE.Mesh(
      new THREE.RingGeometry(R, R + 60, 96),
      new THREE.MeshBasicMaterial({ color: 0x400818, transparent: true, opacity: 0.6 }),
    );
    outside.rotation.x = -Math.PI / 2;
    outside.position.y = 0.03;
    this.scene.add(outside);

    const wall = new THREE.Mesh(
      new THREE.CylinderGeometry(R, R, 3, 160, 1, true),
      new THREE.MeshBasicMaterial({ color: 0xff3366, transparent: true, opacity: 0.25, side: THREE.DoubleSide, depthWrite: false }),
    );
    wall.position.y = 1.5;
    this.scene.add(wall);
    const rim = new THREE.Mesh(new THREE.TorusGeometry(R, 0.25, 8, 220), new THREE.MeshBasicMaterial({ color: 0xff6b8a }));
    rim.rotation.x = Math.PI / 2;
    rim.position.y = 3;
    this.scene.add(rim);

    // Decorative glowing crystals outside the arena.
    const crystalGeo = new THREE.OctahedronGeometry(1, 0);
    for (let i = 0; i < 50; i++) {
      const a = Math.random() * Math.PI * 2;
      const d = R + 8 + Math.random() * 40;
      const s = 1.5 + Math.random() * 4;
      const m = new THREE.Mesh(
        crystalGeo,
        new THREE.MeshStandardMaterial({ color: 0x223355, emissive: new THREE.Color().setHSL(0.5 + Math.random() * 0.4, 0.9, 0.5), emissiveIntensity: 0.7 }),
      );
      m.position.set(Math.cos(a) * d, s * 0.8, Math.sin(a) * d);
      m.scale.set(s * 0.6, s * 1.6, s * 0.6);
      m.rotation.y = Math.random() * 3;
      this.scene.add(m);
    }
  }

  clearWorld() {
    for (const s of this.snakes) s.dispose();
    this.snakes = [];
    this.player = null;
    this.food.clear();
    this.particles.clear();
    for (let i = 0; i < CONFIG.foodCount; i++) this.food.spawnRandom(0.01);
    this.respawnQueue = [];
    this.runTime = 0;
    this.threat = 1;
    this.paused = false;
    this.choosing = false;
  }

  startAttract() {
    this.clearWorld();
    this.state = 'attract';
    for (let i = 0; i < CONFIG.botCount; i++) this.spawnBot();
  }

  startRun(skin, name = 'Player') {
    this.clearWorld();
    this.state = 'playing';
    const p = new Snake(this, { skin, name, x: 0, z: 0, angle: Math.random() * Math.PI * 2, isPlayer: true });
    p.level = 1;
    p.xp = 0;
    p.xpNext = xpForLevel(1);
    p.rerolls = CONFIG.startRerolls;
    p.invuln = 2;
    this.player = p;
    this.pendingLevels = 0;
    this.snakes.push(p);
    for (let i = 0; i < CONFIG.botCount; i++) this.spawnBot();
    this.ui.showHud(p);
  }

  spawnBot() {
    const R = CONFIG.arenaRadius;
    let x, z;
    for (let tries = 0; tries < 30; tries++) {
      const r = R * 0.85 * Math.sqrt(Math.random());
      const a = Math.random() * Math.PI * 2;
      x = Math.cos(a) * r;
      z = Math.sin(a) * r;
      if (!this.player?.alive || Math.hypot(x - this.player.head.x, z - this.player.head.z) > 35) break;
    }
    // Bots spawn bigger as the run goes on: the "threat" ramps up.
    const mass = CONFIG.startMass + Math.random() * 25 * this.threat + (this.threat - 1) * 30;
    const bot = new Snake(this, { skin: randomBotSkin(), name: randomBotName(), x, z, angle: Math.random() * Math.PI * 2, mass });
    bot.brain = new BotBrain(bot, this);
    bot.invuln = 1;
    this.snakes.push(bot);
  }

  // ------------------------------------------------------------------ loop
  frame() {
    const dt = Math.min(this.clock.getDelta(), 0.05);
    if (!this.paused) this.update(dt);
    this.updateCamera(dt);
    this.renderer.render(this.scene, this.camera);
  }

  update(dt) {
    this.time += dt;
    if (this.state === 'playing') {
      this.runTime += dt;
      this.threat = 1 + this.runTime / 90;
    }

    const p = this.player;
    if (this.state === 'playing' && p?.alive) this.applyInput(p);

    for (const s of this.snakes) if (s.brain && s.alive) s.brain.think(dt);
    for (const s of this.snakes) if (s.alive) s.update(dt);

    // Rebuild spatial grids for this frame.
    this.segGrid.clear();
    this.maxSegRadius = 0;
    for (const s of this.snakes) {
      if (!s.alive) continue;
      this.maxSegRadius = Math.max(this.maxSegRadius, s.radius);
      for (const seg of s.segments) this.segGrid.insert(seg.x, seg.z, seg);
    }
    this.food.rebuildGrid();

    for (const s of this.snakes) if (s.alive) this.eat(s, dt);
    for (const s of this.snakes) if (s.alive) this.collide(s);

    // Handle deaths, then remove dead snakes.
    for (const s of this.snakes) if (!s.alive && !s.handled) this.onDeath(s);
    this.snakes = this.snakes.filter((s) => s.alive);

    // Respawn bots after a short delay.
    for (const r of this.respawnQueue) r.t -= dt;
    while (this.respawnQueue.length && this.respawnQueue[0].t <= 0) {
      this.respawnQueue.shift();
      this.spawnBot();
    }

    const goldenChance = 0.01 + (p?.alive ? p.stats.goldenBonus : 0);
    this.food.compact();
    this.food.maintain(goldenChance);
    this.food.render(this.time);
    for (const s of this.snakes) s.render(this.time);
    this.particles.update(dt);

    // Roguelite level-up: pause and offer a choice.
    if (this.state === 'playing' && this.pendingLevels > 0 && !this.choosing) this.openLevelUp();

    this.hudTimer -= dt;
    if (this.hudTimer <= 0 && this.state === 'playing') {
      this.hudTimer = 0.1;
      this.ui.updateHud(this);
    }
  }

  applyInput(p) {
    const turn = this.input.turn;
    if (turn !== 0) {
      p.targetAngle = p.angle + turn; // keep turning at full speed while held
    } else if (this.input.mouseActive) {
      this.raycaster.setFromCamera(this.input.ndc, this.camera);
      if (this.raycaster.ray.intersectPlane(this.ground, this.mouseWorld)) {
        const dx = this.mouseWorld.x - p.head.x;
        const dz = this.mouseWorld.z - p.head.z;
        if (dx * dx + dz * dz > 1) p.targetAngle = Math.atan2(dz, dx);
      }
    }
    p.wantBoost = this.input.boost;
  }

  eat(s, dt) {
    const h = s.head;
    const pickup = s.radius + 0.7;
    const magnet = pickup + CONFIG.baseMagnet + s.stats.magnet;
    const pullSpeed = (14 + s.stats.magnet * 2) * dt;
    this.food.grid.query(h.x, h.z, magnet, (f) => {
      if (!f.alive) return;
      const dx = f.x - h.x;
      const dz = f.z - h.z;
      const d2 = dx * dx + dz * dz;
      if (d2 < pickup * pickup) {
        f.alive = false;
        s.grow(f.value * CONFIG.massPerFood * s.stats.foodMul);
        if (s.isPlayer) {
          this.gainXp(f.value);
          if (f.golden) this.particles.burst(f.x, f.z, '#ffcc33', 10, 5);
        }
      } else if (d2 < magnet * magnet) {
        const d = Math.sqrt(d2);
        const step = Math.min(d, pullSpeed);
        f.x -= (dx / d) * step;
        f.z -= (dz / d) * step;
      }
    });
  }

  collide(s) {
    // The 3D head sticks out in front of the first segment, so test a bit ahead.
    const seg0 = s.segments[0];
    const h = { x: seg0.x + Math.cos(s.angle) * s.radius * 0.9, z: seg0.z + Math.sin(s.angle) * s.radius * 0.9 };
    const hr = s.radius * 0.9;
    const R = CONFIG.arenaRadius;
    const safe = s.invuln > 0 || s.phase > 0;

    // The wall.
    const dc = Math.hypot(h.x, h.z);
    if (dc + hr > R) {
      if (safe) {
        seg0.x *= (R - hr - s.radius - 0.1) / dc;
        seg0.z *= (R - hr - s.radius - 0.1) / dc;
        s.angle = s.targetAngle = Math.atan2(-seg0.z, -seg0.x);
      } else {
        this.hit(s, null);
      }
      return;
    }
    if (safe) return;

    // Other snakes' bodies (your own body is safe, like slither.io).
    let killer = null;
    this.segGrid.query(h.x, h.z, hr + this.maxSegRadius, (seg) => {
      const o = seg.owner;
      if (o === s || !o.alive || o.phase > 0) return;
      const rr = hr + o.radius * 0.85;
      if ((seg.x - h.x) ** 2 + (seg.z - h.z) ** 2 < rr * rr) {
        if (seg.idx === 0 && s.mass > o.mass) return; // head-on: the bigger snake wins
        killer = o;
        return false;
      }
    });
    if (killer) this.hit(s, killer);
  }

  hit(s, killer) {
    if (s.shields > 0) {
      s.shields--;
      s.shieldTimer = 0;
      s.invuln = 2;
      s.angle = s.targetAngle = s.angle + Math.PI;
      this.particles.burst(s.head.x, s.head.z, '#7fe8ff', 30, 10);
      if (s.isPlayer) this.ui.toast('🛡️ Shield broken!');
      return;
    }
    if (s.stats.revives > 0) {
      s.stats.revives--;
      s.mass = Math.max(CONFIG.startMass, s.mass * 0.5);
      s.invuln = 3;
      s.angle = s.targetAngle = s.angle + Math.PI;
      this.particles.burst(s.head.x, s.head.z, '#ff7b39', 50, 12);
      if (s.isPlayer) this.ui.toast('❤️‍🔥 Second Wind! You revived!');
      return;
    }
    s.alive = false;
    s.killer = killer;
  }

  onDeath(s) {
    s.handled = true;
    const killer = s.killer;

    // The body turns into food, colored like the dead snake.
    const n = s.segments.length;
    const drops = Math.ceil(n / 2);
    const total = s.mass * CONFIG.deathDropRatio * (killer ? killer.stats.dropMul : 1);
    const per = Math.max(0.5, total / drops);
    for (let i = 0; i < n; i += 2) {
      const seg = s.segments[i];
      segmentColor(s.skin, i, n, tmpColor);
      this.food.spawn(seg.x + (Math.random() - 0.5) * 1.5, seg.z + (Math.random() - 0.5) * 1.5, per, { color: tmpColor });
    }
    this.particles.burst(s.head.x, s.head.z, s.skin.colorA, 40, 10);

    if (killer?.alive) {
      killer.kills++;
      if (killer.stats.bloodrush) killer.freeBoost = 4;
      if (killer.isPlayer) {
        this.gainXp((15 + s.mass * 0.25) * killer.stats.killXpMul);
        if (killer.upgrades.predator) killer.grow(s.mass * 0.15);
        this.ui.toast(`🐍 You defeated ${s.name}!`);
      }
    }

    s.dispose();
    if (s.isPlayer) this.gameOver();
    else this.respawnQueue.push({ t: 2 + Math.random() * 2 });
  }

  gainXp(amount) {
    const p = this.player;
    if (!p?.alive) return;
    p.xp += amount * p.stats.xpMul;
    while (p.xp >= p.xpNext) {
      p.xp -= p.xpNext;
      p.level++;
      p.xpNext = xpForLevel(p.level);
      if (p.level % 5 === 0) p.rerolls++;
      this.pendingLevels++;
      this.particles.burst(p.head.x, p.head.z, '#ffe066', 30, 8);
    }
  }

  openLevelUp() {
    const p = this.player;
    this.choosing = true;
    this.paused = true;
    this.ui.updateHud(this);
    this.ui.showLevelUp({
      level: p.level - this.pendingLevels + 1,
      choices: rollChoices(p.upgrades),
      rerolls: p.rerolls,
      owned: p.upgrades,
      onReroll: () => {
        if (p.rerolls <= 0) return null;
        p.rerolls--;
        return { choices: rollChoices(p.upgrades), rerolls: p.rerolls };
      },
      onPick: (up) => {
        applyUpgrade(p, up);
        this.pendingLevels--;
        this.choosing = false;
        this.paused = false;
        p.invuln = Math.max(p.invuln, 0.8); // a moment to react after the pause
        this.ui.updateUpgrades(p);
      },
    });
  }

  togglePause() {
    if (this.state !== 'playing' || this.choosing) return;
    this.paused = !this.paused;
    this.ui.showPause(this.paused);
  }

  gameOver() {
    const p = this.player;
    this.state = 'over';
    const stats = {
      level: p.level,
      length: Math.floor(p.maxMass),
      kills: p.kills,
      time: this.runTime,
      upgrades: p.upgrades,
    };
    // Coins for the skin shop: bigger, longer, deadlier runs earn more.
    stats.coins = Math.round(p.maxMass / 4 + p.kills * 15 + p.level * 10 + this.runTime / 6);
    this.onGameOver?.(stats);
    this.ui.showGameOver(stats);
  }

  // ------------------------------------------------------------------ camera
  updateCamera(dt) {
    const p = this.player;
    const k = 1 - Math.exp(-dt * 5);
    const desired = camDesired;
    if (this.state === 'playing' && p?.alive) {
      const h = p.head;
      this.camTarget.lerp(tmpVec.set(h.x, 0, h.z), Math.min(1, k * 2));
      const height = (15 + p.radius * 12) * p.stats.zoomMul;
      desired.set(this.camTarget.x, height, this.camTarget.z + height * 0.62);
    } else {
      // Slow orbit for the menu / game over screen.
      const a = this.time * 0.05;
      this.camTarget.lerp(tmpVec.set(0, 0, 0), k * 0.3);
      desired.set(Math.cos(a) * 110, 85, Math.sin(a) * 110);
    }
    this.camera.position.lerp(desired, this.state === 'playing' ? Math.min(1, k * 2) : k * 0.3);
    this.camera.lookAt(this.camTarget);

    this.sun.position.set(this.camTarget.x + 30, 70, this.camTarget.z + 25);
    this.sun.target.position.copy(this.camTarget);
  }

  resize() {
    const w = window.innerWidth;
    const h = window.innerHeight;
    this.renderer.setSize(w, h);
    this.camera.aspect = w / h;
    this.camera.updateProjectionMatrix();
  }
}
