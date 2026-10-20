using System;
using System.Collections.Generic;
using NUnit.Framework;
using GrovsSDK.Internal;

namespace GrovsSDK.Tests
{
    public class GrovsJsonTests
    {
        const string Payload =
            "{\"id\": 9007199254740993, \"n\": 1.5, \"nested\": {\"a\": [1, \"x\", null]}, \"emoji\": \"🎮\"}";

        static Dictionary<string, object> Expected()
        {
            return new Dictionary<string, object>
            {
                { "id", 9007199254740993L },
                { "n", 1.5d },
                { "nested", new Dictionary<string, object> { { "a", new List<object> { 1L, "x", null } } } },
                { "emoji", "🎮" },
            };
        }

        static void AssertDeepEqual(object expected, object actual)
        {
            if (expected is Dictionary<string, object> ed)
            {
                var ad = (Dictionary<string, object>)actual;
                Assert.AreEqual(ed.Count, ad.Count);
                foreach (var kv in ed)
                {
                    Assert.IsTrue(ad.ContainsKey(kv.Key), kv.Key);
                    AssertDeepEqual(kv.Value, ad[kv.Key]);
                }
            }
            else if (expected is List<object> el)
            {
                var al = (List<object>)actual;
                Assert.AreEqual(el.Count, al.Count);
                for (var i = 0; i < el.Count; i++) AssertDeepEqual(el[i], al[i]);
            }
            else
            {
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public void Parse_NumbersWholeBecomeLong()
        {
            var d = (Dictionary<string, object>)GrovsJson.Parse("{\"a\":3,\"b\":3.0,\"c\":9007199254740993}");
            Assert.AreEqual(3L, d["a"]);
            Assert.AreEqual(3L, d["b"]);
            Assert.AreEqual(9007199254740993L, d["c"]);
        }

        [Test]
        public void Parse_DecimalBecomesDouble()
        {
            Assert.AreEqual(1.5d, GrovsJson.Parse("1.5"));
        }

        [Test]
        public void Parse_NestedAndUnicode()
        {
            AssertDeepEqual(Expected(), GrovsJson.Parse(Payload));
            var escaped = (Dictionary<string, object>)GrovsJson.Parse("{\"e\":\"\\uD83C\\uDFAE\"}");
            Assert.AreEqual("🎮", escaped["e"]);
        }

        [Test]
        public void Serialize_RoundTrip()
        {
            var x = Expected();
            AssertDeepEqual(x, GrovsJson.Parse(GrovsJson.Serialize(x)));
        }

        [Test]
        public void Serialize_EscapesControlChars()
        {
            Assert.AreEqual("\"a\\\"b\\n\"", GrovsJson.Serialize("a\"b\n"));
        }

        [Test]
        public void Serialize_RejectsUnsupported()
        {
            Assert.Throws<ArgumentException>(() => GrovsJson.Serialize(new object()));
        }

        [Test]
        public void Parse_Invalid_ThrowsFormatException()
        {
            Assert.Throws<FormatException>(() => GrovsJson.Parse("{"));
        }
    }
}
