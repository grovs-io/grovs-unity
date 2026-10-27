using System.Threading.Tasks;

namespace GrovsTestApp
{
    public sealed class IdentityActions
    {
        readonly IGrovsApi _api;

        public IdentityActions(IGrovsApi api)
        {
            _api = api;
        }

        public TestAction SetEnabled(bool on) =>
            new TestAction
            {
                Type = LogType.Sdk,
                Title = on ? "SDK enabled" : "SDK disabled",
                Code = CallCode.Format("SetEnabled", ("enabled", on)),
                Repeatable = false,
                Run = () =>
                {
                    _api.SetEnabled(on);
                    return Task.FromResult(new ActionResult { Detail = on ? "sending" : "holding events" });
                }
            };

        public TestAction Save(IdentityForm form)
        {
            var identifier = Calls.Blank(form.Identifier)?.Trim();
            var attributes = KeyValue.ToMap(form.Attributes);
            return new TestAction
            {
                Type = LogType.Sdk,
                Title = "Identity saved",
                Code = CallCode.Format("SetUserIdentifier", ("id", identifier))
                       + "\n\n" + CallCode.Format("SetUserAttributes", ("attrs", attributes)),
                Repeatable = false,
                Run = () =>
                {
                    _api.SetUserIdentifier(identifier);
                    _api.SetUserAttributes(attributes);
                    return Task.FromResult(new ActionResult { Detail = identifier ?? "no identifier" });
                }
            };
        }
    }
}
