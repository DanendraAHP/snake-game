import * as THREE from 'three';

// ---------------------------------------------------------------------------
// Maps / biomes. Each one has its own look and ONE gameplay twist.
// To add a map: add an entry here (floor style + decoration style + twist).
// ---------------------------------------------------------------------------
export const MAPS = [
  {
    id: 'neon', name: 'Neon Arena', desc: 'The classic glowing arena.',
    radius: 140, bg: '#0b1020', floor: ['hex', '#141a33', '#26305a'], outside: '#400818', wall: '#ff6b8a', deco: 'crystals',
  },
  {
    id: 'jungle', name: 'Jungle', desc: 'Lush and full of fruit: +50% food.',
    radius: 140, bg: '#0c1c10', floor: ['leaves', '#21502a', '#2c6634'], outside: '#0d2412', wall: '#b6f04a', deco: 'trees',
    foodMul: 1.5,
  },
  {
    id: 'desert', name: 'Desert', desc: 'A huge sandy map. More room, a bit less food.',
    radius: 180, bg: '#2e1f12', floor: ['dunes', '#d8b06a', '#c49650'], outside: '#6e3f17', wall: '#ffb347', deco: 'cacti',
    foodMul: 0.85,
  },
  {
    id: 'ice', name: 'Frozen Lake', desc: 'Slippery ice: you slide faster but turn 30% slower.',
    radius: 140, bg: '#0d2131', floor: ['cracks', '#8fcbe8', '#c6ecff'], outside: '#e9f8ff', wall: '#7fe8ff', deco: 'icebergs',
    turnMul: 0.7, speedMul: 1.08,
  },
  {
    id: 'volcano', name: 'Volcano', desc: 'Lava pools burn any snake that touches them!',
    radius: 150, bg: '#180705', floor: ['rocks', '#2b1b17', '#3e261f'], outside: '#5a1406', wall: '#ff6a00', deco: 'rocks',
    lava: 8,
  },
  {
    id: 'end', name: 'The End', desc: 'A dark void island. Golden food is 4x more common.',
    radius: 140, bg: '#07030f', floor: ['specks', '#d6d29a', '#bdb878'], outside: '#12001f', wall: '#b45dff', deco: 'pillars',
    goldenBonus: 0.03,
  },
];

export const MAPS_BY_ID = Object.fromEntries(MAPS.map((m) => [m.id, m]));
export const MAP_DEFAULTS = { foodMul: 1, turnMul: 1, speedMul: 1, goldenBonus: 0, lava: 0 };

export function getMap(id) {
  if (id === 'random' || !MAPS_BY_ID[id]) return MAPS[Math.floor(Math.random() * MAPS.length)];
  return MAPS_BY_ID[id];
}

// ---------------------------------------------------------------------------
// Floor textures, drawn on a 2D canvas (one tile, repeated over the floor).
// ---------------------------------------------------------------------------
function floorTexture([style, base, line]) {
  const c = document.createElement('canvas');
  c.width = c.height = 256;
  const g = c.getContext('2d');
  g.fillStyle = base;
  g.fillRect(0, 0, 256, 256);
  g.strokeStyle = g.fillStyle = line;
  g.lineWidth = 4;
  let seed = 7;
  const rnd = () => ((seed = (seed * 16807) % 2147483647) / 2147483647);

  if (style === 'hex') {
    const r = 64, h = Math.sqrt(3) * r;
    c.height = 222; // resizing a canvas resets its drawing settings
    g.fillStyle = base;
    g.fillRect(0, 0, 256, 222);
    g.strokeStyle = line;
    g.lineWidth = 4;
    for (let col = -1; col < 4; col++)
      for (let row = -1; row < 3; row++) {
        const cx = col * r * 1.5, cy = row * h + (col % 2 ? h / 2 : 0);
        g.beginPath();
        for (let i = 0; i < 6; i++) g.lineTo(cx + Math.cos((Math.PI / 3) * i) * r, cy + Math.sin((Math.PI / 3) * i) * r);
        g.closePath();
        g.stroke();
      }
  } else if (style === 'leaves') {
    for (let i = 0; i < 40; i++) {
      g.beginPath();
      g.ellipse(rnd() * 256, rnd() * 256, 10 + rnd() * 18, 5 + rnd() * 8, rnd() * Math.PI, 0, Math.PI * 2);
      g.fill();
    }
  } else if (style === 'dunes') {
    for (let y = 0; y < 256; y += 32) {
      g.beginPath();
      for (let x = 0; x <= 256; x += 8) g.lineTo(x, y + Math.sin((x / 256) * Math.PI * 4) * 8);
      g.stroke();
    }
  } else if (style === 'cracks') {
    g.lineWidth = 3;
    for (let i = 0; i < 14; i++) {
      let x = rnd() * 256, y = rnd() * 256;
      g.beginPath();
      g.moveTo(x, y);
      for (let k = 0; k < 4; k++) g.lineTo((x += (rnd() - 0.5) * 70), (y += (rnd() - 0.5) * 70));
      g.stroke();
    }
  } else if (style === 'rocks') {
    for (let i = 0; i < 26; i++) {
      g.beginPath();
      const x = rnd() * 256, y = rnd() * 256, r = 8 + rnd() * 18;
      for (let k = 0; k < 6; k++) g.lineTo(x + Math.cos(k + rnd()) * r, y + Math.sin(k + rnd()) * r);
      g.closePath();
      g.fill();
    }
  } else if (style === 'specks') {
    for (let i = 0; i < 120; i++) g.fillRect(rnd() * 256, rnd() * 256, 4 + rnd() * 6, 4 + rnd() * 6);
  }
  const tex = new THREE.CanvasTexture(c);
  tex.wrapS = tex.wrapT = THREE.RepeatWrapping;
  tex.colorSpace = THREE.SRGBColorSpace;
  tex.anisotropy = 8;
  return tex;
}

// ---------------------------------------------------------------------------
// Decorations outside the arena wall.
// ---------------------------------------------------------------------------
const std = (color, emissive = 0x000000, ei = 0) => new THREE.MeshStandardMaterial({ color, emissive, emissiveIntensity: ei, roughness: 0.8 });

function decorate(group, map, R) {
  const ring = (count, fn) => {
    for (let i = 0; i < count; i++) {
      const a = Math.random() * Math.PI * 2;
      const d = R + 8 + Math.random() * 45;
      const obj = fn(Math.random());
      obj.position.x = Math.cos(a) * d;
      obj.position.z = Math.sin(a) * d;
      obj.rotation.y = Math.random() * Math.PI * 2;
      obj.traverse((o) => (o.castShadow = o.isMesh));
      group.add(obj);
    }
  };
  switch (map.deco) {
    case 'crystals': {
      const geo = new THREE.OctahedronGeometry(1, 0);
      ring(50, (r) => {
        const s = 1.5 + r * 4;
        const m = new THREE.Mesh(geo, std(0x223355, new THREE.Color().setHSL(0.5 + r * 0.4, 0.9, 0.5), 0.7));
        m.scale.set(s * 0.6, s * 1.6, s * 0.6);
        m.position.y = s * 0.8;
        return m;
      });
      break;
    }
    case 'trees':
      ring(70, (r) => {
        const t = new THREE.Group();
        const h = 3 + r * 5;
        const trunk = new THREE.Mesh(new THREE.CylinderGeometry(0.35, 0.5, h, 6), std(0x6b4423));
        trunk.position.y = h / 2;
        t.add(trunk);
        for (let k = 0; k < 3; k++) {
          const leaf = new THREE.Mesh(new THREE.IcosahedronGeometry(2.2 - k * 0.5, 0), std(new THREE.Color().setHSL(0.28 + r * 0.08, 0.6, 0.32 + k * 0.06)));
          leaf.position.y = h + k * 1.3;
          t.add(leaf);
        }
        return t;
      });
      break;
    case 'cacti':
      ring(45, (r) => {
        const t = new THREE.Group();
        const mat = std(0x3f8f3a);
        const h = 3 + r * 4;
        const body = new THREE.Mesh(new THREE.CapsuleGeometry(0.6, h, 4, 8), mat);
        body.position.y = h / 2 + 0.6;
        t.add(body);
        for (const side of [1, -1]) {
          const arm = new THREE.Mesh(new THREE.CapsuleGeometry(0.4, 1.4, 4, 8), mat);
          arm.position.set(side * 1.1, h * 0.6 + side * 0.4, 0);
          t.add(arm);
        }
        return t;
      });
      break;
    case 'icebergs': {
      const geo = new THREE.IcosahedronGeometry(1, 0);
      ring(45, (r) => {
        const m = new THREE.Mesh(geo, std(0xdff6ff, 0x7fe8ff, 0.15));
        const s = 2 + r * 5;
        m.scale.set(s, s * (0.6 + r), s);
        m.position.y = s * 0.3;
        return m;
      });
      break;
    }
    case 'rocks': {
      const geo = new THREE.DodecahedronGeometry(1, 0);
      ring(50, (r) => {
        const m = new THREE.Mesh(geo, std(0x2a1d1a, 0xff3b00, r < 0.3 ? 0.35 : 0));
        const s = 2 + r * 5;
        m.scale.set(s, s * 0.7, s);
        m.position.y = s * 0.3;
        return m;
      });
      break;
    }
    case 'pillars':
      ring(30, (r) => {
        const t = new THREE.Group();
        const h = 8 + r * 18;
        const p = new THREE.Mesh(new THREE.BoxGeometry(3, h, 3), std(0x1a1024));
        p.position.y = h / 2;
        const top = new THREE.Mesh(new THREE.OctahedronGeometry(1.2, 0), new THREE.MeshBasicMaterial({ color: 0xe57bff }));
        top.position.y = h + 1.5;
        t.add(p, top);
        return t;
      });
      break;
  }
}

// ---------------------------------------------------------------------------
// Builds everything for a map. Returns { group, pools }.
// pools = lava circles [{ x, z, r }] that kill snakes.
// ---------------------------------------------------------------------------
export function buildMap(map) {
  const R = map.radius;
  const group = new THREE.Group();

  const tex = floorTexture(map.floor);
  tex.repeat.set((R * 2) / 14, (R * 2) / 14);
  const floor = new THREE.Mesh(new THREE.CircleGeometry(R + 70, 96), new THREE.MeshStandardMaterial({ map: tex, roughness: 0.9 }));
  floor.rotation.x = -Math.PI / 2;
  floor.receiveShadow = true;
  group.add(floor);

  const outside = new THREE.Mesh(
    new THREE.RingGeometry(R, R + 70, 96),
    new THREE.MeshBasicMaterial({ color: map.outside, transparent: true, opacity: 0.7 }),
  );
  outside.rotation.x = -Math.PI / 2;
  outside.position.y = 0.03;
  group.add(outside);

  const wall = new THREE.Mesh(
    new THREE.CylinderGeometry(R, R, 3, 160, 1, true),
    new THREE.MeshBasicMaterial({ color: map.wall, transparent: true, opacity: 0.25, side: THREE.DoubleSide, depthWrite: false }),
  );
  wall.position.y = 1.5;
  group.add(wall);
  const rim = new THREE.Mesh(new THREE.TorusGeometry(R, 0.25, 8, 220), new THREE.MeshBasicMaterial({ color: map.wall }));
  rim.rotation.x = Math.PI / 2;
  rim.position.y = 3;
  group.add(rim);

  decorate(group, map, R);

  // Lava pools (Volcano): random each run, never in the middle where you spawn.
  const pools = [];
  for (let i = 0; i < (map.lava || 0); i++) {
    for (let tries = 0; tries < 20; tries++) {
      const a = Math.random() * Math.PI * 2;
      const d = R * (0.25 + Math.random() * 0.6);
      const p = { x: Math.cos(a) * d, z: Math.sin(a) * d, r: 6 + Math.random() * 6 };
      if (pools.every((q) => Math.hypot(q.x - p.x, q.z - p.z) > q.r + p.r + 8)) {
        pools.push(p);
        break;
      }
    }
  }
  const lavaMat = new THREE.MeshBasicMaterial({ color: 0xff5a00 });
  const crustMat = new THREE.MeshBasicMaterial({ color: 0x1a0a06 });
  for (const p of pools) {
    const crust = new THREE.Mesh(new THREE.CircleGeometry(p.r + 1.2, 32), crustMat);
    crust.rotation.x = -Math.PI / 2;
    crust.position.set(p.x, 0.04, p.z);
    const lava = new THREE.Mesh(new THREE.CircleGeometry(p.r, 32), lavaMat);
    lava.rotation.x = -Math.PI / 2;
    lava.position.set(p.x, 0.06, p.z);
    group.add(crust, lava);
  }
  group.userData.lavaMat = lavaMat;
  return { group, pools };
}

export function disposeGroup(group) {
  group.traverse((o) => {
    if (!o.isMesh) return;
    o.geometry.dispose();
    const mats = Array.isArray(o.material) ? o.material : [o.material];
    for (const m of mats) {
      m.map?.dispose();
      m.dispose();
    }
  });
  group.removeFromParent();
}
