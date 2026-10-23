using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace GrovsTestApp.Tests
{
    public class KeyValueTests
    {
        static KeyValueRow Row(string key, string value, ValueKind kind) =>
            new KeyValueRow { Key = key, Value = value, Kind = kind };

        [TestCase("true", ValueKind.Bool)]
        [TestCase("FALSE", ValueKind.Bool)]
        [TestCase("-12", ValueKind.Number)]
        [TestCase("3.5", ValueKind.Number)]
        [TestCase("2026-10-23", ValueKind.Date)]
        [TestCase("2026-13-40", ValueKind.Text)]
        [TestCase("12a", ValueKind.Text)]
        [TestCase("", ValueKind.Text)]
        [TestCase(null, ValueKind.Text)]
        public void Detect_picks_the_kind(string input, ValueKind expected)
        {
            Assert.AreEqual(expected, KeyValue.Detect(input));
        }

        [Test]
        public void Typed_converts_by_kind()
        {
            Assert.AreEqual(3L, KeyValue.Typed(Row("a", "3", ValueKind.Number)));
            Assert.AreEqual(3.5, KeyValue.Typed(Row("a", "3.5", ValueKind.Number)));
            Assert.AreEqual(true, KeyValue.Typed(Row("a", "True", ValueKind.Bool)));
            Assert.AreEqual("2026-10-23", KeyValue.Typed(Row("a", "2026-10-23", ValueKind.Date)));
            Assert.AreEqual(" hi ", KeyValue.Typed(Row("a", " hi ", ValueKind.Text)));
        }

        [Test]
        public void Typed_keeps_the_text_when_a_number_does_not_parse()
        {
            Assert.AreEqual("abc", KeyValue.Typed(Row("a", "abc", ValueKind.Number)));
        }

        [Test]
        public void ToMap_skips_blank_keys_and_types_values()
        {
            var map = KeyValue.ToMap(new[]
            {
                Row("level", "3", ValueKind.Number),
                Row("vip", "true", ValueKind.Bool),
                Row("", "x", ValueKind.Text),
            });

            Assert.AreEqual(2, map.Count);
            Assert.AreEqual(3L, map["level"]);
            Assert.AreEqual(true, map["vip"]);
        }

        [Test]
        public void ToMap_last_duplicate_wins()
        {
            var map = KeyValue.ToMap(new[]
            {
                Row("a", "1", ValueKind.Number),
                Row("a", "2", ValueKind.Number),
            });

            Assert.AreEqual(2L, map["a"]);
        }

        [Test]
        public void FromJson_reads_rows_with_kinds()
        {
            var rows = KeyValue.FromJson("{\"level\":3,\"name\":\"a\"}");

            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual("level", rows[0].Key);
            Assert.AreEqual("3", rows[0].Value);
            Assert.AreEqual(ValueKind.Number, rows[0].Kind);
            Assert.AreEqual(ValueKind.Text, rows[1].Kind);
        }

        [TestCase("[1]")]
        [TestCase("3")]
        [TestCase("not json")]
        [TestCase("")]
        public void FromJson_rejects_anything_but_an_object(string json)
        {
            var e = Assert.Throws<FormatException>(() => KeyValue.FromJson(json));
            Assert.AreEqual("Not valid JSON", e.Message);
        }

        [Test]
        public void Json_round_trip_keeps_the_rows()
        {
            var rows = new List<KeyValueRow>
            {
                Row("level", "3", ValueKind.Number),
                Row("ratio", "1.5", ValueKind.Number),
                Row("vip", "true", ValueKind.Bool),
                Row("name", "a \"b\"", ValueKind.Text),
            };

            var back = KeyValue.FromJson(KeyValue.ToJson(rows));

            Assert.AreEqual(rows.Count, back.Count);
            for (var i = 0; i < rows.Count; i++)
            {
                Assert.AreEqual(rows[i].Key, back[i].Key);
                Assert.AreEqual(rows[i].Value, back[i].Value);
                Assert.AreEqual(rows[i].Kind, back[i].Kind);
            }
        }
    }
}
