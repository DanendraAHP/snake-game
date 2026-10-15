using System.Collections.Generic;
using UnityEngine;

namespace SerpentRogue
{
    // The 3D "shop window": a coiled snake far away from the arena, filmed by
    // its own camera into a RenderTexture that the UI draws on screen.
    // It also takes little pictures (thumbnails) of every skin for the cards.
    public class ShopStage : MonoBehaviour
    {
        static readonly Vector3 PreviewSpot = new Vector3(1000, 0, 0);
        static readonly Vector3 ThumbSpot = new Vector3(2000, 0, 0);

        public RenderTexture previewRT;
        Camera previewCam, thumbCam;
        SnakeModel previewModel;
        readonly List<Seg> coil = new List<Seg>();
        readonly List<Seg> line = new List<Seg>();
        bool open;

        readonly Dictionary<string, RenderTexture> thumbs = new Dictionary<string, RenderTexture>();
        readonly Queue<SkinData> bakeQueue = new Queue<SkinData>();
        SnakeModel bakingModel;

        Camera MakeCamera(string name, RenderTexture rt, Color bg)
        {
            var c = new GameObject(name).AddComponent<Camera>();
            c.targetTexture = rt;
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = bg;
            c.nearClipPlane = 0.1f;
            c.farClipPlane = 60;
            c.enabled = false;
            return c;
        }

        void Awake()
        {
            previewRT = new RenderTexture(1024, 900, 24) { name = "ShopPreview" };
            previewCam = MakeCamera("PreviewCamera", previewRT, Toon.Hex("#24489e"));
            previewCam.fieldOfView = 38;
            previewCam.transform.position = PreviewSpot + new Vector3(0, 10, 15);
            previewCam.transform.LookAt(PreviewSpot + new Vector3(0, 0.4f, 0.6f));

            thumbCam = MakeCamera("ThumbCamera", null, Toon.Hex("#7cc8f5"));
            thumbCam.fieldOfView = 30;
            thumbCam.transform.position = ThumbSpot + new Vector3(0.3f, 5.2f, 10);
            thumbCam.transform.LookAt(ThumbSpot + new Vector3(-0.5f, 0.4f, 0));

            for (int i = 0; i < 34; i++) coil.Add(new Seg());
            for (int i = 0; i < 11; i++) line.Add(new Seg { x = 1.6f - i * 0.62f, z = Mathf.Sin(i * 0.6f) * 0.5f });
        }

        public void Open(SkinData skin)
        {
            open = true;
            previewCam.enabled = true;
            ShowSkin(skin);
            foreach (var s in Skins.Catalog) if (!thumbs.ContainsKey(s.id)) bakeQueue.Enqueue(s);
        }

        public void Close()
        {
            open = false;
            previewCam.enabled = false;
            if (previewModel != null) previewModel.Destroy();
            previewModel = null;
        }

        public void ShowSkin(SkinData skin)
        {
            if (previewModel == null) previewModel = new SnakeModel(skin, previewCam);
            else previewModel.SetSkin(skin);
        }

        public Texture Thumb(SkinData s)
        {
            RenderTexture t;
            return thumbs.TryGetValue(s.id, out t) ? t : null;
        }

        // Coil the body into a spiral and lift the neck toward the camera.
        // Returns the head's heading.
        static float Coil(List<Seg> segs, float sway)
        {
            const int Neck = 6;
            const float step = 0.62f;
            for (int i = Neck; i < segs.Count; i++)
            {
                float a = Mathf.PI / 2 - (i - Neck) * 0.22f;
                float r = 3.3f - (i - Neck) * 0.06f;
                segs[i].x = Mathf.Cos(a) * r;
                segs[i].z = Mathf.Sin(a) * r;
            }
            float x = segs[Neck].x, z = segs[Neck].z, heading = Mathf.PI;
            for (int k = 1; k <= Neck; k++)
            {
                heading = Mathf.PI - (float)k / Neck * (Mathf.PI / 2 - sway);
                x += Mathf.Cos(heading) * step;
                z += Mathf.Sin(heading) * step;
                segs[Neck - k].x = x;
                segs[Neck - k].z = z;
            }
            return heading;
        }

        void LateUpdate()
        {
            if (!open) return;
            float t = Time.unscaledTime;

            if (previewModel != null)
            {
                float angle = Coil(coil, Mathf.Sin(t * 1.3f) * 0.35f);
                float chomp = Mathf.Pow(Mathf.Max(0, Mathf.Sin(t * 3)), 4);
                previewModel.Draw(coil, 0.85f, angle, t, new SnakeFx { jaw = 0.05f + chomp * 0.8f }, PreviewSpot + new Vector3(-1.6f, 0, -1.2f));
            }

            // Thumbnails: one skin per frame. The model drawn this frame is
            // filmed by thumbCam at the end of the frame, then removed next frame.
            if (bakingModel != null)
            {
                bakingModel.Destroy();
                bakingModel = null;
            }
            if (bakeQueue.Count > 0)
            {
                SkinData s = bakeQueue.Dequeue();
                var rt = new RenderTexture(256, 180, 24) { name = "Thumb_" + s.id };
                thumbs[s.id] = rt;
                thumbCam.targetTexture = rt;
                thumbCam.enabled = true;
                bakingModel = new SnakeModel(s, thumbCam);
                bakingModel.Draw(line, 0.85f, 0.45f, 0, new SnakeFx { jaw = 0.55f }, ThumbSpot);
            }
            else thumbCam.enabled = false;
        }
    }
}
