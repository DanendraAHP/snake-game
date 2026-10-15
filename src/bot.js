import { CONFIG } from './config.js';
import { wrapAngle } from './snake.js';

// Directions (relative to current heading) the bot "feels" for danger.
const FEELERS = [0, -0.4, 0.4, -0.85, 0.85, -1.4, 1.4, -2.1, 2.1];

// A simple but effective bot brain. Rule of the game: a HIGHER Lv snake eats
// a lower Lv snake on any touch. So bots:
//  1. RUN from bigger snakes that get close.
//  2. CHASE smaller snakes (any part of their body is food!).
//  3. Otherwise eat orbs, or wander.
// Every move is checked with "feelers" for danger (bigger snakes, wall, lava).
export class BotBrain {
  constructor(snake, game, { boss = false } = {}) {
    this.s = snake;
    this.g = game;
    this.boss = boss;
    this.skill = boss ? 1 : 0.55 + Math.random() * 0.45; // higher = reacts faster
    this.aggression = boss ? 1 : 0.35 + Math.random() * 0.55;
    this.thinkTimer = 0;
    this.retarget = 0;
    this.goal = null; // { x, z } point to go to
    this.mode = 'wander'; // 'flee' | 'hunt' | 'food' | 'wander'
    this.wander = snake.angle;
    this.dashTimer = 3;
  }

  // Can `o` eat me? (bigger or equal Lv is dangerous)
  dangerous(o) {
    return o.lv >= this.s.lv && o.phase <= 0;
  }

  think(dt) {
    const s = this.s;
    if (this.boss) {
      // Bosses dash every few seconds without losing length.
      this.dashTimer -= dt;
      if (this.dashTimer <= 0) {
        s.freeBoost = 1.6;
        this.dashTimer = 4 + Math.random() * 3;
      }
    }

    this.thinkTimer -= dt;
    if (this.thinkTimer > 0) return;
    const interval = 0.05 + (1 - this.skill) * 0.15;
    this.thinkTimer = interval;

    this.retarget -= interval;
    if (this.retarget <= 0 || this.mode === 'flee' || this.mode === 'hunt') {
      this.retarget = 0.35 + Math.random() * 0.4;
      this.pickGoal();
    }

    const goal = this.goalAngle();
    const look = 5 + s.radius * 4;
    let target = goal;
    if (this.probe(goal, look) > 0 || this.probe(s.angle, look) > 0) {
      // Danger! Choose the safest feeler, preferring ones closer to the goal.
      let best = Infinity;
      for (const off of FEELERS) {
        const a = s.angle + off;
        const score = this.probe(a, look) * 10 + Math.abs(wrapAngle(a - goal)) * 0.6 + Math.abs(off) * 0.2;
        if (score < best) {
          best = score;
          target = a;
        }
      }
    }
    s.targetAngle = target;

    const d = this.goal ? Math.hypot(this.goal.x - s.head.x, this.goal.z - s.head.z) : Infinity;
    s.wantBoost =
      s.freeBoost > 0 ||
      (this.mode === 'flee' && s.mass > 20) ||
      (this.mode === 'hunt' && d < 12 && s.mass > 25) ||
      (this.mode === 'food' && this.goalValue >= 3 && d < 10 && s.mass > 30);
  }

  pickGoal() {
    const s = this.s;
    const h = s.head;
    const g = this.g;
    this.goal = null;

    // 1. Flee: a bigger snake's head is close.
    let threat = null;
    let threatD = 10 + s.radius * 4;
    for (const o of g.snakes) {
      if (o === s || !o.alive || !this.dangerous(o)) continue;
      const d = Math.hypot(o.head.x - h.x, o.head.z - h.z) - o.radius * 2;
      if (d < threatD) {
        threatD = d;
        threat = o;
      }
    }
    if (threat && !this.boss) {
      const away = Math.atan2(h.z - threat.head.z, h.x - threat.head.x);
      this.mode = 'flee';
      this.goal = { x: h.x + Math.cos(away) * 20, z: h.z + Math.sin(away) * 20 };
      return;
    }

    // 2. Hunt: the closest piece of any smaller snake (the player is a favorite).
    const aggression = Math.min(1, this.aggression + (g.threat - 1) * 0.08);
    if (Math.random() < aggression) {
      let best = null;
      let bestD = this.boss ? 200 : 26;
      g.segGrid.query(h.x, h.z, this.boss ? 60 : 26, (seg) => {
        const o = seg.owner;
        if (o === s || !o.alive || o.lv >= s.lv || o.invuln > 0 || o.phase > 0) return;
        const d = Math.hypot(seg.x - h.x, seg.z - h.z) - (o.isPlayer ? 8 : 0);
        if (d < bestD) {
          bestD = d;
          best = seg;
        }
      });
      // Bosses always know where the player is.
      const p = g.player;
      if (this.boss && !best && p?.alive && p.lv < s.lv) best = p.head;
      if (best) {
        this.mode = 'hunt';
        this.goal = { x: best.x, z: best.z };
        return;
      }
    }

    // 3. Food: best value / distance within range.
    let bestScore = 0;
    let bestFood = null;
    g.food.grid.query(h.x, h.z, 22, (f) => {
      if (!f.alive) return;
      const score = f.value / (Math.hypot(f.x - h.x, f.z - h.z) + 2);
      if (score > bestScore) {
        bestScore = score;
        bestFood = f;
      }
    });
    if (bestFood) {
      this.mode = 'food';
      this.goal = bestFood;
      this.goalValue = bestFood.value;
      return;
    }

    // 4. Wander, drifting back toward the center when far out.
    this.mode = 'wander';
    this.wander += (Math.random() - 0.5) * 1.6;
    if (Math.hypot(h.x, h.z) > CONFIG.arenaRadius * 0.6) this.wander = Math.atan2(-h.z, -h.x);
  }

  goalAngle() {
    const h = this.s.head;
    if (this.goal && (this.goal.alive ?? true)) return Math.atan2(this.goal.z - h.z, this.goal.x - h.x);
    return this.wander;
  }

  // Returns a danger score for moving in direction `a` (0 = safe).
  probe(a, look) {
    const s = this.s;
    const h = s.head;
    const g = this.g;
    const cx = Math.cos(a);
    const cz = Math.sin(a);
    const R = CONFIG.arenaRadius;
    const reach = s.radius + g.maxSegRadius + 0.8;
    let danger = 0;
    for (let k = 1; k <= 3; k++) {
      const d = (look * k) / 3;
      const px = h.x + cx * d;
      const pz = h.z + cz * d;
      if (Math.hypot(px, pz) > R - s.radius * 2 - 2 || g.inLava(px, pz, s.radius + 1.5)) {
        danger += 4 - k;
        continue;
      }
      let hit = false;
      g.segGrid.query(px, pz, reach, (seg) => {
        const o = seg.owner;
        if (o === s || !o.alive || !this.dangerous(o)) return;
        const rr = s.radius + o.radius + 0.5;
        if ((seg.x - px) ** 2 + (seg.z - pz) ** 2 < rr * rr) {
          hit = true;
          return false;
        }
      });
      if (hit) danger += 4 - k; // closer danger counts more
    }
    return danger;
  }
}
