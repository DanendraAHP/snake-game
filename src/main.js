import { Game } from './game.js';
import { Input } from './input.js';
import { UI } from './ui.js';
import { SkinPreview } from './preview.js';
import { PATTERNS, EYES, HATS, loadSkin, saveSkin, randomSkin } from './skins.js';

const $ = (id) => document.getElementById(id);

const ui = new UI();
const input = new Input($('game'));
const game = new Game($('game'), input, ui);
input.onPause = () => game.togglePause();

let skin = loadSkin();
game.startAttract(); // bots play in the background of the menu

function goMenu() {
  ui.hideHud();
  ui.show('menu');
  game.startAttract();
}

// ---------------------------------------------------------------- menu
$('btn-play').onclick = () => game.startRun(skin);
$('btn-howto').onclick = () => ui.show('howto');
document.querySelectorAll('.btn-back').forEach((b) => (b.onclick = () => ui.show('menu')));
$('btn-resume').onclick = () => game.togglePause();
$('btn-quit').onclick = goMenu;
$('btn-again').onclick = () => game.startRun(skin);
$('btn-menu').onclick = goMenu;

// ---------------------------------------------------------------- skin editor
const preview = new SkinPreview($('preview'));
let draft = { ...skin };

function buildOptions(containerId, options, key) {
  const box = $(containerId);
  box.innerHTML = '';
  for (const opt of options) {
    const b = document.createElement('button');
    b.textContent = opt.label;
    b.classList.toggle('active', draft[key] === opt.id);
    b.onclick = () => {
      draft[key] = opt.id;
      refreshEditor();
    };
    box.appendChild(b);
  }
}

function refreshEditor() {
  buildOptions('opt-pattern', PATTERNS, 'pattern');
  buildOptions('opt-eyes', EYES, 'eyes');
  buildOptions('opt-hat', HATS, 'hat');
  $('skin-name').value = draft.name;
  $('skin-colorA').value = draft.colorA;
  $('skin-colorB').value = draft.colorB;
  $('skin-glow').checked = draft.glow;
  preview.setSkin({ ...draft });
}

$('skin-name').oninput = (e) => (draft.name = e.target.value || 'You');
$('skin-colorA').oninput = (e) => {
  draft.colorA = e.target.value;
  preview.setSkin({ ...draft });
};
$('skin-colorB').oninput = (e) => {
  draft.colorB = e.target.value;
  preview.setSkin({ ...draft });
};
$('skin-glow').onchange = (e) => {
  draft.glow = e.target.checked;
  preview.setSkin({ ...draft });
};
$('btn-random').onclick = () => {
  draft = { ...randomSkin(), name: draft.name };
  refreshEditor();
};
$('btn-skin').onclick = () => {
  draft = { ...skin };
  ui.show('skin-screen');
  refreshEditor();
  preview.start();
};
$('btn-skin-save').onclick = () => {
  skin = { ...draft };
  saveSkin(skin);
  preview.stop();
  ui.show('menu');
};

// Handy for experimenting: open the browser console and try `game.player.mass += 100`.
window.game = game;
