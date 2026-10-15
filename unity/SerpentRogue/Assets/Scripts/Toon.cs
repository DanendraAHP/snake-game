using System.Collections.Generic;
using UnityEngine;

namespace SerpentRogue
{
    // Creates the cartoon materials. Shaders live in Assets/Resources.
    public static class Toon
    {
        static Shader toonShader, outlineShader;
        static readonly Dictionary<float, Material> outlines = new Dictionary<float, Material>();

        static Shader ToonShader
        {
            get
            {
                if (toonShader == null)
                {
                    toonShader = Shader.Find("SerpentRogue/Toon");
                    if (toonShader == null)
                    {
                        Debug.LogError("SerpentRogue/Toon shader not found. Is Assets/Resources/SerpentToon.shader in the project?");
                        toonShader = Shader.Find("Unlit/Color");
                    }
                }
                return toonShader;
            }
        }

        public static Material Make(Color color, float flat = 0, Color? glow = null, bool instanced = false)
        {
            var m = new Material(ToonShader);
            m.SetColor("_Color", color);
            m.SetFloat("_Emission", flat);
            m.SetColor("_EmissionColor", glow ?? Color.black);
            m.enableInstancing = instanced;
            return m;
        }

        // Outline materials are shared (one per width).
        public static Material Outline(float width)
        {
            Material m;
            if (!outlines.TryGetValue(width, out m))
            {
                if (outlineShader == null) outlineShader = Shader.Find("SerpentRogue/Outline");
                m = new Material(outlineShader != null ? outlineShader : ToonShader);
                m.SetFloat("_Outline", width);
                m.SetColor("_OutlineColor", new Color(0.04f, 0.04f, 0.07f));
                m.enableInstancing = true;
                outlines[width] = m;
            }
            return m;
        }

        // Colors passed as raw vectors (per-instance colors) are not converted
        // automatically, so convert them when the project uses Linear color space.
        public static Vector4 Lin(Color c)
        {
            return QualitySettings.activeColorSpace == ColorSpace.Linear ? (Vector4)c.linear : (Vector4)c;
        }

        public static void SetLight(Vector3 towardLight)
        {
            Shader.SetGlobalVector("_SR_LightDir", towardLight.normalized);
        }

        public static Color Hex(string hex)
        {
            Color c;
            return ColorUtility.TryParseHtmlString(hex, out c) ? c : Color.magenta;
        }
    }
}
