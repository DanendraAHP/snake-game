import { CONFIG } from './config.js';
import { wrapAngle } from './snake.js';

// Directions (relative to current heading) the bot "feels" for danger.
const FEELERS = [0, -0.4, 0.4, -0.85, 0.85, -1.4, 1.4, -2.1, 2.1];

// A simple but effective bot brain:
//  1. Choose a GOAL (food, a snake to cut off, or wander).
//  2. Probe a few directions ahead for DANGER (bodies, the wall).
//  3. Steer toward the goal unless it's dangerous, then pick the safest direction.
export class BotBrain {
  constructor(snake, game) {
    this.s = snake;
    this.g = game;
    this.skill = 0.55 + Math.random() * 0.45; // higher = reacts faster
    this.aggression = 0.15 + Math.random() * 0.55;
    this.thinkTimer = 0;
    this.retarget = 0;
    this.goalFood = null;
    this.prey = null;
    this.wander = snake.angle;
  }

  think(dt) {
    this.thinkTimer -= dt;
    if (this.thinkTimer > 0) return;
    const interval = 0.05 + (1 - this.skill) * 0.15;
    this.thinkTimer = interval;

    this.retarget -= interval;
    if (this.retarget <= 0) {
      this.retarget = 0.4 + Math.random() * 0.6;
      this.pickGoal();
    }

    const s = this.s;
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

    // Boost when hunting close prey or grabbing a juicy food nearby.
    let boost = false;
    if (this.prey?.alive) {
      const d = Math.hypot(this.prey.head.x - s.head.x, this.prey.head.z - s.head.z);
      boost = d < 14 && s.mass > 25;
    } else if (this.goalFood?.alive && this.goalFood.value >= 3) {
      boost = Math.hypot(this.goalFood.x - s.head.x, this.goalFood.z - s.head.z) < 10 && s.mass > 30;
    }
    s.wantBoost = boost;
  }

  pickGoal() {
    const s = this.s;
    const h = s.head;
    const g = this.g;
    this.prey = null;
    this.goalFood = null;

    // Hunt: try to cut in front of a nearby snake (the player is a favorite target).
    const aggression = Math.min(0.9, this.aggression + (g.threat - 1) * 0.08);
    if (Math.random() < aggression) {
      let bestD = 20;
      for (const o of g.snakes) {
        if (o === s || !o.alive) continue;
        const d = Math.hypot(o.head.x - h.x, o.head.z - h.z) - (o.isPlayer ? 6 : 0);
        if (d < bestD && s.mass > o.mass * 0.6) {
          bestD = d;
          this.prey = o;
        }
      }
      if (this.prey) return;
    }

    // Food: best value / distance within range.
    let bestScore = 0;
    g.food.grid.query(h.x, h.z, 22, (f) => {
      if (!f.alive) return;
      const d = Math.hypot(f.x - h.x, f.z - h.z);
      const score = f.value / (d + 2);
      if (score > bestScore) {
        bestScore = score;
        this.goalFood = f;
      }
    });
    if (this.goalFood) return;

    // Wander, drifting back toward the center when far out.
    this.wander += (Math.random() - 0.5) * 1.6;
    if (Math.hypot(h.x, h.z) > CONFIG.arenaRadius * 0.6) this.wander = Math.atan2(-h.z, -h.x);
  }

  goalAngle() {
    const h = this.s.head;
    if (this.prey?.alive) {
      const p = this.prey;
      const lead = 6 + p.radius * 4;
      const tx = p.head.x + Math.cos(p.angle) * lead;
      const tz = p.head.z + Math.sin(p.angle) * lead;
      return Math.atan2(tz - h.z, tx - h.x);
    }
    if (this.goalFood?.alive) return Math.atan2(this.goalFood.z - h.z, this.goalFood.x - h.x);
    return this.wander;
  }

  // Returns a danger score for moving in direction `a` (0 = safe).
  probe(a, look) {
    const s = this.s;
    const h = s.head;
    const cx = Math.cos(a);
    const cz = Math.sin(a);
    const R = CONFIG.arenaRadius;
    const reach = s.radius + this.g.maxSegRadius + 0.8;
    let danger = 0;
    for (let k = 1; k <= 3; k++) {
      const d = (look * k) / 3;
      const px = h.x + cx * d;
      const pz = h.z + cz * d;
      if (Math.hypot(px, pz) > R - s.radius * 2 - 2) {
        danger += 4 - k;
        continue;
      }
      let hit = false;
      this.g.segGrid.query(px, pz, reach, (seg) => {
        const o = seg.owner;
        if (o === s || !o.alive || o.phase > 0) return;
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
