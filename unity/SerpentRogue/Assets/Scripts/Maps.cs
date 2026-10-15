using System.Collections.Generic;
using UnityEngine;

namespace SerpentRogue
{
    // A map / biome: its own look and ONE gameplay twist.
    public class MapDef
    {
        public string id, name, desc;
        public float radius = 140;
        public string bg, floorStyle, floorBase, floorLine, outside, wall, deco;
        public float foodMul = 1, turnMul = 1, speedMul = 1, goldenBonus;
        public int lava;
    }

    public class LavaPool
    {
        public float x, z, r;
    }

    public static class Maps
    {
        public static readonly List<MapDef> All = new List<MapDef>
        {
            new MapDef { id = "neon", name = "Neon Arena", desc = "The classic glowing arena.", bg = "#0b1020", floorStyle = "grid", floorBase = "#141a33", floorLine = "#26305a", outside = "#400818", wall = "#ff6b8a", deco = "crystals" },
            new MapDef { id = "jungle", name = "Jungle", desc = "Lush and full of fruit: +50% food.", bg = "#0c1c10", floorStyle = "leaves", floorBase = "#21502a", floorLine = "#2c6634", outside = "#0d2412", wall = "#b6f04a", deco = "trees", foodMul = 1.5f },
            new MapDef { id = "desert", name = "Desert", desc = "A huge sandy map. More room, a bit less food.", radius = 180, bg = "#2e1f12", floorStyle = "dunes", floorBase = "#d8b06a", floorLine = "#c49650", outside = "#6e3f17", wall = "#ffb347", deco = "cacti", foodMul = 0.85f },
            new MapDef { id = "ice", name = "Frozen Lake", desc = "Slippery ice: you slide faster but turn 30% slower.", bg = "#0d2131", floorStyle = "cracks", floorBase = "#8fcbe8", floorLine = "#c6ecff", outside = "#e9f8ff", wall = "#7fe8ff", deco = "icebergs", turnMul = 0.7f, speedMul = 1.08f },
            new MapDef { id = "volcano", name = "Volcano", desc = "Lava pools burn any snake that touches them!", radius = 150, bg = "#180705", floorStyle = "rocks", floorBase = "#2b1b17", floorLine = "#3e261f", outside = "#5a1406", wall = "#ff6a00", deco = "rocks", lava = 8 },
            new MapDef { id = "end", name = "The End", desc = "A dark void island. Golden food is 4x more common.", bg = "#07030f", floorStyle = "specks", floorBase = "#d6d29a", floorLine = "#bdb878", outside = "#12001f", wall = "#b45dff", deco = "pillars", goldenBonus = 0.03f },
        };

        public static MapDef Get(string id)
        {
            if (id != "random")
                foreach (var m in All) if (m.id == id) return m;
            return All[Random.Range(0, All.Count)];
        }

        // ------------------------------------------------------------------ floor texture
        static Texture2D FloorTexture(MapDef map)
        {
            const int N = 128;
            var tex = new Texture2D(N, N) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            Color baseC = Toon.Hex(map.floorBase), line = Toon.Hex(map.floorLine);
            var px = new Color[N * N];
            for (int i = 0; i < px.Length; i++) px[i] = baseC;
            var rnd = new System.Random(7);
            System.Action<int, int> dot = (x, y) => px[((y % N + N) % N) * N + ((x % N + N) % N)] = line;
            System.Action<int, int, int> blob = (cx, cy, r) =>
            {
                for (int y = -r; y <= r; y++)
                    for (int x = -r; x <= r; x++)
                        if (x * x + y * y <= r * r) dot(cx + x, cy + y);
            };
            switch (map.floorStyle)
            {
                case "grid":
                    for (int i = 0; i < N; i++) { dot(i, 0); dot(i, 1); dot(0, i); dot(1, i); }
                    break;
                case "leaves":
                    for (int k = 0; k < 18; k++) blob(rnd.Next(N), rnd.Next(N), 3 + rnd.Next(6));
                    break;
                case "dunes":
                    for (int row = 0; row < N; row += 16)
                        for (int x = 0; x < N; x++) { int y = row + Mathf.RoundToInt(Mathf.Sin(x / (float)N * Mathf.PI * 4) * 4); dot(x, y); dot(x, y + 1); }
                    break;
                case "cracks":
                    for (int k = 0; k < 8; k++)
                    {
                        float x = rnd.Next(N), y = rnd.Next(N);
                        for (int seg = 0; seg < 4; seg++)
                        {
                            float a = (float)rnd.NextDouble() * Mathf.PI * 2, len = 10 + rnd.Next(25);
                            for (int t = 0; t < len; t++) dot(Mathf.RoundToInt(x + Mathf.Cos(a) * t), Mathf.RoundToInt(y + Mathf.Sin(a) * t));
                            x += Mathf.Cos(a) * len; y += Mathf.Sin(a) * len;
                        }
                    }
                    break;
                case "rocks":
                    for (int k = 0; k < 14; k++) blob(rnd.Next(N), rnd.Next(N), 4 + rnd.Next(8));
                    break;
                case "specks":
                    for (int k = 0; k < 60; k++) { int x = rnd.Next(N), y = rnd.Next(N), s = 2 + rnd.Next(3); for (int a = 0; a < s; a++) for (int b = 0; b < s; b++) dot(x + a, y + b); }
                    break;
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        static GameObject Spawn(string name, Mesh mesh, Material mat, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        // ------------------------------------------------------------------ decorations
        static void Decorate(Transform parent, MapDef map)
        {
            float R = map.radius;
            System.Func<int, System.Func<float, Transform>, int> ring = (count, make) =>
            {
                for (int i = 0; i < count; i++)
                {
                    float a = Random.value * Mathf.PI * 2, d = R + 8 + Random.value * 45;
                    Transform t = make(Random.value);
                    t.SetParent(parent, true);
                    t.position = new Vector3(Mathf.Cos(a) * d, t.position.y, Mathf.Sin(a) * d);
                    t.rotation = Quaternion.Euler(0, Random.value * 360, 0);
                }
                return count;
            };
            switch (map.deco)
            {
                case "crystals":
                {
                    Mesh m = MeshFactory.Cone(1, 2, 4);
                    ring(50, r =>
                    {
                        float s = 1.5f + r * 4;
                        var go = Spawn("Crystal", m, Toon.Make(Color.HSVToRGB(0.5f + r * 0.4f, 0.7f, 1f), 0.4f), null);
                        go.transform.localScale = new Vector3(s * 0.6f, s * 0.8f, s * 0.6f);
                        go.transform.position = new Vector3(0, s * 0.8f, 0);
                        return go.transform;
                    });
                    break;
                }
                case "trees":
                {
                    Mesh trunk = MeshFactory.Cylinder(0.35f, 0.5f, 1, 6), leaf = MeshFactory.Sphere(4, 6);
                    Material bark = Toon.Make(Toon.Hex("#6b4423"));
                    ring(70, r =>
                    {
                        var t = new GameObject("Tree").transform;
                        float h = 3 + r * 5;
                        var tr = Spawn("Trunk", trunk, bark, t);
                        tr.transform.localScale = new Vector3(1, h, 1);
                        tr.transform.localPosition = new Vector3(0, h / 2, 0);
                        for (int k = 0; k < 3; k++)
                        {
                            var l = Spawn("Leaves", leaf, Toon.Make(Color.HSVToRGB(0.28f + r * 0.08f, 0.6f, 0.4f + k * 0.08f)), t);
                            l.transform.localScale = Vector3.one * (2.2f - k * 0.5f);
                            l.transform.localPosition = new Vector3(0, h + k * 1.3f, 0);
                        }
                        return t;
                    });
                    break;
                }
                case "cacti":
                {
                    Mesh body = MeshFactory.RoundedBox(1.2f, 1, 1.2f, 0.5f, 4);
                    Material green = Toon.Make(Toon.Hex("#3f8f3a"));
                    ring(45, r =>
                    {
                        var t = new GameObject("Cactus").transform;
                        float h = 3 + r * 4;
                        var b = Spawn("Body", body, green, t);
                        b.transform.localScale = new Vector3(1, h, 1);
                        b.transform.localPosition = new Vector3(0, h / 2, 0);
                        foreach (int side in new[] { 1, -1 })
                        {
                            var arm = Spawn("Arm", body, green, t);
                            arm.transform.localScale = new Vector3(0.7f, 1.6f, 0.7f);
                            arm.transform.localPosition = new Vector3(side * 1.1f, h * 0.6f + side * 0.4f, 0);
                        }
                        return t;
                    });
                    break;
                }
                case "icebergs":
                {
                    Mesh m = MeshFactory.Sphere(3, 5);
                    ring(45, r =>
                    {
                        var go = Spawn("Iceberg", m, Toon.Make(Toon.Hex("#dff6ff"), 0.2f), null);
                        float s = 2 + r * 5;
                        go.transform.localScale = new Vector3(s, s * (0.6f + r), s);
                        go.transform.position = new Vector3(0, s * 0.3f, 0);
                        return go.transform;
                    });
                    break;
                }
                case "rocks":
                {
                    Mesh m = MeshFactory.Sphere(3, 5);
                    ring(50, r =>
                    {
                        var go = Spawn("Rock", m, Toon.Make(Toon.Hex("#2a1d1a"), 0, r < 0.3f ? Toon.Hex("#ff3b00") * 0.3f : Color.black), null);
                        float s = 2 + r * 5;
                        go.transform.localScale = new Vector3(s, s * 0.7f, s);
                        go.transform.position = new Vector3(0, s * 0.3f, 0);
                        return go.transform;
                    });
                    break;
                }
                case "pillars":
                {
                    Mesh box = MeshFactory.Box(3, 1, 3), gem = MeshFactory.Cone(1.2f, 2.4f, 4);
                    Material obsidian = Toon.Make(Toon.Hex("#1a1024")), glow = Toon.Make(Toon.Hex("#e57bff"), 1);
                    ring(30, r =>
                    {
                        var t = new GameObject("Pillar").transform;
                        float h = 8 + r * 18;
                        var p = Spawn("Obsidian", box, obsidian, t);
                        p.transform.localScale = new Vector3(1, h, 1);
                        p.transform.localPosition = new Vector3(0, h / 2, 0);
                        var g = Spawn("Crystal", gem, glow, t);
                        g.transform.localPosition = new Vector3(0, h + 1.5f, 0);
                        return t;
                    });
                    break;
                }
            }
        }

        // ------------------------------------------------------------------ build
        // Builds everything for a map under a new root object. Fills `pools` with lava circles.
        public static GameObject Build(MapDef map, List<LavaPool> pools, out Material lavaMat)
        {
            float R = map.radius;
            var root = new GameObject("Map: " + map.name);
            var floorMat = Toon.Make(Color.white, 1);
            floorMat.SetTexture("_MainTex", FloorTexture(map));
            Spawn("Floor", MeshFactory.Disc(R + 70, 128, 1 / 7f), floorMat, root.transform);
            Spawn("Outside", MeshFactory.Ring(R, R + 70, 128), Toon.Make(Toon.Hex(map.outside), 1), root.transform).transform.position = Vector3.up * 0.03f;
            var rim = Spawn("Rim", MeshFactory.Torus(R, 0.35f, 220, 8), Toon.Make(Toon.Hex(map.wall), 1), root.transform);
            rim.transform.rotation = Quaternion.Euler(90, 0, 0);
            rim.transform.position = Vector3.up * 0.4f;
            Decorate(root.transform, map);

            // Lava pools (Volcano): random each run, never in the middle where you spawn.
            pools.Clear();
            for (int i = 0; i < map.lava; i++)
            {
                for (int tries = 0; tries < 20; tries++)
                {
                    float a = Random.value * Mathf.PI * 2, d = R * (0.25f + Random.value * 0.6f);
                    var p = new LavaPool { x = Mathf.Cos(a) * d, z = Mathf.Sin(a) * d, r = 6 + Random.value * 6 };
                    bool ok = true;
                    foreach (var q in pools) if (Vector2.Distance(new Vector2(q.x, q.z), new Vector2(p.x, p.z)) < q.r + p.r + 8) ok = false;
                    if (ok) { pools.Add(p); break; }
                }
            }
            lavaMat = Toon.Make(Toon.Hex("#ff5a00"), 1);
            Material crust = Toon.Make(Toon.Hex("#1a0a06"), 1);
            foreach (var p in pools)
            {
                Spawn("LavaCrust", MeshFactory.Disc(p.r + 1.2f, 32, 0), crust, root.transform).transform.position = new Vector3(p.x, 0.04f, p.z);
                Spawn("Lava", MeshFactory.Disc(p.r, 32, 0), lavaMat, root.transform).transform.position = new Vector3(p.x, 0.06f, p.z);
            }
            return root;
        }
    }
}
