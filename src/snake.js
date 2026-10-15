import { CONFIG } from './config.js';
import { SnakeModel } from './model.js';
import { baseStats } from './upgrades.js';

const TAU = Math.PI * 2;
export function wrapAngle(a) {
  a = (a + Math.PI) % TAU;
  if (a < 0) a += TAU;
  return a - Math.PI;
}

// Snake = game rules (movement, growth, timers). Drawing is done by SnakeModel.
export class Snake {
  constructor(game, { skin, name = 'Snake', x = 0, z = 0, angle = 0, mass = CONFIG.startMass, isPlayer = false }) {
    this.game = game;
    this.skin = skin;
    this.name = name;
    this.isPlayer = isPlayer;
    this.angle = angle;
    this.targetAngle = angle;
    this.mass = mass;
    this.alive = true;
    this.kills = 0;
    this.maxMass = mass;

    this.stats = baseStats();
    this.upgrades = {};
    this.shields = 0;
    this.shieldTimer = 0;
    this.invuln = 0; // seconds of "can't die"
    this.phase = 0; // seconds of "pass through snakes"
    this.phantomCooldown = 0;
    this.freeBoost = 0;
    this.chomp = 0; // jaw animation timer
    this.isBoss = false;
    this.noGrow = false; // bosses keep their size

    this.wantBoost = false;
    this.boosting = false;
    this.dropAccum = 0;

    // Build the initial body as a straight line behind the head.
    this.segments = [];
    const n = this.targetSegments();
    for (let i = 0; i < n; i++) {
      this.addSegment(x - Math.cos(angle) * i * this.spacing, z - Math.sin(angle) * i * this.spacing);
    }

    this.model = new SnakeModel(skin, { label: true });
    game.scene.add(this.model.group);
  }

  get radius() {
    return CONFIG.baseRadius * (1 + Math.sqrt(this.mass) / 12) * this.stats.sizeMul;
  }
  get spacing() {
    return this.radius * 0.75;
  }
  get head() {
    return this.segments[0];
  }
  // The "Lv" shown above every snake. Higher Lv eats lower Lv on touch.
  get lv() {
    return Math.floor(this.mass);
  }

  targetSegments() {
    return Math.min(CONFIG.maxSegments, Math.floor(6 + this.mass * 0.5));
  }

  addSegment(x, z) {
    // Each segment knows its owner so collision checks can tell snakes apart.
    this.segments.push({ x, z, owner: this, idx: this.segments.length });
  }

  grow(amount) {
    if (amount > 0 && this.chomp <= 0) this.chomp = 0.22;
    if (this.noGrow && amount > 0) return;
    this.mass += amount;
    this.maxMass = Math.max(this.maxMass, this.mass);
  }

  update(dt) {
    const st = this.stats;

    // --- Timers ---
    this.invuln = Math.max(0, this.invuln - dt);
    this.phase = Math.max(0, this.phase - dt);
    this.phantomCooldown = Math.max(0, this.phantomCooldown - dt);
    this.freeBoost = Math.max(0, this.freeBoost - dt);
    this.chomp = Math.max(0, this.chomp - dt);
    if (st.regen) this.mass += st.regen * dt;
    if (this.shields < st.shieldMax) {
      this.shieldTimer += dt;
      if (this.shieldTimer >= st.shieldRecharge) {
        this.shields++;
        this.shieldTimer = 0;
      }
    }

    // --- Steering: turn toward targetAngle, limited by turn rate ---
    const map = this.game.map;
    const turnRate = (CONFIG.turnRate * st.turnMul * map.turnMul) / (1 + (this.radius - CONFIG.baseRadius) * 0.35);
    const diff = wrapAngle(this.targetAngle - this.angle);
    const maxTurn = turnRate * dt;
    this.angle = wrapAngle(this.angle + Math.max(-maxTurn, Math.min(maxTurn, diff)));

    // --- Boost: go faster but lose mass, which is dropped as food ---
    const wasBoosting = this.boosting;
    this.boosting = this.wantBoost && (this.mass > CONFIG.minBoostMass || this.freeBoost > 0);
    if (this.boosting && !wasBoosting && st.phantom && this.phantomCooldown <= 0) {
      this.phase = 1.2;
      this.phantomCooldown = 8;
    }
    let speed = CONFIG.baseSpeed * st.speedMul * map.speedMul;
    if (this.boosting) {
      speed *= CONFIG.boostMultiplier * st.boostMul;
      if (this.freeBoost <= 0) {
        const lost = CONFIG.boostCostPerSec * st.boostCostMul * dt;
        this.mass -= lost;
        this.dropAccum += lost;
        if (this.dropAccum >= 1) {
          const tail = this.segments[this.segments.length - 1];
          this.game.food.spawn(tail.x, tail.z, this.dropAccum * 0.7, { color: this.skin.colorA });
          this.dropAccum = 0;
        }
      }
    }

    // --- Move the head, then each segment follows the one in front ---
    const h = this.segments[0];
    h.x += Math.cos(this.angle) * speed * dt;
    h.z += Math.sin(this.angle) * speed * dt;

    const sp = this.spacing;
    for (let i = 1; i < this.segments.length; i++) {
      const prev = this.segments[i - 1];
      const s = this.segments[i];
      const dx = s.x - prev.x;
      const dz = s.z - prev.z;
      const d = Math.hypot(dx, dz);
      if (d > 0.0001) {
        s.x = prev.x + (dx / d) * sp;
        s.z = prev.z + (dz / d) * sp;
      }
    }

    // --- Grow / shrink the body to match mass ---
    const target = this.targetSegments();
    while (this.segments.length < target) {
      const last = this.segments[this.segments.length - 1];
      this.addSegment(last.x, last.z);
    }
    while (this.segments.length > target && this.segments.length > 4) this.segments.pop();
  }

  render(time) {
    const chomp = this.chomp > 0 ? Math.sin((this.chomp / 0.22) * Math.PI) : 0;
    this.model.update(this.segments, this.radius, this.angle, time, {
      jaw: Math.max(chomp, this.boosting ? 0.45 : 0.05),
      label: `${this.isBoss ? 'BOSS ' : ''}Lv${this.lv}\n${this.name}`,
      labelColor: this.labelColor(),
      boosting: this.boosting,
      phase: this.phase > 0,
      invuln: this.invuln > 0,
    });
  }

  // Yellow = you. Red = bigger than you (it can eat you!). White = you can eat it.
  labelColor() {
    if (this.isPlayer) return '#ffe066';
    if (this.isBoss) return '#ff3b3b';
    const p = this.game.player;
    if (p?.alive && this.lv >= p.lv) return '#ff7a7a';
    return '#ffffff';
  }

  dispose() {
    this.model.dispose();
  }
}
