import * as THREE from 'three';
import { CONFIG } from './config.js';
import { BOSS_SKINS } from './skins.js';
const bossVec = new THREE.Vector3();
const BOSS_ICON = '👹';
import { RARITY, UPGRADES_BY_ID } from './upgrades.js';

const $ = (id) => document.getElementById(id);
const SCREENS = ['menu', 'skin-screen', 'howto', 'levelup', 'pause', 'gameover'];

function formatTime(t) {
  const m = Math.floor(t / 60);
  const s = Math.floor(t % 60);
  return `${m}:${String(s).padStart(2, '0')}`;
}

function escapeHtml(text) {
  const div = document.createElement('div');
  div.textContent = text;
  return div.innerHTML;
}

const BEST_KEY = 'serpentRogue.best';
function loadBest() {
  try {
    return JSON.parse(localStorage.getItem(BEST_KEY)) || {};
  } catch {
    return {};
  }
}
function saveBest(best) {
  try {
    localStorage.setItem(BEST_KEY, JSON.stringify(best));
  } catch {
    /* ignore */
  }
}

// Everything HTML: menus, HUD, level-up cards, game over.
export class UI {
  constructor() {
    this.minimap = $('minimap').getContext('2d');
    this.levelUpKeys = null;
    window.addEventListener('keydown', (e) => this.levelUpKeys?.(e));
    this.renderBest();
  }

  show(id) {
    if (id !== 'gameover') clearTimeout(this.gameOverTimer);
    for (const s of SCREENS) $(s).classList.toggle('hidden', s !== id);
  }

  hideAll() {
    this.show(null);
  }

  renderBest() {
    const b = loadBest();
    $('best').innerHTML = b.level
      ? `Best run — ★<b>${b.level}</b> · Length <b>${b.length}</b> · Kills <b>${b.kills}</b> · ${formatTime(b.time)}`
      : 'No runs yet. Go get eaten!';
  }

  // ---------------------------------------------------------------- HUD
  showHud(player) {
    this.hideAll();
    $('hud').classList.remove('hidden');
    this.updateUpgrades(player);
  }

  hideHud() {
    $('hud').classList.add('hidden');
  }

  updateHud(game) {
    const p = game.player;
    if (!p) return;
    $('hud-level').textContent = p.level;
    $('hud-xp').style.width = `${Math.min(100, (p.xp / p.xpNext) * 100)}%`;
    $('hud-length').textContent = Math.floor(p.mass);
    $('hud-kills').textContent = p.kills;
    $('hud-time').textContent = formatTime(game.runTime);
    let extra = '';
    if (p.stats.shieldMax) extra += `🛡️ ${p.shields}/${p.stats.shieldMax} `;
    if (p.stats.revives) extra += `❤️‍🔥 ${p.stats.revives} `;
    if (p.stats.phantom) extra += p.phantomCooldown > 0 ? `👻 ${Math.ceil(p.phantomCooldown)}s` : '👻 ready';
    $('hud-shields').textContent = extra;

    // Leaderboard
    const sorted = [...game.snakes].sort((a, b) => b.mass - a.mass);
    const rank = sorted.indexOf(p);
    let html = sorted
      .slice(0, 8)
      .map((s) => `<li class="${s === p ? 'me' : ''}"><span>${escapeHtml(s.name)}</span><span>${Math.floor(s.mass)}</span></li>`)
      .join('');
    if (rank >= 8) html += `<li class="me" value="${rank + 1}"><span>${escapeHtml(p.name)}</span><span>${Math.floor(p.mass)}</span></li>`;
    $('leaderboard').innerHTML = html;

    // Boss countdown / boss info
    const bossEl = $('hud-boss');
    if (game.boss) {
      // A boss is out: how long until it leaves, and can you eat it?
      const b = game.boss;
      const left = Math.max(0, game.bossLeft);
      const canEat = p.lv > b.lv;
      bossEl.className = 'hud-boss active';
      bossEl.innerHTML = `
        <div class="boss-top">${BOSS_ICON} <b>${escapeHtml(b.name)}</b><span class="boss-lv">Lv${b.lv}</span></div>
        <div class="boss-bar"><div style="width:${(left / CONFIG.bossStay) * 100}%"></div></div>
        <div class="boss-sub">${canEat ? '<span class="good">YOU CAN EAT IT! GO!</span>' : `Need <b>Lv${b.lv + 1}</b> to eat it (you: Lv${p.lv})`} · leaves in ${formatTime(left)}</div>`;
    } else {
      // Countdown to the next boss.
      const t = Math.max(0, game.bossTimer);
      const next = BOSS_SKINS[game.bossCount % BOSS_SKINS.length];
      const fill = (1 - t / (game.bossTimerMax || CONFIG.bossFirst)) * 100;
      bossEl.className = 'hud-boss' + (t <= CONFIG.bossWarning ? ' warn' : t <= 30 ? ' soon' : '');
      bossEl.innerHTML = `
        <div class="boss-top">${BOSS_ICON} Next boss: <b>${escapeHtml(next.label)}</b></div>
        <div class="boss-digits">${formatTime(t)}</div>
        <div class="boss-bar"><div style="width:${fill}%"></div></div>`;
    }

    this.drawMinimap(game);
  }

  // Runs every frame while playing: 3-2-1 countdown, red glow, arrow to the boss.
  updateBossFx(game) {
    const p = game.player;
    const vignette = $('vignette');
    const count = $('boss-count');
    const arrow = $('boss-arrow');

    // Giant countdown in the last 3 seconds, then "BOSS!".
    let text = '';
    if (!game.boss && game.bossTimer <= 3 && game.bossTimer > 0) text = String(Math.ceil(game.bossTimer));
    else if (game.boss && CONFIG.bossStay - game.bossLeft < 1.2) text = 'BOSS!';
    if (text !== this.countText) {
      this.countText = text;
      count.textContent = text;
      count.classList.remove('pop');
      if (text) {
        void count.offsetWidth; // restart the CSS animation
        count.classList.add('pop');
      }
    }

    // Red glow: pulses during the warning, and gets stronger the closer the boss is.
    let glow = 0;
    if (!game.boss && game.bossTimer <= CONFIG.bossWarning) glow = 0.25 + 0.2 * Math.sin(game.time * 8);
    if (game.boss && p?.alive) {
      const d = Math.hypot(game.boss.head.x - p.head.x, game.boss.head.z - p.head.z);
      glow = Math.max(0, Math.min(0.7, 1 - (d - 12) / 45));
    }
    if (!vignette.classList.contains('hit')) vignette.style.opacity = glow;

    // Arrow on the screen edge pointing to the boss when you can't see it.
    if (!game.boss) {
      arrow.style.display = 'none';
      return;
    }
    bossVec.set(game.boss.head.x, 0, game.boss.head.z).project(game.camera);
    let x = bossVec.x;
    let y = -bossVec.y;
    if (bossVec.z > 1) {
      x = -x;
      y = -y;
    }
    const onScreen = bossVec.z < 1 && Math.abs(x) < 0.95 && Math.abs(y) < 0.95;
    if (onScreen) {
      arrow.style.display = 'none';
      return;
    }
    const k = 0.88 / Math.max(Math.abs(x), Math.abs(y), 1e-6);
    const sx = (x * k * 0.5 + 0.5) * window.innerWidth;
    const sy = (y * k * 0.5 + 0.5) * window.innerHeight;
    arrow.style.display = 'block';
    arrow.style.transform = `translate(${sx}px, ${sy}px)`;
    arrow.querySelector('.arrow').style.transform = `rotate(${Math.atan2(y, x)}rad)`;
  }

  flash() {
    const v = $('vignette');
    v.classList.remove('hit');
    void v.offsetWidth;
    v.classList.add('hit');
    v.style.opacity = 0;
    clearTimeout(this.flashTimer);
    this.flashTimer = setTimeout(() => v.classList.remove('hit'), 400);
  }

  drawMinimap(game) {
    const g = this.minimap;
    const size = g.canvas.width;
    const c = size / 2;
    const scale = (c - 6) / CONFIG.arenaRadius;
    g.clearRect(0, 0, size, size);
    g.strokeStyle = '#ff6b8a';
    g.lineWidth = 2;
    g.beginPath();
    g.arc(c, c, c - 4, 0, Math.PI * 2);
    g.stroke();
    // Lava pools
    g.fillStyle = '#ff5a00';
    for (const p of game.pools) {
      g.beginPath();
      g.arc(c + p.x * scale, c + p.z * scale, p.r * scale, 0, Math.PI * 2);
      g.fill();
    }
    for (const s of game.snakes) {
      const me = s === game.player;
      g.fillStyle = me ? '#ffffff' : s.isBoss ? '#ff2020' : s.skin.colorA;
      const r = me ? 4 : s.isBoss ? 7 : 1.5 + Math.sqrt(s.mass) / 6;
      g.beginPath();
      g.arc(c + s.head.x * scale, c + s.head.z * scale, r, 0, Math.PI * 2);
      g.fill();
    }
  }

  updateUpgrades(player) {
    $('hud-upgrades').innerHTML = Object.entries(player.upgrades)
      .map(([id, n]) => `<span class="up-chip" title="${UPGRADES_BY_ID[id].name}">${UPGRADES_BY_ID[id].icon}${n > 1 ? `<small>x${n}</small>` : ''}</span>`)
      .join('');
  }

  toast(msg) {
    const el = document.createElement('div');
    el.className = 'toast';
    el.textContent = msg;
    $('toasts').appendChild(el);
    setTimeout(() => el.remove(), 2400);
  }

  // ---------------------------------------------------------------- level up
  showLevelUp({ level, choices, rerolls, owned, onPick, onReroll }) {
    this.show('levelup');
    $('lu-level').textContent = level;
    let current = choices;

    const render = (rr) => {
      $('lu-rerolls').textContent = rr;
      $('btn-reroll').disabled = rr <= 0;
      const cards = $('lu-cards');
      cards.innerHTML = '';
      current.forEach((u, i) => {
        const have = owned[u.id] || 0;
        const card = document.createElement('button');
        card.className = `card ${u.rarity}`;
        card.innerHTML = `
          <div class="icon">${u.icon}</div>
          <div class="rarity">${RARITY[u.rarity].label}</div>
          <div class="name">${u.name}</div>
          <div class="desc">${u.desc}</div>
          ${have ? `<div class="stack">Owned: ${have}${u.max < 99 ? ` / ${u.max}` : ''}</div>` : ''}
          <div class="key">[${i + 1}]</div>`;
        card.onclick = () => pick(u);
        cards.appendChild(card);
      });
    };
    const pick = (u) => {
      this.levelUpKeys = null;
      this.hideAll();
      onPick(u);
    };
    const reroll = () => {
      const res = onReroll();
      if (!res) return;
      current = res.choices;
      render(res.rerolls);
    };
    $('btn-reroll').onclick = reroll;
    this.levelUpKeys = (e) => {
      const n = Number(e.key);
      if (n >= 1 && n <= current.length) pick(current[n - 1]);
      if (e.code === 'KeyR') reroll();
    };
    render(rerolls);
  }

  showPause(paused) {
    this.show(paused ? 'pause' : null);
  }

  // ---------------------------------------------------------------- game over
  showGameOver(stats) {
    this.hideHud();
    const best = loadBest();
    const isNew = (k) => stats[k] > (best[k] || 0);
    const cell = (label, k, value) => `<div>${label}<b>${value}</b>${isNew(k) ? '<span class="new">NEW BEST!</span>' : ''}</div>`;
    $('go-stats').innerHTML =
      cell('Level', 'level', stats.level) +
      cell('Max length', 'length', stats.length) +
      cell('Kills', 'kills', stats.kills) +
      cell('Survived', 'time', formatTime(stats.time));
    $('go-upgrades').innerHTML = Object.entries(stats.upgrades)
      .map(([id, n]) => `<span class="up-chip" title="${UPGRADES_BY_ID[id].name}">${UPGRADES_BY_ID[id].icon}${n > 1 ? `<small>x${n}</small>` : ''}</span>`)
      .join('');
    $('go-extra').textContent = `Map: ${stats.map} · Bosses eaten: ${stats.bossKills}`;
    $('go-coins').textContent = `+${stats.coins} 🪙 coins for the skin shop`;
    for (const k of ['level', 'length', 'kills', 'time']) if (isNew(k)) best[k] = stats[k];
    saveBest(best);
    this.renderBest();
    // Small delay so you see your snake explode first.
    this.gameOverTimer = setTimeout(() => this.show('gameover'), 900);
  }
}
