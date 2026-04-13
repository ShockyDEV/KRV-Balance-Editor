using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BalanceEditor
{
    /// <summary>Loads localization strings and provides skill description lookup.</summary>
    static class Localization
    {
        static Dictionary<string, string> _strings = new Dictionary<string, string>();

        // ── Hero localization key prefixes ──
        static readonly Dictionary<string, string[]> HeroLoc = new Dictionary<string, string[]>
        {
            ["hero_orc"] = new[] { "VERUK" }, ["hero_asra"] = new[] { "ASRA" },
            ["hero_oloch"] = new[] { "OLOCH" }, ["hero_mortemis"] = new[] { "MORTEMIS" },
            ["hero_tramin"] = new[] { "TRAMIN", "TRAMIS" }, ["hero_margosa"] = new[] { "MARGOSA" },
            ["hero_jigou"] = new[] { "JIGOU" }, ["hero_beresad"] = new[] { "BERESAD" },
            ["hero_tank"] = new[] { "TANK" }, ["hero_naga"] = new[] { "NAGA" },
            ["hero_eiskalt"] = new[] { "EISKALT" }, ["hero_murglun"] = new[] { "MURGLUN" },
            ["hero_jack_o_lantern"] = new[] { "JACKO" }, ["hero_dianyun"] = new[] { "DIANYUN" },
            ["hero_isfet"] = new[] { "ISFET" }, ["hero_lucerna"] = new[] { "LUCERNA" },
            ["hero_mammoth"] = new[] { "MAMMOTH" },
        };

        // ── Hero skill key overrides ──
        static readonly Dictionary<string, Dictionary<string, string>> HeroSkOvr = new Dictionary<string, Dictionary<string, string>>
        {
            ["hero_dianyun"] = new Dictionary<string, string> { ["range_dinayun_ricochet"] = "LIGHTNING_STRIKE" },
            ["hero_eiskalt"] = new Dictionary<string, string> { ["hero_eiskalt_ice_ball"] = "FROSTY" },
            ["hero_isfet"] = new Dictionary<string, string> { ["ultimate"] = "DARKNESS_STORM" },
            ["hero_lucerna"] = new Dictionary<string, string> { ["scurvy_vissage"] = "CURSED_COLORS" },
            ["hero_mammoth"] = new Dictionary<string, string> { ["ancestral_force"] = "FISSURE" },
            ["hero_naga"] = new Dictionary<string, string> { ["banner_courage"] = "BANNER_ALLIES" },
            ["hero_orc"] = new Dictionary<string, string> { ["duelist"] = "PASSIVE" },
        };

        // ── Tower localization prefixes ──
        static readonly Dictionary<string, string> TowerLoc = new Dictionary<string, string>
        {
            ["warmongers_heat_balloon_special"] = "WARMONGER_BALLOON",
            ["warmongers_barrack_orc_dens"] = "WARMONGER_BARRACK",
            ["warmongers_mage_blood_altar"] = "WARMONGER_MAGE",
            ["warmongers_rocket_level4"] = "WARMONGER_ROCKET",
            ["warmongers_archer_spear_throwers"] = "WARMONGER_ARCHER",
            ["dark_army_barrack_dark_knight"] = "DARK_ARMY_BARRACK",
            ["dark_army_archer_level4"] = "DARK_ARMY_ARCHER",
            ["dark_army_melting_furnace_level4"] = "DARK_ARMY_MELTING_FURNACE",
            ["dark_army_blazing_watcher_level4"] = "DARK_ARMY_BLAZING_WATCHER",
            ["ember_lords_mage_level4"] = "EMBER_LORDS_MAGE",
            ["fallen_ones_spirit_mausoleum_level4"] = "FALLEN_ONES_SPIRITS",
            ["fallen_ones_grim_cemetery_level4"] = "FALLEN_ONES_CEMETERY",
            ["fallen_ones_bone_flingers_level4"] = "FALLEN_ONES_BONE_FLINGERS",
            ["rotten_forest_level4"] = "ROTTEN_FOREST",
            ["wicked_sisters_level4"] = "WICKED_SISTERS",
            ["elves_barrack_level4"] = "ELVES_BARRACK",
            ["deep_devils_reef_level4"] = "DEEP_DEVILS",
            ["shaolin_temple_level4"] = "SHAOLIN_TEMPLE",
            ["swamp_monster_level4"] = "SWAMP_MONSTER",
            ["dinos_ignis_altar_level4"] = "DINOS_IGNIS_ALTAR",
            ["sandstorm_tremor_level4"] = "TREMOR",
            ["pirates_ogres_level4"] = "OGRES",
        };

        /// <summary>Load localization strings from the Localized_en file.</summary>
        public static void LoadStrings(string filepath)
        {
            _strings.Clear();
            if (!File.Exists(filepath)) return;
            foreach (var rawLine in File.ReadLines(filepath, System.Text.Encoding.UTF8))
            {
                var line = rawLine.Trim();
                int eq = line.IndexOf(" = ");
                if (eq < 0) continue;
                string key = line.Substring(0, eq).Trim();
                if (!key.Contains("DESCRIPTION")) continue;
                string val = line.Substring(eq + 3).Trim();
                if (val.StartsWith("\"") && val.EndsWith("\"") && val.Length >= 2)
                    val = val.Substring(1, val.Length - 2);
                _strings[key] = val;
            }
        }

        /// <summary>Find a hero skill description.</summary>
        public static string FindHeroSkillDesc(string heroKey, string skillKey)
        {
            if (_strings.Count == 0) return null;
            if (!HeroLoc.TryGetValue(heroKey, out var locs)) return null;

            // Check override first
            if (HeroSkOvr.TryGetValue(heroKey, out var ovr) && ovr.TryGetValue(skillKey, out var ovrKey))
            {
                foreach (var loc in locs)
                {
                    string k = $"HERO_{loc}_SKILL_{ovrKey}_1_DESCRIPTION";
                    if (_strings.TryGetValue(k, out var v)) return v;
                }
            }

            string sk = skillKey.ToUpperInvariant();
            foreach (var loc in locs)
            {
                string pre = $"HERO_{loc}_SKILL_";

                // Direct match
                if (_strings.TryGetValue(pre + sk + "_1_DESCRIPTION", out var v1)) return v1;

                // Strip trailing S
                string skNoS = sk.EndsWith("S") ? sk.Substring(0, sk.Length - 1) : sk;
                if (_strings.TryGetValue(pre + skNoS + "_1_DESCRIPTION", out var v2)) return v2;

                // Strip hero-name prefixes
                string hn = heroKey.Replace("hero_", "").ToUpperInvariant();
                string[] prefixes = { hn + "_", "HERO_" + hn + "_", "RANGE_" + hn.Split('_')[0] + "_", "RANGE_", "FORWARD_" };
                foreach (var pfx in prefixes)
                {
                    if (sk.StartsWith(pfx))
                    {
                        string rest = sk.Substring(pfx.Length);
                        if (_strings.TryGetValue(pre + rest + "_1_DESCRIPTION", out var v3)) return v3;
                        string restNoS = rest.EndsWith("S") ? rest.Substring(0, rest.Length - 1) : rest;
                        if (_strings.TryGetValue(pre + restNoS + "_1_DESCRIPTION", out var v4)) return v4;
                    }
                }

                // Simplified key (remove _OF_THE_ and _OF_)
                string simp = sk.Replace("_OF_THE_", "_").Replace("_OF_", "_");
                if (_strings.TryGetValue(pre + simp + "_1_DESCRIPTION", out var v5)) return v5;

                // Fuzzy match
                foreach (var kv in _strings)
                {
                    if (kv.Key.StartsWith(pre) && kv.Key.EndsWith("_1_DESCRIPTION"))
                    {
                        string part = kv.Key.Substring(pre.Length, kv.Key.Length - pre.Length - 14);
                        if (sk.Contains(part) || part.Contains(sk)) return kv.Value;
                    }
                }
            }
            return null;
        }

        /// <summary>Find a tower skill description.</summary>
        public static string FindTowerSkillDesc(string towerKey, string skillKey)
        {
            if (_strings.Count == 0) return null;
            if (!TowerLoc.TryGetValue(towerKey, out var locT)) return null;

            string pre = $"TOWER_{locT}_LEVEL4_";
            string sk = skillKey.ToUpperInvariant();

            string TryKey(string name)
            {
                if (_strings.TryGetValue(pre + name + "_DESCRIPTION", out var v)) return v;
                if (_strings.TryGetValue(pre + name + "_DESCRIPTION_1", out var v2)) return v2;
                return null;
            }

            string[] prefixes = { "FORWARD_", "FOWARD_", "RANGE_", "PASSIVE_", "UNIT_", "SPAWN_", "SINGLE_", "" };
            foreach (var pfx in prefixes)
            {
                if (pfx == "" || sk.StartsWith(pfx))
                {
                    string rest = pfx == "" ? sk : sk.Substring(pfx.Length);
                    if (string.IsNullOrEmpty(rest)) continue;
                    var r = TryKey(rest);
                    if (r != null) return r;
                    string restNoS = rest.EndsWith("S") ? rest.Substring(0, rest.Length - 1) : rest;
                    r = TryKey(restNoS);
                    if (r != null) return r;
                }
            }

            // Fuzzy match
            foreach (var kv in _strings)
            {
                if (kv.Key.StartsWith(pre) && (kv.Key.EndsWith("_DESCRIPTION") || kv.Key.EndsWith("_DESCRIPTION_1")))
                {
                    string part = kv.Key.Substring(pre.Length);
                    part = part.Replace("_DESCRIPTION_1", "").Replace("_DESCRIPTION", "").ToLowerInvariant();
                    string skl = skillKey.ToLowerInvariant();
                    skl = System.Text.RegularExpressions.Regex.Replace(skl, @"^(forward|foward|range|passive|unit|spawn)_", "");
                    if (skl.Contains(part) || part.Contains(skl)) return kv.Value;
                }
            }
            return null;
        }
    }
}
