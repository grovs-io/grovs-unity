using System;
using NUnit.Framework;

namespace GrovsTestApp.Tests
{
    public class ExportLogTests
    {
        static LogEntry Entry(LogType type, LogState state, string title, string detail, string code, int h, int m, int s) =>
            new LogEntry
            {
                Time = new DateTime(2026, 1, 1, h, m, s, DateTimeKind.Local),
                Type = type,
                State = state,
                Title = title,
                Detail = detail,
                Code = code
            };

        [Test]
        public void Builds_header_and_entries()
        {
            var entries = new[]
            {
                Entry(LogType.Event, LogState.Error, "Track", "bad", "track(\n  a = 1\n)", 10, 5, 9),
                Entry(LogType.InApp, LogState.Neutral, "Shown", "", "", 9, 0, 0)
            };
            var text = ExportLog.Build(entries, "Pixel 9", "1.2.3", "Demo");
            Assert.AreEqual(
                "Grovs test app log\nDevice: Pixel 9\nSDK: 1.2.3 · Project: Demo\n\n" +
                "10:05:09 EVENT ERROR Track\n  bad\n  track(\n    a = 1\n  )\n\n" +
                "09:00:00 IN-APP NEUTRAL Shown",
                text);
        }

        [Test]
        public void State_labels()
        {
            var text = ExportLog.Build(new[]
            {
                Entry(LogType.Sdk, LogState.Ok, "a", null, null, 1, 1, 1),
                Entry(LogType.Sdk, LogState.Warning, "b", null, null, 1, 1, 1)
            }, "d", "v", "p");
            StringAssert.Contains("01:01:01 SDK OK a", text);
            StringAssert.Contains("01:01:01 SDK WARNING b", text);
        }

        [Test]
        public void No_entries_gives_only_the_header()
        {
            Assert.AreEqual("Grovs test app log\nDevice: d\nSDK: v · Project: p",
                ExportLog.Build(new LogEntry[0], "d", "v", "p"));
        }
    }
}
