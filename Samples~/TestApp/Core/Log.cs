using System;
using System.Threading.Tasks;

namespace GrovsTestApp
{
    public enum LogType { Link, Create, Inspect, Event, Screen, Purchase, InApp, Push, Sdk }

    public enum LogState { Ok, Warning, Error, Neutral }

    [Flags]
    public enum LinkSources { None = 0, Event = 1, LastDeeplink = 2 }

    public static class LogTypes
    {
        public static string Label(LogType type)
        {
            switch (type)
            {
                case LogType.Link: return "LINK";
                case LogType.Create: return "CREATE";
                case LogType.Inspect: return "INSPECT";
                case LogType.Event: return "EVENT";
                case LogType.Screen: return "SCREEN";
                case LogType.Purchase: return "PURCHASE";
                case LogType.InApp: return "IN-APP";
                case LogType.Push: return "PUSH";
                default: return "SDK";
            }
        }

        /// <summary>The filter chip a type belongs to, or null when only "All" shows it.</summary>
        public static string Filter(LogType type)
        {
            switch (type)
            {
                case LogType.Link:
                case LogType.Create:
                case LogType.Inspect: return "Links";
                case LogType.Event:
                case LogType.Screen:
                case LogType.Purchase: return "Events";
                case LogType.InApp: return "In-app";
                case LogType.Push: return "Push";
                default: return null;
            }
        }
    }

    public sealed class ActionResult
    {
        public LogState State = LogState.Ok;
        public string Detail;
        public object Value;
        public string Code;
    }

    /// <summary>One SDK call with its values, kept by the log so it can be run again.</summary>
    public sealed class TestAction
    {
        public LogType Type;
        public string Title;
        public string Code;
        /// <summary>False for setters: running an old one again would leave the screen showing a newer value than the SDK has.</summary>
        public bool Repeatable = true;
        /// <summary>The call style the action was built with, for calls that have both styles.</summary>
        public CallStyle? Style;
        public Func<Task<ActionResult>> Run;
    }

    public sealed class LogEntry
    {
        public long Id;
        public DateTime Time;
        public LogType Type;
        public LogState State;
        public string Title;
        public string Detail;
        public string Code;
        public TestAction Action;
        /// <summary>The link a Link entry was opened from, or the link a Create entry made.</summary>
        public string Link;
        public LinkSources Sources;
        /// <summary>How long the action took. Zero for entries that were not run by the runner.</summary>
        public long ElapsedMs;
    }
}
