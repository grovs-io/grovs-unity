using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GrovsSDK.Internal;

namespace GrovsSDK.Tests
{
    public class GrovsValidationTests
    {
        static readonly string[] ReservedNames =
        {
            "view", "open", "install", "reinstall", "app_open", "time_spent", "reactivation",
            "user_referred", "custom", "screen_view", "push_open", "push_delivered", "push_held",
            "in_app_viewed", "in_app_clicked", "in_app_dismissed", "in_app_answered",
            "in_app_commented", "in_app_held", "journey_entered", "journey_held", "journey_path"
        };

        [Test]
        public void Validation_ReservedNames()
        {
            Assert.AreEqual(22, ReservedNames.Length);
            foreach (var name in ReservedNames)
                Assert.IsTrue(GrovsValidation.IsReservedEventName(name), name);
            Assert.IsFalse(GrovsValidation.IsReservedEventName("level_up"));
            Assert.IsTrue(GrovsValidation.IsReservedEventName(""));
            Assert.IsNull(GrovsValidation.CheckEvent("level_up", null, null));
            Assert.IsNotNull(GrovsValidation.CheckEvent("view", null, null));
        }

        [Test]
        public void Validation_Limits()
        {
            var tags = new List<string>();
            for (var i = 0; i < 20; i++) tags.Add("t" + i);
            Assert.IsNull(GrovsValidation.CheckEvent("e", null, tags));
            tags.Add("extra");
            Assert.IsNotNull(GrovsValidation.CheckEvent("e", null, tags));

            Assert.IsNull(GrovsValidation.CheckEvent("e", null, new List<string> { new string('a', 255) }));
            Assert.IsNotNull(GrovsValidation.CheckEvent("e", null, new List<string> { new string('a', 256) }));

            var big = new Dictionary<string, object> { { "k", new string('x', 9000) } };
            Assert.IsNotNull(GrovsValidation.CheckEvent("e", big, null));
            var multibyte = new Dictionary<string, object> { { "k", new string('é', 4100) } };
            Assert.IsNotNull(GrovsValidation.CheckEvent("e", multibyte, null));
            var small = new Dictionary<string, object> { { "k", "v" } };
            Assert.IsNull(GrovsValidation.CheckEvent("e", small, null));
        }

        [Test]
        public void EnsureSerializable_RejectsGameObjectLikeValues()
        {
            var ok = new Dictionary<string, object> { { "a", 1 }, { "b", new List<object> { "x" } } };
            Assert.DoesNotThrow(() => GrovsValidation.EnsureSerializable(ok, "data"));
            Assert.DoesNotThrow(() => GrovsValidation.EnsureSerializable(null, "data"));

            var bad = new Dictionary<string, object> { { "pos", Vector3.one } };
            var ex = Assert.Throws<System.ArgumentException>(() => GrovsValidation.EnsureSerializable(bad, "data"));
            StringAssert.Contains("pos", ex.Message);
        }
    }
}
