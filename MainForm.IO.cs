using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace BalanceEditor
{
    public partial class MainForm
    {
        // ══════════════════════════════════════════════════
        //  SAVE
        // ══════════════════════════════════════════════════

        void SaveButton_Click(object sender, EventArgs e)
        {
            if (Changes.Count == 0)
            {
                MessageBox.Show("No changes to save.", "Save", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                // Expand scalars to arrays where needed (e.g. per-level skill edits)
                foreach (var kv in Changes)
                {
                    var ce = kv.Value;
                    var doc = ce.File == "towers" ? towersDoc : unitsDoc;
                    object rootData = ce.File == "towers" ? (object)towersData : unitsData;
                    PlistHelper.EnsureNavigable(doc, rootData, ce.Path);
                }

                // Validate all paths
                var errors = new List<string>();
                foreach (var kv in Changes)
                {
                    var ce = kv.Value;
                    var doc = ce.File == "towers" ? towersDoc : unitsDoc;
                    if (PlistHelper.Navigate(doc, ce.Path) == null)
                        errors.Add($"Cannot find path: {kv.Key}");
                }

                if (errors.Count > 0)
                {
                    MessageBox.Show("Validation errors:\n" + string.Join("\n", errors.Take(10)),
                        "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                foreach (var kv in Changes)
                {
                    var ce = kv.Value;
                    var doc = ce.File == "towers" ? towersDoc : unitsDoc;
                    PlistHelper.UpdateValue(doc, ce.Path, ce.Value);
                }

                PlistHelper.SavePlist(towersDoc, towersFilePath);
                PlistHelper.SavePlist(unitsDoc, unitsFilePath);

                foreach (var kv in Changes)
                {
                    var ce = kv.Value;
                    object root = ce.File == "towers" ? (object)towersData : (object)unitsData;
                    object storeVal = (ce.Value == Math.Floor(ce.Value) && Math.Abs(ce.Value) < 1e15)
                        ? (object)(long)ce.Value : ce.Value;
                    PlistHelper.SetDataValue(root, ce.Path, storeVal);
                }

                UpdateInputsIn(contentPanel);
                Changes.Clear();
                UpdateStatus();

                MessageBox.Show("Saved successfully!", "Save", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save failed:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void UpdateInputsIn(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is TextBox tb && tb.Tag is InputTag tag)
                {
                    if (double.TryParse(tb.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
                        tag.OriginalValue = val;
                    tb.ForeColor = SystemColors.WindowText;
                    tb.Font = new Font(tb.Font, FontStyle.Regular);
                }
                if (c.HasChildren)
                    UpdateInputsIn(c);
            }
        }

        // ══════════════════════════════════════════════════
        //  RESTORE BACKUP
        // ══════════════════════════════════════════════════

        void RestoreButton_Click(object sender, EventArgs e)
        {
            string towersBackup = towersFilePath + ".backup";
            string unitsBackup = unitsFilePath + ".backup";

            if (!File.Exists(towersBackup) && !File.Exists(unitsBackup))
            {
                MessageBox.Show("No backup files found.", "Restore", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show("Restore backup files? This will overwrite current plist files and reload all data.",
                "Restore Backup", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            try
            {
                if (File.Exists(towersBackup)) File.Copy(towersBackup, towersFilePath, true);
                if (File.Exists(unitsBackup)) File.Copy(unitsBackup, unitsFilePath, true);

                LoadAllData();
                Changes.Clear();

                string cat = cbCategory.SelectedItem?.ToString() ?? "Towers";
                PopulateListBox(cat);
                ClearDetail();
                UpdateStatus();

                MessageBox.Show("Backup restored!", "Restore", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Restore failed:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
