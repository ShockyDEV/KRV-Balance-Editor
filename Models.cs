using System.Collections.Generic;
using System.Drawing;

namespace BalanceEditor
{
    class ChangeEntry
    {
        public string File;
        public List<object> Path;
        public double Value;
    }

    class InputTag
    {
        public double OriginalValue;
        public string FilePath;      // "towers" or "units"
        public List<object> DataPath;
        public string ChangeKey;
    }

    class ListItemTag
    {
        public string Key;
        public string DisplayName;
        public string Faction;
        public string Category; // "tower", "hero", "enemy"
        public Dictionary<string, object> Data;
        public string ParentKey;
        public TowerFamily Family;
        public Bitmap Icon;

        public override string ToString() => DisplayName;
    }

    class TierListData
    {
        public Dictionary<string, List<string>> HeroTiers { get; set; }
        public Dictionary<string, List<string>> TowerTiers { get; set; }
    }

    class TowerFamily
    {
        public string BaseName;
        public string Faction;
        public List<string> Variants = new List<string>();
        public Dictionary<string, Dictionary<string, object>> VariantData = new Dictionary<string, Dictionary<string, object>>();
        public Dictionary<string, string> VariantLabels = new Dictionary<string, string>();
        public Dictionary<string, double> VariantOrder = new Dictionary<string, double>();
        public Dictionary<string, string> VariantParent = new Dictionary<string, string>();

        public List<object> BasePath(string varKey)
        {
            var p = new List<object>();
            if (VariantParent.TryGetValue(varKey, out var parent) && parent != null)
                p.Add(parent);
            p.Add(varKey);
            return p;
        }
    }
}
