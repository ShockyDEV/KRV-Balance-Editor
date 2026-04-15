using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace BalanceEditor
{
    public partial class MainForm
    {
        // ══════════════════════════════════════════════════
        //  INPUT CREATION
        // ══════════════════════════════════════════════════

        TextBox MkInput(double value, string file, List<object> dataPath)
        {
            string changeKey = MakeChangeKey(file, dataPath);

            var tb = new TextBox
            {
                Width = 65,
                Height = 22,
                Font = new Font("Consolas", 9),
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = HorizontalAlignment.Right,
                Text = FormatNumber(value)
            };

            var tag = new InputTag
            {
                OriginalValue = value,
                FilePath = file,
                DataPath = new List<object>(dataPath),
                ChangeKey = changeKey
            };
            tb.Tag = tag;

            if (Changes.TryGetValue(changeKey, out var existing))
            {
                tb.Text = FormatNumber(existing.Value);
                tb.ForeColor = GoldColor;
                tb.Font = new Font(tb.Font, FontStyle.Bold);
            }

            tb.Leave += Input_Leave;
            tb.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; contentPanel.Focus(); }
            };

            return tb;
        }

        void Input_Leave(object sender, EventArgs e)
        {
            var tb = (TextBox)sender;
            var tag = (InputTag)tb.Tag;

            if (!double.TryParse(tb.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double newVal))
            {
                tb.Text = Changes.TryGetValue(tag.ChangeKey, out var ce)
                    ? FormatNumber(ce.Value) : FormatNumber(tag.OriginalValue);
                return;
            }

            if (Math.Abs(newVal - tag.OriginalValue) < 1e-9)
            {
                Changes.Remove(tag.ChangeKey);
                tb.ForeColor = SystemColors.WindowText;
                tb.Font = new Font(tb.Font, FontStyle.Regular);
            }
            else
            {
                Changes[tag.ChangeKey] = new ChangeEntry
                {
                    File = tag.FilePath,
                    Path = tag.DataPath,
                    Value = newVal
                };
                tb.ForeColor = GoldColor;
                tb.Font = new Font(tb.Font, FontStyle.Bold);
            }

            tb.Text = FormatNumber(newVal);
            UpdateStatus();
        }

        // ══════════════════════════════════════════════════
        //  ARMOR TYPE COMBO (Physical = 0, Magical = 1)
        // ══════════════════════════════════════════════════

        static readonly string[] ArmorTypeNames = { "Physical", "Magical" };

        ComboBox MkArmorTypeCombo(double value, string file, List<object> dataPath)
        {
            string changeKey = MakeChangeKey(file, dataPath);
            int idx = (int)Math.Round(value);
            if (idx < 0 || idx >= ArmorTypeNames.Length) idx = 0;

            var cb = new ComboBox
            {
                Width = 90,
                Height = 22,
                Font = new Font("Segoe UI", 8.5f),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };
            cb.Items.AddRange(ArmorTypeNames);
            cb.SelectedIndex = idx;

            var tag = new InputTag
            {
                OriginalValue = value,
                FilePath = file,
                DataPath = new List<object>(dataPath),
                ChangeKey = changeKey
            };
            cb.Tag = tag;

            if (Changes.TryGetValue(changeKey, out var existing))
            {
                int eIdx = (int)Math.Round(existing.Value);
                if (eIdx >= 0 && eIdx < ArmorTypeNames.Length) cb.SelectedIndex = eIdx;
                cb.ForeColor = GoldColor;
                cb.Font = new Font(cb.Font, FontStyle.Bold);
            }

            cb.SelectedIndexChanged += (s, e) =>
            {
                var c = (ComboBox)s;
                var t = (InputTag)c.Tag;
                double newVal = c.SelectedIndex;
                if (Math.Abs(newVal - t.OriginalValue) < 1e-9)
                {
                    Changes.Remove(t.ChangeKey);
                    c.ForeColor = SystemColors.WindowText;
                    c.Font = new Font(c.Font, FontStyle.Regular);
                }
                else
                {
                    Changes[t.ChangeKey] = new ChangeEntry
                    {
                        File = t.FilePath,
                        Path = t.DataPath,
                        Value = newVal
                    };
                    c.ForeColor = GoldColor;
                    c.Font = new Font(c.Font, FontStyle.Bold);
                }
                UpdateStatus();
            };

            return cb;
        }

        int AddArmorTypeRow(Control parent, int y, double value, string file, List<object> path)
        {
            return AddArmorTypeRow(parent, y, value, file, path, 8);
        }

        int AddArmorTypeRow(Control parent, int y, double value, string file, List<object> path, int startX)
        {
            var lbl = new Label
            {
                Text = "Armor Type",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = FieldLabelColor,
                AutoSize = false,
                Size = new Size(140, 20),
                Location = new Point(startX, y + 2),
                TextAlign = ContentAlignment.MiddleLeft
            };
            parent.Controls.Add(lbl);

            var cb = MkArmorTypeCombo(value, file, path);
            cb.Location = new Point(startX + 144, y);
            parent.Controls.Add(cb);

            // Tooltip-style hint after the combo
            var hint = new Label
            {
                Text = "(Magical = magic-resistant)",
                Font = new Font("Segoe UI", 7f, FontStyle.Italic),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(startX + 144 + 96, y + 4)
            };
            parent.Controls.Add(hint);

            return y + 26;
        }

        // ══════════════════════════════════════════════════
        //  FIELD RENDERING HELPERS
        // ══════════════════════════════════════════════════

        int RenderFields(Control parent, Dictionary<string, object> data, string file,
            List<object> basePath, int startY, int depth = 0, HashSet<string> exclude = null,
            bool isVFX = false)
        {
            if (data == null || depth > 7) return startY;
            int y = startY;
            int leftMargin = 8 + depth * 12;
            bool showVFX = cbShowVFX?.Checked ?? false;
            Color? hl = isVFX ? (Color?)VFXHighlight : null;

            foreach (var kv in data.OrderBy(k => k.Key))
            {
                string key = kv.Key;
                bool isObjectKey = key == "object";
                if (StaticData.Skip.Contains(key) && !(showVFX && isObjectKey)) continue;
                if (exclude != null && exclude.Contains(key)) continue;

                var path = new List<object>(basePath) { key };
                double? numVal = PlistHelper.AsNumber(kv.Value);

                if (numVal.HasValue)
                {
                    CreateFieldRow(parent, leftMargin, y, Pretty(key), file, path, numVal.Value, hl);
                    y += 24;
                    continue;
                }

                if (kv.Value is List<object> arr && arr.Count > 0)
                {
                    if (arr.All(o => PlistHelper.AsNumber(o).HasValue))
                    {
                        y = CreateArrayRow(parent, leftMargin, y, Pretty(key), file, path, arr, hl);
                        continue;
                    }
                    // List of dicts (e.g. modifier_tick, effects, paths_objects)
                    if (arr.All(o => o is Dictionary<string, object>))
                    {
                        bool anyNumeric = arr.Cast<Dictionary<string, object>>()
                            .Any(d => HasNumericContent(d, depth + 1));
                        if (!anyNumeric) continue;
                        var lstLbl = new Label
                        {
                            Text = Pretty(key) + ":",
                            Font = new Font("Segoe UI", 8, FontStyle.Bold),
                            ForeColor = isVFX ? Color.FromArgb(120, 100, 0) : FieldLabelColor,
                            BackColor = isVFX ? VFXHighlight : Color.Transparent,
                            AutoSize = true,
                            Location = new Point(leftMargin, y)
                        };
                        parent.Controls.Add(lstLbl);
                        y = lstLbl.Bottom + 2;
                        for (int idx = 0; idx < arr.Count; idx++)
                        {
                            var sd = (Dictionary<string, object>)arr[idx];
                            if (!HasNumericContent(sd, depth + 1)) continue;
                            string idLabel = sd.TryGetValue("id", out var idv) ? idv as string
                                          : sd.TryGetValue("key", out var kv2) ? kv2 as string : null;
                            var idxLbl = new Label
                            {
                                Text = idLabel != null ? $"[{idx}] {Pretty(idLabel)}" : $"[{idx}]",
                                Font = new Font("Segoe UI", 7.5f, FontStyle.Italic),
                                ForeColor = Color.Gray,
                                AutoSize = true,
                                Location = new Point(leftMargin + 12, y)
                            };
                            parent.Controls.Add(idxLbl);
                            y = idxLbl.Bottom + 1;
                            var subPath = new List<object>(path) { idx };
                            y = RenderFields(parent, sd, file, subPath, y, depth + 1, null, isVFX);
                        }
                        continue;
                    }
                }

                if (kv.Value is Dictionary<string, object> sub && HasNumericContent(sub, depth + 1))
                {
                    bool childVFX = isVFX || isObjectKey;
                    var lbl = new Label
                    {
                        Text = Pretty(key),
                        Font = new Font("Segoe UI", 8, FontStyle.Bold),
                        ForeColor = childVFX ? Color.FromArgb(120, 100, 0) : FieldLabelColor,
                        BackColor = childVFX ? VFXHighlight : Color.Transparent,
                        AutoSize = true,
                        Location = new Point(leftMargin, y)
                    };
                    parent.Controls.Add(lbl);
                    y = lbl.Bottom + 2;
                    y = RenderFields(parent, sub, file, path, y, depth + 1, null, childVFX);
                }
            }
            return y;
        }

        bool HasNumericContent(Dictionary<string, object> dict, int depth)
        {
            if (depth > 7) return false;
            foreach (var kv in dict)
            {
                if (StaticData.Skip.Contains(kv.Key)) continue;
                if (PlistHelper.AsNumber(kv.Value).HasValue) return true;
                if (kv.Value is List<object> arr && arr.Count > 0)
                {
                    if (arr.All(o => PlistHelper.AsNumber(o).HasValue)) return true;
                    // List of dicts (e.g. modifier_tick, effects) — recurse
                    if (arr.All(o => o is Dictionary<string, object>))
                    {
                        foreach (var item in arr)
                            if (HasNumericContent((Dictionary<string, object>)item, depth + 1)) return true;
                    }
                }
                if (kv.Value is Dictionary<string, object> sub && HasNumericContent(sub, depth + 1))
                    return true;
            }
            return false;
        }

        void CreateFieldRow(Control parent, int left, int top, string label, string file,
            List<object> path, double value, Color? highlight = null)
        {
            var lbl = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = highlight.HasValue ? Color.FromArgb(120, 100, 0) : FieldLabelColor,
                BackColor = highlight ?? Color.Transparent,
                AutoSize = false,
                Size = new Size(140, 20),
                Location = new Point(left, top + 2),
                TextAlign = ContentAlignment.MiddleLeft
            };
            parent.Controls.Add(lbl);

            var tb = MkInput(value, file, path);
            tb.Location = new Point(left + 144, top);
            parent.Controls.Add(tb);
        }

        int CreateArrayRow(Control parent, int left, int top, string label, string file,
            List<object> basePath, List<object> arr, Color? highlight = null)
        {
            var lbl = new Label
            {
                Text = label + ":",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = highlight.HasValue ? Color.FromArgb(120, 100, 0) : FieldLabelColor,
                BackColor = highlight ?? Color.Transparent,
                AutoSize = true,
                Location = new Point(left, top + 2)
            };
            parent.Controls.Add(lbl);
            top = lbl.Bottom + 2;

            int x = left;
            int count = 0;
            for (int i = 0; i < arr.Count; i++)
            {
                double v = PlistHelper.AsNumber(arr[i]) ?? 0;
                var path = new List<object>(basePath) { i };
                var tb = MkInput(v, file, path);
                tb.Width = 54;
                tb.Location = new Point(x, top);
                parent.Controls.Add(tb);

                var idxLbl = new Label
                {
                    Text = i.ToString(),
                    Font = new Font("Segoe UI", 6.5f),
                    ForeColor = Color.Gray,
                    Size = new Size(54, 12),
                    Location = new Point(x, top - 13),
                    TextAlign = ContentAlignment.BottomCenter
                };
                parent.Controls.Add(idxLbl);

                x += 58;
                count++;
                if (count >= 6 && i < arr.Count - 1)
                {
                    count = 0;
                    x = left;
                    top += 36;
                }
            }
            return top + 28;
        }

        int AddStatRow(Control parent, int y, string label, double value, string file, List<object> path)
        {
            CreateFieldRow(parent, 8, y, label, file, path, value);
            return y + 24;
        }

        // ══════════════════════════════════════════════════
        //  ARMOR PIPS
        // ══════════════════════════════════════════════════

        static int GetArmorPips(double value)
        {
            if (value <= 0) return 0;
            if (value <= 30) return 1;
            if (value <= 60) return 2;
            return 3;
        }

        static readonly string[] ArmorPipLabels = { "None", "Low", "Medium", "High" };

        int AddArmorStatRow(Control parent, int y, string label, double value, string file, List<object> path)
        {
            CreateFieldRow(parent, 8, y, label, file, path, value);

            int pips = GetArmorPips(value);
            int pipX = 8 + 144 + 70;
            for (int i = 0; i < 3; i++)
            {
                var pip = new Panel
                {
                    Size = new Size(10, 10),
                    Location = new Point(pipX + i * 14, y + 5),
                    BackColor = i < pips ? Color.Gold : Color.FromArgb(200, 200, 200),
                };
                parent.Controls.Add(pip);
            }

            var pipLbl = new Label
            {
                Text = $"{ArmorPipLabels[pips]}",
                Font = new Font("Segoe UI", 7f),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(pipX + 46, y + 3)
            };
            parent.Controls.Add(pipLbl);

            return y + 24;
        }

        // ══════════════════════════════════════════════════
        //  TOWER HELPERS
        // ══════════════════════════════════════════════════

        string FindMainSkill(Dictionary<string, object> data)
        {
            string[] priorities = { "range_main", "range", "forward_main", "forward", "single" };
            foreach (var sk in priorities)
                if (data.ContainsKey(sk) && data[sk] is Dictionary<string, object>)
                    return sk;
            foreach (var kv in data)
                if (kv.Value is Dictionary<string, object> sd && sd.ContainsKey("damage_min"))
                    return kv.Key;
            return null;
        }

        string GetTowerLocKey(string variantKey, string baseName)
        {
            if (baseName == "dark_army_mage") return "dark_army_mage_crimson_zealot";
            if (StaticData.TowerSkillIcons.ContainsKey(variantKey)) return variantKey;
            string lv4 = baseName + "_level4";
            if (StaticData.TowerSkillIcons.ContainsKey(lv4)) return lv4;
            string[] suffixes = { "_special", "_orc_dens", "_spear_throwers", "_dark_knight", "_blood_altar", "_crimson_zealot" };
            foreach (var sfx in suffixes)
            {
                string k = baseName + sfx;
                if (StaticData.TowerSkillIcons.ContainsKey(k)) return k;
            }
            return null;
        }

        // ══════════════════════════════════════════════════
        //  UTILITY
        // ══════════════════════════════════════════════════

        static string Pretty(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            s = s.Replace("_", " ");
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s.ToLowerInvariant());
        }

        static string FormatNumber(double v)
        {
            if (v == Math.Floor(v) && Math.Abs(v) < 1e15)
                return ((long)v).ToString();
            return v.ToString("G", CultureInfo.InvariantCulture);
        }

        static string MakeChangeKey(string file, List<object> path)
        {
            var parts = path.Select(p => p is int || p is long ? p.ToString() : "\"" + p + "\"");
            return file + ":[" + string.Join(",", parts) + "]";
        }

        void UpdateStatus()
        {
            int n = Changes.Count;
            lblStatus.Text = n == 0 ? "No changes" : $"{n} unsaved change{(n == 1 ? "" : "s")}";
            lblStatus.ForeColor = n > 0 ? Color.DarkRed : SystemColors.ControlText;
        }
    }
}
