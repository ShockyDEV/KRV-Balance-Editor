using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BalanceEditor
{
    public partial class MainForm
    {
        // ── VFX highlight color (yellow like Word text highlighting) ──
        static readonly Color VFXHighlight = Color.FromArgb(255, 255, 160);

        // ══════════════════════════════════════════════════
        //  TOWER DETAIL
        // ══════════════════════════════════════════════════

        // System skill keys that should be hidden (build/door are not gameplay skills)
        static readonly HashSet<string> TowerSkipSkillKeys = new HashSet<string>
            { "build", "door", "simple_animation", "level_up", "respawn", "idle_shooters", "idle_flip" };

        void ShowTowerDetail(ListItemTag item)
        {
            var fam = item.Family;
            if (fam == null) return;

            cbShowVFX.Visible = true;

            tbDetailName.Text = Pretty(fam.BaseName);
            tbDetailKey.Text = fam.BaseName;
            tbFaction.Text = Pretty(fam.Faction);

            // Variant selector
            cbVariant.Items.Clear();
            cbVariant.SelectedIndexChanged -= CbHeroLevel_Changed; // ensure hero handler removed
            if (fam.Variants.Count > 1)
            {
                lblVariant.Text = "Variant";
                lblVariant.Visible = true;
                cbVariant.Visible = true;
                foreach (var v in fam.Variants)
                    cbVariant.Items.Add(fam.VariantLabels.ContainsKey(v) ? fam.VariantLabels[v] : Pretty(v));

                cbVariant.SelectedIndexChanged -= CbVariant_Changed;
                cbVariant.SelectedIndex = fam.Variants.Count - 1;
                cbVariant.SelectedIndexChanged += CbVariant_Changed;
                cbVariant.Tag = fam;
            }
            else
            {
                lblVariant.Visible = false;
                cbVariant.Visible = false;
                cbVariant.Tag = null;
            }

            // Skill level selector (for tower skills with upgrade levels)
            string initVarKey = fam.Variants[cbVariant.SelectedIndex >= 0 ? cbVariant.SelectedIndex : fam.Variants.Count - 1];
            int maxTowerSkillLvl = GetTowerMaxSkillLevels(fam.VariantData[initVarKey]);

            cbSkillLevel.SelectedIndexChanged -= CbSkillLevel_Changed;
            cbSkillLevel.Items.Clear();
            if (maxTowerSkillLvl > 1)
            {
                for (int i = 0; i < maxTowerSkillLvl; i++)
                    cbSkillLevel.Items.Add("Lv " + i);
                cbSkillLevel.SelectedIndex = maxTowerSkillLvl - 1;
                cbSkillLevel.Visible = true;
                lblSkillLevel.Visible = true;
            }
            else
            {
                cbSkillLevel.Visible = false;
                lblSkillLevel.Visible = false;
            }
            cbSkillLevel.SelectedIndexChanged += CbSkillLevel_Changed;

            int towerSkillLvl = cbSkillLevel.SelectedIndex >= 0 ? cbSkillLevel.SelectedIndex : -1;
            RebuildTowerContent(fam, initVarKey, towerSkillLvl);
        }

        void CbVariant_Changed(object sender, EventArgs e)
        {
            var fam = cbVariant.Tag as TowerFamily;
            if (fam == null || cbVariant.SelectedIndex < 0) return;
            string varKey = fam.Variants[cbVariant.SelectedIndex];

            // Recalculate skill levels for this variant
            int maxSkillLvl = GetTowerMaxSkillLevels(fam.VariantData[varKey]);
            cbSkillLevel.SelectedIndexChanged -= CbSkillLevel_Changed;
            cbSkillLevel.Items.Clear();
            if (maxSkillLvl > 1)
            {
                for (int i = 0; i < maxSkillLvl; i++)
                    cbSkillLevel.Items.Add("Lv " + i);
                cbSkillLevel.SelectedIndex = maxSkillLvl - 1;
                cbSkillLevel.Visible = true;
                lblSkillLevel.Visible = true;
            }
            else
            {
                cbSkillLevel.Visible = false;
                lblSkillLevel.Visible = false;
            }
            cbSkillLevel.SelectedIndexChanged += CbSkillLevel_Changed;

            int skillLvl = cbSkillLevel.SelectedIndex >= 0 ? cbSkillLevel.SelectedIndex : -1;
            RebuildTowerContent(fam, varKey, skillLvl);
        }

        /// <summary>Find main skill in tower's skills array. Returns (dict, index).</summary>
        (Dictionary<string, object> Data, int Index) FindTowerMainSkill(Dictionary<string, object> towerData)
        {
            var skillsList = PlistHelper.GetList(towerData, "skills");
            if (skillsList == null) return (null, -1);

            // First: look for main=true
            for (int i = 0; i < skillsList.Count; i++)
            {
                var sk = skillsList[i] as Dictionary<string, object>;
                if (sk == null) continue;
                if (sk.ContainsKey("main") && (sk["main"] is bool mb && mb || sk["main"] is long ml && ml != 0))
                    return (sk, i);
            }
            // Second: first skill with damage_min
            for (int i = 0; i < skillsList.Count; i++)
            {
                var sk = skillsList[i] as Dictionary<string, object>;
                if (sk == null) continue;
                if (sk.ContainsKey("damage_min"))
                    return (sk, i);
            }
            return (null, -1);
        }

        /// <summary>
        /// Scan tower skills (excluding system/visual) for max numeric array length.
        /// Also checks resolved forward_key unit skills for level arrays.
        /// Returns 0 if no level-indexed arrays found (no skill level selector needed).
        /// </summary>
        int GetTowerMaxSkillLevels(Dictionary<string, object> towerData)
        {
            var skillsList = PlistHelper.GetList(towerData, "skills");
            if (skillsList == null) return 0;

            int max = 0;
            foreach (var item in skillsList)
            {
                var sk = item as Dictionary<string, object>;
                if (sk == null) continue;

                string key = PlistHelper.GetString(sk, "key") ?? "";
                if (TowerSkipSkillKeys.Contains(key)) continue;

                int found = FindMaxNumericArrayLen(sk, 0);
                if (found > max) max = found;

                // Also check resolved forward skills in units data
                if (key == "forward")
                {
                    string fwdKey = PlistHelper.GetString(sk, "forward_key");
                    if (fwdKey != null)
                    {
                        var resolved = ResolveTowerForward(fwdKey);
                        if (resolved.HasValue)
                        {
                            int fwdFound = FindMaxNumericArrayLen(resolved.Value.Data, 0);
                            if (fwdFound > max) max = fwdFound;
                        }
                    }
                }
            }

            // If any level arrays exist, ensure minimum of 4 (KRV standard: 3 upgrade levels + global)
            if (max > 0 && max < 4) max = 4;
            return max;
        }

        /// <summary>
        /// Resolve a tower's forward_key to the actual unit skill data in units_settings.plist.
        /// Searches all factions for a skill with matching id.
        /// </summary>
        (Dictionary<string, object> Data, string Faction, string Unit, int Index)?
            ResolveTowerForward(string forwardKey)
        {
            foreach (var factionKv in unitsData)
            {
                if (factionKv.Key == "default_config" || factionKv.Key == "heroes") continue;
                var factionDict = factionKv.Value as Dictionary<string, object>;
                if (factionDict == null) continue;

                foreach (var unitKv in factionDict)
                {
                    var unitDict = unitKv.Value as Dictionary<string, object>;
                    if (unitDict == null) continue;

                    var uSkills = PlistHelper.GetList(unitDict, "skills");
                    if (uSkills == null) continue;

                    for (int si = 0; si < uSkills.Count; si++)
                    {
                        var usk = uSkills[si] as Dictionary<string, object>;
                        if (usk == null) continue;
                        if (PlistHelper.GetString(usk, "id") == forwardKey)
                            return (usk, factionKv.Key, unitKv.Key, si);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Find the spawned unit data for a barracks tower (soldiers.type → unit key in units data).
        /// </summary>
        (Dictionary<string, object> Data, string Faction, string Unit)?
            FindSpawnedUnit(Dictionary<string, object> towerData)
        {
            var soldiers = PlistHelper.GetDict(towerData, "soldiers");
            if (soldiers == null) return null;

            string unitType = PlistHelper.GetString(soldiers, "type");
            if (string.IsNullOrEmpty(unitType)) return null;

            foreach (var factionKv in unitsData)
            {
                if (factionKv.Key == "default_config" || factionKv.Key == "heroes") continue;
                var factionDict = factionKv.Value as Dictionary<string, object>;
                if (factionDict == null) continue;

                if (factionDict.TryGetValue(unitType, out var unitObj))
                {
                    var unitDict = unitObj as Dictionary<string, object>;
                    if (unitDict != null) return (unitDict, factionKv.Key, unitType);
                }
            }
            return null;
        }

        /// <summary>
        /// Find a unit entry by key across ALL factions (and heroes) in units_settings.plist.
        /// </summary>
        (Dictionary<string, object> Data, string Faction, string UnitKey)?
            FindUnitByKey(string unitKey)
        {
            if (string.IsNullOrEmpty(unitKey)) return null;

            foreach (var factionKv in unitsData)
            {
                if (factionKv.Key == "default_config") continue;
                var factionDict = factionKv.Value as Dictionary<string, object>;
                if (factionDict == null) continue;

                if (factionDict.TryGetValue(unitKey, out var unitObj))
                {
                    var unitDict = unitObj as Dictionary<string, object>;
                    if (unitDict != null) return (unitDict, factionKv.Key, unitKey);
                }
            }
            return null;
        }

        /// <summary>
        /// Recursively collect all summoned unit keys referenced via "unit" fields
        /// that are string values (not dicts). Searches into sub-dicts like fx, object, etc.
        /// </summary>
        List<string> CollectSummonedUnitKeys(Dictionary<string, object> dict, int depth = 0)
        {
            var result = new List<string>();
            if (dict == null || depth > 4) return result;

            foreach (var kv in dict)
            {
                if (kv.Key == "unit" && kv.Value is string unitStr && !string.IsNullOrEmpty(unitStr))
                {
                    if (!result.Contains(unitStr))
                        result.Add(unitStr);
                }
                else if (kv.Value is Dictionary<string, object> sub)
                {
                    // Recurse into sub-dicts (fx, object, etc.) but skip known noise
                    if (StaticData.Skip.Contains(kv.Key) && kv.Key != "object" && kv.Key != "fx") continue;
                    result.AddRange(CollectSummonedUnitKeys(sub, depth + 1));
                }
            }
            return result;
        }

        /// <summary>
        /// Renders a compact "Summoned Unit" sub-section within a skill panel.
        /// Shows the unit's core stats (health, armor, speed) and main attack
        /// as editable inputs. All paths reference units_settings.plist.
        /// </summary>
        int RenderSummonedUnitBlock(Control parent, int startY, string unitKey)
        {
            var found = FindUnitByKey(unitKey);
            if (!found.HasValue) return startY;

            var uData = found.Value.Data;
            string faction = found.Value.Faction;
            string file = "units";
            var uBase = new List<object> { faction, unitKey };

            int y = startY + 2;

            // Header with teal color
            var hdr = new Label
            {
                Text = "\u25B8 Summoned: " + Pretty(unitKey),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 160),
                AutoSize = true,
                MaximumSize = new Size(parent.Width - 24, 0),
                Location = new Point(16, y)
            };
            parent.Controls.Add(hdr);
            y = hdr.Bottom + 4;

            // Icon (if available)
            int statsX = 16;
            var unitIcon = IconExtractor.GetEnemyIcon(unitKey);
            if (unitIcon == null) unitIcon = IconExtractor.GetTowerIcon(unitKey);
            if (unitIcon != null)
            {
                var pic = new PictureBox
                {
                    Image = unitIcon,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Size = new Size(40, 40),
                    Location = new Point(16, y),
                    BackColor = Color.Transparent,
                    BorderStyle = BorderStyle.FixedSingle
                };
                parent.Controls.Add(pic);
                statsX = 62;
            }

            int iconTop = y;

            // Core stats
            string[] coreStats = { "health", "armor", "speed", "block_range" };
            foreach (var stat in coreStats)
            {
                if (!uData.ContainsKey(stat)) continue;
                var val = uData[stat];
                double? nv = PlistHelper.AsNumber(val);

                if (nv.HasValue)
                {
                    CreateFieldRow(parent, statsX, y, Pretty(stat), file,
                        new List<object>(uBase) { stat }, nv.Value);
                    y += 24;

                    // Inline armor type combo right after the armor row
                    if (stat == "armor" && uData.ContainsKey("armor_type"))
                    {
                        double? atNum = PlistHelper.AsNumber(uData["armor_type"]);
                        if (atNum.HasValue)
                            y = AddArmorTypeRow(parent, y, atNum.Value, file,
                                new List<object>(uBase) { "armor_type" }, statsX);
                    }
                }
                else if (val is List<object> arr && arr.Count > 0 && arr.All(o => PlistHelper.AsNumber(o).HasValue))
                {
                    y = CreateArrayRow(parent, statsX, y, Pretty(stat), file,
                        new List<object>(uBase) { stat }, arr);
                }
            }

            // Ensure we're below any icon
            y = Math.Max(y, iconTop + 44);

            // Main attack skill
            var mainSkill = FindTowerMainSkill(uData);
            if (mainSkill.Data != null)
            {
                var mHdr = new Label
                {
                    Text = "Attack:",
                    Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(0, 100, 140),
                    AutoSize = true,
                    Location = new Point(statsX, y)
                };
                parent.Controls.Add(mHdr);
                y = mHdr.Bottom + 2;

                var mPath = new List<object>(uBase) { "skills", mainSkill.Index };
                string[] atkStats = { "damage_min", "damage_max", "cooldown", "range" };
                foreach (var stat in atkStats)
                {
                    if (!mainSkill.Data.ContainsKey(stat)) continue;
                    var val = mainSkill.Data[stat];
                    double? nv = PlistHelper.AsNumber(val);
                    if (nv.HasValue)
                    {
                        CreateFieldRow(parent, statsX, y, Pretty(stat), file,
                            new List<object>(mPath) { stat }, nv.Value);
                        y += 24;
                    }
                    else if (val is List<object> arr && arr.Count > 0 && arr.All(o => PlistHelper.AsNumber(o).HasValue))
                    {
                        y = CreateArrayRow(parent, statsX, y, Pretty(stat), file,
                            new List<object>(mPath) { stat }, arr);
                    }
                }
            }

            return y + 4;
        }

        void RebuildTowerContent(TowerFamily fam, string varKey, int skillLevelIdx = -1)
        {
            contentPanel.SuspendLayout();
            contentPanel.Controls.Clear();
            contentPanel.AutoScrollPosition = new Point(0, 0);

            var data = fam.VariantData[varKey];
            if (data == null) { contentPanel.ResumeLayout(); return; }

            string file = "towers";
            var basePath = fam.BasePath(varKey);
            int maxSkillLvl = GetTowerMaxSkillLevels(data);

            // ── Left side: Core Stats + Icon ──
            var gbStats = new GroupBox
            {
                Text = "Core Stats",
                Location = new Point(4, 4),
                Size = new Size(280, 300),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            int sy = 20;

            // Cost (top-level)
            if (data.TryGetValue("cost", out var costObj))
            {
                double? cost = PlistHelper.AsNumber(costObj);
                if (cost.HasValue)
                    sy = AddStatRow(gbStats, sy, "Cost", cost.Value, file, new List<object>(basePath) { "cost" });
            }

            // Main skill promoted stats (from skills array), level-aware
            var mainResult = FindTowerMainSkill(data);
            if (mainResult.Data != null)
            {
                var sd = mainResult.Data;
                var skillPath = new List<object>(basePath) { "skills", mainResult.Index };
                bool isGlobal = maxSkillLvl > 0 && skillLevelIdx >= maxSkillLvl - 1;

                foreach (string k in new[] { "range", "cooldown", "damage_min", "damage_max" })
                {
                    if (!sd.ContainsKey(k)) continue;
                    var val = sd[k];
                    double? nv = PlistHelper.AsNumber(val);
                    if (nv.HasValue)
                    {
                        if (skillLevelIdx >= 0 && !isGlobal)
                            sy = AddStatRow(gbStats, sy, Pretty(k) + $" [Lv{skillLevelIdx}]", nv.Value, file,
                                new List<object>(skillPath) { k, skillLevelIdx });
                        else
                            sy = AddStatRow(gbStats, sy, Pretty(k), nv.Value, file,
                                new List<object>(skillPath) { k });
                    }
                    else if (val is List<object> arr && arr.Count > 0 && arr.All(o => PlistHelper.AsNumber(o).HasValue))
                    {
                        if (skillLevelIdx >= 0 && !isGlobal)
                        {
                            int ai = Math.Min(skillLevelIdx, arr.Count - 1);
                            double v = PlistHelper.AsNumber(arr[ai]) ?? 0;
                            sy = AddStatRow(gbStats, sy, Pretty(k) + $" [Lv{skillLevelIdx}]", v, file,
                                new List<object>(skillPath) { k, skillLevelIdx });
                        }
                        else
                        {
                            sy = CreateArrayRow(gbStats, 8, sy, Pretty(k), file,
                                new List<object>(skillPath) { k }, arr);
                        }
                    }
                }
            }

            // ── Spawned unit stats (for barracks towers) ──
            var spawnedUnit = FindSpawnedUnit(data);
            if (spawnedUnit.HasValue)
            {
                sy += 4;
                var unitHdr = new Label
                {
                    Text = "── Spawned Unit (" + Pretty(spawnedUnit.Value.Unit) + ") ──",
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(0, 100, 150),
                    AutoSize = true,
                    MaximumSize = new Size(260, 0),
                    Location = new Point(8, sy)
                };
                gbStats.Controls.Add(unitHdr);
                sy = unitHdr.Bottom + 4;

                var uData = spawnedUnit.Value.Data;
                var uBasePath = new List<object> { spawnedUnit.Value.Faction, spawnedUnit.Value.Unit };

                string[] unitCoreStats = { "health", "armor", "speed", "block_range" };
                foreach (var stat in unitCoreStats)
                {
                    if (!uData.ContainsKey(stat)) continue;
                    double? nv = PlistHelper.AsNumber(uData[stat]);
                    if (nv.HasValue)
                    {
                        bool isArmor = (stat == "armor");
                        sy = isArmor
                            ? AddArmorStatRow(gbStats, sy, Pretty(stat), nv.Value, "units",
                                new List<object>(uBasePath) { stat })
                            : AddStatRow(gbStats, sy, Pretty(stat), nv.Value, "units",
                                new List<object>(uBasePath) { stat });
                    }
                }

                // Find unit's melee/main combat skill for damage stats
                var uMainSkill = FindTowerMainSkill(uData);
                if (uMainSkill.Data != null)
                {
                    var uSkillPath = new List<object>(uBasePath) { "skills", uMainSkill.Index };
                    foreach (string k in new[] { "damage_min", "damage_max", "cooldown" })
                    {
                        if (!uMainSkill.Data.ContainsKey(k)) continue;
                        double? nv = PlistHelper.AsNumber(uMainSkill.Data[k]);
                        if (nv.HasValue)
                            sy = AddStatRow(gbStats, sy, Pretty(k), nv.Value, "units",
                                new List<object>(uSkillPath) { k });
                    }
                }
            }

            gbStats.Height = sy + 10;
            contentPanel.Controls.Add(gbStats);

            // Portrait in header
            picPortrait.Image = IconExtractor.GetTowerIcon(varKey);
            picPortrait.Visible = picPortrait.Image != null;
            picPortrait.Location = new Point(headerPanel.ClientSize.Width - picPortrait.Width - 8, 4);

            // ── Right side: Skills (from skills ARRAY) ──
            var gbSkills = new GroupBox
            {
                Text = skillLevelIdx >= 0 ? "Skills (Lv " + skillLevelIdx + ")" : "Skills",
                Location = new Point(322, 4),
                Size = new Size(contentPanel.Width > 530 ? contentPanel.Width - 334 : 400, 20),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                AutoSize = true
            };

            int sky = 20;
            string towerLocKey = GetTowerLocKey(varKey, fam.BaseName);

            var skillsList = PlistHelper.GetList(data, "skills");
            if (skillsList != null)
            {
                for (int i = 0; i < skillsList.Count; i++)
                {
                    var skillDict = skillsList[i] as Dictionary<string, object>;
                    if (skillDict == null) continue;
                    if (!HasNumericContent(skillDict, 0)) continue;

                    // Get skill display name from id, action_key, or key
                    string skillId = PlistHelper.GetString(skillDict, "id")
                        ?? PlistHelper.GetString(skillDict, "action_key")
                        ?? PlistHelper.GetString(skillDict, "key")
                        ?? $"skill_{i}";

                    // Skip system/visual skills
                    string skillKey = PlistHelper.GetString(skillDict, "key") ?? "";
                    if (TowerSkipSkillKeys.Contains(skillKey)) continue;

                    // Skill icon — try action_key first, then id, then key
                    Bitmap skillIcon = null;
                    string actionKey = PlistHelper.GetString(skillDict, "action_key") ?? skillId;
                    if (towerLocKey != null)
                        skillIcon = IconExtractor.GetTowerSkillIcon(towerLocKey, actionKey);

                    // Description
                    string desc = null;
                    if (towerLocKey != null)
                        desc = Localization.FindTowerSkillDesc(towerLocKey, actionKey);

                    var skillPath = new List<object>(basePath) { "skills", i };

                    sky = RenderSkillSection(gbSkills, sky, skillId, skillDict, file,
                        skillPath, skillIcon, desc, skillLevelIdx, 0, maxSkillLvl);

                    // For "forward" skills, resolve to actual unit skill and render combat fields
                    if (skillKey == "forward")
                    {
                        string fwdKey = PlistHelper.GetString(skillDict, "forward_key");
                        if (fwdKey != null)
                        {
                            var resolved = ResolveTowerForward(fwdKey);
                            if (resolved.HasValue)
                            {
                                var resolvedHdr = new Label
                                {
                                    Text = "── " + Pretty(fwdKey) + " (Unit Skill) ──",
                                    Font = new Font("Segoe UI", 8, FontStyle.Bold),
                                    ForeColor = Color.FromArgb(0, 100, 150),
                                    AutoSize = true,
                                    MaximumSize = new Size(gbSkills.Width - 24, 0),
                                    Location = new Point(16, sky)
                                };
                                gbSkills.Controls.Add(resolvedHdr);
                                sky = resolvedHdr.Bottom + 4;

                                var unitPath = new List<object> {
                                    resolved.Value.Faction, resolved.Value.Unit,
                                    "skills", resolved.Value.Index };

                                int fwdMaxLvl = FindMaxNumericArrayLen(resolved.Value.Data, 0);
                                if (fwdMaxLvl > 0 && fwdMaxLvl < 4) fwdMaxLvl = 4;

                                if (skillLevelIdx >= 0 && fwdMaxLvl > 0)
                                    sky = RenderLevelAwareFields(gbSkills, resolved.Value.Data,
                                        "units", unitPath, sky, skillLevelIdx, 0, fwdMaxLvl);
                                else
                                    sky = RenderFields(gbSkills, resolved.Value.Data,
                                        "units", unitPath, sky);

                                sky += 4;
                            }
                        }
                    }
                }
            }

            gbSkills.Height = sky + 10;
            contentPanel.Controls.Add(gbSkills);

            contentPanel.ResumeLayout(true);
        }

        // ══════════════════════════════════════════════════
        //  HERO DETAIL
        // ══════════════════════════════════════════════════

        // Skill keys that belong in the Stats section, not Skills
        static readonly HashSet<string> HeroStatSkillKeys = new HashSet<string>
            { "melee", "regeneration", "respawn" };

        bool IsHeroStatSkill(Dictionary<string, object> skillDict)
        {
            string key = PlistHelper.GetString(skillDict, "key") ?? "";
            if (HeroStatSkillKeys.Contains(key)) return true;
            if (skillDict.ContainsKey("main") && (skillDict["main"] is bool mb && mb || skillDict["main"] is long ml && ml != 0))
                return true;
            return false;
        }

        int GetMaxSkillLevels(List<object> skillsList, Dictionary<string, object> heroData)
        {
            int max = 4; // KRV standard: 3 skill levels + 1 global view (Lv 3 shows all)

            // Scan ability skills recursively (arrays may be nested inside "object" sub-dicts)
            if (skillsList != null)
            {
                foreach (var item in skillsList)
                {
                    var sk = item as Dictionary<string, object>;
                    if (sk == null || IsHeroStatSkill(sk)) continue;
                    int found = FindMaxNumericArrayLen(sk, 0);
                    if (found > max) max = found;
                }
            }

            // Also check power/ultimate recursively
            var powerDict = PlistHelper.GetDict(heroData, "power");
            if (powerDict != null)
            {
                int found = FindMaxNumericArrayLen(powerDict, 0);
                if (found > max) max = found;
            }

            return max;
        }

        /// <summary>
        /// Recursively find the max length of numeric arrays (2-5 elements) within a dict tree.
        /// Used to detect how many skill upgrade levels exist.
        /// Does NOT use StaticData.Skip — must scan into "object" sub-dicts to find nested level arrays.
        /// </summary>
        int FindMaxNumericArrayLen(Dictionary<string, object> dict, int depth)
        {
            if (dict == null || depth > 4) return 0;
            int max = 0;
            foreach (var kv in dict)
            {
                if (kv.Value is List<object> arr && arr.Count > max && arr.Count >= 2 && arr.Count <= 5
                    && arr.All(o => PlistHelper.AsNumber(o).HasValue))
                    max = arr.Count;

                if (kv.Value is Dictionary<string, object> sub)
                {
                    int found = FindMaxNumericArrayLen(sub, depth + 1);
                    if (found > max) max = found;
                }
            }
            return max;
        }

        void ShowHeroDetail(ListItemTag item)
        {
            string heroKey = item.Key;
            var heroData = item.Data;

            string displayName = StaticData.HeroNames.TryGetValue(heroKey, out var hn) ? hn : Pretty(heroKey);
            tbDetailName.Text = displayName;
            tbDetailKey.Text = heroKey;
            tbFaction.Text = "Hero";
            cbShowVFX.Visible = true;

            // Hero level selector
            var healthArr = PlistHelper.GetList(heroData, "health");
            int maxLevel = healthArr != null ? healthArr.Count : 1;

            cbVariant.Items.Clear();
            lblVariant.Text = "Level";
            lblVariant.Visible = true;
            cbVariant.Visible = true;
            for (int i = 1; i <= maxLevel; i++)
                cbVariant.Items.Add("Level " + i);

            cbVariant.SelectedIndexChanged -= CbVariant_Changed;
            cbVariant.SelectedIndexChanged -= CbHeroLevel_Changed;
            cbVariant.Tag = new object[] { heroKey, heroData };
            if (maxLevel > 0) cbVariant.SelectedIndex = maxLevel - 1;
            cbVariant.SelectedIndexChanged += CbHeroLevel_Changed;

            // Skill level selector
            var skillsList = PlistHelper.GetList(heroData, "skills");
            int maxSkillLvl = GetMaxSkillLevels(skillsList, heroData);

            cbSkillLevel.SelectedIndexChanged -= CbSkillLevel_Changed;
            cbSkillLevel.Items.Clear();
            if (maxSkillLvl > 1)
            {
                for (int i = 0; i < maxSkillLvl; i++)
                    cbSkillLevel.Items.Add("Lv " + i);
                cbSkillLevel.SelectedIndex = maxSkillLvl - 1;
                cbSkillLevel.Visible = true;
                lblSkillLevel.Visible = true;
            }
            else
            {
                cbSkillLevel.Visible = false;
                lblSkillLevel.Visible = false;
            }
            cbSkillLevel.SelectedIndexChanged += CbSkillLevel_Changed;

            int skillLvl = cbSkillLevel.SelectedIndex >= 0 ? cbSkillLevel.SelectedIndex : 0;
            RebuildHeroContent(heroKey, heroData, maxLevel - 1, skillLvl);
        }

        void CbHeroLevel_Changed(object sender, EventArgs e)
        {
            var refs = cbVariant.Tag as object[];
            if (refs == null || cbVariant.SelectedIndex < 0) return;
            string heroKey = (string)refs[0];
            var heroData = (Dictionary<string, object>)refs[1];
            int skillLvl = cbSkillLevel.Visible && cbSkillLevel.SelectedIndex >= 0 ? cbSkillLevel.SelectedIndex : 0;
            cbVariant.SelectedIndexChanged -= CbHeroLevel_Changed;
            RebuildHeroContent(heroKey, heroData, cbVariant.SelectedIndex, skillLvl);
            cbVariant.SelectedIndexChanged += CbHeroLevel_Changed;
        }

        void CbSkillLevel_Changed(object sender, EventArgs e)
        {
            if (cbSkillLevel.SelectedIndex < 0) return;

            cbSkillLevel.SelectedIndexChanged -= CbSkillLevel_Changed;

            // Tower context
            if (cbVariant.Tag is TowerFamily fam)
            {
                int varIdx = cbVariant.SelectedIndex >= 0 ? cbVariant.SelectedIndex : fam.Variants.Count - 1;
                string varKey = fam.Variants[varIdx];
                RebuildTowerContent(fam, varKey, cbSkillLevel.SelectedIndex);
            }
            // Hero context
            else if (cbVariant.Tag is object[] refs && cbVariant.SelectedIndex >= 0)
            {
                string heroKey = (string)refs[0];
                var heroData = (Dictionary<string, object>)refs[1];
                RebuildHeroContent(heroKey, heroData, cbVariant.SelectedIndex, cbSkillLevel.SelectedIndex);
            }

            cbSkillLevel.SelectedIndexChanged += CbSkillLevel_Changed;
        }

        void CbShowVFX_Changed(object sender, EventArgs e)
        {
            if (listBox.SelectedIndex < 0) return;
            var tag = listBox.SelectedItem as ListItemTag;
            if (tag == null) return;

            switch (tag.Category)
            {
                case "tower":
                    if (cbVariant.Tag is TowerFamily fam)
                    {
                        int idx = cbVariant.SelectedIndex >= 0 ? cbVariant.SelectedIndex : fam.Variants.Count - 1;
                        int tSkillLvl = cbSkillLevel.Visible && cbSkillLevel.SelectedIndex >= 0 ? cbSkillLevel.SelectedIndex : -1;
                        RebuildTowerContent(fam, fam.Variants[idx], tSkillLvl);
                    }
                    break;
                case "hero":
                    if (cbVariant.Tag is object[] refs && cbVariant.SelectedIndex >= 0)
                    {
                        int skillLvl = cbSkillLevel.Visible && cbSkillLevel.SelectedIndex >= 0 ? cbSkillLevel.SelectedIndex : 0;
                        RebuildHeroContent((string)refs[0], (Dictionary<string, object>)refs[1],
                            cbVariant.SelectedIndex, skillLvl);
                    }
                    break;
                case "enemy":
                    ShowEnemyDetail(tag);
                    break;
            }
        }

        /// <summary>Find main melee skill inside the hero's skills array.</summary>
        (Dictionary<string, object> Data, int Index) FindHeroMainSkill(Dictionary<string, object> heroData)
        {
            var skillsList = PlistHelper.GetList(heroData, "skills");
            if (skillsList == null) return (null, -1);

            for (int i = 0; i < skillsList.Count; i++)
            {
                var sk = skillsList[i] as Dictionary<string, object>;
                if (sk == null) continue;
                if (sk.ContainsKey("main") && (sk["main"] is bool mb && mb || sk["main"] is long ml && ml != 0))
                    return (sk, i);
            }
            for (int i = 0; i < skillsList.Count; i++)
            {
                var sk = skillsList[i] as Dictionary<string, object>;
                if (sk == null) continue;
                if (PlistHelper.GetString(sk, "key") == "melee")
                    return (sk, i);
            }
            for (int i = 0; i < skillsList.Count; i++)
            {
                var sk = skillsList[i] as Dictionary<string, object>;
                if (sk == null) continue;
                if (sk.ContainsKey("damage_min"))
                    return (sk, i);
            }
            return (null, -1);
        }

        /// <summary>Find regeneration skill inside the hero's skills array.</summary>
        (Dictionary<string, object> Data, int Index) FindHeroRegenSkill(Dictionary<string, object> heroData)
        {
            var skillsList = PlistHelper.GetList(heroData, "skills");
            if (skillsList == null) return (null, -1);

            for (int i = 0; i < skillsList.Count; i++)
            {
                var sk = skillsList[i] as Dictionary<string, object>;
                if (sk == null) continue;
                string key = PlistHelper.GetString(sk, "key");
                if (key == "regeneration" || sk.ContainsKey("healing_points"))
                    return (sk, i);
            }
            return (null, -1);
        }

        /// <summary>Find a skill by key name inside the hero's skills array.</summary>
        (Dictionary<string, object> Data, int Index) FindHeroSkillByKey(Dictionary<string, object> heroData, string targetKey)
        {
            var skillsList = PlistHelper.GetList(heroData, "skills");
            if (skillsList == null) return (null, -1);

            for (int i = 0; i < skillsList.Count; i++)
            {
                var sk = skillsList[i] as Dictionary<string, object>;
                if (sk == null) continue;
                if (PlistHelper.GetString(sk, "key") == targetKey)
                    return (sk, i);
            }
            return (null, -1);
        }

        void RebuildHeroContent(string heroKey, Dictionary<string, object> heroData, int levelIdx, int skillLevelIdx)
        {
            contentPanel.SuspendLayout();
            contentPanel.Controls.Clear();
            contentPanel.AutoScrollPosition = new Point(0, 0);

            string file = "units";
            var heroBase = new List<object> { "heroes", heroKey };

            // ── Core Stats GroupBox ──
            var gbStats = new GroupBox
            {
                Text = "Stats (Level " + (levelIdx + 1) + ")",
                Location = new Point(4, 4),
                Size = new Size(310, 400),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            int sy = 20;

            // Top-level array/scalar stats at hero level.
            // Note: magic_resistance is intentionally omitted — confirmed dead data in the
            // game engine (no symbol in Kingdom Rush Vengeance.exe). Use armor_type instead.
            // Order: health, armor, then armor_type combo (right next to armor), then speed, etc.
            string[] topStats = { "health", "armor", "speed", "block_range", "skills_upgrades" };
            foreach (var stat in topStats)
            {
                if (!heroData.ContainsKey(stat)) continue;
                var val = heroData[stat];
                bool isArmor = (stat == "armor");

                if (val is List<object> arr && arr.Count > 0 && arr.All(o => PlistHelper.AsNumber(o).HasValue))
                {
                    if (levelIdx < arr.Count)
                    {
                        double? v = PlistHelper.AsNumber(arr[levelIdx]);
                        if (v.HasValue)
                            sy = isArmor
                                ? AddArmorStatRow(gbStats, sy, Pretty(stat), v.Value, file,
                                    new List<object>(heroBase) { stat, levelIdx })
                                : AddStatRow(gbStats, sy, Pretty(stat), v.Value, file,
                                    new List<object>(heroBase) { stat, levelIdx });
                    }
                }
                else
                {
                    double? nv = PlistHelper.AsNumber(val);
                    if (nv.HasValue)
                        sy = isArmor
                            ? AddArmorStatRow(gbStats, sy, Pretty(stat), nv.Value, file,
                                new List<object>(heroBase) { stat })
                            : AddStatRow(gbStats, sy, Pretty(stat), nv.Value, file,
                                new List<object>(heroBase) { stat });
                }

                // Insert Armor Type combo immediately after the Armor row
                if (stat == "armor")
                {
                    PlistHelper.EnsureField(unitsDoc, heroData, heroBase, "armor_type", 0);
                    double? atNum = PlistHelper.AsNumber(heroData["armor_type"]);
                    if (atNum.HasValue)
                        sy = AddArmorTypeRow(gbStats, sy, atNum.Value, file,
                            new List<object>(heroBase) { "armor_type" });
                }
            }

            // ── Melee Attack sub-section ──
            var mainResult = FindHeroMainSkill(heroData);
            if (mainResult.Data != null)
            {
                sy += 4;
                var meleeHdr = new Label
                {
                    Text = "── Melee Attack ──",
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    ForeColor = Color.DarkSlateBlue,
                    AutoSize = true,
                    Location = new Point(8, sy)
                };
                gbStats.Controls.Add(meleeHdr);
                sy = meleeHdr.Bottom + 4;

                var skillPath = new List<object>(heroBase) { "skills", mainResult.Index };
                sy = RenderLevelAwareFields(gbStats, mainResult.Data, file, skillPath, sy, levelIdx);
            }

            // ── Regeneration sub-section ──
            var regenResult = FindHeroRegenSkill(heroData);
            if (regenResult.Data != null)
            {
                sy += 4;
                var regenHdr = new Label
                {
                    Text = "── Regeneration ──",
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    ForeColor = Color.DarkSlateBlue,
                    AutoSize = true,
                    Location = new Point(8, sy)
                };
                gbStats.Controls.Add(regenHdr);
                sy = regenHdr.Bottom + 4;

                var regenPath = new List<object>(heroBase) { "skills", regenResult.Index };
                sy = RenderLevelAwareFields(gbStats, regenResult.Data, file, regenPath, sy, levelIdx);
            }

            // ── Respawn sub-section ──
            var respawnResult = FindHeroSkillByKey(heroData, "respawn");
            if (respawnResult.Data != null && HasNumericContent(respawnResult.Data, 0))
            {
                sy += 4;
                var respawnHdr = new Label
                {
                    Text = "── Respawn ──",
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    ForeColor = Color.DarkSlateBlue,
                    AutoSize = true,
                    Location = new Point(8, sy)
                };
                gbStats.Controls.Add(respawnHdr);
                sy = respawnHdr.Bottom + 4;

                var respawnPath = new List<object>(heroBase) { "skills", respawnResult.Index };
                sy = RenderLevelAwareFields(gbStats, respawnResult.Data, file, respawnPath, sy, levelIdx);
            }

            gbStats.Height = sy + 10;
            contentPanel.Controls.Add(gbStats);

            // Portrait in header
            picPortrait.Image = IconExtractor.GetHeroIcon(heroKey);
            picPortrait.Visible = picPortrait.Image != null;
            picPortrait.Location = new Point(headerPanel.ClientSize.Width - picPortrait.Width - 8, 4);

            // ── Skills GroupBox (ability skills only, at skill level) ──
            var gbSkills = new GroupBox
            {
                Text = "Skills (Lv " + skillLevelIdx + ")",
                Location = new Point(322, 4),
                Size = new Size(contentPanel.Width > 530 ? contentPanel.Width - 334 : 400, 20),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                AutoSize = true
            };

            int sky = 20;
            var skillsList = PlistHelper.GetList(heroData, "skills");
            int maxSkillLvl = GetMaxSkillLevels(skillsList, heroData);
            if (skillsList != null)
            {
                for (int i = 0; i < skillsList.Count; i++)
                {
                    var skillDict = skillsList[i] as Dictionary<string, object>;
                    if (skillDict == null) continue;
                    if (IsHeroStatSkill(skillDict)) continue;
                    if (!HasNumericContent(skillDict, 0)) continue;

                    string skillId = PlistHelper.GetString(skillDict, "id")
                        ?? PlistHelper.GetString(skillDict, "key")
                        ?? $"skill_{i}";

                    var skillIcon = IconExtractor.GetHeroSkillIcon(heroKey, skillId);
                    var desc = Localization.FindHeroSkillDesc(heroKey, skillId);
                    var skillPath = new List<object>(heroBase) { "skills", i };

                    sky = RenderSkillSection(gbSkills, sky, skillId, skillDict, file,
                        skillPath, skillIcon, desc, skillLevelIdx, 0, maxSkillLvl);
                }
            }

            // Power (ultimate) at skill level
            var powerDict = PlistHelper.GetDict(heroData, "power");
            if (powerDict != null && HasNumericContent(powerDict, 0))
            {
                var powerIcon = IconExtractor.GetHeroSkillIcon(heroKey, "ultimate");
                var powerDesc = Localization.FindHeroSkillDesc(heroKey, "ultimate");
                var powerPath = new List<object>(heroBase) { "power" };
                sky = RenderSkillSection(gbSkills, sky, "Ultimate", powerDict, file,
                    powerPath, powerIcon, powerDesc, skillLevelIdx, 0, maxSkillLvl);
            }

            gbSkills.Height = sky + 10;
            contentPanel.Controls.Add(gbSkills);

            contentPanel.ResumeLayout(true);
        }

        // ══════════════════════════════════════════════════
        //  ENEMY DETAIL
        // ══════════════════════════════════════════════════

        void ShowEnemyDetail(ListItemTag item)
        {
            cbSkillLevel.Visible = false;
            lblSkillLevel.Visible = false;
            cbSkillLevel.Items.Clear();
            cbShowVFX.Visible = true;

            tbDetailName.Text = Pretty(item.Key);
            tbDetailKey.Text = item.Key;
            tbFaction.Text = Pretty(item.Faction);

            lblVariant.Visible = false;
            cbVariant.Visible = false;
            cbVariant.Items.Clear();

            contentPanel.SuspendLayout();
            contentPanel.Controls.Clear();
            contentPanel.AutoScrollPosition = new Point(0, 0);

            var data = item.Data;
            string file = "units";
            var basePath = new List<object> { item.Faction, item.Key };

            // ── Core Stats GroupBox ──
            var gbStats = new GroupBox
            {
                Text = "Stats",
                Location = new Point(4, 4),
                Size = new Size(280, 300),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            int sy = 20;
            // magic_resistance intentionally omitted — confirmed dead in the engine.
            // Armor type combo is rendered immediately after the armor row.
            string[] coreStats = { "health", "armor", "speed", "damage_min", "damage_max",
                                   "cooldown", "gold", "skulls" };

            foreach (var stat in coreStats)
            {
                if (!data.ContainsKey(stat)) continue;
                var val = data[stat];
                double? nv = PlistHelper.AsNumber(val);
                if (nv.HasValue)
                {
                    sy = AddStatRow(gbStats, sy, Pretty(stat), nv.Value, file, new List<object>(basePath) { stat });
                }
                else if (val is List<object> arr && arr.Count > 0 && arr.All(o => PlistHelper.AsNumber(o).HasValue))
                {
                    sy = CreateArrayRow(gbStats, 8, sy, Pretty(stat), file, new List<object>(basePath) { stat }, arr);
                }

                // Armor Type combo right after the Armor row (Magical = magic-resistant,
                // e.g. Glacial Wolf, Apex Stalker, Draugr)
                if (stat == "armor")
                {
                    double? atNum = PlistHelper.AsNumber(data.ContainsKey("armor_type") ? data["armor_type"] : null);
                    if (atNum.HasValue)
                        sy = AddArmorTypeRow(gbStats, sy, atNum.Value, file,
                            new List<object>(basePath) { "armor_type" });
                }
            }

            // Remaining numeric fields not in coreStats
            var coreSet = new HashSet<string>(coreStats) { "armor_type" };
            var remaining = data.Keys
                .Where(k => !coreSet.Contains(k) && !StaticData.Skip.Contains(k))
                .Where(k =>
                {
                    var v = data[k];
                    if (PlistHelper.AsNumber(v).HasValue) return true;
                    if (v is List<object> arr && arr.Count > 0 && arr.All(o => PlistHelper.AsNumber(o).HasValue)) return true;
                    return false;
                })
                .OrderBy(k => k)
                .ToList();

            foreach (var rk in remaining)
            {
                var val = data[rk];
                double? nv = PlistHelper.AsNumber(val);
                if (nv.HasValue)
                    sy = AddStatRow(gbStats, sy, Pretty(rk), nv.Value, file, new List<object>(basePath) { rk });
                else if (val is List<object> arr)
                    sy = CreateArrayRow(gbStats, 8, sy, Pretty(rk), file, new List<object>(basePath) { rk }, arr);
            }

            gbStats.Height = sy + 10;
            contentPanel.Controls.Add(gbStats);

            // Portrait in header
            picPortrait.Image = IconExtractor.GetEnemyIcon(item.Key);
            picPortrait.Visible = picPortrait.Image != null;
            picPortrait.Location = new Point(headerPanel.ClientSize.Width - picPortrait.Width - 8, 4);

            // ── Skills GroupBox ──
            var skillKeys = data.Keys
                .Where(k => data[k] is Dictionary<string, object> && !StaticData.Skip.Contains(k))
                .Where(k => HasNumericContent(data[k] as Dictionary<string, object>, 0))
                .OrderBy(k => k)
                .ToList();

            if (skillKeys.Count > 0)
            {
                var gbSkills = new GroupBox
                {
                    Text = "Skills",
                    Location = new Point(296, 4),
                    Size = new Size(contentPanel.Width > 500 ? contentPanel.Width - 308 : 400, 20),
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    AutoSize = true
                };

                int sky = 20;
                foreach (var sk in skillKeys)
                {
                    var skillDict = data[sk] as Dictionary<string, object>;
                    sky = RenderSkillSection(gbSkills, sky, sk, skillDict, file,
                        new List<object>(basePath) { sk }, null, null);
                }

                gbSkills.Height = sky + 10;
                contentPanel.Controls.Add(gbSkills);
            }

            contentPanel.ResumeLayout(true);
        }

        // ══════════════════════════════════════════════════
        //  SHARED: RENDER A SKILL SECTION WITHIN A GROUPBOX
        // ══════════════════════════════════════════════════

        /// <summary>
        /// Renders a skill section (header + icon + description + fields) inside a parent GroupBox.
        /// Returns the Y offset after this section.
        /// </summary>
        int RenderSkillSection(Control parent, int startY, string skillKey,
            Dictionary<string, object> skillDict, string file, List<object> skillPath,
            Bitmap skillIcon, string description, int levelIdx = -1, int levelLabelOffset = 1, int maxLevels = -1)
        {
            int y = startY;

            // Separator line
            if (y > 20)
            {
                var sep = new Label
                {
                    AutoSize = false,
                    Size = new Size(parent.Width - 24, 1),
                    Location = new Point(8, y),
                    BackColor = SystemColors.ControlDark
                };
                parent.Controls.Add(sep);
                y += 6;
            }

            // Skill name header + icon (56x56)
            int headerX = 8;
            int iconH = 0;

            if (skillIcon != null)
            {
                var pic = new PictureBox
                {
                    Image = skillIcon,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Size = new Size(56, 56),
                    Location = new Point(headerX, y),
                    BackColor = Color.Transparent
                };
                parent.Controls.Add(pic);
                headerX += 62;
                iconH = 56;
            }

            var nameLabel = new Label
            {
                Text = Pretty(skillKey),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.DarkSlateBlue,
                AutoSize = true,
                Location = new Point(headerX, y + 2)
            };
            parent.Controls.Add(nameLabel);

            // Description (next to icon, below name)
            int descBottom = nameLabel.Bottom;
            if (!string.IsNullOrEmpty(description))
            {
                var descLbl = new Label
                {
                    Text = description,
                    Font = new Font("Segoe UI", 7.5f, FontStyle.Italic),
                    ForeColor = Color.FromArgb(100, 100, 120),
                    MaximumSize = new Size(parent.Width - headerX - 12, 0),
                    AutoSize = true,
                    Location = new Point(headerX, nameLabel.Bottom + 2)
                };
                parent.Controls.Add(descLbl);
                descBottom = descLbl.Bottom;
            }

            y = Math.Max(descBottom, y + iconH) + 4;

            // Render fields
            if (levelIdx >= 0)
                y = RenderLevelAwareFields(parent, skillDict, file, skillPath, y, levelIdx, levelLabelOffset, maxLevels);
            else
                y = RenderFields(parent, skillDict, file, skillPath, y);

            // ── Summoned unit stats (detect "unit" references in skill data) ──
            var summonedKeys = CollectSummonedUnitKeys(skillDict);
            foreach (var uk in summonedKeys)
                y = RenderSummonedUnitBlock(parent, y, uk);

            return y + 4;
        }

        /// <summary>
        /// Like RenderFields but level-aware: when levelIdx is within an array's bounds,
        /// show only the value at that level (single editable textbox). Scalars always
        /// render plain — we don't expand them into per-level arrays, because the game
        /// engine (esp. on Android) can't always handle that (e.g. Veruk's `cant` only
        /// supports 2 levels; growing it breaks summoning).
        /// isVFX=true highlights labels with yellow (for "object" sub-dict content).
        /// </summary>
        int RenderLevelAwareFields(Control parent, Dictionary<string, object> data, string file,
            List<object> basePath, int startY, int levelIdx, int levelLabelOffset = 1,
            int maxLevels = -1, bool isVFX = false)
        {
            if (data == null) return startY;
            int y = startY;
            bool showVFX = cbShowVFX?.Checked ?? false;
            Color? hl = isVFX ? (Color?)VFXHighlight : null;

            foreach (var kv in data.OrderBy(k => k.Key))
            {
                string key = kv.Key;
                bool isObjectKey = key == "object";
                if (StaticData.Skip.Contains(key))
                {
                    if (!(showVFX && isObjectKey)) continue;
                }

                var path = new List<object>(basePath) { key };
                double? numVal = PlistHelper.AsNumber(kv.Value);

                if (numVal.HasValue)
                {
                    // Scalars always render as plain scalars (no per-level indexing).
                    // Expanding a scalar into an array of per-level values breaks Android compat.
                    CreateFieldRow(parent, 8, y, Pretty(key), file, path, numVal.Value, hl);
                    y += 24;
                    continue;
                }

                if (kv.Value is List<object> arr && arr.Count > 0)
                {
                    if (arr.All(o => PlistHelper.AsNumber(o).HasValue))
                    {
                        if (levelIdx < arr.Count)
                        {
                            // In-bounds: show only the value at the selected level.
                            double v = PlistHelper.AsNumber(arr[levelIdx]) ?? 0;
                            CreateFieldRow(parent, 8, y, Pretty(key) + $" [Lv{levelIdx + levelLabelOffset}]", file,
                                new List<object>(path) { levelIdx }, v, hl);
                            y += 24;
                        }
                        else
                        {
                            // Out-of-bounds: show the full array read-only-ish (don't grow it).
                            // Growing arrays beyond the game's supported level count breaks things
                            // (e.g. Veruk's `cant` only supports 2 levels; 3 elements broke summoning).
                            y = CreateArrayRow(parent, 8, y, Pretty(key), file, path, arr, hl);
                        }
                        continue;
                    }
                    // List of dicts (e.g. modifier_tick, effects, paths_objects, object[fx,decal])
                    if (arr.All(o => o is Dictionary<string, object>))
                    {
                        bool anyNumeric = arr.Cast<Dictionary<string, object>>()
                            .Any(d => HasNumericContent(d, 1));
                        if (!anyNumeric) continue;
                        var lstLbl = new Label
                        {
                            Text = Pretty(key) + ":",
                            Font = new Font("Segoe UI", 8, FontStyle.Bold),
                            ForeColor = isVFX ? Color.FromArgb(120, 100, 0) : FieldLabelColor,
                            BackColor = isVFX ? VFXHighlight : Color.Transparent,
                            AutoSize = true,
                            Location = new Point(8, y)
                        };
                        parent.Controls.Add(lstLbl);
                        y = lstLbl.Bottom + 2;
                        for (int idx = 0; idx < arr.Count; idx++)
                        {
                            var sd = (Dictionary<string, object>)arr[idx];
                            if (!HasNumericContent(sd, 1)) continue;
                            string idLabel = sd.TryGetValue("id", out var idv) ? idv as string
                                          : sd.TryGetValue("key", out var kv2) ? kv2 as string : null;
                            var idxLbl = new Label
                            {
                                Text = idLabel != null ? $"[{idx}] {Pretty(idLabel)}" : $"[{idx}]",
                                Font = new Font("Segoe UI", 7.5f, FontStyle.Italic),
                                ForeColor = Color.Gray,
                                AutoSize = true,
                                Location = new Point(20, y)
                            };
                            parent.Controls.Add(idxLbl);
                            y = idxLbl.Bottom + 1;
                            var subPath = new List<object>(path) { idx };
                            y = RenderLevelAwareFields(parent, sd, file, subPath, y, levelIdx,
                                levelLabelOffset, maxLevels, isVFX);
                        }
                        continue;
                    }
                }

                if (kv.Value is Dictionary<string, object> sub && HasNumericContent(sub, 1))
                {
                    bool childVFX = isVFX || isObjectKey;
                    var lbl = new Label
                    {
                        Text = Pretty(key),
                        Font = new Font("Segoe UI", 8, FontStyle.Bold),
                        ForeColor = childVFX ? Color.FromArgb(120, 100, 0) : FieldLabelColor,
                        BackColor = childVFX ? VFXHighlight : Color.Transparent,
                        AutoSize = true,
                        Location = new Point(8, y)
                    };
                    parent.Controls.Add(lbl);
                    y = lbl.Bottom + 2;
                    y = RenderLevelAwareFields(parent, sub, file, path, y, levelIdx,
                        levelLabelOffset, maxLevels, childVFX);
                }
            }
            return y;
        }
    }
}
