import * as THREE from 'three';
import { CONFIG, xpForLevel } from './config.js';
import { Snake } from './snake.js';
import { BotBrain } from './bot.js';
import { FoodField } from './food.js';
import { SpatialHash } from './grid.js';
import { Particles } from './particles.js';
import { randomBotSkin, randomBotName, segmentColor, BOSS_SKINS } from './skins.js';
import { MAPS, MAP_DEFAULTS, getMap, buildMap, disposeGroup } from './maps.js';
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

    this.food = new FoodField(this.scene);
    this.food.blocked = (x, z) => this.inLava(x, z, 2);
    this.setMap(MAPS[0]);
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
  // Switch to a map/biome (see maps.js): rebuilds the floor, wall, decorations and lava.
  setMap(map) {
    if (this.arena) disposeGroup(this.arena);
    this.map = { ...MAP_DEFAULTS, ...map };
    CONFIG.arenaRadius = map.radius;
    const built = buildMap(this.map);
    this.arena = built.group;
    this.pools = built.pools;
    this.scene.add(this.arena);
    this.scene.background = new THREE.Color(map.bg);
    this.scene.fog = new THREE.Fog(map.bg, 90, 300);
  }

  // Is the circle (x, z, margin) touching a lava pool?
  inLava(x, z, margin = 0) {
    for (const p of this.pools) if ((x - p.x) ** 2 + (z - p.z) ** 2 < (p.r + margin) ** 2) return true;
    return false;
  }

  clearWorld() {
    for (const s of this.snakes) s.dispose();
    this.snakes = [];
    this.player = null;
    this.food.clear();
    this.particles.clear();
    for (let i = 0; i < CONFIG.foodCount * this.map.foodMul; i++) this.food.spawnRandom(0.01);
    this.respawnQueue = [];
    this.boss = null;
    this.bossCount = 0;
    this.bossKills = 0;
    this.bossTimer = CONFIG.bossFirst;
    this.bossWarned = false;
    this.runTime = 0;
    this.threat = 1;
    this.paused = false;
    this.choosing = false;
  }

  startAttract(mapId) {
    if (mapId) this.setMap(getMap(mapId));
    this.clearWorld();
    this.state = 'attract';
    for (let i = 0; i < CONFIG.botCount; i++) this.spawnBot();
  }

  startRun(skin, name = 'Player', mapId = 'neon') {
    this.setMap(getMap(mapId));
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
    this.ui.toast(`🗺️ ${this.map.name}: ${this.map.desc}`);
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
    // Bots spawn around YOUR size (some smaller = food, some bigger = danger),
    // and a bit bigger as the run goes on: the "threat" ramps up.
    const base = this.player?.alive ? this.player.mass : 30;
    const mass = Math.max(8, base * (0.35 + Math.random() * 1.0)) * (1 + (this.threat - 1) * 0.15);
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

    if (this.state === 'playing') this.updateBoss(dt);

    const goldenChance = 0.01 + this.map.goldenBonus + (p?.alive ? p.stats.goldenBonus : 0);
    this.food.compact();
    this.food.maintain(goldenChance, CONFIG.foodCount * this.map.foodMul);
    if (this.arena.userData.lavaMat) this.arena.userData.lavaMat.color.setHSL(0.06 + Math.sin(this.time * 2) * 0.015, 1, 0.5);
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

    // The wall and lava pools.
    const dc = Math.hypot(h.x, h.z);
    const inLava = this.inLava(h.x, h.z, -0.3);
    if (dc + hr > R || inLava) {
      if (safe) {
        if (inLava) {
          s.angle = s.targetAngle = s.angle + Math.PI;
        } else {
          seg0.x *= (R - hr - s.radius - 0.1) / dc;
          seg0.z *= (R - hr - s.radius - 0.1) / dc;
          s.angle = s.targetAngle = Math.atan2(-seg0.z, -seg0.x);
        }
      } else {
        if (inLava && s.isPlayer) this.ui.toast('🔥 Burned in lava!');
        this.hit(s, null);
      }
      return;
    }
    if (s.phase > 0) return; // phased snakes pass through everything

    // Touching another snake: the HIGHER Lv eats the lower one, from any side.
    let other = null;
    this.segGrid.query(h.x, h.z, hr + this.maxSegRadius, (seg) => {
      const o = seg.owner;
      if (o === s || !o.alive || o.phase > 0) return;
      const rr = hr + o.radius * 0.85;
      if ((seg.x - h.x) ** 2 + (seg.z - h.z) ** 2 < rr * rr) {
        other = o;
        return false;
      }
    });
    if (!other) return;
    if (s.lv > other.lv) {
      if (other.invuln <= 0) this.hit(other, s, true); // chomp!
    } else if (s.lv < other.lv) {
      if (!safe) this.hit(s, other, true);
    } else {
      // Same Lv: bounce off each other.
      s.angle = s.targetAngle = s.angle + Math.PI;
      s.invuln = Math.max(s.invuln, 0.4);
    }
  }

  // Something fatal happened to `s`. Shields and Second Wind can save it.
  hit(s, killer, eaten = false) {
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
    s.eaten = eaten;
  }

  onDeath(s) {
    s.handled = true;
    const killer = s.killer;

    // Eaten: the eater swallows a big part of it right away.
    if (s.eaten && killer?.alive) {
      killer.grow(s.mass * (killer.isPlayer ? CONFIG.eatAbsorb : CONFIG.botEatAbsorb));
      killer.chomp = 0.22;
      this.particles.burst(s.head.x, s.head.z, '#ffffff', 25, 9);
    }

    // The (rest of the) body turns into food, colored like the dead snake.
    const n = s.segments.length;
    const drops = Math.ceil(n / 2);
    const ratio = s.eaten ? CONFIG.eatDrop : CONFIG.deathDropRatio;
    const total = s.mass * ratio * (killer ? killer.stats.dropMul : 1);
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
        if (!s.isBoss) this.ui.toast(`🐍 You ate ${s.name}!`);
      }
    }

    s.dispose();
    if (s.isBoss) this.onBossDefeated(s, killer);
    else if (s.isPlayer) this.gameOver();
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
      bossKills: this.bossKills,
      map: this.map.name,
    };
    // Coins for the skin shop: bigger, longer, deadlier runs earn more.
    stats.coins = Math.round(p.maxMass / 4 + p.kills * 15 + p.level * 10 + this.runTime / 6 + this.bossKills * 150);
    this.onGameOver?.(stats);
    this.ui.showGameOver(stats);
  }

  // ------------------------------------------------------------------ bosses
  updateBoss(dt) {
    const p = this.player;
    if (!p?.alive) return;
    if (!this.boss) {
      this.bossTimer -= dt;
      if (!this.bossWarned && this.bossTimer <= CONFIG.bossWarning) {
        this.bossWarned = true;
        this.ui.toast('⚠️ A BOSS is coming! Grow fast or stay away!');
      }
      if (this.bossTimer <= 0) this.spawnBoss();
      return;
    }
    this.bossLeft -= dt;
    if (this.bossLeft <= 0) {
      // You survived: the boss gives up and leaves.
      const b = this.boss;
      this.particles.burst(b.head.x, b.head.z, b.skin.colorA, 60, 14);
      b.alive = false;
      b.handled = true;
      b.dispose();
      this.snakes = this.snakes.filter((s) => s !== b);
      this.boss = null;
      this.bossTimer = CONFIG.bossInterval;
      this.bossWarned = false;
      this.gainXp(25 + this.bossCount * 10);
      this.ui.toast(`🏃 You survived ${b.name}! Bonus XP!`);
    }
  }

  spawnBoss() {
    const p = this.player;
    const R = CONFIG.arenaRadius;
    // Come in from the far side of the map.
    const a = Math.atan2(-p.head.z, -p.head.x) + (Math.random() - 0.5);
    const x = Math.cos(a) * R * 0.75;
    const z = Math.sin(a) * R * 0.75;
    const skin = BOSS_SKINS[this.bossCount % BOSS_SKINS.length];
    // Always much bigger than you, and each boss bigger than the last.
    const mass = Math.max(180, p.mass * 1.8) * (1 + this.bossCount * 0.25);
    const boss = new Snake(this, { skin, name: skin.label, x, z, angle: a + Math.PI, mass });
    boss.isBoss = true;
    boss.noGrow = true;
    boss.stats.sizeMul = 1.2;
    boss.stats.speedMul = 0.92;
    boss.invuln = 2;
    boss.brain = new BotBrain(boss, this, { boss: true });
    this.snakes.push(boss);
    this.boss = boss;
    this.bossCount++;
    this.bossLeft = CONFIG.bossStay;
    this.particles.burst(x, z, skin.colorA, 80, 16);
    this.ui.toast(`👹 BOSS: ${skin.label} (Lv${boss.lv}) appeared!`);
  }

  onBossDefeated(b, killer) {
    this.boss = null;
    this.bossTimer = CONFIG.bossInterval;
    this.bossWarned = false;
    if (killer?.isPlayer) {
      this.bossKills++;
      this.gainXp(80 + b.lv * 0.2);
      this.ui.toast(`🏆 You ate the boss ${b.name}! +150 coins at the end of the run!`);
    }
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
