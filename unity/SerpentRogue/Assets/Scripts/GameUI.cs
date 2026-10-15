using System.Collections.Generic;
using UnityEngine;

namespace SerpentRogue
{
    public enum UIScreen { Menu, Shop, HowTo, Hud, LevelUp, Pause, GameOver }

    // All menus and the HUD, drawn with Unity's immediate-mode GUI (OnGUI).
    // It needs no scene setup at all. Everything is laid out on a virtual
    // 720-pixel-tall screen and scaled to the real window.
    public class GameUI : MonoBehaviour
    {
        GameManager g;
        public UIScreen screen = UIScreen.Menu;
        float scale, W;
        const float H = 720;

        // level-up
        List<UpgradeDef> choices;
        int choiceLevel;
        // game over
        float gameOverTimer = -1;
        // toasts
        readonly List<KeyValuePair<string, float>> toasts = new List<KeyValuePair<string, float>>();
        // shop
        string tab = "common";
        string selected;
        Vector2 gridScroll, customScroll;

        void Start() { g = GameManager.Instance; }

        public void Show(UIScreen s)
        {
            if (screen == UIScreen.Shop && s != UIScreen.Shop) g.shop.Close();
            screen = s;
            if (s != UIScreen.GameOver) gameOverTimer = -1;
        }

        public void Toast(string msg) { toasts.Add(new KeyValuePair<string, float>(msg, Time.unscaledTime)); }

        public void OpenLevelUp(int level, List<UpgradeDef> rolled)
        {
            choiceLevel = level;
            choices = rolled;
            Show(UIScreen.LevelUp);
        }

        public void ShowGameOverSoon() { gameOverTimer = 0.9f; }

        void Update()
        {
            if (gameOverTimer > 0)
            {
                gameOverTimer -= Time.unscaledDeltaTime;
                if (gameOverTimer <= 0) Show(UIScreen.GameOver);
            }
            if (screen == UIScreen.LevelUp && choices != null)
            {
                if (GameInput.Pressed(GameKey.One) && choices.Count > 0) g.PickUpgrade(choices[0]);
                else if (GameInput.Pressed(GameKey.Two) && choices.Count > 1) g.PickUpgrade(choices[1]);
                else if (GameInput.Pressed(GameKey.Three) && choices.Count > 2) g.PickUpgrade(choices[2]);
                else if (GameInput.Pressed(GameKey.Reroll)) DoReroll();
            }
        }

        void DoReroll()
        {
            var r = g.Reroll();
            if (r != null) choices = r;
        }

        // ================================================================== styles
        bool stylesReady;
        GUIStyle panel, btn, bigBtn, tabBtn, tabActive, label, small, title, center, cardStyle;
        Texture2D white, circle, ring, shopBg;

        static Texture2D Rounded(Color c, int size = 32, int radius = 12)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Max(radius - x - 0.5f, x + 0.5f - (size - radius))) ;
                    float dy = Mathf.Max(0, Mathf.Max(radius - y - 0.5f, y + 0.5f - (size - radius)));
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(radius - d + 0.5f);
                    t.SetPixel(x, y, new Color(c.r, c.g, c.b, c.a * a));
                }
            t.Apply();
            return t;
        }

        static Texture2D Disc(int size, float inner, Color fill, Color edge)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            float r = size / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                    Color c = d < r * inner ? fill : edge;
                    c.a *= Mathf.Clamp01(r - d);
                    t.SetPixel(x, y, c);
                }
            t.Apply();
            return t;
        }

        GUIStyle Make(Texture2D bg, int fontSize, Color text, TextAnchor anchor = TextAnchor.MiddleCenter, int border = 12)
        {
            var s = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                alignment = anchor,
                wordWrap = true,
                border = new RectOffset(border, border, border, border),
                padding = new RectOffset(10, 10, 6, 6),
            };
            s.normal.textColor = text;
            s.normal.background = bg;
            return s;
        }

        void InitStyles()
        {
            if (stylesReady) return;
            stylesReady = true;
            white = Texture2D.whiteTexture;
            circle = Disc(64, 1, Color.white, Color.white);
            ring = Disc(160, 0.96f, new Color(0.06f, 0.08f, 0.18f, 0.8f), Toon.Hex("#ff6b8a"));
            shopBg = new Texture2D(1, 64) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 64; y++) shopBg.SetPixel(0, y, Color.Lerp(Toon.Hex("#0b1020"), Toon.Hex("#3d7fe0"), y / 63f));
            shopBg.Apply();

            Color text = Toon.Hex("#e8ecff");
            panel = Make(Rounded(new Color(0.06f, 0.09f, 0.18f, 0.85f)), 16, text, TextAnchor.UpperLeft);
            btn = Make(Rounded(Toon.Hex("#232c58")), 18, text);
            btn.hover.background = Rounded(Toon.Hex("#2f3b78"));
            btn.hover.textColor = Color.white;
            btn.active.background = btn.hover.background;
            bigBtn = Make(Rounded(Toon.Hex("#2bd46e")), 22, Toon.Hex("#052012"));
            bigBtn.hover.background = Rounded(Toon.Hex("#43f08a"));
            bigBtn.hover.textColor = bigBtn.normal.textColor;
            tabBtn = Make(Rounded(Toon.Hex("#1d3a8a")), 15, Toon.Hex("#b8c8ff"));
            tabBtn.hover.background = Rounded(Toon.Hex("#2649a8"));
            tabActive = Make(Rounded(Toon.Hex("#2f64e0")), 15, Color.white);
            label = Make(null, 16, text, TextAnchor.MiddleLeft);
            label.padding = new RectOffset(0, 0, 0, 0);
            small = new GUIStyle(label) { fontSize = 13, fontStyle = FontStyle.Normal };
            small.normal.textColor = Toon.Hex("#9aa5d6");
            title = new GUIStyle(label) { fontSize = 64, alignment = TextAnchor.MiddleCenter };
            center = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter };
            cardStyle = Make(Rounded(Toon.Hex("#7cc8f5")), 14, Toon.Hex("#0d2a6b"), TextAnchor.LowerCenter);
            cardStyle.hover.background = Rounded(Toon.Hex("#9fdcff"));
            cardStyle.hover.textColor = cardStyle.normal.textColor;
        }

        // Rounded colored box styles, cached so we don't create textures every frame.
        readonly Dictionary<string, GUIStyle> pills = new Dictionary<string, GUIStyle>();
        GUIStyle Pill(Color bg, int size, Color text)
        {
            string key = ColorUtility.ToHtmlStringRGBA(bg) + size + ColorUtility.ToHtmlStringRGBA(text);
            GUIStyle st;
            if (!pills.TryGetValue(key, out st))
            {
                st = Make(Rounded(bg), size, text);
                st.padding = new RectOffset(4, 4, 2, 2);
                pills[key] = st;
            }
            return st;
        }

        GUIStyle Sized(GUIStyle s, int size, Color? color = null, TextAnchor? anchor = null)
        {
            var c = new GUIStyle(s) { fontSize = size };
            if (color.HasValue) c.normal.textColor = color.Value;
            if (anchor.HasValue) c.alignment = anchor.Value;
            return c;
        }

        // Text with a black outline (for the floating snake labels).
        void Outlined(Rect r, string text, GUIStyle style, Color fill)
        {
            Color old = style.normal.textColor;
            style.normal.textColor = new Color(0.04f, 0.04f, 0.07f);
            float o = Mathf.Max(1.5f, style.fontSize * 0.09f);
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if (dx != 0 || dy != 0) GUI.Label(new Rect(r.x + dx * o, r.y + dy * o, r.width, r.height), text, style);
            style.normal.textColor = fill;
            GUI.Label(r, text, style);
            style.normal.textColor = old;
        }

        void Fill(Rect r, Color c)
        {
            Color old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, white);
            GUI.color = old;
        }

        static string Time2(float t) { return Mathf.FloorToInt(t / 60) + ":" + Mathf.FloorToInt(t % 60).ToString("00"); }

        // ================================================================== OnGUI
        void OnGUI()
        {
            if (g == null) return;
            InitStyles();
            scale = Screen.height / H;
            W = Screen.width / scale;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1));

            if (screen != UIScreen.Shop) DrawLabels();
            if (g.state == GameState.Playing && screen != UIScreen.GameOver) DrawHud();

            switch (screen)
            {
                case UIScreen.Menu: DrawMenu(); break;
                case UIScreen.HowTo: DrawHowTo(); break;
                case UIScreen.Shop: DrawShop(); break;
                case UIScreen.LevelUp: DrawLevelUp(); break;
                case UIScreen.Pause: DrawPause(); break;
                case UIScreen.GameOver: DrawGameOver(); break;
            }
            DrawToasts();
        }

        // ------------------------------------------------------------------ labels above snakes
        void DrawLabels()
        {
            var style = Sized(center, 16);
            foreach (var s in g.snakes)
            {
                float r = s.Radius;
                Vector3 world = s.model.HeadPosition + new Vector3(0, r * 3, r * 2.2f);
                Vector3 sp = g.cam.WorldToScreenPoint(world);
                if (sp.z <= 0) continue;
                float dist = Vector3.Distance(g.cam.transform.position, world);
                int size = Mathf.Clamp(Mathf.RoundToInt((1.7f + r * 1.2f) * H / (1.04f * dist) * 0.42f), 8, 44);
                style.fontSize = size;
                float x = sp.x / scale, y = (Screen.height - sp.y) / scale;
                Color c = s.LabelColor(); // red = bigger than you
                Outlined(new Rect(x - 150, y - size * 2.2f, 300, size * 1.2f), (s.isBoss ? "BOSS Lv" : "Lv") + s.Lv, style, c);
                Outlined(new Rect(x - 150, y - size * 1.05f, 300, size * 1.2f), s.name, style, c);
            }
        }

        // ------------------------------------------------------------------ HUD
        void DrawHud()
        {
            Snake p = g.player;
            if (p == null) return;
            GUI.Box(new Rect(14, 14, 320, 76), "", panel);
            GUI.Label(new Rect(24, 24, 78, 28), "PWR " + p.level, Pill(Toon.Hex("#ffd25d"), 17, Toon.Hex("#3a2800")));
            Fill(new Rect(108, 33, 210, 12), Toon.Hex("#1a2147"));
            Fill(new Rect(108, 33, 210 * Mathf.Clamp01(p.xp / p.xpNext), 12), Toon.Hex("#ffb23d"));
            string extra = "";
            if (p.stats.shieldMax > 0) extra += "  Shield " + p.shields + "/" + p.stats.shieldMax;
            if (p.stats.revives > 0) extra += "  Revive " + p.stats.revives;
            if (p.stats.phantom) extra += p.phantomCooldown > 0 ? "  Ghost " + Mathf.CeilToInt(p.phantomCooldown) + "s" : "  Ghost ready";
            GUI.Label(new Rect(28, 58, 300, 24), "Length " + Mathf.FloorToInt(p.mass) + "   Kills " + p.kills + "   " + Time2(g.runTime) + extra, Sized(label, 14));

            // Leaderboard
            var sorted = new List<Snake>(g.snakes);
            sorted.Sort((a, b) => b.mass.CompareTo(a.mass));
            float lx = W - 234;
            int rank = sorted.IndexOf(p);
            int rows = Mathf.Min(8, sorted.Count) + (rank >= 8 ? 1 : 0);
            GUI.Box(new Rect(lx, 14, 220, 40 + rows * 22), "", panel);
            GUI.Label(new Rect(lx + 14, 20, 200, 22), "LEADERBOARD", small);
            for (int i = 0; i < Mathf.Min(8, sorted.Count); i++) LeaderRow(lx, 44 + i * 22, i, sorted[i], sorted[i] == p);
            if (rank >= 8) LeaderRow(lx, 44 + 8 * 22, rank, p, true);

            // Minimap
            float mm = 170, mx = W - mm - 14, my = H - mm - 14;
            GUI.DrawTexture(new Rect(mx, my, mm, mm), ring);
            float lavaScale = (mm / 2 - 6) / Config.ArenaRadius;
            GUI.color = Toon.Hex("#ff5a00");
            foreach (var pool in g.pools)
            {
                float pr = pool.r * lavaScale;
                GUI.DrawTexture(new Rect(mx + mm / 2 + pool.x * lavaScale - pr, my + mm / 2 - pool.z * lavaScale - pr, pr * 2, pr * 2), circle);
            }
            float sc = (mm / 2 - 6) / Config.ArenaRadius;
            foreach (var s in g.snakes)
            {
                bool me = s == p;
                float rr = me ? 4 : s.isBoss ? 7 : 1.5f + Mathf.Sqrt(s.mass) / 6;
                GUI.color = me ? Color.white : s.isBoss ? Color.red : s.skin.A;
                // screen up = +z in the world
                GUI.DrawTexture(new Rect(mx + mm / 2 + s.Head.x * sc - rr, my + mm / 2 - s.Head.z * sc - rr, rr * 2, rr * 2), circle);
            }
            GUI.color = Color.white;

            // Upgrade chips
            float cx = 14;
            foreach (var kv in p.upgrades)
            {
                UpgradeDef u = Upgrades.ById(kv.Key);
                var chip = Pill(Upgrades.RarityColor(u.rarity) * 0.55f + new Color(0, 0, 0, 0.45f), 13, Color.white);
                Texture2D icon = Icons.Get(u.id);
                string count = kv.Value > 1 ? "x" + kv.Value : "";
                float w = 34 + count.Length * 8;
                GUI.Label(new Rect(cx, H - 48, w, 34), new GUIContent("", u.name), chip);
                if (icon != null) GUI.DrawTexture(new Rect(cx + 5, H - 43, 24, 24), icon);
                else GUI.Label(new Rect(cx, H - 48, 34, 34), u.icon, Sized(center, 11));
                if (count.Length > 0) GUI.Label(new Rect(cx + 29, H - 48, w - 29, 34), count, Sized(label, 12));
                cx += w + 6;
            }
            GUI.Label(new Rect(0, H - 26, W, 20), "Mouse to steer  ·  Hold click / Space to boost  ·  Esc to pause  ·  RED Lv = bigger than you, run!", Sized(small, 12, null, TextAnchor.MiddleCenter));

            // Boss countdown / boss info
            string bossText;
            Color bossBg;
            if (g.boss != null)
            {
                bossText = "BOSS  " + g.boss.name + "  Lv" + g.boss.Lv + "  ·  leaves in " + Time2(Mathf.Max(0, g.bossLeft));
                bossBg = new Color(0.47f, 0.04f, 0.08f, 0.9f);
            }
            else
            {
                float t = Mathf.Max(0, g.bossTimer);
                bossText = "Boss in " + Time2(t);
                bool warn = t <= Config.BossWarning && Mathf.Repeat(Time.unscaledTime, 0.6f) < 0.3f;
                bossBg = warn ? new Color(0.6f, 0.1f, 0.1f, 0.9f) : new Color(0.06f, 0.09f, 0.18f, 0.85f);
            }
            GUI.Label(new Rect(W / 2 - 190, 14, 380, 36), bossText, Pill(bossBg, 15, Color.white));
        }

        void LeaderRow(float lx, float y, int i, Snake s, bool me)
        {
            var st = Sized(label, 14, me ? Toon.Hex("#3be37a") : Toon.Hex("#e8ecff"));
            GUI.Label(new Rect(lx + 14, y, 150, 22), (i + 1) + ". " + s.name, st);
            GUI.Label(new Rect(lx + 14, y, 192, 22), Mathf.FloorToInt(s.mass).ToString(), Sized(st, 14, null, TextAnchor.MiddleRight));
        }

        void DrawToasts()
        {
            toasts.RemoveAll(t => Time.unscaledTime - t.Value > 2.4f);
            for (int i = 0; i < toasts.Count; i++)
            {
                float age = Time.unscaledTime - toasts[i].Value;
                GUI.color = new Color(1, 1, 1, Mathf.Clamp01((2.4f - age) / 0.5f));
                GUI.Label(new Rect(W / 2 - 200, 100 + i * 46, 400, 38), toasts[i].Key, Sized(panel, 16, null, TextAnchor.MiddleCenter));
            }
            GUI.color = Color.white;
        }

        // ------------------------------------------------------------------ menu
        void DrawMenu()
        {
            float cx = W / 2;
            Outlined(new Rect(cx - 400, 90, 800, 90), "SERPENT", Sized(title, 72), Toon.Hex("#3be37a"));
            Outlined(new Rect(cx - 400, 170, 800, 90), "ROGUE", Sized(title, 72), Toon.Hex("#ffd25d"));
            GUI.Label(new Rect(cx - 300, 262, 600, 30), "Eat. Grow. Level up. Choose your power. Outlast the bots.", Sized(center, 17, Toon.Hex("#c3cbf0")));

            GUI.Label(new Rect(cx - 160, 312, 110, 34), "Your name", Sized(label, 15, Toon.Hex("#9aa5d6")));
            string n = GUI.TextField(new Rect(cx - 50, 312, 210, 34), g.profile.name, 14, Sized(btn, 16));
            if (n != g.profile.name)
            {
                g.profile.name = n;
                g.profile.Save();
            }

            // Map picker: "Random" plus every map in Maps.cs.
            int count = Maps.All.Count + 1;
            int idx = 0;
            for (int i = 0; i < Maps.All.Count; i++) if (Maps.All[i].id == g.profile.map) idx = i + 1;
            int step = 0;
            if (GUI.Button(new Rect(cx - 210, 358, 44, 52), "<", btn)) step = -1;
            if (GUI.Button(new Rect(cx + 166, 358, 44, 52), ">", btn)) step = 1;
            string mapName = idx == 0 ? "Random map" : Maps.All[idx - 1].name;
            string mapDesc = idx == 0 ? "A different biome every run." : Maps.All[idx - 1].desc;
            GUI.Box(new Rect(cx - 160, 358, 320, 52), "", panel);
            GUI.Label(new Rect(cx - 160, 360, 320, 26), mapName, Sized(center, 17, Toon.Hex("#ffd25d")));
            GUI.Label(new Rect(cx - 160, 384, 320, 22), mapDesc, Sized(small, 12, null, TextAnchor.MiddleCenter));
            if (step != 0)
            {
                idx = (idx + step + count) % count;
                g.profile.map = idx == 0 ? "random" : Maps.All[idx - 1].id;
                g.profile.Save();
                g.StartAttract(true); // preview the map behind the menu
            }

            if (GUI.Button(new Rect(cx - 130, 424, 260, 54), "PLAY", bigBtn)) g.StartRun();
            if (GUI.Button(new Rect(cx - 130, 488, 260, 46), "Skins   (" + (Config.UnlimitedCoins ? "unlimited" : g.profile.coins.ToString()) + " coins)", btn))
            {
                selected = g.profile.equipped;
                SkinData sk = SelectedSkin();
                tab = sk.rarity == "custom" ? "custom" : sk.rarity;
                screen = UIScreen.Shop;
                g.shop.Open(sk);
            }
            if (GUI.Button(new Rect(cx - 130, 544, 260, 46), "How to Play", btn)) Show(UIScreen.HowTo);

            Profile pr = g.profile;
            string best = pr.bestLevel > 0
                ? "Best run:  Power " + pr.bestLevel + "  ·  Length " + pr.bestLength + "  ·  Kills " + pr.bestKills + "  ·  " + Time2(pr.bestTime)
                : "No runs yet. Go get eaten!";
            GUI.Label(new Rect(cx - 300, 604, 600, 24), best, Sized(small, 14, null, TextAnchor.MiddleCenter));
        }

        void DrawHowTo()
        {
            var r = new Rect(W / 2 - 360, 40, 720, 640);
            GUI.Box(r, "", panel);
            GUI.Label(new Rect(r.x, r.y + 16, r.width, 40), "How to play", Sized(center, 28));
            string[] lines =
            {
                "Your snake follows the MOUSE. Or use A / D (arrow keys) to turn.",
                "Hold LEFT CLICK or SPACE to boost. Boosting burns length.",
                "Eat glowing orbs to grow and earn XP. Golden orbs are worth 5x.",
                "Every snake shows its Lv (size). When two snakes touch, the HIGHER Lv eats the lower one, from any side!",
                "RED labels are bigger than you: run! WHITE labels are smaller: hunt them.",
                "Every few minutes a giant BOSS hunts you. Survive until it leaves, or grow big enough to eat it.",
                "Every level up, pick 1 of 3 power-ups (keys 1 / 2 / 3, R = reroll).",
                "Bots get bigger and meaner over time. How long can you last?",
                "Every run earns coins. Spend them on new skins in the Skins shop.",
            };
            for (int i = 0; i < lines.Length; i++) GUI.Label(new Rect(r.x + 36, r.y + 76 + i * 50, r.width - 72, 46), "•  " + lines[i], Sized(label, 16));
            if (GUI.Button(new Rect(W / 2 - 80, r.yMax - 66, 160, 44), "Back", btn)) Show(UIScreen.Menu);
        }

        // ------------------------------------------------------------------ level up
        void DrawLevelUp()
        {
            if (choices == null) return;
            Fill(new Rect(0, 0, W, H), new Color(0.02f, 0.03f, 0.08f, 0.6f));
            Outlined(new Rect(0, 120, W, 60), "POWER UP!  Level " + choiceLevel, Sized(title, 44), Toon.Hex("#ffd25d"));
            GUI.Label(new Rect(0, 180, W, 30), "Choose a power-up (keys 1 / 2 / 3)", Sized(center, 16, Toon.Hex("#9aa5d6")));
            float cw = 230, ch = 270, gap = 22, total = choices.Count * cw + (choices.Count - 1) * gap, x0 = (W - total) / 2;
            for (int i = 0; i < choices.Count; i++)
            {
                UpgradeDef u = choices[i];
                var r = new Rect(x0 + i * (cw + gap), 230, cw, ch);
                Color rc = Upgrades.RarityColor(u.rarity);
                GUI.Label(r, "", Pill(rc, 1, Color.white));
                var inner = new Rect(r.x + 3, r.y + 3, r.width - 6, r.height - 6);
                if (GUI.Button(inner, "", Pill(Toon.Hex("#141b38"), 1, Color.white))) { g.PickUpgrade(u); return; }
                GUI.Label(new Rect(inner.x + 10, inner.y + 10, inner.width - 20, 70), "", Pill(rc * 0.45f + new Color(0, 0, 0, 0.55f), 1, Color.white));
                Texture2D icon = Icons.Get(u.id);
                if (icon != null) GUI.DrawTexture(new Rect(inner.center.x - 30, inner.y + 15, 60, 60), icon);
                else GUI.Label(new Rect(inner.x, inner.y + 18, inner.width, 60), u.icon, Sized(center, 22));
                GUI.Label(new Rect(inner.x, inner.y + 86, inner.width, 20), u.rarity.ToUpper(), Sized(center, 12, rc));
                GUI.Label(new Rect(inner.x + 10, inner.y + 106, inner.width - 20, 30), u.name, Sized(center, 19));
                GUI.Label(new Rect(inner.x + 14, inner.y + 140, inner.width - 28, 80), u.desc, Sized(center, 14, Toon.Hex("#9aa5d6"), TextAnchor.UpperCenter));
                int have;
                g.player.upgrades.TryGetValue(u.id, out have);
                if (have > 0) GUI.Label(new Rect(inner.x, inner.y + 214, inner.width, 20), "Owned: " + have + (u.max < 99 ? " / " + u.max : ""), Sized(center, 12, Toon.Hex("#ffd25d")));
                GUI.Label(new Rect(inner.x, inner.yMax - 26, inner.width, 20), "[" + (i + 1) + "]", Sized(center, 12, Toon.Hex("#9aa5d6")));
            }
            GUI.enabled = g.player.rerolls > 0;
            if (GUI.Button(new Rect(W / 2 - 110, 530, 220, 44), "Reroll (" + g.player.rerolls + ")  ·  R", btn)) DoReroll();
            GUI.enabled = true;
        }

        void DrawPause()
        {
            Fill(new Rect(0, 0, W, H), new Color(0.02f, 0.03f, 0.08f, 0.55f));
            var r = new Rect(W / 2 - 200, 220, 400, 240);
            GUI.Box(r, "", panel);
            GUI.Label(new Rect(r.x, r.y + 20, r.width, 40), "Paused", Sized(center, 30));
            if (GUI.Button(new Rect(W / 2 - 130, r.y + 86, 260, 50), "Resume", bigBtn)) g.TogglePause();
            if (GUI.Button(new Rect(W / 2 - 130, r.y + 150, 260, 46), "Quit to menu", btn)) g.GoMenu();
        }

        void DrawGameOver()
        {
            RunStats s = g.lastRun;
            if (s == null) return;
            Fill(new Rect(0, 0, W, H), new Color(0.02f, 0.03f, 0.08f, 0.55f));
            var r = new Rect(W / 2 - 280, 90, 560, 540);
            GUI.Box(r, "", panel);
            GUI.Label(new Rect(r.x, r.y + 20, r.width, 44), "You were eaten!", Sized(center, 32));
            StatCell(new Rect(r.x + 30, r.y + 80, 240, 84), "Power level", s.level.ToString(), s.newBestLevel);
            StatCell(new Rect(r.x + 290, r.y + 80, 240, 84), "Max length", s.length.ToString(), s.newBestLength);
            StatCell(new Rect(r.x + 30, r.y + 176, 240, 84), "Kills", s.kills.ToString(), s.newBestKills);
            StatCell(new Rect(r.x + 290, r.y + 176, 240, 84), "Survived", Time2(s.time), s.newBestTime);
            // The power-ups you collected, as icons (hover one to see its name).
            if (s.upgrades.Count == 0)
                GUI.Label(new Rect(r.x + 30, r.y + 268, r.width - 60, 40), "No power-ups this run.", Sized(center, 13, Toon.Hex("#9aa5d6")));
            else
            {
                float size = 36, gap = 6, total = s.upgrades.Count * (size + gap) - gap;
                float ix = r.center.x - Mathf.Min(total, r.width - 40) / 2;
                foreach (var kv in s.upgrades)
                {
                    UpgradeDef u = Upgrades.ById(kv.Key);
                    var cell = new Rect(ix, r.y + 270, size, size);
                    GUI.Label(cell, new GUIContent("", u.name + (kv.Value > 1 ? " x" + kv.Value : "")), Pill(Upgrades.RarityColor(u.rarity) * 0.5f + new Color(0, 0, 0, 0.5f), 1, Color.white));
                    Texture2D icon = Icons.Get(u.id);
                    if (icon != null) GUI.DrawTexture(new Rect(ix + 4, r.y + 274, size - 8, size - 8), icon);
                    if (kv.Value > 1) GUI.Label(new Rect(ix, r.y + 270 + size - 14, size, 14), "x" + kv.Value, Sized(center, 10));
                    ix += size + gap;
                }
            }
            GUI.Label(new Rect(r.x, r.y + 318, r.width, 22), "Map: " + s.mapName + "   ·   Bosses eaten: " + s.bossKills, Sized(center, 13, Toon.Hex("#9aa5d6")));
            GUI.Label(new Rect(r.x, r.y + 340, r.width, 30), "+" + s.coins + " coins for the skin shop", Sized(center, 20, Toon.Hex("#ffd25d")));
            if (GUI.Button(new Rect(W / 2 - 130, r.y + 392, 260, 52), "Play again", bigBtn)) g.StartRun();
            if (GUI.Button(new Rect(W / 2 - 130, r.y + 454, 260, 46), "Menu", btn)) g.GoMenu();
        }

        void StatCell(Rect r, string name, string value, bool isNew)
        {
            Fill(r, Toon.Hex("#1a2147"));
            GUI.Label(new Rect(r.x, r.y + 6, r.width, 20), name, Sized(center, 14));
            GUI.Label(new Rect(r.x, r.y + 26, r.width, 34), value, Sized(center, 26, Toon.Hex("#ffd25d")));
            if (isNew) GUI.Label(new Rect(r.x, r.y + 60, r.width, 18), "NEW BEST!", Sized(center, 12, Toon.Hex("#3be37a")));
        }

        // ------------------------------------------------------------------ skin shop
        SkinData SelectedSkin()
        {
            if (selected == "custom") return g.profile.custom;
            return Skins.ById(selected) ?? Skins.Catalog[0];
        }

        void Select(string id)
        {
            selected = id;
            g.shop.ShowSkin(SelectedSkin());
        }

        void DrawShop()
        {
            GUI.DrawTexture(new Rect(0, 0, W, H), shopBg);
            Profile pr = g.profile;
            if (GUI.Button(new Rect(16, 14, 110, 42), "< Back", btn)) Show(UIScreen.Menu);
            Outlined(new Rect(140, 12, 500, 46), "COLLECT SKINS", Sized(label, 32), Color.white);
            GUI.Label(new Rect(W - 200, 14, 184, 42), (Config.UnlimitedCoins ? "Unlimited" : pr.coins.ToString()) + " coins", Pill(new Color(0, 0, 0, 0.35f), 18, Toon.Hex("#ffd25d")));

            // Left: 3D preview
            float leftW = W * 0.52f;
            var stage = new Rect(16, 70, leftW - 24, H - 86);
            if (g.shop.previewRT != null) GUI.DrawTexture(stage, g.shop.previewRT, ScaleMode.ScaleAndCrop);
            SkinData sel = SelectedSkin();
            Color rc = Skins.RarityColor(sel.rarity);
            GUI.Label(new Rect(stage.center.x - 60, stage.yMax - 128, 120, 22), sel.rarity.ToUpper(), Pill(rc, 12, Toon.Hex("#0a0a12")));
            Outlined(new Rect(stage.x, stage.yMax - 104, stage.width, 40), sel.label, Sized(center, 28), Color.white);

            bool owned = selected == "custom" || pr.owned.Contains(selected);
            var actionRect = new Rect(stage.center.x - 110, stage.yMax - 58, 220, 48);
            if (pr.equipped == selected)
            {
                GUI.enabled = false;
                GUI.Button(actionRect, "Equipped", bigBtn);
                GUI.enabled = true;
            }
            else if (owned)
            {
                if (GUI.Button(actionRect, "Equip", bigBtn)) { pr.equipped = selected; pr.Save(); }
            }
            else
            {
                GUI.enabled = Config.UnlimitedCoins || pr.coins >= sel.price;
                if (GUI.Button(actionRect, "Buy  " + sel.price + " coins", bigBtn))
                {
                    if (!Config.UnlimitedCoins) pr.coins -= sel.price;
                    pr.owned.Add(selected);
                    pr.equipped = selected;
                    pr.Save();
                    Toast("Unlocked " + sel.label + "!");
                }
                GUI.enabled = true;
            }

            // Right: tabs + grid / custom editor
            float rx = leftW + 4, rw = W - rx - 16;
            GUI.Box(new Rect(rx, 70, rw, H - 86), "", Pill(Toon.Hex("#2f64e0"), 1, Color.white));
            string[] tabs = { "common", "rare", "epic", "legend", "mythic", "custom" };
            float tw = (rw - 20) / tabs.Length;
            for (int i = 0; i < tabs.Length; i++)
            {
                bool act = tab == tabs[i];
                if (GUI.Button(new Rect(rx + 10 + i * tw, 80, tw - 4, 40), tabs[i].ToUpper(), act ? tabActive : tabBtn))
                {
                    tab = tabs[i];
                    if (tab == "custom") Select("custom");
                }
            }
            var content = new Rect(rx + 10, 128, rw - 20, H - 154);
            if (tab == "custom") DrawCustom(content);
            else DrawGrid(content);
        }

        void DrawGrid(Rect area)
        {
            var list = new List<SkinData>();
            foreach (var s in Skins.Catalog) if (s.rarity == tab) list.Add(s);
            float cw = 168, ch = 150;
            int cols = Mathf.Max(1, Mathf.FloorToInt((area.width - 10) / (cw + 10)));
            int rows = Mathf.CeilToInt(list.Count / (float)cols);
            gridScroll = GUI.BeginScrollView(area, gridScroll, new Rect(0, 0, area.width - 20, rows * (ch + 10) + 10));
            for (int i = 0; i < list.Count; i++)
            {
                SkinData s = list[i];
                var r = new Rect(5 + (i % cols) * (cw + 10), 5 + (i / cols) * (ch + 10), cw, ch);
                if (selected == s.id) Fill(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), Toon.Hex("#ffd25d"));
                bool owned = g.profile.owned.Contains(s.id);
                string price = owned ? (g.profile.equipped == s.id ? "Equipped" : "Owned") : s.price + " coins";
                if (GUI.Button(r, s.label + "\n" + price, cardStyle)) Select(s.id);
                Texture thumb = g.shop.Thumb(s);
                var img = new Rect(r.x + 6, r.y + 6, r.width - 12, r.height - 52);
                if (thumb != null) GUI.DrawTexture(img, thumb, ScaleMode.ScaleToFit);
                else
                {
                    Fill(new Rect(img.x + 20, img.center.y - 12, img.width - 40, 24), s.A);
                    Fill(new Rect(img.x + 40, img.center.y - 6, img.width - 80, 12), s.B);
                }
                if (!owned) GUI.Label(new Rect(r.xMax - 50, r.y + 4, 46, 18), "LOCK", Sized(center, 10, Toon.Hex("#0d2a6b")));
            }
            GUI.EndScrollView();
        }

        static readonly string[] Swatches =
        {
            "#ff3b1f", "#ff8f3d", "#ffd23d", "#9be52f", "#3bd16f", "#17703f", "#2fd6c8", "#2f8bff",
            "#123a8f", "#8a3fd1", "#ff7ac8", "#ffffff", "#9aa5b5", "#3a3a46", "#1b1b22", "#8a5a2b",
        };

        void DrawCustom(Rect area)
        {
            SkinData c = g.profile.custom;
            bool changed = false;
            customScroll = GUI.BeginScrollView(area, customScroll, new Rect(0, 0, area.width - 20, 760));
            float y = 4;
            changed |= OptionRow(ref y, area.width, "Head", Skins.Heads, ref c.head);
            if (c.head == "round") changed |= OptionRow(ref y, area.width, "Eyes", Skins.Eyes, ref c.eyes);
            changed |= OptionRow(ref y, area.width, "Body", Skins.Bodies, ref c.body);
            changed |= OptionRow(ref y, area.width, "Back", Skins.Backs, ref c.spikes);
            changed |= OptionRow(ref y, area.width, "Pattern", Skins.Patterns, ref c.pattern);
            changed |= OptionRow(ref y, area.width, "Hat", Skins.Hats, ref c.hat);
            changed |= ColorRow(ref y, "Main color", ref c.colorA);
            changed |= ColorRow(ref y, "Second color", ref c.colorB);
            changed |= ColorRow(ref y, "Accent", ref c.colorC);
            changed |= ColorRow(ref y, "Eyes", ref c.eyeColor);
            var toggle = new GUIStyle(GUI.skin.toggle) { fontSize = 16, fontStyle = FontStyle.Bold };
            toggle.normal.textColor = toggle.onNormal.textColor = Color.white;
            bool glow = GUI.Toggle(new Rect(4, y, 200, 26), c.glow, "  Glow", toggle);
            if (glow != c.glow) { c.glow = glow; changed = true; }
            y += 36;
            if (GUI.Button(new Rect(4, y, 200, 42), "Randomize", btn))
            {
                SkinData r = Skins.RandomCustom();
                r.id = "custom"; r.label = "My Custom"; r.rarity = "custom";
                g.profile.custom = c = r;
                changed = true;
            }
            GUI.EndScrollView();
            if (changed)
            {
                g.profile.Save();
                g.shop.ShowSkin(c);
            }
        }

        bool OptionRow(ref float y, float width, string name, string[][] options, ref string value)
        {
            GUI.Label(new Rect(4, y, 200, 22), name, Sized(label, 14, Toon.Hex("#dce6ff")));
            y += 24;
            bool changed = false;
            float x = 4;
            foreach (var o in options)
            {
                float w = 20 + o[1].Length * 9;
                if (x + w > width - 30) { x = 4; y += 38; }
                if (GUI.Button(new Rect(x, y, w, 32), o[1], value == o[0] ? tabActive : tabBtn) && value != o[0])
                {
                    value = o[0];
                    changed = true;
                }
                x += w + 6;
            }
            y += 44;
            return changed;
        }

        bool ColorRow(ref float y, string name, ref string value)
        {
            GUI.Label(new Rect(4, y, 200, 22), name, Sized(label, 14, Toon.Hex("#dce6ff")));
            y += 24;
            bool changed = false;
            for (int i = 0; i < Swatches.Length; i++)
            {
                var r = new Rect(4 + i * 30, y, 26, 26);
                if (string.Equals(value, Swatches[i], System.StringComparison.OrdinalIgnoreCase)) Fill(new Rect(r.x - 2, r.y - 2, 30, 30), Color.white);
                Fill(r, Toon.Hex(Swatches[i]));
                if (GUI.Button(r, "", GUIStyle.none)) { value = Swatches[i]; changed = true; }
            }
            y += 38;
            return changed;
        }
    }
}
