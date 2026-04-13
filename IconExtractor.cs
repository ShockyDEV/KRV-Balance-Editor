using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace BalanceEditor
{
    /// <summary>Extracts icon sub-images from sprite atlas PNGs using GDI+.</summary>
    static class IconExtractor
    {
        // Cached extracted icons
        public static Dictionary<string, Bitmap> HeroIconBitmaps = new Dictionary<string, Bitmap>();
        public static Dictionary<string, Bitmap> TowerIconBitmaps = new Dictionary<string, Bitmap>();
        public static Dictionary<string, Bitmap> EnemyIconBitmaps = new Dictionary<string, Bitmap>();
        public static Dictionary<int, Bitmap> HPIBitmaps = new Dictionary<int, Bitmap>();
        public static Dictionary<int, Bitmap> HSPIBitmaps = new Dictionary<int, Bitmap>();
        public static Dictionary<int, Bitmap> TIBitmaps = new Dictionary<int, Bitmap>();

        /// <summary>Extract a sub-image from an atlas, handling rotation.</summary>
        static Bitmap ExtractSprite(Bitmap atlas, StaticData.IconCoord ic)
        {
            if (ic.W <= 0 || ic.H <= 0) return null;
            if (ic.X + ic.W > atlas.Width || ic.Y + ic.H > atlas.Height) return null;

            if (ic.Rotated)
            {
                // Rotated: source rect is (x,y,w,h) but output is (h,w) after -90° rotation
                var bmp = new Bitmap(ic.H, ic.W);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.TranslateTransform(0, ic.W);
                    g.RotateTransform(-90);
                    g.DrawImage(atlas,
                        new Rectangle(0, 0, ic.W, ic.H),
                        new Rectangle(ic.X, ic.Y, ic.W, ic.H),
                        GraphicsUnit.Pixel);
                }
                return bmp;
            }
            else
            {
                var bmp = new Bitmap(ic.W, ic.H);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.DrawImage(atlas,
                        new Rectangle(0, 0, ic.W, ic.H),
                        new Rectangle(ic.X, ic.Y, ic.W, ic.H),
                        GraphicsUnit.Pixel);
                }
                return bmp;
            }
        }

        /// <summary>Load all icon bitmaps from the game's atlas PNGs.</summary>
        public static void ExtractAllIcons(string gameRoot)
        {
            // GUI atlas: hero + tower icons
            string guiPath = Path.Combine(gameRoot, StaticData.GuiAtlas);
            if (File.Exists(guiPath))
            {
                using (var atlas = new Bitmap(guiPath))
                {
                    foreach (var kv in StaticData.HeroIcons)
                        HeroIconBitmaps[kv.Key] = ExtractSprite(atlas, kv.Value);
                    foreach (var kv in StaticData.TowerIcons)
                        TowerIconBitmaps[kv.Key] = ExtractSprite(atlas, kv.Value);
                }
            }

            // Heroroom atlas: hero skill icons (HPI + HSPI)
            string heroPath = Path.Combine(gameRoot, StaticData.HeroroomAtlas);
            if (File.Exists(heroPath))
            {
                using (var atlas = new Bitmap(heroPath))
                {
                    foreach (var kv in StaticData.HPI)
                        HPIBitmaps[kv.Key] = ExtractSprite(atlas, kv.Value);
                    foreach (var kv in StaticData.HSPI)
                        HSPIBitmaps[kv.Key] = ExtractSprite(atlas, kv.Value);
                }
            }

            // Tower encyclopedia atlas: tower skill icons
            string tencPath = Path.Combine(gameRoot, StaticData.TowerEncAtlas);
            if (File.Exists(tencPath))
            {
                using (var atlas = new Bitmap(tencPath))
                {
                    foreach (var kv in StaticData.TI)
                        TIBitmaps[kv.Key] = ExtractSprite(atlas, kv.Value);
                }
            }

            // Enemy encyclopedia atlas: enemy portraits
            string enemyPath = Path.Combine(gameRoot, StaticData.EnemyEncAtlas);
            if (File.Exists(enemyPath))
            {
                using (var atlas = new Bitmap(enemyPath))
                {
                    foreach (var kv in StaticData.EnemyIcons)
                        EnemyIconBitmaps[kv.Key] = ExtractSprite(atlas, kv.Value);
                }
            }
        }

        /// <summary>Get hero portrait icon bitmap.</summary>
        public static Bitmap GetHeroIcon(string heroKey)
        {
            HeroIconBitmaps.TryGetValue(heroKey, out var bmp);
            return bmp;
        }

        /// <summary>Get tower icon bitmap with cascading key strip (same as JS makeTowerSprite).</summary>
        public static Bitmap GetTowerIcon(string towerKey)
        {
            // Strip _levelN(_old) suffix
            string baseKey = System.Text.RegularExpressions.Regex.Replace(towerKey, @"_level\d+(_old)?$", "");
            if (TowerIconBitmaps.TryGetValue(baseKey, out var bmp) && bmp != null) return bmp;

            // Strip named variant suffixes
            baseKey = System.Text.RegularExpressions.Regex.Replace(baseKey,
                @"_(?:blood_altar|spear_throwers|orc_dens|dark_knight|crimson_zealot|special)$", "");
            if (TowerIconBitmaps.TryGetValue(baseKey, out bmp) && bmp != null) return bmp;

            // Progressive prefix stripping
            var parts = baseKey.Split('_');
            for (int i = parts.Length; i >= 2; i--)
            {
                string pre = string.Join("_", parts, 0, i);
                if (TowerIconBitmaps.TryGetValue(pre, out bmp) && bmp != null) return bmp;
            }
            return null;
        }

        /// <summary>Get enemy portrait bitmap.</summary>
        public static Bitmap GetEnemyIcon(string enemyKey)
        {
            EnemyIconBitmaps.TryGetValue(enemyKey, out var bmp);
            return bmp;
        }

        /// <summary>Get hero skill icon bitmap.</summary>
        public static Bitmap GetHeroSkillIcon(string heroKey, string skillKey)
        {
            if (!StaticData.HeroSkillIcons.TryGetValue(heroKey, out var skills)) return null;
            if (!skills.TryGetValue(skillKey, out var poolId)) return null;
            // poolId[0] = 0 for HPI, 1 for HSPI; poolId[1] = index
            var pool = poolId[0] == 0 ? HPIBitmaps : HSPIBitmaps;
            pool.TryGetValue(poolId[1], out var bmp);
            return bmp;
        }

        /// <summary>Get tower skill icon bitmap.</summary>
        public static Bitmap GetTowerSkillIcon(string towerKey, string actionKey)
        {
            if (!StaticData.TowerSkillIcons.TryGetValue(towerKey, out var skills)) return null;
            if (!skills.TryGetValue(actionKey, out int tiIdx)) return null;
            TIBitmaps.TryGetValue(tiIdx, out var bmp);
            return bmp;
        }
    }
}
