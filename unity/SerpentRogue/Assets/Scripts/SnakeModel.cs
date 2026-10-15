using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SerpentRogue
{
    // One body piece. `owner`/`idx` let collision code tell snakes apart.
    public class Seg
    {
        public float x, z;
        public Snake owner;
        public int idx;
    }

    public struct SnakeFx
    {
        public float jaw;       // 0 = closed, 1 = wide open
        public bool boosting, phase, invuln;
    }

    // Only knows how to DRAW a snake (no game rules).
    // The body is drawn with GPU instancing: hundreds of copies of one mesh
    // in a single draw call (Graphics.DrawMeshInstanced).
    // Each segment uses local space: +X = forward, +Y = up.
    public class SnakeModel
    {
        const float BodyOutline = 0.07f, SpikeOutline = 0.04f;
        const int Max = 1023; // DrawMeshInstanced limit per call

        static readonly Dictionary<string, Mesh> bodyMeshes = new Dictionary<string, Mesh>();
        static readonly Dictionary<string, Mesh> spikeMeshes = new Dictionary<string, Mesh>();

        static Mesh bellyMesh;

        public SkinData skin;
        Material bellyMat;
        int spikeEvery = 1;
        readonly Vector4[] bellyColors = new Vector4[Max];
        readonly MaterialPropertyBlock bellyBlock = new MaterialPropertyBlock();
        int spikeCount;
        readonly Camera onlyCamera; // null = draw for every camera
        Mesh bodyMesh, spikeMesh;
        Material bodyMat, spikeMat;
        Head head;
        float yOffset, tilt, glowBase;
        Color colA, colB;

        readonly Matrix4x4[] matrices = new Matrix4x4[Max];
        readonly Matrix4x4[] spikeMatrices = new Matrix4x4[Max];
        readonly Vector4[] colors = new Vector4[Max];
        readonly Vector4[] spikeColors = new Vector4[Max];
        readonly MaterialPropertyBlock bodyBlock = new MaterialPropertyBlock();
        readonly MaterialPropertyBlock spikeBlock = new MaterialPropertyBlock();
        int coloredCount = -1;

        public Vector3 HeadPosition { get; private set; }

        public SnakeModel(SkinData skin, Camera onlyCamera = null)
        {
            this.onlyCamera = onlyCamera;
            SetSkin(skin);
        }

        static Mesh BodyMesh(string style)
        {
            Mesh m;
            if (!bodyMeshes.TryGetValue(style, out m))
            {
                if (style == "smooth") m = MeshFactory.Sphere(10, 14);
                else if (style == "blocky") m = MeshFactory.RoundedBox(1.35f, 1.45f, 1.75f, 0.14f, 4);
                else if (style == "cube") m = MeshFactory.Box(1.4f, 1.4f, 1.7f);
                else if (style == "bone") m = MeshFactory.RoundedBox(1.15f, 1.35f, 1.6f, 0.3f, 5);
                else m = MeshFactory.RoundedBox(1.45f, 1.55f, 1.85f, 0.42f, 5);
                bodyMeshes[style] = m;
            }
            return m;
        }

        // What grows on the back of each segment (null = nothing).
        static Mesh SpikeMesh(string style)
        {
            Mesh m;
            if (spikeMeshes.TryGetValue(style, out m)) return m;
            var b = new PartBuilder();
            if (style == "spikes")
                b.Add("main", MeshFactory.Cone(0.26f, 0.9f, 6), new Vector3(-0.15f, 1.05f, 0), new Vector3(0, 0, 0.6f), Vector3.one, false);
            else if (style == "fins")
            {
                b.Add("main", MeshFactory.Fin(1, 1.25f), new Vector3(0, 0.55f, 0), false);
                foreach (int side in new[] { 1, -1 })
                    b.Add("main", MeshFactory.Fin(0.8f, 0.85f), new Vector3(0, 0.2f, side * 0.75f), new Vector3(side * 1.1f, 0, 0), Vector3.one, false);
            }
            else if (style == "plates")
            {
                foreach (int side in new[] { 1, -1 })
                    b.Add("main", MeshFactory.Box(0.42f, 0.6f, 0.14f), new Vector3(0, 0.8f, side * 0.5f), new Vector3(side * 0.55f, 0, 0), Vector3.one, false);
                b.Add("main", MeshFactory.Box(0.5f, 0.18f, 1.2f), new Vector3(0, 0.62f, 0), false);
            }
            else if (style == "ribs")
            {
                // Long bone spikes sticking out sideways (Zabimaru).
                foreach (int side in new[] { 1, -1 })
                    b.Add("main", MeshFactory.Cone(0.11f, 2.0f, 6), new Vector3(-0.35f, 1.0f, side * 1.05f), new Vector3(side * 0.95f, 0, 0.55f), Vector3.one, false);
            }
            else if (style == "cubes")
                b.Add("main", MeshFactory.Box(0.38f, 0.5f, 0.34f), new Vector3(0, 0.9f, 0), false);
            else
            {
                spikeMeshes[style] = null;
                return null;
            }
            m = b.Build()["main"];
            spikeMeshes[style] = m;
            return m;
        }

        public void SetSkin(SkinData s)
        {
            DestroyParts();
            skin = s;
            colA = s.A;
            colB = s.B;
            bodyMesh = BodyMesh(s.body);
            spikeMesh = SpikeMesh(s.spikes);
            glowBase = s.glow ? 0.3f : 0f;
            bodyMat = Toon.Make(Color.white, 0, colA * glowBase, true);
            spikeMat = spikeMesh != null ? Toon.Make(Color.white, 0, s.glow ? s.Spike * 0.4f : Color.black, true) : null;
            Vector4 c = Toon.Lin(s.Spike);
            // Long bone spikes would be too crowded on every segment.
            spikeEvery = s.spikes == "ribs" ? 3 : s.spikes == "cubes" ? 2 : 1;
            // Optional belly: a wider, flatter plate under each segment in the second color.
            if (s.belly)
            {
                if (bellyMesh == null)
                {
                    bellyMesh = MeshFactory.RoundedBox(1.3f, 0.6f, 2.15f, 0.25f, 4);
                    var v = bellyMesh.vertices;
                    for (int i = 0; i < v.Length; i++) v[i].y -= 0.42f;
                    bellyMesh.vertices = v;
                    bellyMesh.RecalculateBounds();
                }
                bellyMat = Toon.Make(Color.white, 0, null, true);
                Vector4 bc = Toon.Lin(s.B);
                for (int i = 0; i < Max; i++) bellyColors[i] = bc;
                bellyBlock.SetVectorArray("_Color", bellyColors);
            }
            for (int i = 0; i < Max; i++) spikeColors[i] = c;
            spikeBlock.SetVectorArray("_Color", spikeColors);
            head = HeadBuilder.Build(s);
            yOffset = s.body == "smooth" ? 1f : 0.8f;
            tilt = s.body == "smooth" ? 0f : 0.14f * Mathf.Rad2Deg; // plates tilt up -> overlapping scales
            coloredCount = -1;
        }

        void DestroyParts()
        {
            if (head != null) head.Destroy();
            if (bodyMat != null) Object.Destroy(bodyMat);
            if (spikeMat != null) Object.Destroy(spikeMat);
            if (bellyMat != null) Object.Destroy(bellyMat);
            bellyMat = null;
            head = null;
        }

        public void Destroy() { DestroyParts(); }

        // Call EVERY frame (instanced drawing only lasts one frame).
        public void Draw(List<Seg> segs, float radius, float angle, float time, SnakeFx fx, Vector3 offset = default(Vector3))
        {
            bool visible = !fx.invuln || Mathf.Sin(time * 28) > -0.3f;
            head.root.SetActive(visible);
            if (!visible) return;

            int n = Mathf.Min(segs.Count, Max + 1);
            int bodyN = n - 1;

            // Recolor only when the length changes (patterns depend on the index).
            if (bodyN != coloredCount)
            {
                for (int j = 0; j < bodyN; j++) colors[j] = Toon.Lin(Skins.SegmentColor(skin, colA, colB, j + 1));
                bodyBlock.SetVectorArray("_Color", colors);
                coloredCount = bodyN;
            }

            float taperStart = bodyN * 0.65f;
            int spikeEnd = Mathf.FloorToInt(bodyN * 0.9f);
            Quaternion tiltQ = Quaternion.Euler(0, 0, tilt);
            for (int j = 0; j < bodyN; j++)
            {
                Seg s = segs[j + 1], front = segs[j];
                float k = 1;
                if (j > taperStart) k = 1 - 0.6f * (j - taperStart) / (bodyN - taperStart);
                float r = radius * k;
                // Face the segment toward the one in front of it.
                float yaw = Mathf.Atan2(front.z - s.z, front.x - s.x);
                Quaternion q = Quaternion.Euler(0, -yaw * Mathf.Rad2Deg, 0) * tiltQ;
                matrices[j] = Matrix4x4.TRS(new Vector3(s.x, r * yOffset, s.z) + offset, q, new Vector3(r, r, r));
                if (j == 0) spikeCount = 0;
                if (j < spikeEnd && j % spikeEvery == 0) spikeMatrices[spikeCount++] = matrices[j];
            }

            // Brighter while boosting, flashing white while phased.
            float glow = fx.boosting ? 0.45f + Mathf.Sin(time * 20) * 0.15f : glowBase;
            Color glowColor = colA * glow;
            if (fx.phase) glowColor = Color.white * (0.35f + 0.25f * Mathf.Sin(time * 30));
            bodyMat.SetColor("_EmissionColor", glowColor);

            if (bodyN > 0)
            {
                Graphics.DrawMeshInstanced(bodyMesh, 0, bodyMat, matrices, bodyN, bodyBlock, ShadowCastingMode.Off, false, 0, onlyCamera);
                Graphics.DrawMeshInstanced(bodyMesh, 0, Toon.Outline(BodyOutline), matrices, bodyN, null, ShadowCastingMode.Off, false, 0, onlyCamera);
                if (spikeMesh != null && spikeCount > 0)
                {
                    Graphics.DrawMeshInstanced(spikeMesh, 0, spikeMat, spikeMatrices, spikeCount, spikeBlock, ShadowCastingMode.Off, false, 0, onlyCamera);
                    Graphics.DrawMeshInstanced(spikeMesh, 0, Toon.Outline(SpikeOutline), spikeMatrices, spikeCount, null, ShadowCastingMode.Off, false, 0, onlyCamera);
                }
                if (bellyMat != null)
                {
                    Graphics.DrawMeshInstanced(bellyMesh, 0, bellyMat, matrices, bodyN, bellyBlock, ShadowCastingMode.Off, false, 0, onlyCamera);
                    Graphics.DrawMeshInstanced(bellyMesh, 0, Toon.Outline(BodyOutline), matrices, bodyN, null, ShadowCastingMode.Off, false, 0, onlyCamera);
                }
            }

            Seg h = segs[0];
            HeadPosition = new Vector3(h.x, radius * yOffset, h.z) + offset;
            head.root.transform.position = HeadPosition;
            head.root.transform.rotation = Quaternion.Euler(0, -angle * Mathf.Rad2Deg, 0);
            head.root.transform.localScale = Vector3.one * radius * 1.4f; // big cartoon heads
            head.jaw.localRotation = Quaternion.Euler(0, 0, -fx.jaw * 0.75f * Mathf.Rad2Deg);
        }
    }
}
