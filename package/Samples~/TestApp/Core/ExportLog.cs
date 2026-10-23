using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GrovsTestApp
{
    public static class ExportLog
    {
        public static string Build(IEnumerable<LogEntry> entries, string device, string sdkVersion, string project)
        {
            var header = "Grovs test app log\nDevice: " + device + "\nSDK: " + sdkVersion + " · Project: " + project;
            return string.Join("\n\n", new[] { header }.Concat(entries.Select(Entry)));
        }

        static string Entry(LogEntry e)
        {
            var lines = new List<string>
            {
                e.Time.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + " " + LogTypes.Label(e.Type) + " " +
                e.State.ToString().ToUpperInvariant() + " " + e.Title
            };
            AddIndented(lines, e.Detail);
            AddIndented(lines, e.Code);
            return string.Join("\n", lines);
        }

        static void AddIndented(List<string> lines, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            lines.AddRange(text.Split('\n').Select(l => "  " + l.TrimEnd('\r')));
        }
    }
}
