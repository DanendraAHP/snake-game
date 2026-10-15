using System;
using System.Collections.Generic;
using UnityEngine;

namespace SerpentRogue
{
    // A skin is a small data object (easy to save as JSON).
    //   head:    dragon | viper | round
    //   body:    scales | blocky | smooth
    //   spikes:  none | spikes | fins | plates   (what grows on the back)
    //   pattern: how colorA / colorB are spread along the body
    [Serializable]
    public class SkinData
    {
        public string id = "custom", label = "My Custom", rarity = "custom";
        public int price;
        public string head = "dragon", body = "scales", spikes = "spikes", pattern = "solid", eyes = "normal", hat = "none";
        public bool glow;
        public string colorA = "#3bd16f", colorB = "#17703f", colorC = "#d8ff6b", eyeColor = "#ffd23d";

        public SkinData Clone() { return (SkinData)MemberwiseClone(); }
        public string ShapeKey { get { return head + "|" + hat + "|" + eyes; } }
        public Color A { get { return Toon.Hex(colorA); } }
        public Color B { get { return Toon.Hex(colorB); } }
        public Color C { get { return Toon.Hex(colorC); } }
        public Color Eye { get { return Toon.Hex(eyeColor); } }
    }

    [Serializable]
    public class Profile
    {
        public string name = "Player";
        public int coins = Config.StartCoins;
        public List<string> owned = new List<string> { "jungle" };
        public string equipped = "jungle";
        public SkinData custom;
        public int bestLevel, bestLength, bestKills;
        public float bestTime;

        const string Key = "serpentRogue.profile";

        public static Profile Load()
        {
            Profile p = null;
            string json = PlayerPrefs.GetString(Key, "");
            if (json.Length > 0)
            {
                try { p = JsonUtility.FromJson<Profile>(json); } catch (Exception) { p = null; }
            }
            if (p == null) p = new Profile();
            if (p.custom == null || string.IsNullOrEmpty(p.custom.head)) p.custom = Skins.DefaultCustom();
            if (p.owned == null) p.owned = new List<string> { "jungle" };
            return p;
        }

        public void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(this));
            PlayerPrefs.Save();
        }

        public SkinData EquippedSkin()
        {
            if (equipped == "custom") return custom;
            SkinData s = Skins.ById(equipped);
            return s ?? Skins.Catalog[0];
        }
    }

    public static class Skins
    {
        public static readonly string[][] Heads = { new[] { "dragon", "Dragon" }, new[] { "viper", "Viper" }, new[] { "round", "Cute" } };
        public static readonly string[][] Bodies = { new[] { "scales", "Scales" }, new[] { "blocky", "Blocky" }, new[] { "smooth", "Smooth" } };
        public static readonly string[][] Backs = { new[] { "none", "None" }, new[] { "spikes", "Spikes" }, new[] { "fins", "Flame Fins" }, new[] { "plates", "Plates" } };
        public static readonly string[][] Patterns = { new[] { "solid", "Solid" }, new[] { "stripes", "Stripes" }, new[] { "bands", "Bands" }, new[] { "spots", "Spots" }, new[] { "gradient", "Gradient" }, new[] { "rainbow", "Rainbow" } };
        public static readonly string[][] Eyes = { new[] { "normal", "Normal" }, new[] { "cute", "Cute" }, new[] { "angry", "Angry" }, new[] { "cyclops", "Cyclops" } };
        public static readonly string[][] Hats = { new[] { "none", "None" }, new[] { "crown", "Crown" }, new[] { "horns", "Horns" }, new[] { "ears", "Jackal Ears" }, new[] { "halo", "Halo" }, new[] { "tophat", "Top Hat" } };
        public static readonly string[] Rarities = { "common", "rare", "epic", "legend" };

        static SkinData S(string id, string label, string rarity, int price, Action<SkinData> setup)
        {
            var s = new SkinData { id = id, label = label, rarity = rarity, price = price };
            setup(s);
            return s;
        }

        // The skin shop. Prices are in coins, earned at the end of every run.
        public static readonly List<SkinData> Catalog = new List<SkinData>
        {
            S("jungle", "Jungle Viper", "common", 0, s => { s.head = "viper"; s.spikes = "none"; s.pattern = "stripes"; s.colorA = "#3bd16f"; s.colorB = "#17703f"; s.colorC = "#d8ff6b"; }),
            S("oriole", "Violet Spots", "common", 150, s => { s.head = "viper"; s.spikes = "none"; s.pattern = "spots"; s.colorA = "#8a3fd1"; s.colorB = "#d6a8ff"; s.colorC = "#ffffff"; }),
            S("coral", "Coral Snake", "common", 200, s => { s.head = "viper"; s.body = "smooth"; s.spikes = "none"; s.pattern = "bands"; s.colorA = "#e8323c"; s.colorB = "#1b1b22"; s.colorC = "#ffd23d"; }),
            S("bubble", "Bubblegum", "common", 250, s => { s.head = "round"; s.body = "smooth"; s.spikes = "none"; s.pattern = "gradient"; s.eyes = "cute"; s.colorA = "#ff7ac8"; s.colorB = "#7ad7ff"; s.colorC = "#ffffff"; }),
            S("ice", "Ice Wyrm", "rare", 600, s => { s.pattern = "gradient"; s.colorA = "#9ff0ff"; s.colorB = "#2d6cdf"; s.colorC = "#ffffff"; s.eyeColor = "#7fe8ff"; }),
            S("toxic", "Toxic Drake", "rare", 650, s => { s.pattern = "stripes"; s.colorA = "#9be52f"; s.colorB = "#2f6b1c"; s.colorC = "#ffe93d"; }),
            S("sand", "Sandstorm", "rare", 700, s => { s.head = "viper"; s.body = "blocky"; s.spikes = "plates"; s.pattern = "bands"; s.colorA = "#e3b874"; s.colorB = "#8a5a2b"; s.colorC = "#5a3a1a"; }),
            S("python", "Royal Python", "rare", 900, s => { s.head = "viper"; s.pattern = "spots"; s.spikes = "none"; s.hat = "crown"; s.colorA = "#ffc83d"; s.colorB = "#2a1d10"; s.colorC = "#ffffff"; }),
            S("fire", "Fire Dragon", "epic", 1800, s => { s.spikes = "fins"; s.pattern = "gradient"; s.glow = true; s.colorA = "#ff3b1f"; s.colorB = "#ffb52e"; s.colorC = "#ffd23d"; s.eyeColor = "#ffe066"; }),
            S("storm", "Storm Serpent", "epic", 1800, s => { s.pattern = "stripes"; s.glow = true; s.colorA = "#2f8bff"; s.colorB = "#123a8f"; s.colorC = "#e8f4ff"; s.eyeColor = "#ffe93d"; }),
            S("rainbow", "Prism Drake", "epic", 2200, s => { s.pattern = "rainbow"; s.colorA = "#ff4d4d"; s.colorB = "#ffffff"; s.colorC = "#ffffff"; }),
            S("sakura", "Sakura", "epic", 2000, s => { s.spikes = "fins"; s.pattern = "gradient"; s.colorA = "#ff9ccf"; s.colorB = "#fff0f6"; s.colorC = "#ffc83d"; s.eyeColor = "#ff4d8d"; }),
            S("anubis", "Anubis", "legend", 5000, s => { s.body = "blocky"; s.spikes = "plates"; s.hat = "ears"; s.colorA = "#2a2a33"; s.colorB = "#3a3a46"; s.colorC = "#ffc83d"; s.eyeColor = "#ffae00"; }),
            S("inferno", "Inferno King", "legend", 5500, s => { s.spikes = "fins"; s.pattern = "gradient"; s.hat = "crown"; s.glow = true; s.colorA = "#ffd23d"; s.colorB = "#ff2a00"; s.colorC = "#ff6a00"; s.eyeColor = "#ff2a00"; }),
            S("void", "Void Leviathan", "legend", 6000, s => { s.spikes = "fins"; s.pattern = "gradient"; s.glow = true; s.colorA = "#8a2be2"; s.colorB = "#140a26"; s.colorC = "#ff3bd4"; s.eyeColor = "#ff3bd4"; }),
        };

        public static SkinData ById(string id)
        {
            foreach (var s in Catalog) if (s.id == id) return s;
            return null;
        }

        public static SkinData DefaultCustom()
        {
            var c = Catalog[0].Clone();
            c.id = "custom"; c.label = "My Custom"; c.rarity = "custom"; c.price = 0;
            return c;
        }

        public static Color RarityColor(string rarity)
        {
            switch (rarity)
            {
                case "rare": return Toon.Hex("#4fa9ff");
                case "epic": return Toon.Hex("#c76bff");
                case "legend": return Toon.Hex("#ffb020");
                case "custom": return Toon.Hex("#ff6bd0");
                default: return Toon.Hex("#9aa5d6");
            }
        }

        // ------------------------------------------------------------------ bots
        static readonly System.Random rng = new System.Random();
        static T Pick<T>(T[] arr) { return arr[rng.Next(arr.Length)]; }

        public static SkinData RandomBotSkin()
        {
            if (UnityEngine.Random.value < 0.3f) return RandomCustom();
            int total = 0;
            foreach (var s in Catalog) total += BotWeight(s.rarity);
            int r = rng.Next(total);
            foreach (var s in Catalog)
            {
                r -= BotWeight(s.rarity);
                if (r < 0) return s;
            }
            return Catalog[0];
        }

        static int BotWeight(string rarity)
        {
            switch (rarity) { case "common": return 6; case "rare": return 4; case "epic": return 2; default: return 1; }
        }

        static string Hsl(float h, float s, float l)
        {
            // HSV is close enough for random colors.
            return "#" + ColorUtility.ToHtmlStringRGB(Color.HSVToRGB(Mathf.Repeat(h, 1), s, Mathf.Clamp01(l + 0.3f)));
        }

        public static SkinData RandomCustom()
        {
            float h = UnityEngine.Random.value;
            return new SkinData
            {
                id = "random",
                head = Pick(Heads)[0],
                body = Pick(Bodies)[0],
                spikes = Pick(Backs)[0],
                pattern = Pick(Patterns)[0],
                eyes = Pick(Eyes)[0],
                hat = UnityEngine.Random.value < 0.35f ? Pick(Hats)[0] : "none",
                glow = UnityEngine.Random.value < 0.2f,
                colorA = Hsl(h, 0.75f, 0.55f),
                colorB = Hsl(h + 0.08f + UnityEngine.Random.value * 0.5f, 0.7f, 0.3f + UnityEngine.Random.value * 0.3f),
                colorC = Hsl(h + 0.5f, 0.6f, 0.65f),
                eyeColor = Hsl(0.1f + UnityEngine.Random.value * 0.1f, 1f, 0.6f),
            };
        }

        static readonly string[] Names =
        {
            "Noodle", "Slinky", "Viper", "Zigzag", "Mamba", "Wiggles", "Boa", "Sssam", "Kaa", "Fang",
            "Cobra", "Hissy", "Twisty", "Python", "Nibbles", "Sidewinder", "Coily", "Basilisk", "Rattle", "Spaghetti",
            "Rachel", "Josh", "Lin", "Biggus", "Snoops", "Speedy", "Gomes", "Jinx",
        };

        public static string RandomBotName()
        {
            return Pick(Names) + (UnityEngine.Random.value < 0.4f ? rng.Next(99).ToString() : "");
        }

        // ------------------------------------------------------------------ colors along the body
        // Color of body segment i (0 = head).
        public static Color SegmentColor(SkinData skin, Color a, Color b, int i)
        {
            switch (skin.pattern)
            {
                case "stripes": return (i / 2) % 2 == 1 ? b : a;
                case "bands": return (i / 5) % 2 == 1 ? b : a;
                case "spots": return i % 4 == 2 ? b : a;
                case "gradient": return Color.Lerp(a, b, 0.5f - 0.5f * Mathf.Cos(i / 24f * Mathf.PI));
                case "rainbow":
                    float h, s, v;
                    Color.RGBToHSV(a, out h, out s, out v);
                    return Color.HSVToRGB(Mathf.Repeat(h + i * 0.03f, 1), 0.75f, 0.95f);
                default: return a;
            }
        }
    }
}
