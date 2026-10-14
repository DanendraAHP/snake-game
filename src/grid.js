// A spatial hash: splits the world into square cells so we only test
// collisions against things that are nearby instead of everything.
export class SpatialHash {
  constructor(cellSize) {
    this.size = cellSize;
    this.cells = new Map();
    this.used = [];
  }

  key(cx, cz) {
    return (cx + 4096) * 8192 + (cz + 4096);
  }

  clear() {
    for (const cell of this.used) cell.length = 0;
    this.used.length = 0;
  }

  insert(x, z, item) {
    const k = this.key(Math.floor(x / this.size), Math.floor(z / this.size));
    let cell = this.cells.get(k);
    if (!cell) {
      cell = [];
      this.cells.set(k, cell);
    }
    if (cell.length === 0) this.used.push(cell);
    cell.push(item);
  }

  // Calls fn(item) for every item in cells overlapping the circle (x, z, r).
  // Return false from fn to stop early.
  query(x, z, r, fn) {
    const s = this.size;
    const x0 = Math.floor((x - r) / s), x1 = Math.floor((x + r) / s);
    const z0 = Math.floor((z - r) / s), z1 = Math.floor((z + r) / s);
    for (let cx = x0; cx <= x1; cx++) {
      for (let cz = z0; cz <= z1; cz++) {
        const cell = this.cells.get(this.key(cx, cz));
        if (!cell) continue;
        for (let i = 0; i < cell.length; i++) {
          if (fn(cell[i]) === false) return;
        }
      }
    }
  }
}
