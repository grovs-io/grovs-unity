using System.Collections.Generic;
using System.Threading.Tasks;

namespace GrovsTestApp
{
    public sealed class InAppActions
    {
        readonly IGrovsApi _api;

        public InAppActions(IGrovsApi api)
        {
            _api = api;
        }

        public TestAction Pause() =>
            Make("Pause messages", "PauseInAppMessages", false, "paused", () => _api.PauseInAppMessages());

        public TestAction Resume() =>
            Make("Resume messages", "ResumeInAppMessages", false, "running", () => _api.ResumeInAppMessages());

        public TestAction Trigger(string name)
        {
            var trimmed = name.Trim();
            return new TestAction
            {
                Type = LogType.Event,
                Title = "Trigger " + trimmed,
                Code = CallCode.Format("Track", ("name", trimmed)),
                Run = () =>
                {
                    _api.Track(trimmed, null, null);
                    return Task.FromResult(new ActionResult { Detail = "sent" });
                }
            };
        }

        public TestAction DiscardWaiting() => Discard("Discard waiting messages", false);

        public TestAction CloseCurrent() => Discard("Close current message", true);

        public TestAction SetDebug(bool on) =>
            Make("Debug logging " + (on ? "on" : "off"), "SetInAppDebugLogging", false, "applied",
                () => _api.SetInAppDebugLogging(on), ("enabled", on));

        public TestAction SetExcluded(List<string> screens) =>
            Make("Excluded screens set", "SetInAppExcludedScreens", false,
                screens.Count == 0 ? "none" : string.Join(", ", screens),
                () => _api.SetInAppExcludedScreens(screens), ("screens", screens));

        TestAction Discard(string title, bool dismissCurrent) =>
            Make(title, "DiscardInAppMessages", true, "paused",
                () => _api.DiscardInAppMessages(dismissCurrent), ("dismissCurrent", dismissCurrent));

        static TestAction Make(string title, string method, bool repeatable, string detail, System.Action call,
            params (string key, object value)[] args) =>
            new TestAction
            {
                Type = LogType.InApp,
                Title = title,
                Code = CallCode.Format(method, args),
                Repeatable = repeatable,
                Run = () =>
                {
                    call();
                    return Task.FromResult(new ActionResult { Detail = detail });
                }
            };
    }
}
