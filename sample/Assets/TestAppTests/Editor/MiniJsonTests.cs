using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace GrovsTestApp.Tests
{
    public class MiniJsonTests
    {
        [Test]
        public void Parse_reads_escapes()
        {
            var text = (string)MiniJson.Parse("\"q\\\" b\\\\ n\\n \\u00e9 é\"");

            Assert.AreEqual("q\" b\\ n\n é é", text);
        }

        [Test]
        public void Parse_reads_nested_objects_and_arrays()
        {
            var root = (Dictionary<string, object>)MiniJson.Parse("{\"a\":{\"b\":[1,2.5,true,null,\"x\"]}}");
            var list = (List<object>)((Dictionary<string, object>)root["a"])["b"];

            Assert.AreEqual(1L, list[0]);
            Assert.AreEqual(2.5, list[1]);
            Assert.AreEqual(true, list[2]);
            Assert.IsNull(list[3]);
            Assert.AreEqual("x", list[4]);
        }

        [TestCase("[1,]")]
        [TestCase("{\"a\":1,}")]
        [TestCase("{\"a\" 1}")]
        [TestCase("[1] x")]
        [TestCase("\"open")]
        [TestCase("")]
        public void Parse_fails_on_bad_input(string json)
        {
            Assert.Throws<FormatException>(() => MiniJson.Parse(json));
        }

        [Test]
        public void Serialize_escapes_and_round_trips()
        {
            var value = new Dictionary<string, object>
            {
                ["s"] = "q\" b\\ n\n é",
                ["n"] = 4L,
                ["d"] = 1.5,
                ["b"] = false,
                ["z"] = null,
                ["l"] = new List<object> { 1L, "x" },
            };

            var back = (Dictionary<string, object>)MiniJson.Parse(MiniJson.Serialize(value));

            Assert.AreEqual("q\" b\\ n\n é", back["s"]);
            Assert.AreEqual(4L, back["n"]);
            Assert.AreEqual(1.5, back["d"]);
            Assert.AreEqual(false, back["b"]);
            Assert.IsNull(back["z"]);
            Assert.AreEqual(2, ((List<object>)back["l"]).Count);
        }
    }
}
