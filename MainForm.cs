using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml.Linq;

namespace BalanceEditor
{
    public partial class MainForm : Form
    {
        // ── Highlight color for changes ──
        static readonly Color GoldColor = ColorTranslator.FromHtml("#ffd700");
        static readonly Color FieldLabelColor = Color.FromArgb(60, 60, 80);

        // ── Data ──
        string gameRoot;
        Dictionary<string, object> towersData;
        XDocument towersDoc;
        Dictionary<string, object> unitsData;
        XDocument unitsDoc;
        string towersFilePath, unitsFilePath;

        // ── Changes ──
        Dictionary<string, ChangeEntry> Changes = new Dictionary<string, ChangeEntry>();

        // ── UI controls ──
        ListBox listBox;
        ComboBox cbCategory;
        TextBox tbSearchName, tbSearchKey;
        Panel rightPanel, headerPanel, contentPanel;
        TextBox tbDetailName, tbDetailKey, tbFaction;
        ComboBox cbVariant;
        Label lblVariant;
        ComboBox cbSkillLevel;
        Label lblSkillLevel;
        CheckBox cbShowVFX;
        PictureBox picPortrait;
        Label lblStatus;
        Button btnSave, btnRestore;

        // ── ListBox data ──
        List<ListItemTag> allItems = new List<ListItemTag>();
        Dictionary<string, TowerFamily> towerFamilies;

        // ══════════════════════════════════════════════════
        //  CONSTRUCTOR
        // ══════════════════════════════════════════════════

        public MainForm()
        {
            Text = "KRV Balance Editor By Shock";
            ClientSize = new Size(1050, 750);
            MinimumSize = new Size(900, 600);
            BackColor = Color.LightBlue;
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;

            try
            {
                gameRoot = FindGameRoot();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not find game root (looking for KR4 subfolder):\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
                return;
            }

            towersFilePath = Path.Combine(gameRoot, @"KR4\Settings\towers_settings.plist");
            unitsFilePath = Path.Combine(gameRoot, @"KR4\Settings\units_settings.plist");

            LoadAllData();
            BuildUI();
            cbCategory.SelectedIndex = 0;
        }

        string FindGameRoot()
        {
            string dir = Path.GetDirectoryName(Application.ExecutablePath);
            for (int i = 0; i < 10; i++)
            {
                if (dir == null) break;
                if (Directory.Exists(Path.Combine(dir, "KR4")))
                    return dir;
                dir = Path.GetDirectoryName(dir);
            }
            throw new DirectoryNotFoundException("Could not find game root with KR4 subfolder.");
        }

        void LoadAllData()
        {
            var t = PlistHelper.LoadPlist(towersFilePath);
            towersData = t.Data;
            towersDoc = t.Doc;

            var u = PlistHelper.LoadPlist(unitsFilePath);
            unitsData = u.Data;
            unitsDoc = u.Doc;

            Localization.LoadStrings(Path.Combine(gameRoot, @"KR4\Localization\Localized_en"));

            try { IconExtractor.ExtractAllIcons(gameRoot); }
            catch { /* icons are optional */ }
        }

        // ══════════════════════════════════════════════════
        //  BUILD UI
        // ══════════════════════════════════════════════════

        void BuildUI()
        {
            SuspendLayout();

            // ── Menu strip ──
            var menu = new MenuStrip();
            var dataMenu = new ToolStripMenuItem("Data");
            dataMenu.DropDownItems.Add("Save", null, (s, e) => SaveButton_Click(s, e));
            dataMenu.DropDownItems.Add("Restore Backup", null, (s, e) => RestoreButton_Click(s, e));
            dataMenu.DropDownItems.Add(new ToolStripSeparator());
            dataMenu.DropDownItems.Add("Exit", null, (s, e) => Close());
            menu.Items.Add(dataMenu);
            MainMenuStrip = menu;

            // ── Top search panel ──
            var topPanel = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = Color.LightBlue };

            var lblName = new Label { Text = "Name", AutoSize = true, Location = new Point(8, 8) };
            tbSearchName = new TextBox { Width = 150, Location = new Point(50, 5) };
            tbSearchName.TextChanged += (s, e) => ApplyFilter();

            var lblCat = new Label { Text = "Category", AutoSize = true, Location = new Point(212, 8) };
            cbCategory = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 100,
                Location = new Point(272, 4),
                Items = { "Towers", "Heroes", "Enemies", "Balance Status" }
            };
            cbCategory.SelectedIndexChanged += (s, e) =>
            {
                string cat = cbCategory.SelectedItem?.ToString() ?? "Towers";
                if (cat == "Balance Status")
                {
                    HideTierList(); // ensure clean state
                    ShowTierList();
                    return;
                }
                HideTierList();
                PopulateListBox(cat);
                ClearDetail();
            };

            var lblKey = new Label { Text = "Key", AutoSize = true, Location = new Point(386, 8) };
            tbSearchKey = new TextBox { Width = 150, Location = new Point(416, 5) };
            tbSearchKey.TextChanged += (s, e) => ApplyFilter();

            topPanel.Controls.AddRange(new Control[] { lblName, tbSearchName, lblCat, cbCategory, lblKey, tbSearchKey });

            // ── Left panel (ListBox) ──
            var leftPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 240,
                BorderStyle = BorderStyle.Fixed3D,
                BackColor = SystemColors.Window
            };
            listBox = new ListBox
            {
                Dock = DockStyle.Fill,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 28,
                IntegralHeight = false,
                BorderStyle = BorderStyle.None
            };
            listBox.DrawItem += ListBox_DrawItem;
            listBox.SelectedIndexChanged += (s, e) => OnItemSelected();
            leftPanel.Controls.Add(listBox);

            // ── Right panel ──
            rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.LightBlue,
                Padding = new Padding(4)
            };

            headerPanel = new Panel { Dock = DockStyle.Top, Height = 84, BackColor = Color.LightBlue };

            var lblN = new Label { Text = "Name", Location = new Point(4, 6), AutoSize = true };
            tbDetailName = new TextBox { Location = new Point(58, 4), Width = 400, ReadOnly = true };

            var lblK = new Label { Text = "Key", Location = new Point(4, 32), AutoSize = true };
            tbDetailKey = new TextBox { Location = new Point(58, 30), Width = 220, ReadOnly = true };

            var lblF = new Label { Text = "Faction", Location = new Point(290, 32), AutoSize = true };
            tbFaction = new TextBox { Location = new Point(340, 30), Width = 120, ReadOnly = true };

            lblVariant = new Label { Text = "Variant", Location = new Point(4, 58), AutoSize = true, Visible = false };
            cbVariant = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(58, 56),
                Width = 200,
                Visible = false
            };

            lblSkillLevel = new Label { Text = "Skill Lv", Location = new Point(270, 58), AutoSize = true, Visible = false };
            cbSkillLevel = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(328, 56),
                Width = 100,
                Visible = false
            };

            cbShowVFX = new CheckBox
            {
                Text = "Show VFX",
                AutoSize = true,
                Location = new Point(440, 58),
                Font = new Font("Segoe UI", 8),
                Visible = false
            };
            cbShowVFX.CheckedChanged += CbShowVFX_Changed;

            picPortrait = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(74, 74),
                BackColor = SystemColors.ControlDark,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };

            headerPanel.Controls.AddRange(new Control[] { lblN, tbDetailName, lblK, tbDetailKey, lblF, tbFaction, lblVariant, cbVariant, lblSkillLevel, cbSkillLevel, cbShowVFX, picPortrait });

            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.LightBlue
            };

            rightPanel.Controls.Add(contentPanel);
            rightPanel.Controls.Add(headerPanel);

            // ── Bottom bar ──
            var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 34, BackColor = Color.LightBlue };

            var lblCredit = new Label
            {
                Text = "By Shock",
                Font = new Font(Font, FontStyle.Italic),
                AutoSize = true,
                Location = new Point(8, 8),
                ForeColor = Color.DarkSlateBlue
            };

            lblStatus = new Label { Text = "No changes", AutoSize = true, Location = new Point(200, 8) };

            btnRestore = new Button
            {
                Text = "Restore Backup",
                Width = 110, Height = 26,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnRestore.Location = new Point(bottomPanel.Width - 260, 4);
            btnRestore.Click += RestoreButton_Click;

            btnSave = new Button
            {
                Text = "Save Changes",
                Width = 110, Height = 26,
                Font = new Font(Font, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnSave.Location = new Point(bottomPanel.Width - 140, 4);
            btnSave.Click += SaveButton_Click;

            bottomPanel.Controls.AddRange(new Control[] { lblCredit, lblStatus, btnRestore, btnSave });

            Controls.Add(rightPanel);
            Controls.Add(leftPanel);
            Controls.Add(bottomPanel);
            Controls.Add(topPanel);
            Controls.Add(menu);

            ResumeLayout(true);
        }

        // ══════════════════════════════════════════════════
        //  ITEM SELECTION → DETAIL DISPATCH
        // ══════════════════════════════════════════════════

        void OnItemSelected()
        {
            if (listBox.SelectedIndex < 0) { ClearDetail(); return; }
            var tag = listBox.SelectedItem as ListItemTag;
            if (tag == null) return;

            switch (tag.Category)
            {
                case "tower": ShowTowerDetail(tag); break;
                case "hero": ShowHeroDetail(tag); break;
                case "enemy": ShowEnemyDetail(tag); break;
            }
        }

        void ClearDetail()
        {
            tbDetailName.Text = "";
            tbDetailKey.Text = "";
            tbFaction.Text = "";
            cbVariant.Visible = false;
            lblVariant.Visible = false;
            cbSkillLevel.Visible = false;
            lblSkillLevel.Visible = false;
            cbShowVFX.Visible = false;
            picPortrait.Image = null;
            picPortrait.Visible = false;
            cbVariant.Items.Clear();
            cbSkillLevel.Items.Clear();
            contentPanel.Controls.Clear();
        }
    }
}
