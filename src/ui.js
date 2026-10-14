import { CONFIG } from './config.js';
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
      ? `Best run — Level <b>${b.level}</b> · Length <b>${b.length}</b> · Kills <b>${b.kills}</b> · ${formatTime(b.time)}`
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

    this.drawMinimap(game);
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
    for (const s of game.snakes) {
      const me = s === game.player;
      g.fillStyle = me ? '#ffffff' : s.skin.colorA;
      const r = me ? 4 : 1.5 + Math.sqrt(s.mass) / 6;
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
    for (const k of ['level', 'length', 'kills', 'time']) if (isNew(k)) best[k] = stats[k];
    saveBest(best);
    this.renderBest();
    // Small delay so you see your snake explode first.
    this.gameOverTimer = setTimeout(() => this.show('gameover'), 900);
  }
}
