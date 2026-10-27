using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace GrovsTestApp
{
    /// <summary>Small JSON reader and writer. Objects are dictionaries, arrays are lists, numbers are long or double.</summary>
    public static class MiniJson
    {
        public static object Parse(string json)
        {
            if (json == null) throw new FormatException("Not valid JSON");
            var reader = new Reader(json);
            var value = reader.ReadValue();
            reader.SkipWhitespace();
            if (!reader.AtEnd) throw reader.Error();
            return value;
        }

        public static string Serialize(object value, bool indented = false)
        {
            var sb = new StringBuilder();
            Write(sb, value, indented, 0);
            return sb.ToString();
        }

        static void Write(StringBuilder sb, object value, bool indented, int depth)
        {
            switch (value)
            {
                case null: sb.Append("null"); break;
                case string s: WriteString(sb, s); break;
                case bool b: sb.Append(b ? "true" : "false"); break;
                case double d: sb.Append(d.ToString("R", CultureInfo.InvariantCulture)); break;
                case float f: sb.Append(f.ToString("R", CultureInfo.InvariantCulture)); break;
                case IDictionary dict: WriteObject(sb, dict, indented, depth); break;
                case IEnumerable list: WriteArray(sb, list, indented, depth); break;
                case IFormattable n: sb.Append(n.ToString(null, CultureInfo.InvariantCulture)); break;
                default: WriteString(sb, value.ToString()); break;
            }
        }

        static void WriteObject(StringBuilder sb, IDictionary dict, bool indented, int depth)
        {
            if (dict.Count == 0) { sb.Append("{}"); return; }
            sb.Append('{');
            var first = true;
            foreach (DictionaryEntry entry in dict)
            {
                if (!first) sb.Append(',');
                first = false;
                NewLine(sb, indented, depth + 1);
                WriteString(sb, entry.Key.ToString());
                sb.Append(indented ? ": " : ":");
                Write(sb, entry.Value, indented, depth + 1);
            }
            NewLine(sb, indented, depth);
            sb.Append('}');
        }

        static void WriteArray(StringBuilder sb, IEnumerable list, bool indented, int depth)
        {
            sb.Append('[');
            var first = true;
            foreach (var item in list)
            {
                if (!first) sb.Append(',');
                first = false;
                NewLine(sb, indented, depth + 1);
                Write(sb, item, indented, depth + 1);
            }
            if (!first) NewLine(sb, indented, depth);
            sb.Append(']');
        }

        static void NewLine(StringBuilder sb, bool indented, int depth)
        {
            if (!indented) return;
            sb.Append('\n').Append(' ', depth * 2);
        }

        static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (var c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < ' ') sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        sealed class Reader
        {
            readonly string _s;
            int _i;

            public Reader(string s) { _s = s; }

            public bool AtEnd => _i >= _s.Length;

            public FormatException Error() => new FormatException("Not valid JSON");

            public void SkipWhitespace()
            {
                while (_i < _s.Length && char.IsWhiteSpace(_s[_i])) _i++;
            }

            public object ReadValue()
            {
                SkipWhitespace();
                if (AtEnd) throw Error();
                switch (_s[_i])
                {
                    case '{': return ReadObject();
                    case '[': return ReadArray();
                    case '"': return ReadString();
                    case 't': ReadWord("true"); return true;
                    case 'f': ReadWord("false"); return false;
                    case 'n': ReadWord("null"); return null;
                    default: return ReadNumber();
                }
            }

            Dictionary<string, object> ReadObject()
            {
                var result = new Dictionary<string, object>();
                _i++;
                SkipWhitespace();
                if (Peek('}')) { _i++; return result; }
                while (true)
                {
                    SkipWhitespace();
                    if (!Peek('"')) throw Error();
                    var key = ReadString();
                    SkipWhitespace();
                    Expect(':');
                    result[key] = ReadValue();
                    SkipWhitespace();
                    if (Peek(',')) { _i++; continue; }
                    Expect('}');
                    return result;
                }
            }

            List<object> ReadArray()
            {
                var result = new List<object>();
                _i++;
                SkipWhitespace();
                if (Peek(']')) { _i++; return result; }
                while (true)
                {
                    result.Add(ReadValue());
                    SkipWhitespace();
                    if (Peek(',')) { _i++; continue; }
                    Expect(']');
                    return result;
                }
            }

            string ReadString()
            {
                var sb = new StringBuilder();
                _i++;
                while (true)
                {
                    if (AtEnd) throw Error();
                    var c = _s[_i++];
                    if (c == '"') return sb.ToString();
                    if (c != '\\') { sb.Append(c); continue; }
                    if (AtEnd) throw Error();
                    var e = _s[_i++];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'u':
                            if (_i + 4 > _s.Length || !int.TryParse(_s.Substring(_i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code))
                                throw Error();
                            sb.Append((char)code);
                            _i += 4;
                            break;
                        default: throw Error();
                    }
                }
            }

            object ReadNumber()
            {
                var start = _i;
                while (_i < _s.Length && "+-0123456789.eE".IndexOf(_s[_i]) >= 0) _i++;
                var text = _s.Substring(start, _i - start);
                if (text.Length == 0) throw Error();
                if (long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var l)) return l;
                if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) return d;
                throw Error();
            }

            void ReadWord(string word)
            {
                if (string.CompareOrdinal(_s, _i, word, 0, word.Length) != 0) throw Error();
                _i += word.Length;
            }

            bool Peek(char c) => _i < _s.Length && _s[_i] == c;

            void Expect(char c)
            {
                if (!Peek(c)) throw Error();
                _i++;
            }
        }
    }
}
