using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SerpentRogue
{
    // Tiny "juice" system: little cubes that burst out and fall.
    public class Particles
    {
        class P
        {
            public Vector3 pos, vel;
            public float life;
            public Color color;
        }

        const int Max = 800;
        readonly List<P> list = new List<P>();
        readonly Mesh mesh = MeshFactory.Box(0.25f, 0.25f, 0.25f);
        readonly Material material = Toon.Make(Color.white, 1, null, true);
        readonly Matrix4x4[] matrices = new Matrix4x4[Max];
        readonly Vector4[] colors = new Vector4[Max];
        readonly MaterialPropertyBlock block = new MaterialPropertyBlock();

        public void Burst(float x, float z, Color color, int count = 20, float speed = 8)
        {
            for (int i = 0; i < count && list.Count < Max; i++)
            {
                float a = Random.value * Mathf.PI * 2;
                float v = speed * (0.4f + Random.value * 0.6f);
                list.Add(new P
                {
                    pos = new Vector3(x, 1, z),
                    vel = new Vector3(Mathf.Cos(a) * v, 4 + Random.value * 6, Mathf.Sin(a) * v),
                    life = 0.6f + Random.value * 0.6f,
                    color = color,
                });
            }
        }

        public void Clear() { list.Clear(); }

        public void Update(float dt)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                P p = list[i];
                p.life -= dt;
                if (p.life <= 0) { list.RemoveAt(i); continue; }
                p.vel.y -= 18 * dt;
                p.pos += p.vel * dt;
                if (p.pos.y < 0.1f) p.pos.y = 0.1f;
            }
        }

        public void Draw()
        {
            int n = list.Count;
            if (n == 0) return;
            for (int i = 0; i < n; i++)
            {
                P p = list[i];
                float s = Mathf.Min(1, p.life / 0.4f) * 1.3f;
                matrices[i] = Matrix4x4.TRS(p.pos, Quaternion.identity, new Vector3(s, s, s));
                colors[i] = Toon.Lin(p.color);
            }
            block.SetVectorArray("_Color", colors);
            Graphics.DrawMeshInstanced(mesh, 0, material, matrices, n, block, ShadowCastingMode.Off, false);
        }
    }
}
