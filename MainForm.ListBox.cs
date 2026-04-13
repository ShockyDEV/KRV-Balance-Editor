using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace BalanceEditor
{
    public partial class MainForm
    {
        // ══════════════════════════════════════════════════
        //  LISTBOX POPULATION
        // ══════════════════════════════════════════════════

        void PopulateListBox(string category)
        {
            allItems.Clear();

            switch (category)
            {
                case "Towers": BuildTowerItems(); break;
                case "Heroes": BuildHeroItems(); break;
                case "Enemies": BuildEnemyItems(); break;
            }

            ApplyFilter();
        }

        void BuildTowerItems()
        {
            towerFamilies = GroupTowerFamilies();

            foreach (var fam in towerFamilies.Values.OrderBy(f => f.Faction).ThenBy(f => f.BaseName))
            {
                string topVar = fam.Variants.Last();
                allItems.Add(new ListItemTag
                {
                    Key = fam.BaseName,
                    DisplayName = Pretty(fam.BaseName),
                    Faction = fam.Faction,
                    Category = "tower",
                    Data = fam.VariantData[topVar],
                    ParentKey = fam.VariantParent.ContainsKey(topVar) ? fam.VariantParent[topVar] : null,
                    Family = fam,
                    Icon = IconExtractor.GetTowerIcon(topVar)
                });
            }
        }

        void BuildHeroItems()
        {
            var heroesDict = PlistHelper.GetDict(unitsData, "heroes");
            if (heroesDict == null) return;

            foreach (var kv in heroesDict.OrderBy(h => h.Key))
            {
                var heroData = kv.Value as Dictionary<string, object>;
                if (heroData == null) continue;
                string displayName = StaticData.HeroNames.TryGetValue(kv.Key, out var hn) ? hn : Pretty(kv.Key);

                allItems.Add(new ListItemTag
                {
                    Key = kv.Key,
                    DisplayName = displayName,
                    Faction = "heroes",
                    Category = "hero",
                    Data = heroData,
                    Icon = IconExtractor.GetHeroIcon(kv.Key)
                });
            }
        }

        void BuildEnemyItems()
        {
            foreach (var factionKv in unitsData.OrderBy(f => f.Key))
            {
                if (factionKv.Key == "default_config" || factionKv.Key == "heroes") continue;
                var factionDict = factionKv.Value as Dictionary<string, object>;
                if (factionDict == null) continue;

                foreach (var enemyKv in factionDict.OrderBy(e => e.Key))
                {
                    var enemyData = enemyKv.Value as Dictionary<string, object>;
                    if (enemyData == null) continue;

                    bool hasHealth = enemyData.ContainsKey("health");
                    bool hasSpeed = enemyData.ContainsKey("speed");
                    bool isHero = enemyData.ContainsKey("is_hero") &&
                        (enemyData["is_hero"] is bool b && b || enemyData["is_hero"] is long l && l != 0);
                    if ((!hasHealth && !hasSpeed) || isHero) continue;

                    allItems.Add(new ListItemTag
                    {
                        Key = enemyKv.Key,
                        DisplayName = Pretty(factionKv.Key) + ": " + Pretty(enemyKv.Key),
                        Faction = factionKv.Key,
                        Category = "enemy",
                        Data = enemyData,
                        ParentKey = factionKv.Key,
                        Icon = IconExtractor.GetEnemyIcon(enemyKv.Key)
                    });
                }
            }
        }

        // ══════════════════════════════════════════════════
        //  TOWER FAMILY GROUPING (TWO-PASS)
        // ══════════════════════════════════════════════════

        Dictionary<string, TowerFamily> GroupTowerFamilies()
        {
            var skipGroups = new HashSet<string> { "holder_types", "holder", "default_config", "examples" };
            var allTowers = new Dictionary<string, (Dictionary<string, object> Data, string Parent)>();

            foreach (var kv in towersData)
            {
                if (skipGroups.Contains(kv.Key)) continue;
                if (!(kv.Value is Dictionary<string, object> td)) continue;
                if (td.ContainsKey("cost"))
                    allTowers[kv.Key] = (td, null);
                else
                {
                    foreach (var sub in td)
                    {
                        if (sub.Value is Dictionary<string, object> sd && sd.ContainsKey("cost"))
                            allTowers[sub.Key] = (sd, kv.Key);
                    }
                }
            }

            var families = new Dictionary<string, TowerFamily>();
            var regex = new Regex(@"^(.+?)_level(\d+)(_old)?$");
            var assigned = new HashSet<string>();
            var keys = allTowers.Keys.OrderBy(k => k).ToList();

            // Pass 1: group _levelN keys
            foreach (var key in keys)
            {
                var m = regex.Match(key);
                if (!m.Success) continue;
                string baseName = m.Groups[1].Value;
                int lvl = int.Parse(m.Groups[2].Value);
                bool isOld = m.Groups[3].Success;
                if (!families.ContainsKey(baseName))
                    families[baseName] = new TowerFamily { BaseName = baseName, Faction = allTowers[key].Parent ?? baseName.Split('_')[0] };
                families[baseName].Variants.Add(key);
                families[baseName].VariantLabels[key] = isOld ? $"Level {lvl} (old)" : $"Level {lvl}";
                families[baseName].VariantOrder[key] = lvl + (isOld ? 0.5 : 0.0);
                families[baseName].VariantData[key] = allTowers[key].Data;
                families[baseName].VariantParent[key] = allTowers[key].Parent;
                assigned.Add(key);
            }

            // Pass 2: named variants & standalones
            foreach (var key in keys)
            {
                if (assigned.Contains(key)) continue;
                string foundBase = null;
                foreach (var bk in families.Keys)
                {
                    if (key.StartsWith(bk + "_") && key != bk)
                        if (foundBase == null || bk.Length > foundBase.Length) foundBase = bk;
                }
                if (foundBase != null)
                {
                    string suffix = key.Substring(foundBase.Length + 1);
                    families[foundBase].Variants.Add(key);
                    families[foundBase].VariantLabels[key] = Pretty(suffix);
                    families[foundBase].VariantOrder[key] = 4.1;
                    families[foundBase].VariantData[key] = allTowers[key].Data;
                    families[foundBase].VariantParent[key] = allTowers[key].Parent;
                }
                else
                {
                    if (!families.ContainsKey(key))
                        families[key] = new TowerFamily { BaseName = key, Faction = allTowers[key].Parent ?? key.Split('_')[0] };
                    families[key].Variants.Add(key);
                    families[key].VariantLabels[key] = Pretty(key);
                    families[key].VariantOrder[key] = 1;
                    families[key].VariantData[key] = allTowers[key].Data;
                    families[key].VariantParent[key] = allTowers[key].Parent;
                }
                assigned.Add(key);
            }

            // Sort variants
            foreach (var fam in families.Values)
                fam.Variants.Sort((a, b) =>
                    (fam.VariantOrder.ContainsKey(a) ? fam.VariantOrder[a] : 0)
                    .CompareTo(fam.VariantOrder.ContainsKey(b) ? fam.VariantOrder[b] : 0));

            // Fix multi-word factions
            foreach (var fam in families.Values)
            {
                if (fam.BaseName.StartsWith("dark_army")) fam.Faction = "dark_army";
                else if (fam.BaseName.StartsWith("fallen_ones")) fam.Faction = "fallen_ones";
                else if (fam.BaseName.StartsWith("ember_lords")) fam.Faction = "ember_lords";
                else if (fam.Faction == null && fam.Variants.Count > 0)
                    fam.Faction = allTowers[fam.Variants[0]].Parent ?? fam.BaseName.Split('_')[0];
            }

            return families;
        }

        // ══════════════════════════════════════════════════
        //  LISTBOX OWNER-DRAW
        // ══════════════════════════════════════════════════

        void ListBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.DrawBackground();

            var tag = listBox.Items[e.Index] as ListItemTag;
            if (tag == null) return;

            int iconSize = e.Bounds.Height - 4;
            int textX = 4;

            Color fc = StaticData.GetFactionColor(tag.Faction);
            using (var brush = new SolidBrush(fc))
                e.Graphics.FillRectangle(brush, e.Bounds.X + 2, e.Bounds.Y + 2, iconSize, iconSize);

            if (tag.Icon != null)
            {
                e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                e.Graphics.DrawImage(tag.Icon, e.Bounds.X + 2, e.Bounds.Y + 2, iconSize, iconSize);
            }
            textX = iconSize + 8;

            using (var brush = new SolidBrush(e.ForeColor))
                e.Graphics.DrawString(tag.DisplayName, e.Font, brush,
                    e.Bounds.X + textX, e.Bounds.Y + (e.Bounds.Height - e.Font.Height) / 2);

            e.DrawFocusRectangle();
        }

        // ══════════════════════════════════════════════════
        //  SEARCH / FILTER
        // ══════════════════════════════════════════════════

        void ApplyFilter()
        {
            string nameQ = (tbSearchName?.Text ?? "").Trim().ToLowerInvariant().Replace(' ', '_');
            string keyQ = (tbSearchKey?.Text ?? "").Trim().ToLowerInvariant();

            listBox.BeginUpdate();
            listBox.Items.Clear();
            foreach (var item in allItems)
            {
                bool match = true;
                if (nameQ.Length > 0 && !item.DisplayName.ToLowerInvariant().Replace(' ', '_').Contains(nameQ))
                    match = false;
                if (keyQ.Length > 0 && !item.Key.ToLowerInvariant().Contains(keyQ))
                    match = false;
                if (match) listBox.Items.Add(item);
            }
            listBox.EndUpdate();
        }
    }
}
