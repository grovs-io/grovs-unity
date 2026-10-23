using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace GrovsTestApp.Tests
{
    public class ThemeTests
    {
        static readonly string[] Tokens =
        {
            "--background", "--surface", "--text", "--text-muted", "--text-faint", "--border", "--divider",
            "--chip-fill", "--accent", "--accent-soft", "--ok", "--ok-soft", "--warning", "--warning-soft",
            "--error", "--error-soft", "--neutral-dot"
        };

        [TestCase("Light.uss")]
        [TestCase("Dark.uss")]
        public void Light_and_dark_define_every_token(string file)
        {
            var path = Path.Combine(Application.dataPath, "TestApp", "UI", "Theme", file);
            var css = File.ReadAllText(path);
            foreach (var token in Tokens)
                Assert.That(css, Does.Contain(token + ":"), file + " must define " + token);
        }
    }
}
