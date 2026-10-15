using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SerpentRogue
{
    // Builds simple 3D shapes from code (no model files needed).
    // Later you can replace these with real models made in Blender!
    public static class MeshFactory
    {
        // Makes every triangle face the same way as its vertex normals
        // (Unity draws triangles that are clockwise when seen from the front).
        static void Finish(Mesh mesh, List<Vector3> v, List<Vector3> n, List<int> t, List<Vector2> uv = null)
        {
            for (int i = 0; i < t.Count; i += 3)
            {
                Vector3 a = v[t[i]], b = v[t[i + 1]], c = v[t[i + 2]];
                Vector3 face = Vector3.Cross(b - a, c - a);
                Vector3 avg = n[t[i]] + n[t[i + 1]] + n[t[i + 2]];
                if (Vector3.Dot(face, avg) < 0)
                {
                    int tmp = t[i + 1];
                    t[i + 1] = t[i + 2];
                    t[i + 2] = tmp;
                }
            }
            if (v.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            if (uv != null) mesh.SetUVs(0, uv);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateBounds();
        }

        static void Quad(List<int> t, int a, int b, int c, int d)
        {
            t.Add(a); t.Add(b); t.Add(c);
            t.Add(a); t.Add(c); t.Add(d);
        }

        // A box with rounded edges and corners, centered at the origin.
        public static Mesh RoundedBox(float w, float h, float d, float r, int seg = 7)
        {
            Vector3 half = new Vector3(w / 2, h / 2, d / 2);
            r = Mathf.Min(r, Mathf.Min(half.x, Mathf.Min(half.y, half.z)) * 0.999f);
            Vector3 inner = half - Vector3.one * r;
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var t = new List<int>();
            Vector3[] normals = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (Vector3 fn in normals)
            {
                Vector3 u = Mathf.Abs(fn.y) > 0.5f ? Vector3.right : Vector3.up;
                Vector3 w2 = Vector3.Cross(fn, u);
                int start = v.Count;
                for (int i = 0; i <= seg; i++)
                    for (int j = 0; j <= seg; j++)
                    {
                        Vector3 p = fn + u * (2f * i / seg - 1) + w2 * (2f * j / seg - 1);
                        Vector3 q = Vector3.Scale(p, half);
                        Vector3 c = new Vector3(Mathf.Clamp(q.x, -inner.x, inner.x), Mathf.Clamp(q.y, -inner.y, inner.y), Mathf.Clamp(q.z, -inner.z, inner.z));
                        Vector3 dir = (q - c).normalized;
                        v.Add(c + dir * r);
                        n.Add(dir);
                    }
                for (int i = 0; i < seg; i++)
                    for (int j = 0; j < seg; j++)
                    {
                        int a = start + i * (seg + 1) + j;
                        Quad(t, a, a + seg + 1, a + seg + 2, a + 1);
                    }
            }
            var mesh = new Mesh { name = "RoundedBox" };
            Finish(mesh, v, n, t);
            return mesh;
        }

        // A plain box with flat faces.
        public static Mesh Box(float w, float h, float d)
        {
            Vector3 half = new Vector3(w / 2, h / 2, d / 2);
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var t = new List<int>();
            Vector3[] normals = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (Vector3 fn in normals)
            {
                Vector3 u = Mathf.Abs(fn.y) > 0.5f ? Vector3.right : Vector3.up;
                Vector3 w2 = Vector3.Cross(fn, u);
                int s = v.Count;
                v.Add(Vector3.Scale(fn - u - w2, half));
                v.Add(Vector3.Scale(fn + u - w2, half));
                v.Add(Vector3.Scale(fn + u + w2, half));
                v.Add(Vector3.Scale(fn - u + w2, half));
                for (int k = 0; k < 4; k++) n.Add(fn);
                Quad(t, s, s + 1, s + 2, s + 3);
            }
            var mesh = new Mesh { name = "Box" };
            Finish(mesh, v, n, t);
            return mesh;
        }

        public static Mesh Sphere(int lat = 12, int lon = 16)
        {
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var t = new List<int>();
            for (int i = 0; i <= lat; i++)
            {
                float th = Mathf.PI * i / lat;
                for (int j = 0; j <= lon; j++)
                {
                    float ph = 2 * Mathf.PI * j / lon;
                    Vector3 p = new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph));
                    v.Add(p);
                    n.Add(p);
                }
            }
            for (int i = 0; i < lat; i++)
                for (int j = 0; j < lon; j++)
                {
                    int a = i * (lon + 1) + j;
                    Quad(t, a, a + 1, a + lon + 2, a + lon + 1);
                }
            var mesh = new Mesh { name = "Sphere" };
            Finish(mesh, v, n, t);
            return mesh;
        }

        // Cone pointing up (+Y), centered on its height like three.js.
        public static Mesh Cone(float radius, float height, int seg = 8)
        {
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var t = new List<int>();
            float y0 = -height / 2, y1 = height / 2;
            float slope = radius / height;
            for (int j = 0; j < seg; j++)
            {
                float a0 = 2 * Mathf.PI * j / seg, a1 = 2 * Mathf.PI * (j + 1) / seg, am = (a0 + a1) / 2;
                int s = v.Count;
                v.Add(new Vector3(Mathf.Cos(a0) * radius, y0, Mathf.Sin(a0) * radius));
                n.Add(new Vector3(Mathf.Cos(a0), slope, Mathf.Sin(a0)).normalized);
                v.Add(new Vector3(Mathf.Cos(a1) * radius, y0, Mathf.Sin(a1) * radius));
                n.Add(new Vector3(Mathf.Cos(a1), slope, Mathf.Sin(a1)).normalized);
                v.Add(new Vector3(0, y1, 0));
                n.Add(new Vector3(Mathf.Cos(am), slope, Mathf.Sin(am)).normalized);
                t.Add(s); t.Add(s + 1); t.Add(s + 2);
                // base
                int b = v.Count;
                v.Add(new Vector3(0, y0, 0)); n.Add(Vector3.down);
                v.Add(new Vector3(Mathf.Cos(a0) * radius, y0, Mathf.Sin(a0) * radius)); n.Add(Vector3.down);
                v.Add(new Vector3(Mathf.Cos(a1) * radius, y0, Mathf.Sin(a1) * radius)); n.Add(Vector3.down);
                t.Add(b); t.Add(b + 1); t.Add(b + 2);
            }
            var mesh = new Mesh { name = "Cone" };
            Finish(mesh, v, n, t);
            return mesh;
        }

        public static Mesh Cylinder(float rTop, float rBottom, float height, int seg = 20, bool open = false)
        {
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var t = new List<int>();
            float y0 = -height / 2, y1 = height / 2;
            for (int j = 0; j <= seg; j++)
            {
                float a = 2 * Mathf.PI * j / seg;
                Vector3 dir = new Vector3(Mathf.Cos(a), (rBottom - rTop) / height, Mathf.Sin(a)).normalized;
                v.Add(new Vector3(Mathf.Cos(a) * rBottom, y0, Mathf.Sin(a) * rBottom)); n.Add(dir);
                v.Add(new Vector3(Mathf.Cos(a) * rTop, y1, Mathf.Sin(a) * rTop)); n.Add(dir);
            }
            for (int j = 0; j < seg; j++) Quad(t, j * 2, j * 2 + 1, j * 2 + 3, j * 2 + 2);
            if (!open)
            {
                foreach (int side in new[] { 1, -1 })
                {
                    float y = side > 0 ? y1 : y0;
                    float r = side > 0 ? rTop : rBottom;
                    int c = v.Count;
                    v.Add(new Vector3(0, y, 0)); n.Add(Vector3.up * side);
                    for (int j = 0; j <= seg; j++)
                    {
                        float a = 2 * Mathf.PI * j / seg;
                        v.Add(new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r)); n.Add(Vector3.up * side);
                    }
                    for (int j = 0; j < seg; j++) { t.Add(c); t.Add(c + 1 + j); t.Add(c + 2 + j); }
                }
            }
            var mesh = new Mesh { name = "Cylinder" };
            Finish(mesh, v, n, t);
            return mesh;
        }

        // Ring lying in the XY plane (rotate 90 degrees on X to lay it flat).
        public static Mesh Torus(float radius, float tube, int seg = 32, int tubeSeg = 8)
        {
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var t = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float a = 2 * Mathf.PI * i / seg;
                Vector3 center = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0);
                for (int j = 0; j <= tubeSeg; j++)
                {
                    float b = 2 * Mathf.PI * j / tubeSeg;
                    Vector3 dir = new Vector3(Mathf.Cos(a) * Mathf.Cos(b), Mathf.Sin(a) * Mathf.Cos(b), Mathf.Sin(b));
                    v.Add(center + dir * tube);
                    n.Add(dir);
                }
            }
            for (int i = 0; i < seg; i++)
                for (int j = 0; j < tubeSeg; j++)
                {
                    int a = i * (tubeSeg + 1) + j;
                    Quad(t, a, a + tubeSeg + 1, a + tubeSeg + 2, a + 1);
                }
            var mesh = new Mesh { name = "Torus" };
            Finish(mesh, v, n, t);
            return mesh;
        }

        // A swept-back flame/shark fin: a curved triangle extruded a little in Z.
        public static Mesh Fin(float w, float h, float depth = 0.1f)
        {
            var outline = new List<Vector2> { new Vector2(w * 0.45f, 0), new Vector2(-w * 0.55f, 0), new Vector2(-w * 0.9f, h) };
            Vector2 p0 = new Vector2(-w * 0.9f, h), ctrl = new Vector2(-w * 0.1f, h * 0.55f), p1 = new Vector2(w * 0.45f, 0);
            for (int i = 1; i < 6; i++)
            {
                float s = i / 6f;
                outline.Add((1 - s) * (1 - s) * p0 + 2 * (1 - s) * s * ctrl + s * s * p1);
            }
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var t = new List<int>();
            int count = outline.Count;
            foreach (float side in new[] { 1f, -1f })
            {
                int s0 = v.Count;
                foreach (Vector2 p in outline) { v.Add(new Vector3(p.x, p.y, side * depth / 2)); n.Add(Vector3.forward * side); }
                for (int i = 1; i < count - 1; i++) { t.Add(s0); t.Add(s0 + i); t.Add(s0 + i + 1); }
            }
            for (int i = 0; i < count; i++)
            {
                Vector2 a = outline[i], b = outline[(i + 1) % count];
                Vector2 e = (b - a).normalized;
                Vector3 nn = new Vector3(e.y, -e.x, 0);
                // pick the outward direction (away from the shape's middle)
                Vector2 mid = (a + b) / 2, centroid = new Vector2(-w * 0.3f, h * 0.3f);
                if (Vector2.Dot(new Vector2(nn.x, nn.y), mid - centroid) < 0) nn = -nn;
                int s = v.Count;
                v.Add(new Vector3(a.x, a.y, depth / 2)); v.Add(new Vector3(b.x, b.y, depth / 2));
                v.Add(new Vector3(b.x, b.y, -depth / 2)); v.Add(new Vector3(a.x, a.y, -depth / 2));
                for (int k = 0; k < 4; k++) n.Add(nn);
                Quad(t, s, s + 1, s + 2, s + 3);
            }
            var mesh = new Mesh { name = "Fin" };
            Finish(mesh, v, n, t);
            return mesh;
        }

        // Flat disc on the ground (XZ plane) with UVs for a tiled texture.
        public static Mesh Disc(float radius, int seg, float uvScale)
        {
            var v = new List<Vector3> { Vector3.zero };
            var n = new List<Vector3> { Vector3.up };
            var uv = new List<Vector2> { Vector2.zero };
            var t = new List<int>();
            for (int j = 0; j <= seg; j++)
            {
                float a = 2 * Mathf.PI * j / seg;
                Vector3 p = new Vector3(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius);
                v.Add(p); n.Add(Vector3.up); uv.Add(new Vector2(p.x, p.z) * uvScale);
            }
            for (int j = 0; j < seg; j++) { t.Add(0); t.Add(j + 1); t.Add(j + 2); }
            var mesh = new Mesh { name = "Disc" };
            Finish(mesh, v, n, t, uv);
            return mesh;
        }

        // Flat ring on the ground between two radii.
        public static Mesh Ring(float inner, float outer, int seg)
        {
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var t = new List<int>();
            for (int j = 0; j <= seg; j++)
            {
                float a = 2 * Mathf.PI * j / seg;
                Vector3 d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                v.Add(d * inner); v.Add(d * outer); n.Add(Vector3.up); n.Add(Vector3.up);
            }
            for (int j = 0; j < seg; j++) Quad(t, j * 2, j * 2 + 1, j * 2 + 3, j * 2 + 2);
            var mesh = new Mesh { name = "Ring" };
            Finish(mesh, v, n, t);
            return mesh;
        }
    }

    // Collects many small shapes into a few merged meshes (one per material
    // "slot") so a whole head is only a handful of draw calls.
    public class PartBuilder
    {
        readonly Dictionary<string, List<CombineInstance>> slots = new Dictionary<string, List<CombineInstance>>();

        // rotation is in RADIANS, applied X then Y then Z (same as the web version).
        public PartBuilder Add(string slot, Mesh mesh, Vector3 pos, Vector3 rot, Vector3 scale, bool outline = true)
        {
            Quaternion q = Quaternion.Euler(rot.x * Mathf.Rad2Deg, 0, 0) * Quaternion.Euler(0, rot.y * Mathf.Rad2Deg, 0) * Quaternion.Euler(0, 0, rot.z * Mathf.Rad2Deg);
            var ci = new CombineInstance { mesh = mesh, transform = Matrix4x4.TRS(pos, q, scale) };
            Get(slot).Add(ci);
            if (outline) Get("outline").Add(ci);
            return this;
        }

        public PartBuilder Add(string slot, Mesh mesh, Vector3 pos, bool outline = true)
        {
            return Add(slot, mesh, pos, Vector3.zero, Vector3.one, outline);
        }

        List<CombineInstance> Get(string slot)
        {
            List<CombineInstance> list;
            if (!slots.TryGetValue(slot, out list))
            {
                list = new List<CombineInstance>();
                slots[slot] = list;
            }
            return list;
        }

        public Dictionary<string, Mesh> Build()
        {
            var result = new Dictionary<string, Mesh>();
            foreach (var kv in slots)
            {
                var mesh = new Mesh { name = kv.Key, indexFormat = IndexFormat.UInt32 };
                mesh.CombineMeshes(kv.Value.ToArray(), true, true);
                mesh.RecalculateBounds();
                result[kv.Key] = mesh;
            }
            return result;
        }
    }
}
