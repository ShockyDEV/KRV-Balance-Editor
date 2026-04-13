using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace BalanceEditor
{
    public partial class MainForm
    {
        // ══════════════════════════════════════════════════
        //  TIER LIST — BALANCE STATUS
        // ══════════════════════════════════════════════════

        static readonly (string Id, string Label, Color Color)[] TierDefs =
        {
            ("S", "Broken",      Color.FromArgb(255, 150, 160)),
            ("A", "Really Good", Color.FromArgb(255, 190, 110)),
            ("B", "Strong",      Color.FromArgb(255, 230, 100)),
            ("C", "Balanced",    Color.FromArgb(210, 230, 110)),
            ("D", "Useless",     Color.FromArgb(170, 220, 150)),
        };

        Panel tierListPanel;
        ComboBox cbTierMode;
        FlowLayoutPanel[] tierRows;
        FlowLayoutPanel tierPool;
        ToolTip tierToolTip;
        string tierListFilePath;

        // ── Show / Hide ──

        void ShowTierList()
        {
            listBox.Items.Clear();
            allItems.Clear();
            ClearDetail();

            headerPanel.Visible = false;
            contentPanel.Visible = false;
            listBox.Parent.Visible = false; // hide left panel

            if (tierListPanel == null)
                BuildTierListUI();

            tierListPanel.Visible = true;

            if (cbTierMode.SelectedIndex < 0)
                cbTierMode.SelectedIndex = 0;
            else
                PopulateTierIcons(cbTierMode.SelectedItem.ToString());
        }

        void HideTierList()
        {
            if (tierListPanel != null)
                tierListPanel.Visible = false;

            headerPanel.Visible = true;
            contentPanel.Visible = true;
            listBox.Parent.Visible = true;
        }

        // ── Build UI ──

        void BuildTierListUI()
        {
            tierListFilePath = Path.Combine(
                Path.GetDirectoryName(Application.ExecutablePath), "tier_list.json");
            tierToolTip = new ToolTip { AutoPopDelay = 5000, InitialDelay = 300 };

            tierListPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(45, 45, 55)
            };

            // ── Toolbar ──
            var toolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.FromArgb(55, 55, 65),
                Padding = new Padding(6, 4, 6, 4)
            };

            var lblMode = new Label
            {
                Text = "Mode:",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9),
                AutoSize = true,
                Location = new Point(8, 8)
            };
            cbTierMode = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 100,
                Location = new Point(56, 5),
                Items = { "Heroes", "Towers" }
            };
            cbTierMode.SelectedIndexChanged += (s, e) =>
                PopulateTierIcons(cbTierMode.SelectedItem?.ToString() ?? "Heroes");

            var btnSave = new Button
            {
                Text = "Save Tiers",
                Width = 90, Height = 26,
                Location = new Point(172, 5),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(80, 160, 80),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8, FontStyle.Bold)
            };
            btnSave.Click += (s, e) => SaveTierList();

            var btnLoad = new Button
            {
                Text = "Load Tiers",
                Width = 90, Height = 26,
                Location = new Point(270, 5),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(80, 120, 200),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8, FontStyle.Bold)
            };
            btnLoad.Click += (s, e) => LoadTierList();

            toolbar.Controls.AddRange(new Control[] { lblMode, cbTierMode, btnSave, btnLoad });

            // ── Tier rows (added bottom-up since DockStyle.Top stacks in reverse add order) ──
            tierRows = new FlowLayoutPanel[TierDefs.Length];

            // Pool (unassigned) — added first so it ends up at bottom
            var poolHeader = new Label
            {
                Text = "  Unassigned",
                Dock = DockStyle.Top,
                Height = 24,
                ForeColor = Color.LightGray,
                BackColor = Color.FromArgb(60, 60, 70),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };

            tierPool = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(55, 55, 65),
                AllowDrop = true,
                WrapContents = true,
                Padding = new Padding(4)
            };
            tierPool.DragEnter += TierRow_DragEnter;
            tierPool.DragDrop += TierRow_DragDrop;

            // Build tier rows (reverse order for Dock.Top stacking)
            var tierRowPanels = new List<Panel>();
            for (int i = TierDefs.Length - 1; i >= 0; i--)
            {
                var def = TierDefs[i];
                var rowPanel = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 80,
                    BackColor = def.Color,
                    Padding = new Padding(0)
                };

                var tierLabel = new Label
                {
                    Text = def.Id + "\n" + def.Label,
                    Dock = DockStyle.Left,
                    Width = 90,
                    Font = new Font("Segoe UI", 12, FontStyle.Bold),
                    ForeColor = Color.FromArgb(60, 40, 40),
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.FromArgb(
                        Math.Max(0, def.Color.R - 30),
                        Math.Max(0, def.Color.G - 30),
                        Math.Max(0, def.Color.B - 30))
                };

                var flow = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    AutoScroll = false,
                    WrapContents = true,
                    AllowDrop = true,
                    BackColor = def.Color,
                    Padding = new Padding(4, 4, 4, 4),
                    AutoSize = false
                };
                flow.DragEnter += TierRow_DragEnter;
                flow.DragDrop += TierRow_DragDrop;

                rowPanel.Controls.Add(flow);
                rowPanel.Controls.Add(tierLabel);

                tierRows[i] = flow;
                tierRowPanels.Add(rowPanel);

                // Separator line between tiers
                var sep = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 2,
                    BackColor = Color.FromArgb(35, 35, 45)
                };
                tierRowPanels.Add(sep);
            }

            // Assemble: pool first (fills remaining), then tier rows bottom-up, then toolbar on top
            tierListPanel.Controls.Add(tierPool);
            tierListPanel.Controls.Add(poolHeader);
            foreach (var rp in tierRowPanels)
                tierListPanel.Controls.Add(rp);
            tierListPanel.Controls.Add(toolbar);

            rightPanel.Controls.Add(tierListPanel);
        }

        // ── Populate icons ──

        void PopulateTierIcons(string mode)
        {
            // Clear all rows
            foreach (var row in tierRows)
                row.Controls.Clear();
            tierPool.Controls.Clear();

            // Build icon lookup: key → (displayName, bitmap)
            var icons = new Dictionary<string, (string Name, Bitmap Icon)>();

            if (mode == "Heroes")
            {
                var heroesDict = PlistHelper.GetDict(unitsData, "heroes");
                if (heroesDict != null)
                {
                    foreach (var kv in heroesDict)
                    {
                        string name = StaticData.HeroNames.TryGetValue(kv.Key, out var hn) ? hn : Pretty(kv.Key);
                        var icon = IconExtractor.GetHeroIcon(kv.Key);
                        icons[kv.Key] = (name, icon);
                    }
                }
            }
            else // Towers
            {
                var families = GroupTowerFamilies();
                foreach (var fam in families.Values)
                {
                    string topVar = fam.Variants.Last();
                    var icon = IconExtractor.GetTowerIcon(topVar);
                    icons[fam.BaseName] = (Pretty(fam.BaseName), icon);
                }
            }

            // Load saved assignments
            var saved = LoadTierListFromFile();
            var tierMap = new Dictionary<string, string>(); // key → tier ID
            if (saved != null)
            {
                var tiers = mode == "Heroes" ? saved.HeroTiers : saved.TowerTiers;
                if (tiers != null)
                {
                    foreach (var kv in tiers)
                        foreach (var key in kv.Value)
                            tierMap[key] = kv.Key;
                }
            }

            // Place icons
            foreach (var kv in icons.OrderBy(k => k.Value.Name))
            {
                var pic = CreateTierIcon(kv.Key, kv.Value.Name, kv.Value.Icon);

                if (tierMap.TryGetValue(kv.Key, out var tierId))
                {
                    int idx = Array.FindIndex(TierDefs, t => t.Id == tierId);
                    if (idx >= 0)
                        tierRows[idx].Controls.Add(pic);
                    else
                        tierPool.Controls.Add(pic);
                }
                else
                {
                    tierPool.Controls.Add(pic);
                }
            }
        }

        PictureBox CreateTierIcon(string key, string displayName, Bitmap icon)
        {
            var pic = new PictureBox
            {
                Size = new Size(64, 64),
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = icon,
                BackColor = Color.FromArgb(30, 30, 40),
                Margin = new Padding(3),
                Cursor = Cursors.Hand,
                Tag = key,
                BorderStyle = BorderStyle.FixedSingle
            };

            tierToolTip.SetToolTip(pic, displayName);
            pic.MouseDown += TierIcon_MouseDown;
            return pic;
        }

        // ── Drag & Drop with floating ghost ──

        Form dragGhost;

        void TierIcon_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var pic = (PictureBox)sender;
                ShowDragGhost(pic);
                pic.GiveFeedback += DragGhost_GiveFeedback;
                var result = pic.DoDragDrop(pic, DragDropEffects.Move);
                pic.GiveFeedback -= DragGhost_GiveFeedback;
                HideDragGhost();
            }
        }

        void ShowDragGhost(PictureBox source)
        {
            dragGhost = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                ShowInTaskbar = false,
                TopMost = true,
                Size = new Size(68, 68),
                StartPosition = FormStartPosition.Manual,
                BackColor = Color.Magenta,
                TransparencyKey = Color.Magenta,
                Opacity = 0.85
            };

            var pic = new PictureBox
            {
                Image = source.Image,
                SizeMode = PictureBoxSizeMode.Zoom,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };
            dragGhost.Controls.Add(pic);

            var pos = Cursor.Position;
            dragGhost.Location = new Point(pos.X - 34, pos.Y - 34);
            dragGhost.Show();
        }

        void DragGhost_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            if (dragGhost != null && dragGhost.Visible)
            {
                var pos = Cursor.Position;
                dragGhost.Location = new Point(pos.X - 34, pos.Y - 34);
            }
            e.UseDefaultCursors = true;
        }

        void HideDragGhost()
        {
            if (dragGhost != null)
            {
                dragGhost.Close();
                dragGhost.Dispose();
                dragGhost = null;
            }
        }

        void TierRow_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(PictureBox)))
                e.Effect = DragDropEffects.Move;
        }

        void TierRow_DragDrop(object sender, DragEventArgs e)
        {
            var pic = e.Data.GetData(typeof(PictureBox)) as PictureBox;
            if (pic == null) return;

            var target = (FlowLayoutPanel)sender;
            if (pic.Parent == target) return;

            pic.Parent.Controls.Remove(pic);
            target.Controls.Add(pic);
        }

        // ── Persistence ──

        void SaveTierList()
        {
            try
            {
                var data = LoadTierListFromFile() ?? new TierListData();
                if (data.HeroTiers == null)
                    data.HeroTiers = new Dictionary<string, List<string>>();
                if (data.TowerTiers == null)
                    data.TowerTiers = new Dictionary<string, List<string>>();

                string mode = cbTierMode?.SelectedItem?.ToString() ?? "Heroes";
                var currentTiers = BuildTiersFromUI();

                if (mode == "Heroes")
                    data.HeroTiers = currentTiers;
                else
                    data.TowerTiers = currentTiers;

                var serializer = new JavaScriptSerializer();
                string json = serializer.Serialize(data);
                File.WriteAllText(tierListFilePath, json);

                MessageBox.Show($"{mode} tier list saved!", "Balance Status",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save failed:\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void LoadTierList()
        {
            if (!File.Exists(tierListFilePath))
            {
                MessageBox.Show("No tier list file found.", "Load",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            PopulateTierIcons(cbTierMode?.SelectedItem?.ToString() ?? "Heroes");
            MessageBox.Show("Tier list loaded!", "Balance Status",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        Dictionary<string, List<string>> BuildTiersFromUI()
        {
            var result = new Dictionary<string, List<string>>();
            for (int i = 0; i < TierDefs.Length; i++)
            {
                var keys = new List<string>();
                foreach (Control c in tierRows[i].Controls)
                {
                    if (c is PictureBox pb && pb.Tag is string key)
                        keys.Add(key);
                }
                result[TierDefs[i].Id] = keys;
            }
            return result;
        }

        TierListData LoadTierListFromFile()
        {
            try
            {
                if (tierListFilePath == null)
                    tierListFilePath = Path.Combine(
                        Path.GetDirectoryName(Application.ExecutablePath), "tier_list.json");

                if (!File.Exists(tierListFilePath)) return null;
                string json = File.ReadAllText(tierListFilePath);
                var serializer = new JavaScriptSerializer();
                return serializer.Deserialize<TierListData>(json);
            }
            catch
            {
                return null;
            }
        }
    }
}
