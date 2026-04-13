using System.Collections.Generic;
using System.Drawing;

namespace BalanceEditor
{
    /// <summary>Icon coordinates, hero names, localization keys, SKIP set, faction colors.</summary>
    static class StaticData
    {
        public struct IconCoord
        {
            public int X, Y, W, H;
            public bool Rotated;
            public IconCoord(int x, int y, int w, int h, bool r = false) { X = x; Y = y; W = w; H = h; Rotated = r; }
        }

        // ── Hero bottom-info icons (100x98 each, from kr4_gui.png) ──
        public static readonly Dictionary<string, IconCoord> HeroIcons = new Dictionary<string, IconCoord>
        {
            ["hero_orc"] = new IconCoord(3509, 1501, 100, 98),
            ["hero_asra"] = new IconCoord(3507, 1603, 100, 98),
            ["hero_oloch"] = new IconCoord(3507, 1705, 100, 98),
            ["hero_mortemis"] = new IconCoord(3503, 1807, 100, 98),
            ["hero_tramin"] = new IconCoord(3503, 1909, 100, 98),
            ["hero_margosa"] = new IconCoord(3503, 2011, 100, 98),
            ["hero_jigou"] = new IconCoord(3503, 2113, 100, 98),
            ["hero_beresad"] = new IconCoord(3686, 889, 100, 98),
            ["hero_tank"] = new IconCoord(3559, 2317, 100, 98),
            ["hero_naga"] = new IconCoord(3663, 2317, 100, 98),
            ["hero_eiskalt"] = new IconCoord(3620, 991, 100, 98),
            ["hero_murglun"] = new IconCoord(3695, 2419, 100, 98),
            ["hero_jack_o_lantern"] = new IconCoord(3615, 1297, 100, 98),
            ["hero_dianyun"] = new IconCoord(3615, 1195, 100, 98),
            ["hero_mammoth"] = new IconCoord(3615, 1093, 100, 98),
            ["hero_isfet"] = new IconCoord(3613, 1399, 100, 98),
            ["hero_lucerna"] = new IconCoord(3613, 1501, 100, 98),
        };

        // ── Tower bottom-info icons (100x98 each, from kr4_gui.png) ──
        public static readonly Dictionary<string, IconCoord> TowerIcons = new Dictionary<string, IconCoord>
        {
            ["warmongers_barrack"] = new IconCoord(3908, 695, 100, 98),
            ["warmongers_archer"] = new IconCoord(3790, 785, 100, 98),
            ["warmongers_rocket"] = new IconCoord(3790, 887, 100, 98, true),
            ["warmongers_heat_balloon"] = new IconCoord(3894, 797, 100, 98),
            ["warmongers_mage"] = new IconCoord(3821, 1409, 100, 98),
            ["ember_lords_mage"] = new IconCoord(3892, 899, 100, 98),
            ["dark_army_archer"] = new IconCoord(3828, 1001, 100, 98),
            ["dark_army_melting_furnace"] = new IconCoord(3823, 1103, 100, 98),
            ["fallen_ones_spirit_mausoleum"] = new IconCoord(3823, 1205, 100, 98),
            ["dark_army_barrack"] = new IconCoord(3823, 1307, 100, 98),
            ["fallen_ones_grim_cemetery"] = new IconCoord(3821, 1511, 100, 98),
            ["fallen_ones_bone_flingers"] = new IconCoord(3819, 1613, 100, 98),
            ["dark_army_blazing_watcher"] = new IconCoord(3819, 1715, 100, 98),
            ["mercenary_troll_hut"] = new IconCoord(3815, 1817, 100, 98),
            ["spider_nest"] = new IconCoord(3815, 1919, 100, 98),
            ["rotten_forest"] = new IconCoord(3815, 2021, 100, 98),
            ["wicked_sisters"] = new IconCoord(3932, 1001, 100, 98),
            ["dark_army_mage"] = new IconCoord(2764, 581, 100, 98),
            ["elves_barrack"] = new IconCoord(3927, 1307, 100, 98),
            ["deep_devils_reef"] = new IconCoord(3925, 1409, 100, 98),
            ["shaolin_temple"] = new IconCoord(3925, 1511, 100, 98),
            ["swamp_monster"] = new IconCoord(3923, 1613, 100, 98),
            ["dinos_ignis_altar"] = new IconCoord(3923, 1715, 100, 98),
            ["sandstorm_tremor"] = new IconCoord(3919, 1817, 100, 98),
            ["pirates_ogres"] = new IconCoord(3919, 2021, 100, 98),
        };

        // ── Hero portrait icons (heroroom atlas) ──
        public static readonly Dictionary<int, IconCoord> HPI = new Dictionary<int, IconCoord>
        {
            [1] = new IconCoord(2411, 2979, 132, 130),
            [2] = new IconCoord(2507, 1921, 132, 130),
            [3] = new IconCoord(2791, 1667, 132, 130, true),
            [4] = new IconCoord(2877, 2073, 132, 130),
            [5] = new IconCoord(3339, 1699, 132, 130, true),
            [6] = new IconCoord(3013, 2103, 132, 128),
            [7] = new IconCoord(3149, 2105, 132, 128),
            [8] = new IconCoord(3285, 2105, 132, 128),
            [9] = new IconCoord(2687, 2207, 132, 128),
            [10] = new IconCoord(2823, 2207, 132, 128),
            [11] = new IconCoord(2551, 2339, 132, 128),
            [12] = new IconCoord(2687, 2339, 132, 128),
            [13] = new IconCoord(3339, 1835, 132, 130, true),
            [14] = new IconCoord(3333, 1971, 132, 130),
            [15] = new IconCoord(2551, 2205, 132, 130),
            [16] = new IconCoord(2823, 2339, 132, 130),
            [17] = new IconCoord(2545, 2471, 132, 130),
            [18] = new IconCoord(2545, 2605, 132, 130),
            [19] = new IconCoord(2681, 2471, 132, 130),
            [20] = new IconCoord(2545, 2739, 132, 130),
            [21] = new IconCoord(2681, 2605, 132, 130),
            [22] = new IconCoord(2681, 2739, 132, 130),
            [23] = new IconCoord(2817, 2473, 132, 130),
            [24] = new IconCoord(2817, 2607, 132, 130),
            [25] = new IconCoord(2817, 2741, 132, 130),
            [26] = new IconCoord(2959, 2235, 132, 130),
            [27] = new IconCoord(2959, 2369, 132, 130),
            [28] = new IconCoord(2953, 2503, 132, 130),
            [29] = new IconCoord(2953, 2637, 132, 130),
            [30] = new IconCoord(2953, 2771, 132, 130),
            [31] = new IconCoord(3095, 2237, 132, 130),
            [32] = new IconCoord(3231, 2237, 132, 130),
            [33] = new IconCoord(3095, 2371, 132, 130),
            [34] = new IconCoord(3231, 2371, 132, 130),
            [35] = new IconCoord(3089, 2505, 132, 130),
            [36] = new IconCoord(3089, 2639, 132, 130),
            [37] = new IconCoord(3225, 2505, 132, 130),
            [38] = new IconCoord(3225, 2639, 132, 130),
            [39] = new IconCoord(3089, 2773, 132, 130),
            [40] = new IconCoord(3225, 2773, 132, 130),
            [41] = new IconCoord(2547, 2873, 132, 130),
            [42] = new IconCoord(2683, 2873, 132, 130, true),
            [43] = new IconCoord(2817, 2875, 132, 130),
            [44] = new IconCoord(2547, 3007, 132, 130),
            [45] = new IconCoord(2683, 3009, 132, 130),
            [46] = new IconCoord(555, 2985, 134, 132),
            [47] = new IconCoord(2645, 1531, 134, 132),
            [48] = new IconCoord(2783, 1531, 134, 132),
            [49] = new IconCoord(2399, 1649, 134, 130),
            [50] = new IconCoord(2369, 1783, 134, 132),
            [51] = new IconCoord(2369, 1919, 134, 132),
            [52] = new IconCoord(2507, 1783, 134, 132, true),
            [53] = new IconCoord(2653, 1667, 134, 132),
            [54] = new IconCoord(2643, 1939, 134, 130),
            [55] = new IconCoord(2781, 1939, 134, 130),
            [56] = new IconCoord(2601, 2073, 134, 128),
            [57] = new IconCoord(2739, 2073, 134, 130),
            [58] = new IconCoord(2643, 1803, 134, 132),
            [59] = new IconCoord(2781, 1803, 134, 132),
            [60] = new IconCoord(2463, 2055, 134, 132),
            [61] = new IconCoord(2925, 1643, 134, 132),
            [62] = new IconCoord(3063, 1699, 134, 130),
            [63] = new IconCoord(3201, 1699, 134, 130),
            [64] = new IconCoord(2925, 1779, 134, 130),
            [65] = new IconCoord(3063, 1833, 134, 130),
            [66] = new IconCoord(2919, 1913, 134, 132),
            [67] = new IconCoord(3201, 1833, 134, 132),
            [68] = new IconCoord(3057, 1967, 134, 132),
            [69] = new IconCoord(3195, 1969, 134, 132),
        };

        // ── Hero skill portrait icons (heroroom atlas, larger) ──
        public static readonly Dictionary<int, IconCoord> HSPI = new Dictionary<int, IconCoord>
        {
            [1] = new IconCoord(2329, 2115, 142, 130, true),
            [2] = new IconCoord(2271, 2261, 142, 130),
            [3] = new IconCoord(2265, 2395, 142, 130),
            [4] = new IconCoord(2265, 2529, 142, 130),
            [5] = new IconCoord(2277, 2663, 142, 130, true),
            [6] = new IconCoord(2277, 2809, 142, 130, true),
            [7] = new IconCoord(2411, 2395, 142, 130, true),
            [8] = new IconCoord(2411, 2541, 142, 130, true),
            [9] = new IconCoord(2411, 2687, 142, 130, true),
            [10] = new IconCoord(2411, 2833, 142, 130, true),
            [11] = new IconCoord(1255, 2967, 142, 130, true),
            [12] = new IconCoord(1933, 2371, 180, 160, true),
            [13] = new IconCoord(2041, 1791, 180, 160, true),
            [14] = new IconCoord(2183, 2115, 142, 138),
            [15] = new IconCoord(2265, 1393, 142, 130, true),
            [16] = new IconCoord(2265, 1539, 142, 130, true),
            [17] = new IconCoord(2399, 1515, 142, 130),
        };

        // ── Hero skill icons mapping ──
        public static readonly Dictionary<string, Dictionary<string, int[]>> HeroSkillIcons = new Dictionary<string, Dictionary<string, int[]>>
        {
            // format: skill_key -> [pool, id] where pool 0 = HPI, pool 1 = HSPI
            ["hero_orc"] = new Dictionary<string, int[]>
            {
                ["aimed_slashes"] = new[] { 0, 2 }, ["brute_force"] = new[] { 0, 3 },
                ["duelist"] = new[] { 0, 4 }, ["inspiring_leader"] = new[] { 0, 1 }, ["ultimate"] = new[] { 1, 1 },
            },
            ["hero_asra"] = new Dictionary<string, int[]>
            {
                ["spider_bite"] = new[] { 0, 5 }, ["onix_arrows"] = new[] { 0, 6 },
                ["quiver_of_sorrow"] = new[] { 0, 7 }, ["shield_of_shadows"] = new[] { 0, 8 }, ["ultimate"] = new[] { 1, 2 },
            },
            ["hero_oloch"] = new Dictionary<string, int[]>
            {
                ["duplication"] = new[] { 0, 12 }, ["magma_eruption"] = new[] { 0, 10 },
                ["hellish_infusion"] = new[] { 0, 11 }, ["demonic_blast"] = new[] { 0, 9 }, ["ultimate"] = new[] { 1, 3 },
            },
            ["hero_mortemis"] = new Dictionary<string, int[]>
            {
                ["call_of_the_haunted"] = new[] { 0, 25 }, ["deadly_fumes"] = new[] { 0, 26 },
                ["grim_presence"] = new[] { 0, 27 }, ["undead_servitude"] = new[] { 0, 28 }, ["ultimate"] = new[] { 1, 4 },
            },
            ["hero_margosa"] = new Dictionary<string, int[]>
            {
                ["bat_familiar"] = new[] { 0, 21 }, ["myst_form"] = new[] { 0, 22 },
                ["dark_call"] = new[] { 0, 23 }, ["vampiric_touch"] = new[] { 0, 24 }, ["ultimate"] = new[] { 1, 5 },
            },
            ["hero_tramin"] = new Dictionary<string, int[]>
            {
                ["hero_tramin_bombots"] = new[] { 0, 13 }, ["nitro_rush"] = new[] { 0, 14 },
                ["flashbang"] = new[] { 0, 15 }, ["rocket_barrage"] = new[] { 0, 16 }, ["ultimate"] = new[] { 1, 6 },
            },
            ["hero_jigou"] = new Dictionary<string, int[]>
            {
                ["ice_shard"] = new[] { 0, 17 }, ["frozen_breath"] = new[] { 0, 18 },
                ["jigou_earthshake"] = new[] { 0, 19 }, ["glacial_form"] = new[] { 0, 20 }, ["ultimate"] = new[] { 1, 7 },
            },
            ["hero_beresad"] = new Dictionary<string, int[]>
            {
                ["conflagration"] = new[] { 0, 34 }, ["fear_dragon"] = new[] { 0, 35 },
                ["dragon_spawn"] = new[] { 0, 36 }, ["remove_existence"] = new[] { 0, 37 }, ["ultimate"] = new[] { 1, 8 },
            },
            ["hero_tank"] = new Dictionary<string, int[]>
            {
                ["heat_missiles"] = new[] { 0, 29 }, ["ground_slam"] = new[] { 0, 30 },
                ["expendables"] = new[] { 0, 32 }, ["scorching_cannon"] = new[] { 0, 33 }, ["ultimate"] = new[] { 1, 9 },
            },
            ["hero_naga"] = new Dictionary<string, int[]>
            {
                ["tidal_wave"] = new[] { 0, 41 }, ["banner_courage"] = new[] { 0, 39 },
                ["gaze_of_the_naga"] = new[] { 0, 40 }, ["tri_splash"] = new[] { 0, 38 }, ["ultimate"] = new[] { 1, 10 },
            },
            ["hero_eiskalt"] = new Dictionary<string, int[]>
            {
                ["fierce_breath"] = new[] { 0, 47 }, ["eiskalt_cold_fury"] = new[] { 0, 49 },
                ["hero_eiskalt_ice_ball"] = new[] { 0, 46 }, ["eiskalt_ice_peaks"] = new[] { 0, 48 }, ["ultimate"] = new[] { 1, 12 },
            },
            ["hero_murglun"] = new Dictionary<string, int[]>
            {
                ["magma_pool"] = new[] { 0, 42 }, ["tar_maker"] = new[] { 0, 43 },
                ["murglun_geyser"] = new[] { 0, 45 }, ["murglun_infernal_heat"] = new[] { 0, 44 }, ["ultimate"] = new[] { 1, 11 },
            },
            ["hero_dianyun"] = new Dictionary<string, int[]>
            {
                ["range_dinayun_ricochet"] = new[] { 0, 53 }, ["lord_storm"] = new[] { 0, 52 },
                ["dianyun_divine_rain"] = new[] { 0, 51 }, ["supreme_wave"] = new[] { 0, 50 }, ["ultimate"] = new[] { 1, 13 },
            },
            ["hero_mammoth"] = new Dictionary<string, int[]>
            {
                ["ancestral_force"] = new[] { 0, 58 }, ["frenzy"] = new[] { 0, 59 },
                ["whirlwind"] = new[] { 0, 60 }, ["legacy"] = new[] { 0, 61 }, ["ultimate"] = new[] { 1, 15 },
            },
            ["hero_jack_o_lantern"] = new Dictionary<string, int[]>
            {
                ["explosive_head"] = new[] { 0, 54 }, ["haunted_blade"] = new[] { 0, 55 },
                ["spirit_vengeance"] = new[] { 0, 57 }, ["hero_jacko_thriller"] = new[] { 0, 56 }, ["ultimate"] = new[] { 1, 14 },
            },
            ["hero_isfet"] = new Dictionary<string, int[]>
            {
                ["black_cloud"] = new[] { 0, 65 }, ["frog_curse"] = new[] { 0, 63 },
                ["rain_of_fire_ice"] = new[] { 0, 64 }, ["blood_pool"] = new[] { 0, 62 }, ["ultimate"] = new[] { 1, 16 },
            },
            ["hero_lucerna"] = new Dictionary<string, int[]>
            {
                ["scurvy_vissage"] = new[] { 0, 66 }, ["fire_at_will"] = new[] { 0, 67 },
                ["damned_crew"] = new[] { 0, 68 }, ["pirates_pillage"] = new[] { 0, 69 }, ["ultimate"] = new[] { 1, 17 },
            },
        };

        // ── Tower skill icons (encyclopedia atlas) ──
        public static readonly Dictionary<int, IconCoord> TI = new Dictionary<int, IconCoord>
        {
            [2] = new IconCoord(3804, 1, 116, 118),
            [3] = new IconCoord(2047, 2346, 116, 118),
            [4] = new IconCoord(3804, 123, 116, 118),
            [5] = new IconCoord(3924, 1, 116, 118),
            [6] = new IconCoord(2047, 2468, 116, 118),
            [7] = new IconCoord(2167, 2346, 116, 118),
            [8] = new IconCoord(3924, 123, 116, 118),
            [9] = new IconCoord(2167, 2468, 116, 118),
            [10] = new IconCoord(2287, 2346, 116, 118),
            [11] = new IconCoord(2287, 2468, 116, 118),
            [12] = new IconCoord(2407, 2346, 116, 118),
            [13] = new IconCoord(2407, 2468, 116, 118),
            [14] = new IconCoord(2527, 2346, 116, 118),
            [15] = new IconCoord(2527, 2468, 116, 118),
            [16] = new IconCoord(2647, 2346, 116, 118),
            [17] = new IconCoord(2647, 2468, 116, 118),
            [18] = new IconCoord(2767, 2346, 116, 118),
            [19] = new IconCoord(2767, 2468, 116, 118),
            [20] = new IconCoord(2887, 2346, 116, 118),
            [21] = new IconCoord(2887, 2468, 116, 118),
            [22] = new IconCoord(3007, 2346, 116, 118),
            [23] = new IconCoord(3007, 2468, 116, 118),
            [24] = new IconCoord(3127, 2346, 116, 118),
            [25] = new IconCoord(3127, 2468, 116, 118),
            [26] = new IconCoord(3247, 2346, 116, 118),
            [27] = new IconCoord(3633, 2590, 116, 116),
            [28] = new IconCoord(3247, 2468, 116, 118),
            [29] = new IconCoord(3367, 2346, 116, 118),
            [30] = new IconCoord(3367, 2468, 116, 118),
            [31] = new IconCoord(3487, 2346, 116, 118),
            [32] = new IconCoord(3487, 2468, 116, 118),
            [33] = new IconCoord(3607, 2346, 116, 118),
            [34] = new IconCoord(3607, 2468, 116, 118),
            [35] = new IconCoord(3727, 2382, 116, 118),
            [36] = new IconCoord(3847, 2382, 116, 118),
            [37] = new IconCoord(2047, 2590, 116, 118, true),
            [38] = new IconCoord(2169, 2590, 116, 118, true),
            [39] = new IconCoord(2291, 2590, 116, 118, true),
            [40] = new IconCoord(2413, 2590, 116, 118, true),
            [41] = new IconCoord(2535, 2590, 116, 118, true),
            [42] = new IconCoord(2657, 2590, 116, 118, true),
            [43] = new IconCoord(2779, 2590, 116, 118, true),
            [44] = new IconCoord(2901, 2590, 116, 118, true),
            [45] = new IconCoord(3023, 2590, 116, 118, true),
            [46] = new IconCoord(3145, 2590, 116, 118, true),
            [47] = new IconCoord(3267, 2590, 116, 118, true),
            [48] = new IconCoord(3389, 2590, 116, 118, true),
            [49] = new IconCoord(3511, 2590, 116, 118, true),
            [50] = new IconCoord(683, 2681, 116, 118, true),
            [51] = new IconCoord(805, 2681, 116, 118, true),
            [52] = new IconCoord(927, 2681, 116, 118, true),
            [53] = new IconCoord(1049, 2681, 116, 118, true),
            [54] = new IconCoord(1171, 2681, 116, 118, true),
            [55] = new IconCoord(1293, 2681, 116, 118, true),
            [56] = new IconCoord(1415, 2681, 116, 118, true),
            [57] = new IconCoord(2757, 2710, 116, 116),
            [58] = new IconCoord(1537, 2681, 116, 118, true),
            [59] = new IconCoord(1659, 2681, 116, 118, true),
            [60] = new IconCoord(1781, 2681, 116, 118, true),
            [61] = new IconCoord(1903, 2681, 116, 118, true),
            [62] = new IconCoord(2025, 2710, 116, 118, true),
            [63] = new IconCoord(2147, 2710, 116, 118, true),
            [64] = new IconCoord(2269, 2710, 116, 118, true),
            [65] = new IconCoord(3837, 2710, 114, 112, true),
            [66] = new IconCoord(2391, 2710, 116, 118, true),
            [67] = new IconCoord(2513, 2710, 116, 118, true),
            [68] = new IconCoord(2635, 2710, 116, 118, true),
            [69] = new IconCoord(2877, 2710, 116, 116),
            [70] = new IconCoord(2997, 2710, 116, 116),
            [71] = new IconCoord(3117, 2710, 116, 116),
            [72] = new IconCoord(3237, 2710, 116, 116),
            [73] = new IconCoord(3357, 2710, 116, 116),
            [74] = new IconCoord(3477, 2710, 116, 116),
            [77] = new IconCoord(3597, 2710, 116, 116),
            [78] = new IconCoord(3717, 2710, 116, 116),
            [79] = new IconCoord(3753, 2590, 116, 116),
        };

        // ── Tower skill icon mapping (tower_key -> skill_key -> TI index) ──
        public static readonly Dictionary<string, Dictionary<string, int>> TowerSkillIcons = new Dictionary<string, Dictionary<string, int>>
        {
            ["warmongers_heat_balloon_special"] = new Dictionary<string, int> { ["forward_aura"] = 16, ["forward_parachute"] = 15, ["forward_splash"] = 14 },
            ["warmongers_barrack_orc_dens"] = new Dictionary<string, int> { ["forward_damage_multiplier"] = 5, ["forward_regeneration"] = 7, ["unit_swap"] = 6 },
            ["warmongers_mage_blood_altar"] = new Dictionary<string, int> { ["range_main"] = 4, ["range_object"] = 3, ["range_object_healing_roots"] = 2 },
            ["warmongers_rocket_level4"] = new Dictionary<string, int> { ["forward_mine"] = 13, ["range_cluster"] = 11, ["range_special"] = 12 },
            ["warmongers_archer_spear_throwers"] = new Dictionary<string, int> { ["forward_big_boomerang"] = 10, ["forward_honey_range"] = 8, ["stun"] = 9 },
            ["dark_army_barrack_dark_knight"] = new Dictionary<string, int> { ["foward_instakill"] = 29, ["foward_shield"] = 28, ["foward_spiked_armor"] = 30 },
            ["dark_army_archer_level4"] = new Dictionary<string, int> { ["forward_instakill"] = 19, ["forward_pet"] = 21, ["forward_range_unit_shadow_mark"] = 20 },
            ["dark_army_mage_crimson_zealot"] = new Dictionary<string, int> { ["range_modifier"] = 3, ["range_object"] = 2, ["unit_attach"] = 4 },
            ["dark_army_melting_furnace_level4"] = new Dictionary<string, int> { ["abrasive_heat"] = 33, ["melting_furnace_burning_fuel"] = 31, ["range_fissure"] = 32 },
            ["dark_army_dreadknight_level4"] = new Dictionary<string, int> { ["forward_dodge"] = 7, ["forward_specialAttack"] = 5, ["forward_stun"] = 6 },
            ["dark_army_blazing_watcher_level4"] = new Dictionary<string, int> { ["blazing_watcher_charged_blast"] = 42, ["inner_volatily"] = 41, ["range_locked"] = 40 },
            ["ember_lords_mage_level4"] = new Dictionary<string, int> { ["forward_object_on_target_affliction"] = 23, ["forward_object_on_target_infernal_portal"] = 24, ["forward_object_on_target_overcharge"] = 22 },
            ["fallen_ones_spirit_mausoleum_level4"] = new Dictionary<string, int> { ["range"] = 27, ["spirit_mausoleum_possesion"] = 25, ["unit_attach"] = 26 },
            ["fallen_ones_grim_cemetery_level4"] = new Dictionary<string, int> { ["forward_necromancer_bloated_zombies"] = 36, ["necromancer_tower"] = 34, ["passive_range_object"] = 35 },
            ["fallen_ones_bone_flingers_level4"] = new Dictionary<string, int> { ["range"] = 39, ["unit_attach"] = 37, ["unit_spawner"] = 38 },
            ["rotten_forest_level4"] = new Dictionary<string, int> { ["range_modifier_passive"] = 48, ["range_modifier_passive_fog"] = 49, ["spawn_unit_on_target"] = 50 },
            ["wicked_sisters_level4"] = new Dictionary<string, int> { ["forward_froggifcation"] = 46, ["passive_range_object"] = 45, ["rally"] = 47 },
            ["elves_barrack_level4"] = new Dictionary<string, int> { ["forward_dodge"] = 52, ["forward_multishoot"] = 51, ["forward_spawn_on_death"] = 53 },
            ["deep_devils_reef_level4"] = new Dictionary<string, int> { ["forward_deep_devils_armor"] = 60, ["forward_deep_devils_reef_tower_redspine_net"] = 59, ["forward_deep_devils_reef_tower_shooter_cloud"] = 58 },
            ["shaolin_temple_level4"] = new Dictionary<string, int> { ["shaolin_abundance"] = 62, ["shaolin_monks"] = 64, ["unit_attach_dragon_warrior"] = 63 },
            ["swamp_monster_level4"] = new Dictionary<string, int> { ["foward_carnivore"] = 68, ["foward_instakill"] = 67, ["foward_stun"] = 66 },
            ["dinos_ignis_altar_level4"] = new Dictionary<string, int> { ["range_upgraded"] = 70, ["single_extinction"] = 71, ["unit_attach_ignis_altar"] = 69 },
            ["sandstorm_tremor_level4"] = new Dictionary<string, int> { ["range_spit"] = 74, ["tremor_instakill"] = 73, ["tremor_spawn"] = 72 },
            ["pirates_ogres_level4"] = new Dictionary<string, int> { ["forward_goblin_launcher"] = 79, ["forward_ogre_multishoot"] = 78, ["forward_ogres_armor"] = 77 },
        };

        // ── Enemy portrait icons (338x331 each, from kr4_encyclopedia_enemies_pc.png) ──
        public static readonly Dictionary<string, IconCoord> EnemyIcons = new Dictionary<string, IconCoord>
        {
            ["alric"] = new IconCoord(3037, 2681, 338, 331, true),
            ["amphiptere"] = new IconCoord(3400, 671, 338, 331),
            ["anurian_boss"] = new IconCoord(3400, 1676, 338, 331),
            ["apemate"] = new IconCoord(1690, 3358, 338, 331),
            ["apex_shard"] = new IconCoord(3400, 1, 338, 331),
            ["apex_stalker"] = new IconCoord(2032, 343, 338, 331),
            ["arcane_magus"] = new IconCoord(1048, 1, 338, 331, true),
            ["assassin"] = new IconCoord(1341, 2360, 338, 331, true),
            ["banner_bearer"] = new IconCoord(671, 2395, 338, 331, true),
            ["black_corsair"] = new IconCoord(2709, 1683, 338, 331),
            ["blackthorne"] = new IconCoord(1341, 2018, 338, 331, true),
            ["blue_wyvern"] = new IconCoord(671, 1369, 338, 331, true),
            ["boatswain"] = new IconCoord(3707, 3016, 338, 331),
            ["bomber_parrot"] = new IconCoord(3707, 3351, 338, 331, true),
            ["bone_carrier"] = new IconCoord(1690, 1013, 338, 331),
            ["boom_baboon"] = new IconCoord(1348, 3358, 338, 331),
            ["boss_dwarf_mecha"] = new IconCoord(2374, 1348, 338, 331),
            ["boss_great_t"] = new IconCoord(2025, 1683, 338, 331),
            ["bruiser"] = new IconCoord(1, 1081, 338, 331, true),
            ["bucaneer"] = new IconCoord(3714, 2681, 338, 331),
            ["bullshark_dasher"] = new IconCoord(2032, 3358, 338, 331),
            ["bullywags_channeler"] = new IconCoord(1348, 678, 338, 331),
            ["bullywags_erudite"] = new IconCoord(1006, 678, 338, 331),
            ["bullywags_golem"] = new IconCoord(1690, 678, 338, 331),
            ["camel_rider"] = new IconCoord(2018, 2018, 338, 331),
            ["carnival_dragon_head"] = new IconCoord(3742, 1341, 338, 331),
            ["charly"] = new IconCoord(2032, 1348, 338, 331),
            ["chaser"] = new IconCoord(3742, 336, 338, 331),
            ["chomp_bot"] = new IconCoord(1, 1765, 338, 331, true),
            ["clockwork_spider"] = new IconCoord(1, 2791, 338, 331, true),
            ["corpse_recruiter"] = new IconCoord(2374, 3358, 338, 331),
            ["corrosive_soul"] = new IconCoord(2032, 1013, 338, 331),
            ["corsair"] = new IconCoord(3372, 3016, 338, 331, true),
            ["cursed_sailor"] = new IconCoord(2716, 3358, 338, 331),
            ["cyclopter_pilot"] = new IconCoord(336, 1081, 338, 331, true),
            ["deep_king"] = new IconCoord(3393, 2011, 338, 331),
            ["desert_eagle"] = new IconCoord(3728, 2346, 338, 331),
            ["devoted_priest"] = new IconCoord(1348, 343, 338, 331),
            ["djini"] = new IconCoord(2702, 2018, 338, 331),
            ["dragon_king_boss"] = new IconCoord(1341, 1683, 338, 331),
            ["draugr"] = new IconCoord(336, 2449, 338, 331, true),
            ["elephant_lancer"] = new IconCoord(2011, 2353, 338, 331),
            ["elite_footman"] = new IconCoord(671, 2053, 338, 331, true),
            ["elven_warrior"] = new IconCoord(2388, 1, 338, 331, true),
            ["falconeer"] = new IconCoord(3386, 2346, 338, 331),
            ["farmer_bucket"] = new IconCoord(2374, 343, 338, 331),
            ["filibusters"] = new IconCoord(1006, 3023, 338, 331),
            ["flying_ghost_ship"] = new IconCoord(3735, 2011, 338, 331),
            ["footman"] = new IconCoord(671, 1711, 338, 331, true),
            ["freebooter"] = new IconCoord(3372, 2681, 338, 331),
            ["frost_giant"] = new IconCoord(671, 1027, 338, 331, true),
            ["frozen_heart"] = new IconCoord(2374, 678, 338, 331),
            ["frozen_soul"] = new IconCoord(2716, 678, 338, 331),
            ["ghost"] = new IconCoord(3742, 1006, 338, 331),
            ["ghostly_barge"] = new IconCoord(2374, 3023, 338, 331),
            ["glacial_wolf"] = new IconCoord(671, 685, 338, 331, true),
            ["golem_house"] = new IconCoord(3058, 1, 338, 331),
            ["great_macaw"] = new IconCoord(1348, 3023, 338, 331),
            ["griffin_bombardier"] = new IconCoord(1718, 1, 338, 331, true),
            ["guardian_eagle"] = new IconCoord(3058, 336, 338, 331),
            ["hammermage"] = new IconCoord(2353, 2688, 338, 331),
            ["hanged_captain"] = new IconCoord(2716, 3023, 338, 331),
            ["haunted_skeleton"] = new IconCoord(1006, 1013, 338, 331),
            ["high_sorcerer"] = new IconCoord(1690, 343, 338, 331),
            ["human_woodcutter"] = new IconCoord(1, 397, 338, 331, true),
            ["human_worker"] = new IconCoord(1, 739, 338, 331, true),
            ["hunting_dog"] = new IconCoord(3742, 1, 338, 331),
            ["ice_golem"] = new IconCoord(2032, 678, 338, 331),
            ["ice_reaper"] = new IconCoord(3058, 1006, 338, 331),
            ["ice_witch"] = new IconCoord(671, 343, 338, 331, true),
            ["infuser"] = new IconCoord(3742, 671, 338, 331),
            ["knight_rider"] = new IconCoord(1383, 1, 338, 331, true),
            ["leap_dragon"] = new IconCoord(2053, 1, 338, 331, true),
            ["legion_archer"] = new IconCoord(1676, 2018, 338, 331),
            ["legion_nomad"] = new IconCoord(2360, 2018, 338, 331),
            ["legionnaire"] = new IconCoord(1006, 2025, 338, 331, true),
            ["lemonshark"] = new IconCoord(2011, 2688, 338, 331),
            ["lich"] = new IconCoord(2716, 1013, 338, 331),
            ["lightseeker"] = new IconCoord(2723, 1, 338, 331, true),
            ["lord_of_afterlife"] = new IconCoord(1006, 1683, 338, 331, true),
            ["lord_of_afterlife_2"] = new IconCoord(1683, 1683, 338, 331),
            ["macaque"] = new IconCoord(3051, 2011, 338, 331),
            ["magic_carpet"] = new IconCoord(3044, 2346, 338, 331),
            ["malik"] = new IconCoord(2367, 1683, 338, 331),
            ["mechadwarf"] = new IconCoord(336, 739, 338, 331, true),
            ["mega_boss_dragon"] = new IconCoord(2716, 1348, 338, 331),
            ["mega_knight"] = new IconCoord(3058, 1676, 338, 331),
            ["megalodon"] = new IconCoord(3058, 3358, 338, 331),
            ["mirage"] = new IconCoord(2353, 2353, 338, 331),
            ["mogwai"] = new IconCoord(3058, 1341, 338, 331),
            ["musketeer"] = new IconCoord(671, 2737, 338, 331, true),
            ["nanoq_warbear"] = new IconCoord(336, 3133, 338, 331, true),
            ["nian"] = new IconCoord(3400, 1341, 338, 331),
            ["northern_berserker"] = new IconCoord(336, 2791, 338, 331, true),
            ["northern_huntress"] = new IconCoord(336, 1423, 338, 331, true),
            ["northern_wildling"] = new IconCoord(336, 1765, 338, 331, true),
            ["paladin"] = new IconCoord(1006, 343, 338, 331),
            ["prehistoric_dwarf"] = new IconCoord(1006, 1348, 338, 331),
            ["pterodactyl"] = new IconCoord(1690, 1348, 338, 331),
            ["quarry_worker"] = new IconCoord(1, 2107, 338, 331, true),
            ["risen_cutthroat"] = new IconCoord(2695, 2688, 338, 331),
            ["rushing_monkey"] = new IconCoord(1006, 3358, 338, 331),
            ["sand_mysthic"] = new IconCoord(1676, 2353, 338, 331, true),
            ["screecher_bat"] = new IconCoord(2374, 1013, 338, 331),
            ["smokebeard_engineer"] = new IconCoord(1, 2449, 338, 331, true),
            ["stonebeard_geomancer"] = new IconCoord(378, 1, 338, 331, true),
            ["sulfur_alchemist"] = new IconCoord(336, 397, 338, 331, true),
            ["svell_druid"] = new IconCoord(713, 1, 338, 331, true),
            ["tailblade"] = new IconCoord(1690, 3023, 338, 331),
            ["tigershark_rager"] = new IconCoord(2032, 3023, 338, 331),
            ["tinbeard_gunman"] = new IconCoord(1, 3133, 338, 331, true),
            ["tower_shield_knight"] = new IconCoord(671, 3079, 338, 331, true),
            ["valkyrie"] = new IconCoord(336, 2107, 338, 331, true),
            ["velociraptor"] = new IconCoord(1348, 1348, 338, 331),
            ["war_elephant"] = new IconCoord(1006, 2367, 338, 331, true),
            ["war_wagon"] = new IconCoord(3400, 336, 338, 331),
            ["warden"] = new IconCoord(3058, 671, 338, 331),
            ["warhammer_guard"] = new IconCoord(1, 1423, 338, 331, true),
            ["werewolf"] = new IconCoord(1348, 1013, 338, 331),
            ["winter_lord"] = new IconCoord(3400, 1006, 338, 331),
            ["winter_queen"] = new IconCoord(3742, 1676, 338, 331),
        };

        // ── Hero display names ──
        public static readonly Dictionary<string, string> HeroNames = new Dictionary<string, string>
        {
            ["hero_orc"] = "Veruk", ["hero_asra"] = "Asra", ["hero_oloch"] = "Oloch",
            ["hero_mortemis"] = "Mortemis", ["hero_tramin"] = "Tramin", ["hero_margosa"] = "Margosa",
            ["hero_jigou"] = "Jigou", ["hero_beresad"] = "Beresad", ["hero_tank"] = "Jun'pai",
            ["hero_naga"] = "Naga", ["hero_eiskalt"] = "Eiskalt", ["hero_murglun"] = "Murglun",
            ["hero_jack_o_lantern"] = "Jack O'Lantern", ["hero_dianyun"] = "Dianyun",
            ["hero_isfet"] = "Isfet", ["hero_lucerna"] = "Lucerna", ["hero_mammoth"] = "Mammoth",
        };

        // ── SKIP set (non-numeric / display fields to skip in renderFields) ──
        public static readonly HashSet<string> Skip = new HashSet<string>
        {
            "info","anchor","position","animations","decos","shadow","selector","portrait",
            "life_bar","hit_position","head_position","modifier_position","fx_unit_explode","blood_decal_type",
            "notification","quickmenu","tooltip","touch_offset","decal","animation_start","animation_shooter",
            "animation_loop","animation_random","sound_start","release_sound","taunt_sound","death_sound",
            "single_frame","shoot_position","center_offset","destination_offset","offset_position_target",
            "custom_spawn_points","sprite","file","frame","image","string","icon","pop","pop_critical",
            "effects","bar","display_name","display_bottom_image","description","description_short",
            "build_taunt","is_hero","can_be_moved","walk_type","achievement_level10","achievement_on_damage",
            "achievements_on_finish","update_priority","custom_fills","power_frame_normal","power_frame_enabled",
            "power_frame_selected","power_portrait","power_mask","power_frame_hover","cursor","init_closed",
            "show_confirm","object","notification_","on_path","on_unit","on_unit_army",
            "supreme_architects_system","y_position_adjust","proyectile_layer","lost_target_inactive",
            "damage_on_first_hit","use_hit_pos_flying_unit","destroy_after_anim","override_damage_multiplier",
            "override_other_skill","override_conditions","cast_while_fighting","cast_on_moving",
            "run_on_inactive","can_be_canceled","flip_toward_target","exclude_boss","replacement",
            "use_center","is_blocked","dummy","unit_key","runes_of_power_upgrade","probabilities",
            "layer","position_type","fade_in","fade_out","can_scale","rotate_unit","shooter_as_end",
            "animation_shooter_updown","bottom_text","items","icon_type","position_offset","text",
            "image_confirm","image_off","image_off_top","action_type","action_key",
            "has_layers","flip_on_shoot","modifier_size","is_holder","canBeAffectedByRange",
            "canBeAffectedByDamage","taunt_demon_goonies","taunt_demon_guards","taunt_demon_tridents",
            "taunt_veznan_disciples","button_image","tooltip_text","tooltip_image","show_level",
            "type","respawn_sound","__data__","build","level_up","respawn","idle_shooters","idle_flip",
            "respawn_away_from_path","refresh_recently_damaged","power","units","shooters","display_priority",
            "image_off_left","confirm_on_action","position_adjust","y_adjust","x_adjust","level"
        };

        // ── Faction colors for card left borders ──
        public static readonly Dictionary<string, Color> FactionColors = new Dictionary<string, Color>
        {
            ["warmongers"] = Color.FromArgb(200, 50, 50),
            ["dark_army"] = Color.FromArgb(100, 60, 160),
            ["fallen_ones"] = Color.FromArgb(50, 160, 80),
            ["ember_lords"] = Color.FromArgb(220, 120, 30),
            ["mercenary"] = Color.FromArgb(160, 140, 60),
            ["heroes"] = Color.FromArgb(255, 215, 0),
        };

        public static Color GetFactionColor(string faction)
        {
            if (faction == null) return Color.FromArgb(100, 100, 100);
            foreach (var kv in FactionColors)
                if (faction.StartsWith(kv.Key)) return kv.Value;
            return Color.FromArgb(100, 100, 100);
        }

        // ── Atlas file paths (relative to game root) ──
        public const string GuiAtlas = @"KR4\Sprites\HD\Gameplay\Gui\kr4_gui.png";
        public const string HeroroomAtlas = @"KR4\Sprites\HD\Map\kr4_map_heroroom.png";
        public const string TowerEncAtlas = @"KR4_PC\Sprites\HD\Encyclopedia\kr4_encyclopedia_towers_pc.png";
        public const string EnemyEncAtlas = @"KR4_PC\Sprites\HD\Encyclopedia\kr4_encyclopedia_enemies_pc.png";
    }
}
