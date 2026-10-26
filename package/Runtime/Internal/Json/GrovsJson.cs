using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace GrovsSDK.Internal
{
    /// <summary>Minimal JSON reader and writer for the native bridge messages.</summary>
    internal static class GrovsJson
    {
        const double MaxSafeInteger = 9007199254740992d;
        const int MaxDepth = 64;
        static readonly string TooDeep = "JSON is nested deeper than " + MaxDepth + " levels";

        public static object Parse(string json)
        {
            if (json == null) throw new FormatException("JSON is null");
            var parser = new Parser(json);
            var value = parser.ParseDocument();
            return value;
        }

        public static string Serialize(object value)
        {
            var sb = new StringBuilder();
            Write(sb, value, 0);
            return sb.ToString();
        }

        static void Write(StringBuilder sb, object value, int depth)
        {
            switch (value)
            {
                case null:
                    sb.Append("null");
                    break;
                case string s:
                    WriteString(sb, s);
                    break;
                case bool b:
                    sb.Append(b ? "true" : "false");
                    break;
                case int _:
                case long _:
                case short _:
                case byte _:
                case sbyte _:
                case ushort _:
                case uint _:
                case ulong _:
                    sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
                    break;
                case float f:
                    WriteDouble(sb, f);
                    break;
                case double d:
                    WriteDouble(sb, d);
                    break;
                case decimal m:
                    sb.Append(m.ToString(CultureInfo.InvariantCulture));
                    break;
                case IDictionary dict:
                    WriteObject(sb, dict, depth + 1);
                    break;
                case IEnumerable list:
                    WriteArray(sb, list, depth + 1);
                    break;
                default:
                    throw new ArgumentException("Unsupported type for JSON: " + value.GetType().FullName);
            }
        }

        static void WriteDouble(StringBuilder sb, double d)
        {
            if (double.IsNaN(d) || double.IsInfinity(d))
                throw new ArgumentException("NaN and Infinity are not valid JSON numbers");
            sb.Append(d.ToString("R", CultureInfo.InvariantCulture));
        }

        static void WriteObject(StringBuilder sb, IDictionary dict, int depth)
        {
            if (depth > MaxDepth) throw new ArgumentException(TooDeep);
            sb.Append('{');
            var first = true;
            foreach (DictionaryEntry entry in dict)
            {
                if (!(entry.Key is string key))
                    throw new ArgumentException("JSON object keys must be strings");
                if (!first) sb.Append(',');
                first = false;
                WriteString(sb, key);
                sb.Append(':');
                Write(sb, entry.Value, depth);
            }
            sb.Append('}');
        }

        static void WriteArray(StringBuilder sb, IEnumerable list, int depth)
        {
            if (depth > MaxDepth) throw new ArgumentException(TooDeep);
            sb.Append('[');
            var first = true;
            foreach (var item in list)
            {
                if (!first) sb.Append(',');
                first = false;
                Write(sb, item, depth);
            }
            sb.Append(']');
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
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        sealed class Parser
        {
            readonly string _s;
            int _i;
            int _depth;

            public Parser(string s) { _s = s; }

            public object ParseDocument()
            {
                SkipWhitespace();
                var value = ParseValue();
                SkipWhitespace();
                if (_i != _s.Length) throw Error("Unexpected trailing characters");
                return value;
            }

            FormatException Error(string message)
            {
                return new FormatException(message + " at position " + _i);
            }

            void Enter()
            {
                if (++_depth > MaxDepth) throw Error(TooDeep);
            }

            void SkipWhitespace()
            {
                while (_i < _s.Length && (_s[_i] == ' ' || _s[_i] == '\t' || _s[_i] == '\n' || _s[_i] == '\r')) _i++;
            }

            object ParseValue()
            {
                if (_i >= _s.Length) throw Error("Unexpected end of input");
                switch (_s[_i])
                {
                    case '{': return ParseObject();
                    case '[': return ParseArray();
                    case '"': return ParseString();
                    case 't': ExpectLiteral("true"); return true;
                    case 'f': ExpectLiteral("false"); return false;
                    case 'n': ExpectLiteral("null"); return null;
                    default: return ParseNumber();
                }
            }

            void ExpectLiteral(string literal)
            {
                if (string.CompareOrdinal(_s, _i, literal, 0, literal.Length) != 0)
                    throw Error("Invalid literal");
                _i += literal.Length;
            }

            Dictionary<string, object> ParseObject()
            {
                var result = new Dictionary<string, object>();
                Enter();
                _i++;
                SkipWhitespace();
                if (_i < _s.Length && _s[_i] == '}') { _i++; _depth--; return result; }
                while (true)
                {
                    SkipWhitespace();
                    if (_i >= _s.Length || _s[_i] != '"') throw Error("Expected string key");
                    var key = ParseString();
                    SkipWhitespace();
                    if (_i >= _s.Length || _s[_i] != ':') throw Error("Expected ':'");
                    _i++;
                    SkipWhitespace();
                    result[key] = ParseValue();
                    SkipWhitespace();
                    if (_i >= _s.Length) throw Error("Unterminated object");
                    if (_s[_i] == ',') { _i++; continue; }
                    if (_s[_i] == '}') { _i++; _depth--; return result; }
                    throw Error("Expected ',' or '}'");
                }
            }

            List<object> ParseArray()
            {
                var result = new List<object>();
                Enter();
                _i++;
                SkipWhitespace();
                if (_i < _s.Length && _s[_i] == ']') { _i++; _depth--; return result; }
                while (true)
                {
                    SkipWhitespace();
                    result.Add(ParseValue());
                    SkipWhitespace();
                    if (_i >= _s.Length) throw Error("Unterminated array");
                    if (_s[_i] == ',') { _i++; continue; }
                    if (_s[_i] == ']') { _i++; _depth--; return result; }
                    throw Error("Expected ',' or ']'");
                }
            }

            string ParseString()
            {
                var sb = new StringBuilder();
                _i++;
                while (true)
                {
                    if (_i >= _s.Length) throw Error("Unterminated string");
                    var c = _s[_i++];
                    if (c == '"') return sb.ToString();
                    if (c < 0x20) throw Error("Control character in string");
                    if (c != '\\') { sb.Append(c); continue; }
                    if (_i >= _s.Length) throw Error("Unterminated escape");
                    var e = _s[_i++];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (_i + 4 > _s.Length) throw Error("Bad unicode escape");
                            if (!int.TryParse(_s.Substring(_i, 4), NumberStyles.AllowHexSpecifier,
                                    CultureInfo.InvariantCulture, out var code))
                                throw Error("Bad unicode escape");
                            sb.Append((char)code);
                            _i += 4;
                            break;
                        default: throw Error("Bad escape");
                    }
                }
            }

            object ParseNumber()
            {
                var start = _i;
                var isFloat = false;
                if (_i < _s.Length && _s[_i] == '-') _i++;
                while (_i < _s.Length)
                {
                    var c = _s[_i];
                    if (c >= '0' && c <= '9') { _i++; }
                    else if (c == '.' || c == 'e' || c == 'E' || c == '+' || c == '-') { isFloat = true; _i++; }
                    else break;
                }
                var text = _s.Substring(start, _i - start);
                if (text.Length == 0 || text == "-") throw Error("Invalid value");

                if (!isFloat && long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var l))
                    return l;

                if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                    throw Error("Invalid number");
                if (Math.Floor(d) == d && Math.Abs(d) <= MaxSafeInteger) return (long)d;
                return d;
            }
        }
    }
}
