using System.Collections.Generic;
using UnityEngine;

namespace SerpentRogue
{
    // Tiny 12x12 pixel-art icons for the power-ups, drawn from text.
    // Each letter is a color (see Palette), "." is transparent.
    // Change a picture by editing its letters, or add one for a new upgrade id.
    public static class Icons
    {
        static readonly Dictionary<char, string> Palette = new Dictionary<char, string>
        {
            { 'k', "#111111" }, { 'w', "#ffffff" }, { 'y', "#ffd23d" }, { 'o', "#ff8a1f" },
            { 'r', "#e8323c" }, { 'g', "#5ad35a" }, { 'G', "#257a2e" }, { 'b', "#2f6fe0" },
            { 'B', "#163a8a" }, { 'c', "#5fe0ff" }, { 'p', "#9b3fd8" }, { 'P', "#ff7ac8" },
            { 'n', "#8a5a2b" }, { 's', "#a7acb8" }, { 'S', "#5d6170" },
        };

        static readonly Dictionary<string, string[]> Art = new Dictionary<string, string[]>
        {
            { "speed", new[] {
                "......yyyy..",
                ".....yyyy...",
                "....yyyy....",
                "...yyyy.....",
                "..yyyyyyyy..",
                ".yyyyyyyy...",
                "......yyy...",
                ".....yyy....",
                "....yyy.....",
                "...yy.......",
                "..yy........",
                ".y..........",
            } },
            { "turn", new[] {
                "...cccccc...",
                "..cc....cc..",
                ".cc......cc.",
                ".c...cc...c.",
                ".c..c..c..c.",
                ".c..c..c..c.",
                ".c..c.cc..c.",
                ".c..c.....c.",
                ".cc..c...cc.",
                "..cc..ccccc.",
                "...cc.......",
                "....ccc.....",
            } },
            { "magnet", new[] {
                ".sss....sss.",
                ".sss....sss.",
                ".rrr....rrr.",
                ".rrr....rrr.",
                ".rrr....rrr.",
                ".rrr....rrr.",
                ".rrr....rrr.",
                ".rrrr..rrrr.",
                "..rrrrrrrr..",
                "...rrrrrr...",
                "............",
                "............",
            } },
            { "glutton", new[] {
                ".......ww...",
                "......wwww..",
                ".....wwww...",
                "....ww......",
                "...nnn......",
                "..nnnnn.....",
                ".nnooonn....",
                ".nnoooonn...",
                ".nooooonn...",
                ".nnooonn....",
                "..nnnnn.....",
                "............",
            } },
            { "scholar", new[] {
                "............",
                ".BBBBBBBBBB.",
                ".BbbbbbbbbB.",
                ".BbwwwwwwbB.",
                ".BbbbbbbbbB.",
                ".BbwwwwbbbB.",
                ".BbbbbbbbbB.",
                ".BbbbbbbbbB.",
                ".BbbbbbbbbB.",
                ".BwwwwwwwwB.",
                ".BBBBBBBBBB.",
                "............",
            } },
            { "regen", new[] {
                "..........G.",
                ".......gggG.",
                ".....gggggG.",
                "....gggggGg.",
                "...ggggGggg.",
                "...gggGgggg.",
                "..ggGggggg..",
                "..gGgggggg..",
                "..Gggggg....",
                ".G..gggg....",
                "G...........",
                "............",
            } },
            { "hawk", new[] {
                "............",
                "............",
                "...wwwwww...",
                "..ww....ww..",
                ".ww..bb..ww.",
                "ww..bkkb..ww",
                "ww..bkkb..ww",
                ".ww..bb..ww.",
                "..ww....ww..",
                "...wwwwww...",
                "............",
                "............",
            } },
            { "nitro", new[] {
                ".....ww.....",
                "....wwww....",
                "....wbbw....",
                "....wbbw....",
                "....wwww....",
                "...wwwwww...",
                "...wwwwww...",
                "..rwwwwwwr..",
                "..rr.ww.rr..",
                ".....oo.....",
                "....oyyo....",
                ".....yy.....",
            } },
            { "turbo", new[] {
                ".....o......",
                "....oo......",
                "....ooo..o..",
                "...oooo.oo..",
                "..ooooooooo.",
                "..oooyyoooo.",
                ".ooooyyyooo.",
                ".oooyyyyyoo.",
                ".ooyyyyyyoo.",
                "..oyyyyyyo..",
                "...oyyyyo...",
                "....oooo....",
            } },
            { "shield", new[] {
                ".bbbbbbbbbb.",
                ".bccccccccb.",
                ".bccccccccb.",
                ".bccwwwwccb.",
                ".bccwccwccb.",
                ".bccwccwccb.",
                ".bccccccccb.",
                "..bccccccb..",
                "..bccccccb..",
                "...bccccb...",
                "....bccb....",
                ".....bb.....",
            } },
            { "predator", new[] {
                ".....rr.....",
                ".....rr.....",
                "....rrrr....",
                "....rrrr....",
                "...rrrrrr...",
                "..rrrrrrrr..",
                "..rrwrrrrr..",
                ".rrrwrrrrrr.",
                ".rrrrrrrrrr.",
                ".rrrrrrrrrr.",
                "..rrrrrrrr..",
                "...rrrrrr...",
            } },
            { "lucky", new[] {
                ".....yy.....",
                ".....yy.....",
                "....yyyy....",
                "yyyyyyyyyyyy",
                ".yyyyyyyyyy.",
                "..yyyyyyyy..",
                "...yyyyyy...",
                "...yyyyyy...",
                "..yyy..yyy..",
                "..yy....yy..",
                ".yy......yy.",
                "............",
            } },
            { "reaper", new[] {
                "...wwwwww...",
                "..wwwwwwww..",
                ".wwwwwwwwww.",
                ".wwkkwwkkww.",
                ".wwkkwwkkww.",
                ".wwwwwwwwww.",
                "..wwwkkwww..",
                "..wwwwwwww..",
                "...wkwkwk...",
                "...wwwwww...",
                "............",
                "............",
            } },
            { "feast", new[] {
                ".....r......",
                ".....w......",
                ".....w......",
                "..PPPPPPPP..",
                ".PwwwwwwwwP.",
                ".PPPPPPPPPP.",
                ".nnnnnnnnnn.",
                ".nPPnPPnPPn.",
                ".nnnnnnnnnn.",
                ".nnnnnnnnnn.",
                ".SSSSSSSSSS.",
                "............",
            } },
            { "titan", new[] {
                "............",
                ".....SS.....",
                "....SssS....",
                "...SsssS....",
                "...SssssS...",
                "..SsssssS...",
                "..SsssssSS..",
                ".SsssssssS..",
                ".SssssssssS.",
                "SsssssssssS.",
                "SSSSSSSSSSSS",
                "............",
            } },
            { "phantom", new[] {
                "....wwww....",
                "..wwwwwwww..",
                ".wwwwwwwwww.",
                ".wwkkwwkkww.",
                ".wwkkwwkkww.",
                ".wwwwwwwwww.",
                ".wwwwkkwwww.",
                ".wwwwwwwwww.",
                ".wwwwwwwwww.",
                ".wwwwwwwwww.",
                ".ww.ww.ww.w.",
                ".w..w..w....",
            } },
            { "secondwind", new[] {
                "............",
                "..rrr..rrr..",
                ".rrrrrrrrrr.",
                "rrwrrrrrrrrr",
                "rrwrrrrrrrrr",
                "rrrrrrrrrrrr",
                ".rrrrrrrrrr.",
                "..rrrrrrrr..",
                "...rrrrrr...",
                "....rrrr....",
                ".....rr.....",
                "............",
            } },
            { "bloodrush", new[] {
                ".p........p.",
                ".pp......pp.",
                "..pppppppp..",
                ".pppppppppp.",
                ".ppyppppypp.",
                ".ppyppppypp.",
                ".pppppppppp.",
                ".ppkkkkkkpp.",
                ".pppkwwkppp.",
                "..pppppppp..",
                "...pppppp...",
                "............",
            } },
        };

        static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

        // Returns the icon texture for an upgrade id (null if there is no art for it).
        public static Texture2D Get(string id)
        {
            Texture2D tex;
            if (cache.TryGetValue(id, out tex)) return tex;
            string[] rows;
            if (!Art.TryGetValue(id, out rows)) return null;
            int n = rows.Length;
            tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    char c = x < rows[y].Length ? rows[y][x] : '.';
                    string hex;
                    // Texture rows go bottom-to-top, the text goes top-to-bottom.
                    tex.SetPixel(x, n - 1 - y, Palette.TryGetValue(c, out hex) ? Toon.Hex(hex) : Color.clear);
                }
            tex.Apply();
            cache[id] = tex;
            return tex;
        }
    }
}
