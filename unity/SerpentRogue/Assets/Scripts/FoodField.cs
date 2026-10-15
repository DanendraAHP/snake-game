using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SerpentRogue
{
    public class Food
    {
        public float x, z, value, phase;
        public bool alive = true, natural, golden;
        public Color color;
    }

    // All food is drawn with GPU instancing (one mesh, many copies).
    public class FoodField
    {
        static readonly Color[] Palette =
        {
            Toon.Hex("#ff5d73"), Toon.Hex("#ffd25d"), Toon.Hex("#5dff9b"), Toon.Hex("#5dd6ff"),
            Toon.Hex("#b45dff"), Toon.Hex("#ff8f3d"), Toon.Hex("#ffffff"),
        };
        static readonly Color Gold = Toon.Hex("#ffcc33");

        public readonly List<Food> items = new List<Food>();
        public readonly SpatialHash<Food> grid = new SpatialHash<Food>(4);
        int natural;
        public System.Func<float, float, bool> blocked; // e.g. "is this inside lava?" 

        readonly Mesh mesh;
        readonly Material material;
        readonly Matrix4x4[] matrices = new Matrix4x4[1023];
        readonly Vector4[] colors = new Vector4[1023];
        readonly MaterialPropertyBlock block = new MaterialPropertyBlock();

        public FoodField()
        {
            mesh = MeshFactory.Sphere(5, 7);
            material = Toon.Make(Color.white, 0.75f, null, true); // mostly unlit = glowy candy
        }

        public void Clear()
        {
            items.Clear();
            natural = 0;
        }

        public Food Spawn(float x, float z, float value, Color? color, bool isNatural, bool golden)
        {
            if (items.Count >= Config.FoodCapacity) return null;
            var f = new Food
            {
                x = x, z = z, value = value, natural = isNatural, golden = golden,
                color = golden ? Gold : (color ?? Palette[Random.Range(0, Palette.Length)]),
                phase = Random.value * Mathf.PI * 2,
            };
            items.Add(f);
            if (isNatural) natural++;
            return f;
        }

        public void SpawnRandom(float goldenChance)
        {
            float r = 0, a = 0;
            for (int tries = 0; tries < 10; tries++)
            {
                r = Config.ArenaRadius * 0.97f * Mathf.Sqrt(Random.value);
                a = Random.value * Mathf.PI * 2;
                if (blocked == null || !blocked(Mathf.Cos(a) * r, Mathf.Sin(a) * r)) break;
            }
            bool golden = Random.value < goldenChance;
            float value = golden ? 5 : (Random.value < 0.15f ? 2 : 1);
            Spawn(Mathf.Cos(a) * r, Mathf.Sin(a) * r, value, null, true, golden);
        }

        // Keep the map stocked with food.
        public void Maintain(float goldenChance, float target)
        {
            for (int i = 0; i < 25 && natural < target; i++) SpawnRandom(goldenChance);
        }

        public void RebuildGrid()
        {
            grid.Clear();
            foreach (var f in items) if (f.alive) grid.Insert(f.x, f.z, f);
        }

        // Remove eaten food (once per frame so the list doesn't change mid-frame).
        public void Compact()
        {
            int w = 0;
            for (int i = 0; i < items.Count; i++)
            {
                Food f = items[i];
                if (f.alive) items[w++] = f;
                else if (f.natural) natural--;
            }
            items.RemoveRange(w, items.Count - w);
        }

        public void Draw(float time)
        {
            int batch = 0;
            for (int i = 0; i < items.Count; i++)
            {
                Food f = items[i];
                float s = (0.7f + Mathf.Sqrt(f.value) * 0.45f) * (f.golden ? 1.4f : 1) * 0.32f;
                Vector3 pos = new Vector3(f.x, 0.55f + Mathf.Sin(time * 2.5f + f.phase) * 0.15f, f.z);
                matrices[batch] = Matrix4x4.TRS(pos, Quaternion.Euler(0, (time * 1.5f + f.phase) * Mathf.Rad2Deg, 0), new Vector3(s, s, s));
                colors[batch] = Toon.Lin(f.color);
                batch++;
                if (batch == 1023 || i == items.Count - 1)
                {
                    block.SetVectorArray("_Color", colors);
                    Graphics.DrawMeshInstanced(mesh, 0, material, matrices, batch, block, ShadowCastingMode.Off, false);
                    batch = 0;
                }
            }
        }
    }
}
