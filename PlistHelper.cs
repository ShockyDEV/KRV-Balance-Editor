using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace BalanceEditor
{
    /// <summary>
    /// Parses Apple plist XML files into C# objects, navigates paths, updates values, and saves.
    /// </summary>
    static class PlistHelper
    {
        /// <summary>Load a plist file. Returns (parsed dict, XDocument for mutation).</summary>
        public static (Dictionary<string, object> Data, XDocument Doc) LoadPlist(string filepath)
        {
            var doc = XDocument.Load(filepath);
            var plist = doc.Root; // <plist>
            var first = plist?.Elements().FirstOrDefault();
            if (first == null)
                return (new Dictionary<string, object>(), doc);
            var data = ParseElement(first) as Dictionary<string, object>;
            return (data ?? new Dictionary<string, object>(), doc);
        }

        /// <summary>Recursively convert an XElement to a C# object.</summary>
        public static object ParseElement(XElement el)
        {
            switch (el.Name.LocalName)
            {
                case "dict":
                {
                    var d = new Dictionary<string, object>();
                    var children = el.Elements().ToList();
                    int i = 0;
                    while (i < children.Count)
                    {
                        if (children[i].Name.LocalName == "key")
                        {
                            string key = children[i].Value ?? "";
                            if (i + 1 < children.Count && children[i + 1].Name.LocalName != "key")
                            {
                                d[key] = ParseElement(children[i + 1]);
                                i += 2;
                            }
                            else
                            {
                                d[key] = null;
                                i += 1;
                            }
                        }
                        else
                        {
                            i += 1;
                        }
                    }
                    return d;
                }
                case "array":
                    return el.Elements().Select(ParseElement).ToList<object>();
                case "integer":
                    return long.TryParse(el.Value, out long lv) ? lv : 0L;
                case "real":
                    return double.TryParse(el.Value, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out double dv) ? dv : 0.0;
                case "string":
                    return el.Value ?? "";
                case "true":
                    return true;
                case "false":
                    return false;
                case "data":
                    return el.Value ?? "";
                default:
                    return null;
            }
        }

        /// <summary>Navigate an XDocument plist tree along a path of string keys / int indices.</summary>
        public static XElement Navigate(XDocument doc, List<object> path)
        {
            var plist = doc.Root;
            var cur = plist?.Elements().FirstOrDefault();
            if (cur == null) return null;

            foreach (var part in path)
            {
                if (cur == null) return null;

                if (cur.Name.LocalName == "dict")
                {
                    string key = part.ToString();
                    var children = cur.Elements().ToList();
                    bool found = false;
                    for (int i = 0; i < children.Count; i++)
                    {
                        if (children[i].Name.LocalName == "key" && children[i].Value == key)
                        {
                            if (i + 1 < children.Count)
                            {
                                cur = children[i + 1];
                                found = true;
                            }
                            break;
                        }
                    }
                    if (!found) return null;
                }
                else if (cur.Name.LocalName == "array")
                {
                    int idx;
                    if (part is int pi) idx = pi;
                    else if (part is long pl) idx = (int)pl;
                    else if (!int.TryParse(part.ToString(), out idx)) return null;

                    var children = cur.Elements().ToList();
                    if (idx >= 0 && idx < children.Count)
                        cur = children[idx];
                    else
                        return null;
                }
                else
                {
                    return null;
                }
            }
            return cur;
        }

        /// <summary>Update a scalar value in the plist XDocument. Returns true on success.</summary>
        public static bool UpdateValue(XDocument doc, List<object> path, double value)
        {
            var elem = Navigate(doc, path);
            if (elem == null) return false;

            switch (elem.Name.LocalName)
            {
                case "integer":
                    elem.Value = ((long)Math.Round(value)).ToString();
                    return true;
                case "real":
                    elem.Value = value.ToString("G", System.Globalization.CultureInfo.InvariantCulture);
                    return true;
                case "string":
                    elem.Value = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    return true;
                case "true":
                case "false":
                    elem.Name = (value == 1.0 || value != 0.0) ? "true" : "false";
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Ensures a path is navigable. If the path ends with an int index but
        /// the parent field is a scalar, expands the scalar to an array (filling
        /// with copies of the original value). Returns true if path is (now) valid.
        /// </summary>
        public static bool EnsureNavigable(XDocument doc, object rootData, List<object> path)
        {
            if (Navigate(doc, path) != null) return true;
            if (path.Count < 2) return false;

            // Path must end with an int index
            var lastPart = path[path.Count - 1];
            int targetIdx;
            if (lastPart is int pi) targetIdx = pi;
            else if (lastPart is long pl) targetIdx = (int)pl;
            else return false;

            var parentPath = path.GetRange(0, path.Count - 1);
            var parentEl = Navigate(doc, parentPath);
            if (parentEl == null) return false;

            // Parent is already an array but too short — extend it
            if (parentEl.Name.LocalName == "array")
            {
                var children = parentEl.Elements().ToList();
                if (targetIdx < children.Count) return true;
                string lastVal = children.Count > 0 ? children.Last().Value : "0";
                string elemType = children.Count > 0 ? children.Last().Name.LocalName : "integer";
                while (children.Count <= targetIdx)
                {
                    var el = new XElement(elemType, lastVal);
                    parentEl.Add(el);
                    children.Add(el);
                }
                var dataList = NavigateData(rootData, parentPath) as List<object>;
                if (dataList != null)
                {
                    object lastObj = dataList.Count > 0 ? dataList[dataList.Count - 1] : 0L;
                    while (dataList.Count <= targetIdx)
                        dataList.Add(lastObj);
                }
                return true;
            }

            // Parent is a scalar → expand to array
            if (parentEl.Name.LocalName != "integer" && parentEl.Name.LocalName != "real")
                return false;

            string origValue = parentEl.Value;
            string origType = parentEl.Name.LocalName;

            // Determine array size from sibling arrays in the same dict
            int arraySize = targetIdx + 1;
            if (path.Count >= 3)
            {
                var dictPath = path.GetRange(0, path.Count - 2);
                var dictData = NavigateData(rootData, dictPath) as Dictionary<string, object>;
                if (dictData != null)
                {
                    foreach (var kv in dictData)
                    {
                        if (kv.Value is List<object> arr && arr.Count > arraySize
                            && arr.All(o => AsNumber(o).HasValue))
                            arraySize = arr.Count;
                    }
                }
            }

            // Build replacement array XML
            var newArray = new XElement("array");
            for (int i = 0; i < arraySize; i++)
                newArray.Add(new XElement(origType, origValue));
            parentEl.ReplaceWith(newArray);

            // Update in-memory data
            string fieldKey = parentPath[parentPath.Count - 1].ToString();
            var dictPath2 = parentPath.GetRange(0, parentPath.Count - 1);
            var dictData2 = NavigateData(rootData, dictPath2) as Dictionary<string, object>;
            if (dictData2 != null && dictData2.ContainsKey(fieldKey))
            {
                double origNum = AsNumber(dictData2[fieldKey]) ?? 0;
                object origObj = origType == "integer" ? (object)(long)origNum : origNum;
                var list = new List<object>();
                for (int i = 0; i < arraySize; i++)
                    list.Add(origObj);
                dictData2[fieldKey] = list;
            }

            return Navigate(doc, path) != null;
        }

        /// <summary>Save plist back to file. Creates a .backup on first save.</summary>
        public static void SavePlist(XDocument doc, string filepath)
        {
            string backup = filepath + ".backup";
            if (!File.Exists(backup))
                File.Copy(filepath, backup);

            // Write with plist header
            string xml = doc.Root.ToString();
            string header =
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                "<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" " +
                "\"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n";
            File.WriteAllText(filepath, header + xml + "\n", System.Text.Encoding.UTF8);
        }

        /// <summary>Navigate a parsed C# data tree (dicts/lists) along a path.</summary>
        public static object NavigateData(object root, List<object> path)
        {
            object cur = root;
            foreach (var part in path)
            {
                if (cur == null) return null;
                if (cur is Dictionary<string, object> dict)
                {
                    string key = part.ToString();
                    if (!dict.TryGetValue(key, out cur))
                        return null;
                }
                else if (cur is List<object> list)
                {
                    int idx;
                    if (part is int pi) idx = pi;
                    else if (part is long pl) idx = (int)pl;
                    else if (!int.TryParse(part.ToString(), out idx)) return null;

                    if (idx >= 0 && idx < list.Count)
                        cur = list[idx];
                    else
                        return null;
                }
                else
                {
                    return null;
                }
            }
            return cur;
        }

        /// <summary>Set a value in the parsed C# data tree. Used to update in-memory data after save.</summary>
        public static bool SetDataValue(object root, List<object> path, object value)
        {
            if (path.Count == 0) return false;
            object parent = NavigateData(root, path.GetRange(0, path.Count - 1));
            var lastKey = path[path.Count - 1];

            if (parent is Dictionary<string, object> dict)
            {
                dict[lastKey.ToString()] = value;
                return true;
            }
            else if (parent is List<object> list)
            {
                int idx;
                if (lastKey is int pi) idx = pi;
                else if (lastKey is long pl) idx = (int)pl;
                else if (!int.TryParse(lastKey.ToString(), out idx)) return false;

                if (idx >= 0 && idx < list.Count)
                {
                    list[idx] = value;
                    return true;
                }
            }
            return false;
        }

        /// <summary>Ensure a field exists in both in-memory data and XDocument. Creates it with defaultValue if missing.</summary>
        public static void EnsureField(XDocument doc, Dictionary<string, object> data,
            List<object> parentPath, string fieldName, long defaultValue)
        {
            if (data.ContainsKey(fieldName)) return;

            data[fieldName] = defaultValue;

            var parentEl = Navigate(doc, parentPath);
            if (parentEl != null && parentEl.Name.LocalName == "dict")
            {
                parentEl.Add(new XElement("key", fieldName));
                parentEl.Add(new XElement("integer", defaultValue.ToString()));
            }
        }

        /// <summary>Get a numeric value from the parsed data, following a path.</summary>
        public static double? GetNumber(object root, params object[] path)
        {
            var obj = NavigateData(root, path.ToList());
            if (obj is long l) return l;
            if (obj is double d) return d;
            if (obj is int i) return i;
            return null;
        }

        /// <summary>Get a string value from the parsed data.</summary>
        public static string GetString(object root, params object[] path)
        {
            var obj = NavigateData(root, path.ToList());
            return obj as string;
        }

        /// <summary>Get a dict from parsed data.</summary>
        public static Dictionary<string, object> GetDict(object root, params object[] path)
        {
            var obj = NavigateData(root, path.ToList());
            return obj as Dictionary<string, object>;
        }

        /// <summary>Get a list from parsed data.</summary>
        public static List<object> GetList(object root, params object[] path)
        {
            var obj = NavigateData(root, path.ToList());
            return obj as List<object>;
        }

        /// <summary>Convert an object to a double if it's numeric.</summary>
        public static double? AsNumber(object obj)
        {
            if (obj is long l) return l;
            if (obj is double d) return d;
            if (obj is int i) return i;
            // Some plist values are stored as <string>123</string> instead of <integer>/<real>.
            // Goblin minion HP, projectile damage on certain heroes etc. Parse them too —
            // SavePlist (UpdateValue) preserves the original XML element type, so we don't
            // accidentally convert <string> to <integer> on save.
            if (obj is string s && double.TryParse(s,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double v)) return v;
            return null;
        }
    }
}
