using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace GrovsTestApp.Tests
{
    public class CallCodeTests
    {
        [Test]
        public void Formats_named_arguments_one_per_line()
        {
            var code = CallCode.Format("track", ("name", "a"), ("tags", new[] { "x", "y" }));
            Assert.AreEqual("track(\n  name = \"a\",\n  tags = [x, y]\n)", code);
        }

        [Test]
        public void No_arguments_gives_empty_parentheses()
        {
            Assert.AreEqual("reset()", CallCode.Format("reset"));
        }

        [Test]
        public void Formats_maps()
        {
            var map = new Dictionary<string, object> { { "level", 3 } };
            Assert.AreEqual("f(\n  data = { level: 3 }\n)", CallCode.Format("f", ("data", map)));
        }

        [Test]
        public void Maps_format_nested_values_by_the_same_rules()
        {
            var map = new Dictionary<string, object> { { "a", "s" }, { "b", true }, { "c", null } };
            Assert.AreEqual("f(\n  d = { a: \"s\", b: true, c: null }\n)", CallCode.Format("f", ("d", map)));
        }

        [Test]
        public void Formats_read_only_maps()
        {
            IReadOnlyDictionary<string, object> map = new Dictionary<string, object> { { "level", 3 } };
            Assert.AreEqual("f(\n  d = { level: 3 }\n)", CallCode.Format("f", ("d", map)));
        }

        [Test]
        public void Empty_map_and_list()
        {
            Assert.AreEqual("f(\n  a = {},\n  b = []\n)",
                CallCode.Format("f", ("a", new Dictionary<string, object>()), ("b", new List<string>())));
        }

        [Test]
        public void Formats_dates_as_year_month_day()
        {
            Assert.AreEqual("f(\n  d = 2026-03-07\n)", CallCode.Format("f", ("d", new DateTime(2026, 3, 7, 13, 5, 0))));
        }

        [Test]
        public void Formats_scalars()
        {
            Assert.AreEqual("f(\n  a = null,\n  b = true,\n  c = false,\n  d = 5,\n  e = 1.5\n)",
                CallCode.Format("f", ("a", null), ("b", true), ("c", false), ("d", 5), ("e", 1.5)));
        }

        [Test]
        public void Numbers_use_the_invariant_culture()
        {
            var old = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
                Assert.AreEqual("f(\n  a = 1.5\n)", CallCode.Format("f", ("a", 1.5)));
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = old;
            }
        }
    }
}
