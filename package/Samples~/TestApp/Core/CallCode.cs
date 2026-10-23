using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GrovsSDK;

namespace GrovsTestApp
{
    /// <summary>Writes an SDK call the way it would look in code, for the log detail view.</summary>
    public static class CallCode
    {
        public static string Format(string name, params (string key, object value)[] args)
        {
            if (args == null || args.Length == 0) return name + "()";
            var lines = args.Select(a => "  " + a.key + " = " + Value(a.value, false));
            return name + "(\n" + string.Join(",\n", lines) + "\n)";
        }

        /// <summary>One value written the way it appears inside a call.</summary>
        public static string Render(object value) => Value(value, false);

        static string Value(object value, bool inList)
        {
            switch (value)
            {
                case null: return "null";
                case string s: return inList ? s : "\"" + s + "\"";
                case bool b: return b ? "true" : "false";
                case DateTime d: return d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                case GrovsTracking t: return Map(Pairs(t));
                case IReadOnlyDictionary<string, object> ro: return Map(ro.Select(p => (p.Key, p.Value)));
                case IDictionary dict: return Map(dict.Keys.Cast<object>().Select(k => (Convert.ToString(k, CultureInfo.InvariantCulture), dict[k])));
                case IEnumerable list: return "[" + string.Join(", ", list.Cast<object>().Select(v => Value(v, true))) + "]";
                case IFormattable f: return f.ToString(null, CultureInfo.InvariantCulture);
                default: return value.ToString();
            }
        }

        static string Map(IEnumerable<(string key, object value)> pairs)
        {
            var items = pairs.Select(p => p.key + ": " + Value(p.value, false)).ToList();
            return items.Count == 0 ? "{}" : "{ " + string.Join(", ", items) + " }";
        }

        static IEnumerable<(string, object)> Pairs(GrovsTracking t)
        {
            yield return ("campaign", t.Campaign);
            yield return ("source", t.Source);
            yield return ("medium", t.Medium);
        }
    }
}
