import { Game } from './game.js';
import { Input } from './input.js';
import { UI } from './ui.js';
import { SkinPreview, skinThumbnail } from './preview.js';
import { OPTIONS, CATALOG, CATALOG_BY_ID, RARITIES, loadProfile, saveProfile, equippedSkin, randomCustomSkin } from './skins.js';

const $ = (id) => document.getElementById(id);

const ui = new UI();
const input = new Input($('game'));
const game = new Game($('game'), input, ui);
input.onPause = () => game.togglePause();

const profile = loadProfile();
game.startAttract(); // bots play in the background of the menu

function save() {
  saveProfile(profile);
  for (const el of document.querySelectorAll('.coin-count')) el.textContent = profile.coins;
}
save();

// Coins earned at the end of every run.
game.onGameOver = (stats) => {
  profile.coins += stats.coins;
  save();
};

function play() {
  game.startRun(equippedSkin(profile), profile.name);
}

function goMenu() {
  ui.hideHud();
  ui.show('menu');
  game.startAttract();
}

// ---------------------------------------------------------------- menu
$('player-name').value = profile.name;
$('player-name').oninput = (e) => {
  profile.name = e.target.value.trim() || 'Player';
  save();
};
$('btn-play').onclick = play;
$('btn-howto').onclick = () => ui.show('howto');
$('btn-resume').onclick = () => game.togglePause();
$('btn-quit').onclick = goMenu;
$('btn-again').onclick = play;
$('btn-menu').onclick = goMenu;
document.querySelectorAll('.btn-back').forEach(
  (b) =>
    (b.onclick = () => {
      preview.stop();
      ui.show('menu');
    }),
);

// ---------------------------------------------------------------- skin shop
const preview = new SkinPreview($('preview'));
let tab = 'common';
let selected = profile.equipped;

function skinById(id) {
  return id === 'custom' ? profile.custom : CATALOG_BY_ID[id];
}

function renderTabs() {
  const tabs = $('shop-tabs');
  tabs.innerHTML = '';
  for (const r of [...RARITIES, { id: 'custom', label: '🎨 Custom' }]) {
    const b = document.createElement('button');
    b.textContent = r.label.toUpperCase();
    b.className = (r.id === tab ? 'active ' : '') + (r.id === 'custom' ? 'custom' : '');
    b.onclick = () => {
      tab = r.id;
      if (tab === 'custom') select('custom');
      renderShop();
    };
    tabs.appendChild(b);
  }
}

function renderGrid() {
  const grid = $('shop-grid');
  grid.innerHTML = '';
  for (const skin of CATALOG.filter((s) => s.rarity === tab)) {
    const owned = profile.owned.includes(skin.id);
    const card = document.createElement('button');
    card.className = `skin-card ${owned ? '' : 'locked'} ${selected === skin.id ? 'selected' : ''}`;
    card.innerHTML = `
      <img alt="" src="${skinThumbnail(skin)}" />
      <div class="card-name">${skin.label}</div>
      <div class="card-price">${owned ? (profile.equipped === skin.id ? '✔ Equipped' : 'Owned') : `🪙 ${skin.price}`}</div>
      ${owned ? '' : '<span class="badge">🔒</span>'}`;
    card.onclick = () => {
      select(skin.id);
      renderGrid();
    };
    grid.appendChild(card);
  }
}

function select(id) {
  selected = id;
  const skin = skinById(id);
  preview.setSkin(skin);
  $('sel-name').textContent = skin.label;
  const tag = $('sel-rarity');
  tag.textContent = skin.rarity;
  tag.className = `rarity-tag r-${skin.rarity}`;

  const btn = $('btn-equip');
  const owned = id === 'custom' || profile.owned.includes(id);
  if (profile.equipped === id) {
    btn.textContent = '✔ Equipped';
    btn.disabled = true;
  } else if (owned) {
    btn.textContent = 'Equip';
    btn.disabled = false;
  } else {
    btn.textContent = `Buy 🪙 ${skin.price}`;
    btn.disabled = profile.coins < skin.price;
  }
}

$('btn-equip').onclick = () => {
  const skin = skinById(selected);
  if (selected !== 'custom' && !profile.owned.includes(selected)) {
    if (profile.coins < skin.price) return;
    profile.coins -= skin.price;
    profile.owned.push(selected);
    ui.toast(`🎉 Unlocked ${skin.label}!`);
  }
  profile.equipped = selected;
  save();
  renderShop();
};

function renderShop() {
  renderTabs();
  const custom = tab === 'custom';
  $('shop-grid').classList.toggle('hidden', custom);
  $('custom-editor').classList.toggle('hidden', !custom);
  if (custom) renderCustom();
  else renderGrid();
  select(selected);
}

// --- custom skin editor ---
function renderCustom() {
  const c = profile.custom;
  const box = $('custom-fields');
  box.innerHTML = '';
  const fields = [
    ['head', 'Head'],
    ['body', 'Body'],
    ['spikes', 'Back'],
    ['pattern', 'Pattern'],
    ['hat', 'Hat'],
  ];
  if (c.head === 'round') fields.splice(1, 0, ['eyes', 'Eyes']);
  for (const [key, label] of fields) {
    const field = document.createElement('div');
    field.className = 'field';
    field.innerHTML = `<span>${label}</span><div class="options"></div>`;
    for (const opt of OPTIONS[key]) {
      const b = document.createElement('button');
      b.textContent = opt.label;
      b.classList.toggle('active', c[key] === opt.id);
      b.onclick = () => {
        c[key] = opt.id;
        save();
        renderCustom();
        preview.setSkin(c);
      };
      field.lastChild.appendChild(b);
    }
    box.appendChild(field);
  }
  for (const key of ['colorA', 'colorB', 'colorC', 'eyeColor']) $('c-' + key).value = c[key];
  $('c-glow').checked = c.glow;
}

for (const key of ['colorA', 'colorB', 'colorC', 'eyeColor']) {
  $('c-' + key).oninput = (e) => {
    profile.custom[key] = e.target.value;
    preview.setSkin(profile.custom);
  };
  $('c-' + key).onchange = save;
}
$('c-glow').onchange = (e) => {
  profile.custom.glow = e.target.checked;
  save();
  preview.setSkin(profile.custom);
};
$('btn-random').onclick = () => {
  Object.assign(profile.custom, randomCustomSkin(), { id: 'custom', label: 'My Custom', rarity: 'custom', price: 0 });
  save();
  renderCustom();
  preview.setSkin(profile.custom);
};

$('btn-skin').onclick = () => {
  selected = profile.equipped;
  const skin = skinById(selected);
  tab = skin.rarity === 'custom' ? 'custom' : skin.rarity;
  ui.show('skin-screen');
  renderShop();
  preview.start();
};

// Handy for experimenting: open the browser console and try `game.player.mass += 100`.
window.game = game;
window.profile = profile;
