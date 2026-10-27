using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GrovsTestApp
{
    public enum ValueKind { Text, Number, Bool, Date }

    [Serializable]
    public sealed class KeyValueRow
    {
        public string Key;
        public string Value;
        public ValueKind Kind;
    }

    public static class KeyValue
    {
        static readonly Regex NumberPattern = new Regex(@"^-?[0-9]+(\.[0-9]+)?$");
        static readonly Regex DatePattern = new Regex(@"^[0-9]{4}-[0-9]{2}-[0-9]{2}$");

        public static ValueKind Detect(string raw)
        {
            var v = (raw ?? "").Trim();
            if (v.Equals("true", StringComparison.OrdinalIgnoreCase) || v.Equals("false", StringComparison.OrdinalIgnoreCase))
                return ValueKind.Bool;
            if (NumberPattern.IsMatch(v)) return ValueKind.Number;
            if (DatePattern.IsMatch(v) && IsRealDate(v)) return ValueKind.Date;
            return ValueKind.Text;
        }

        public static object Typed(KeyValueRow row)
        {
            var raw = row.Value ?? "";
            var v = raw.Trim();
            switch (row.Kind)
            {
                case ValueKind.Number:
                    if (long.TryParse(v, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var l)) return l;
                    if (double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)
                        && !double.IsInfinity(d) && !double.IsNaN(d)) return d;
                    return raw;
                case ValueKind.Bool:
                    return v.Equals("true", StringComparison.OrdinalIgnoreCase);
                default:
                    return raw;
            }
        }

        public static IDictionary<string, object> ToMap(IEnumerable<KeyValueRow> rows)
        {
            var map = new Dictionary<string, object>();
            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.Key)) continue;
                map[row.Key.Trim()] = Typed(row);
            }
            return map;
        }

        public static List<KeyValueRow> FromJson(string json)
        {
            Dictionary<string, object> obj;
            try
            {
                obj = MiniJson.Parse(json) as Dictionary<string, object>;
            }
            catch (FormatException)
            {
                obj = null;
            }
            if (obj == null) throw new FormatException("Not valid JSON");

            var rows = new List<KeyValueRow>();
            foreach (var pair in obj)
            {
                var row = new KeyValueRow { Key = pair.Key };
                switch (pair.Value)
                {
                    case null: row.Value = ""; row.Kind = ValueKind.Text; break;
                    case bool b: row.Value = b ? "true" : "false"; row.Kind = ValueKind.Bool; break;
                    case long or double: row.Value = MiniJson.Serialize(pair.Value); row.Kind = ValueKind.Number; break;
                    case string s: row.Value = s; row.Kind = ValueKind.Text; break;
                    default: row.Value = MiniJson.Serialize(pair.Value); row.Kind = ValueKind.Text; break;
                }
                rows.Add(row);
            }
            return rows;
        }

        public static string ToJson(IEnumerable<KeyValueRow> rows) => MiniJson.Serialize(ToMap(rows), true);

        static bool IsRealDate(string v) =>
            DateTime.TryParseExact(v, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
    }
}
