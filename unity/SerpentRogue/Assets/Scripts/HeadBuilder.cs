using System.Collections.Generic;
using UnityEngine;

namespace SerpentRogue
{
    // A built head: root object + the jaw (rotate jaw on Z, negative = open).
    public class Head
    {
        public GameObject root;
        public Transform jaw;
        public readonly List<Material> materials = new List<Material>();

        public void Destroy()
        {
            // Destroy() only happens at the end of the frame, so hide it right away.
            root.SetActive(false);
            foreach (var m in materials) Object.Destroy(m);
            Object.Destroy(root);
        }
    }

    // Builds the 3D heads (dragon, viper, cute) and hats from simple shapes.
    // A head is ~1 unit "radius", faces +X, with +Y up.
    public static class HeadBuilder
    {
        static Mesh sphere, tooth, fang, horn, smallSpike;
        static readonly Dictionary<string, Mesh> rbCache = new Dictionary<string, Mesh>();
        static readonly Vector3 Down = new Vector3(Mathf.PI, 0, 0);

        static void InitShapes()
        {
            if (sphere != null) return;
            sphere = MeshFactory.Sphere(12, 16);
            tooth = MeshFactory.Cone(0.085f, 0.3f, 5);
            fang = MeshFactory.Cone(0.11f, 0.45f, 6);
            horn = MeshFactory.Cone(0.17f, 1.0f, 7);
            smallSpike = MeshFactory.Cone(0.1f, 0.35f, 5);
        }

        static Mesh RB(float w, float h, float d, float r)
        {
            string key = w + "," + h + "," + d + "," + r;
            Mesh m;
            if (!rbCache.TryGetValue(key, out m))
            {
                m = MeshFactory.RoundedBox(w, h, d, r, 5);
                rbCache[key] = m;
            }
            return m;
        }

        static Vector3 V(float x, float y, float z) { return new Vector3(x, y, z); }
        static Vector3 One(float s) { return new Vector3(s, s, s); }

        static void Eyes(PartBuilder b, float x, float y, float z, float size)
        {
            foreach (int side in new[] { 1, -1 })
            {
                b.Add("eye", sphere, V(x, y, side * z), Vector3.zero, V(size, size, size * 0.7f));
                // Slit pupil, like a reptile.
                b.Add("dark", sphere, V(x + size * 0.15f, y, side * (z + size * 0.55f)), Vector3.zero, V(size * 0.28f, size * 0.85f, size * 0.25f), false);
                b.Add("white", sphere, V(x + size * 0.35f, y + size * 0.35f, side * (z + size * 0.5f)), Vector3.zero, V(size * 0.15f, size * 0.15f, size * 0.12f), false);
            }
        }

        static float Dragon(PartBuilder up, PartBuilder jaw, bool viper)
        {
            if (viper)
            {
                up.Add("skin", RB(1.45f, 0.78f, 1.7f, 0.3f), V(0.05f, 0.08f, 0));
                up.Add("skin", RB(1.05f, 0.5f, 1.1f, 0.22f), V(0.95f, 0.02f, 0));
                Eyes(up, 0.4f, 0.4f, 0.66f, 0.28f);
                foreach (int side in new[] { 1, -1 })
                {
                    up.Add("skin", RB(0.45f, 0.12f, 0.26f, 0.05f), V(0.3f, 0.68f, side * 0.55f), V(side * 0.35f, 0, -0.15f), Vector3.one);
                    up.Add("white", fang, V(1.15f, -0.32f, side * 0.3f), Down, Vector3.one, false);
                    up.Add("dark", sphere, V(1.45f, 0.18f, side * 0.22f), Vector3.zero, One(0.06f), false);
                }
                jaw.Add("belly", RB(1.45f, 0.26f, 1.2f, 0.1f), V(0.85f, -0.13f, 0));
                jaw.Add("mouth", MeshFactory.Box(1.3f, 0.05f, 0.95f), V(0.85f, 0.02f, 0), false);
                return 0.5f; // height of the top of the head (for hats)
            }

            // Dragon: big skull, long snout, teeth, horns.
            up.Add("skin", RB(1.5f, 1.05f, 1.45f, 0.35f), V(0, 0.15f, 0));
            up.Add("skin", RB(1.4f, 0.6f, 1.15f, 0.25f), V(1.05f, 0.05f, 0));
            up.Add("skin", RB(0.9f, 0.22f, 0.55f, 0.1f), V(1.0f, 0.38f, 0));
            Eyes(up, 0.5f, 0.5f, 0.6f, 0.27f);
            foreach (int side in new[] { 1, -1 })
            {
                up.Add("skin", RB(0.45f, 0.14f, 0.28f, 0.06f), V(0.4f, 0.84f, side * 0.5f), V(side * 0.35f, 0, -0.2f), Vector3.one);
                up.Add("accent", horn, V(-0.5f, 0.85f, side * 0.42f), V(side * 0.35f, 0, 1.15f), Vector3.one);
                up.Add("dark", sphere, V(1.7f, 0.28f, side * 0.24f), Vector3.zero, One(0.07f), false);
                foreach (float x in new[] { 0.55f, 0.9f, 1.25f }) up.Add("white", tooth, V(x, -0.3f, side * 0.48f), Down, Vector3.one, false);
                up.Add("white", fang, V(1.6f, -0.33f, side * 0.38f), Down, Vector3.one, false);
            }
            foreach (float x in new[] { -0.6f, -0.2f, 0.2f }) up.Add("accent", smallSpike, V(x, 0.78f, 0), V(0, 0, 0.6f), Vector3.one);

            jaw.Add("belly", RB(1.8f, 0.32f, 1.05f, 0.12f), V(0.95f, -0.15f, 0));
            jaw.Add("mouth", MeshFactory.Box(1.55f, 0.05f, 0.85f), V(0.95f, 0.02f, 0), false);
            foreach (int side in new[] { 1, -1 })
                foreach (float x in new[] { 0.7f, 1.05f, 1.4f }) jaw.Add("white", tooth, V(x, 0.12f, side * 0.4f), false);
            return 0.75f;
        }

        // Bone skull with stripes and huge teeth (inspired by Zabimaru).
        static float Bone(PartBuilder up, PartBuilder jaw)
        {
            up.Add("skin", RB(1.5f, 1.0f, 1.4f, 0.3f), V(0, 0.15f, 0));
            up.Add("skin", RB(1.7f, 0.55f, 1.0f, 0.2f), V(1.2f, 0.02f, 0));
            foreach (float x in new[] { -0.35f, 0.05f, 0.45f, 1.0f, 1.4f })
                up.Add("dark", MeshFactory.Box(0.13f, 0.05f, x > 0.8f ? 0.7f : 1.0f), V(x, x > 0.8f ? 0.31f : 0.67f, 0), V(0, 0.35f, 0), Vector3.one, false);
            foreach (int side in new[] { 1, -1 })
            {
                up.Add("dark", sphere, V(0.42f, 0.45f, side * 0.52f), Vector3.zero, V(0.32f, 0.3f, 0.2f), false); // eye socket
                for (int k = 0; k < 5; k++) up.Add("white", tooth, V(0.55f + k * 0.32f, -0.32f, side * 0.4f), Down, V(1.4f, 1.5f, 1.4f), false);
            }
            Eyes(up, 0.45f, 0.45f, 0.56f, 0.24f);
            jaw.Add("belly", RB(2.0f, 0.3f, 0.95f, 0.12f), V(1.0f, -0.15f, 0));
            jaw.Add("mouth", MeshFactory.Box(1.75f, 0.05f, 0.8f), V(1.0f, 0.02f, 0), false);
            foreach (int side in new[] { 1, -1 })
                for (int k = 0; k < 5; k++) jaw.Add("white", tooth, V(0.6f + k * 0.32f, 0.14f, side * 0.36f), Vector3.zero, V(1.3f, 1.4f, 1.3f), false);
            return 0.7f;
        }

        // Big cube head with glowing eyes (inspired by the Ender Dragon).
        static float Block(PartBuilder up, PartBuilder jaw)
        {
            up.Add("skin", MeshFactory.Box(1.4f, 1.1f, 1.45f), V(0, 0.2f, 0));
            up.Add("skin", MeshFactory.Box(1.3f, 0.5f, 1.0f), V(1.25f, 0.02f, 0));
            foreach (int side in new[] { 1, -1 })
            {
                up.Add("skin", MeshFactory.Box(0.22f, 0.16f, 0.22f), V(1.6f, 0.33f, side * 0.28f));       // nostrils
                up.Add("glow", MeshFactory.Box(0.42f, 0.06f, 0.34f), V(0.5f, 0.76f, side * 0.4f), false);  // eyes (top)
                up.Add("glow", MeshFactory.Box(0.06f, 0.2f, 0.42f), V(0.71f, 0.42f, side * 0.4f), false);  // eyes (front)
                up.Add("accent", MeshFactory.Box(0.24f, 0.42f, 0.24f), V(-0.35f, 0.95f, side * 0.32f));   // head spines
                up.Add("accent", MeshFactory.Box(0.2f, 0.32f, 0.2f), V(0.15f, 0.9f, side * 0.32f));
            }
            jaw.Add("skin", MeshFactory.Box(1.55f, 0.3f, 0.95f), V(0.95f, -0.15f, 0));
            jaw.Add("glow", MeshFactory.Box(1.35f, 0.05f, 0.8f), V(0.95f, 0.02f, 0), false); // glowing mouth
            return 0.75f;
        }

        // Long eastern dragon head with antlers and whiskers (inspired by Shenron).
        static float Eastern(PartBuilder up, PartBuilder jaw)
        {
            up.Add("skin", RB(1.4f, 0.9f, 1.3f, 0.3f), V(0, 0.15f, 0));
            up.Add("skin", RB(1.75f, 0.5f, 0.95f, 0.2f), V(1.2f, 0.05f, 0));
            up.Add("skin", RB(0.45f, 0.32f, 0.75f, 0.12f), V(2.0f, 0.15f, 0));
            Eyes(up, 0.45f, 0.48f, 0.55f, 0.24f);
            foreach (int side in new[] { 1, -1 })
            {
                up.Add("skin", RB(0.6f, 0.16f, 0.34f, 0.06f), V(0.45f, 0.72f, side * 0.45f), V(side * 0.35f, 0, -0.2f), Vector3.one);
                // antlers: a main branch with two small branches
                up.Add("accent", MeshFactory.Cone(0.1f, 1.6f, 6), V(-0.75f, 1.15f, side * 0.55f), V(side * 0.35f, 0, 0.85f), Vector3.one);
                up.Add("accent", MeshFactory.Cone(0.06f, 0.7f, 5), V(-0.55f, 1.3f, side * 0.5f), V(side * 0.2f, 0, -0.1f), Vector3.one);
                up.Add("accent", MeshFactory.Cone(0.06f, 0.6f, 5), V(-1.05f, 1.55f, side * 0.65f), V(side * 0.3f, 0, 0.1f), Vector3.one);
                // long whiskers: start at the sides of the nose and flow backward and outward
                up.Add("accent", MeshFactory.Cone(0.06f, 1.8f, 5), V(0.88f, 0, side * 0.99f), V(side * 1.654f, 0, 0.926f), Vector3.one, false);
                up.Add("white", fang, V(1.75f, -0.3f, side * 0.3f), Down, Vector3.one, false);
                up.Add("dark", sphere, V(2.15f, 0.25f, side * 0.2f), Vector3.zero, One(0.06f), false);
                foreach (float x in new[] { 0.6f, 0.95f, 1.3f }) up.Add("white", tooth, V(x, -0.28f, side * 0.42f), Down, Vector3.one, false);
            }
            jaw.Add("belly", RB(1.9f, 0.28f, 0.9f, 0.12f), V(1.0f, -0.14f, 0));
            jaw.Add("mouth", MeshFactory.Box(1.65f, 0.05f, 0.75f), V(1.0f, 0.02f, 0), false);
            return 0.6f;
        }

        static float Round(PartBuilder up, string eyeStyle)
        {
            up.Add("skin", sphere, V(0, 0.05f, 0), Vector3.zero, V(1.25f, 0.95f, 1.1f));
            System.Action<float, float, float, float, bool> ball = (x, y, z, size, shine) =>
            {
                up.Add("white", sphere, V(x, y, z), Vector3.zero, One(size));
                up.Add("dark", sphere, V(x + size * 0.6f, y, z), Vector3.zero, One(size * 0.55f), false);
                if (shine) up.Add("white", sphere, V(x + size * 0.85f, y + size * 0.3f, z + size * 0.15f), Vector3.zero, One(size * 0.2f), false);
            };
            if (eyeStyle == "cyclops") ball(0.65f, 0.5f, 0, 0.5f, true);
            else
            {
                bool big = eyeStyle == "cute";
                foreach (int side in new[] { 1, -1 }) ball(0.55f, 0.45f, side * (big ? 0.5f : 0.48f), big ? 0.42f : 0.32f, big);
                if (eyeStyle == "angry")
                    foreach (int side in new[] { 1, -1 })
                        up.Add("dark", MeshFactory.Box(0.12f, 0.09f, 0.42f), V(0.72f, 0.8f, side * 0.45f), V(side * 0.45f, 0, 0), Vector3.one, false);
            }
            return 0.95f;
        }

        static void Hat(PartBuilder b, string kind, float top)
        {
            switch (kind)
            {
                case "crown":
                    b.Add("gold", MeshFactory.Cylinder(0.5f, 0.56f, 0.34f, 18, true), V(-0.05f, top + 0.15f, 0), false);
                    for (int k = 0; k < 6; k++)
                    {
                        float a = k / 6f * Mathf.PI * 2;
                        b.Add("gold", MeshFactory.Cone(0.12f, 0.34f, 6), V(-0.05f + Mathf.Cos(a) * 0.48f, top + 0.47f, Mathf.Sin(a) * 0.48f));
                    }
                    break;
                case "horns":
                    foreach (int side in new[] { 1, -1 }) b.Add("ivory", horn, V(-0.1f, top + 0.35f, side * 0.5f), V(side * 0.55f, 0, 0.35f), One(1.1f));
                    break;
                case "ears": // Anubis-style tall ears
                    foreach (int side in new[] { 1, -1 })
                    {
                        b.Add("skin", MeshFactory.Cone(0.3f, 1.3f, 4), V(-0.35f, top + 0.55f, side * 0.42f), V(side * 0.2f, 0, 0.25f), V(1, 1, 0.45f));
                        b.Add("gold", MeshFactory.Cone(0.16f, 0.85f, 4), V(-0.3f, top + 0.5f, side * 0.48f), V(side * 0.2f, 0, 0.25f), V(1, 1, 0.3f), false);
                    }
                    break;
                case "mane":
                {
                    // A wild mane of fur spikes sweeping back from the head.
                    var rnd = new System.Random(42);
                    System.Func<float> r = () => (float)rnd.NextDouble();
                    for (int k = 0; k < 16; k++)
                    {
                        int side = k % 2 == 1 ? 1 : -1;
                        b.Add("mane", MeshFactory.Cone(0.2f, 1.1f + r() * 0.6f, 5),
                            V(-0.4f - r() * 0.8f, top - 0.1f + r() * 0.4f, side * r() * 0.75f),
                            V(side * (0.3f + r() * 0.5f), 0, 1.25f + r() * 0.4f), Vector3.one);
                    }
                    break;
                }
                case "halo":
                    b.Add("halo", MeshFactory.Torus(0.55f, 0.07f), V(-0.1f, top + 0.65f, 0), V(Mathf.PI / 2, 0, 0), Vector3.one, false);
                    break;
                case "tophat":
                    b.Add("black", MeshFactory.Cylinder(0.75f, 0.75f, 0.06f, 24), V(-0.05f, top + 0.05f, 0));
                    b.Add("black", MeshFactory.Cylinder(0.42f, 0.45f, 0.75f, 20), V(-0.05f, top + 0.45f, 0));
                    b.Add("red", MeshFactory.Cylinder(0.46f, 0.46f, 0.14f, 20), V(-0.05f, top + 0.17f, 0), false);
                    break;
            }
        }

        class Shapes
        {
            public Dictionary<string, Mesh> upper, jaw;
        }

        // Geometry doesn't depend on colors, so it's cached by shape options.
        static readonly Dictionary<string, Shapes> cache = new Dictionary<string, Shapes>();

        static Shapes GetShapes(SkinData skin)
        {
            Shapes s;
            if (!cache.TryGetValue(skin.ShapeKey, out s))
            {
                InitShapes();
                var up = new PartBuilder();
                var jaw = new PartBuilder();
                float top;
                switch (skin.head)
                {
                    case "round": top = Round(up, skin.eyes); break;
                    case "viper": top = Dragon(up, jaw, true); break;
                    case "bone": top = Bone(up, jaw); break;
                    case "block": top = Block(up, jaw); break;
                    case "eastern": top = Eastern(up, jaw); break;
                    default: top = Dragon(up, jaw, false); break;
                }
                Hat(up, skin.hat, top);
                s = new Shapes { upper = up.Build(), jaw = jaw.Build() };
                cache[skin.ShapeKey] = s;
            }
            return s;
        }

        static Material SlotMaterial(string slot, SkinData skin)
        {
            switch (slot)
            {
                case "skin": return Toon.Make(skin.A, 0, skin.glow ? skin.A * 0.3f : Color.black);
                case "belly": return Toon.Make(Color.Lerp(skin.B, Color.white, 0.25f));
                case "accent": return Toon.Make(skin.C);
                case "eye": return Toon.Make(skin.Eye);
                case "white": return Toon.Make(Color.white);
                case "dark": return Toon.Make(new Color(0.07f, 0.07f, 0.07f), 1);
                case "mouth": return Toon.Make(Toon.Hex("#c8102e"));
                case "gold": return Toon.Make(Toon.Hex("#ffc83d"));
                case "ivory": return Toon.Make(Toon.Hex("#f2e6c9"));
                case "halo": return Toon.Make(Toon.Hex("#fff27a"), 1);
                case "mane": return Toon.Make(skin.Mane);
                case "glow": return Toon.Make(skin.Eye, 1);
                case "black": return Toon.Make(Toon.Hex("#1b1b22"));
                case "red": return Toon.Make(Toon.Hex("#c8283c"));
                default: return Toon.Make(Color.magenta);
            }
        }

        static void AddMeshes(Transform parent, Dictionary<string, Mesh> meshes, SkinData skin, Head head, float outline)
        {
            foreach (var kv in meshes)
            {
                var go = new GameObject(kv.Key);
                go.transform.SetParent(parent, false);
                go.AddComponent<MeshFilter>().sharedMesh = kv.Value;
                var mr = go.AddComponent<MeshRenderer>();
                if (kv.Key == "outline") mr.sharedMaterial = Toon.Outline(outline);
                else
                {
                    var mat = SlotMaterial(kv.Key, skin);
                    head.materials.Add(mat);
                    mr.sharedMaterial = mat;
                }
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }
        }

        public static Head Build(SkinData skin)
        {
            Shapes shapes = GetShapes(skin);
            var head = new Head { root = new GameObject("Head") };
            AddMeshes(head.root.transform, shapes.upper, skin, head, 0.05f);
            var jaw = new GameObject("Jaw").transform;
            jaw.SetParent(head.root.transform, false);
            jaw.localPosition = new Vector3(-0.2f, -0.15f, 0);
            AddMeshes(jaw, shapes.jaw, skin, head, 0.04f);
            head.jaw = jaw;
            return head;
        }
    }
}
