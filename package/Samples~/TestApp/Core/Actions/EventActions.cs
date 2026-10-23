using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GrovsSDK;

namespace GrovsTestApp
{
    public sealed class EventActions
    {
        readonly IGrovsApi _api;
        readonly Func<DateTime> _clock;
        readonly bool _isIos;

        public EventActions(IGrovsApi api, Func<DateTime> clock, bool isIos)
        {
            _api = api;
            _clock = clock;
            _isIos = isIos;
        }

        public TestAction Track(EventForm form, bool anyway)
        {
            var name = form.Name.Trim();
            var properties = Calls.MapOrNull(form.Properties);
            var tags = Calls.NullIfEmpty(form.Tags);
            return new TestAction
            {
                Type = LogType.Event,
                Title = "track " + name,
                Code = CallCode.Format("Track", ("name", name), ("properties", properties), ("tags", tags)),
                Run = () =>
                {
                    _api.Track(name, properties, tags);
                    var result = anyway
                        ? new ActionResult { State = LogState.Warning, Detail = "sent anyway, the SDK should reject it" }
                        : new ActionResult { Detail = tags != null ? "tags " + string.Join(", ", tags) : "sent" };
                    return Task.FromResult(result);
                }
            };
        }

        public TestAction Screen(ScreenForm form)
        {
            var name = form.Name.Trim();
            var properties = Calls.MapOrNull(form.Properties);
            return new TestAction
            {
                Type = LogType.Screen,
                Title = "Screen " + name,
                Code = CallCode.Format("TrackScreenView", ("screenName", name), ("properties", properties)),
                Run = () =>
                {
                    _api.TrackScreenView(name, properties);
                    return Task.FromResult(new ActionResult { Detail = "sent" });
                }
            };
        }

        public TestAction Purchase(PurchaseForm form)
        {
            var cents = Money.ParseCents(form.Amount);
            var date = _clock().AddDays(-form.DaysAgo);
            var type = form.Type;
            var currency = form.Currency;
            var product = form.Product;
            return new TestAction
            {
                Type = LogType.Purchase,
                Title = form.Label(),
                Code = CallCode.Format("LogCustomPurchaseAsync",
                    ("type", type), ("priceInCents", cents), ("currency", currency), ("productId", product), ("date", date)),
                Run = async () =>
                {
                    if (cents == null) throw new ArgumentException("Enter an amount like 4.99");
                    var handed = await _api.LogCustomPurchaseAsync(type, cents.Value, currency, product, date);
                    return Sent(handed, cents.Value + " cents · " + product);
                }
            };
        }

        public TestAction StoreReceipt(string receipt)
        {
            return new TestAction
            {
                Type = LogType.Purchase,
                Title = _isIos ? "App Store purchase" : "Google Play purchase",
                Code = CallCode.Format("LogInAppPurchaseAsync", ("storeTransaction", receipt)),
                Run = async () => Sent(await _api.LogInAppPurchaseAsync(receipt), "sent")
            };
        }

        public TestAction GlobalTags(List<string> tags)
        {
            var sent = Calls.NullIfEmpty(tags);
            return new TestAction
            {
                Type = LogType.Sdk,
                Title = "Global tags set",
                Code = CallCode.Format("SetGlobalTags", ("tags", sent)),
                Repeatable = false,
                Run = () =>
                {
                    _api.SetGlobalTags(sent);
                    return Task.FromResult(new ActionResult { Detail = sent == null ? "cleared" : string.Join(", ", sent) });
                }
            };
        }

        public TestAction Aliases(List<KeyValueRow> rows)
        {
            var map = new Dictionary<string, string>();
            foreach (var row in rows)
            {
                if (!string.IsNullOrWhiteSpace(row.Key)) map[row.Key.Trim()] = row.Value ?? "";
            }
            return new TestAction
            {
                Type = LogType.Sdk,
                Title = "Screen aliases set",
                Code = CallCode.Format("SetScreenAliases", ("aliases", map)),
                Repeatable = false,
                Run = () =>
                {
                    _api.SetScreenAliases(map);
                    return Task.FromResult(new ActionResult { Detail = map.Count + " aliases" });
                }
            };
        }

        static ActionResult Sent(bool handed, string detail) =>
            handed
                ? new ActionResult { Detail = detail }
                : new ActionResult { State = LogState.Warning, Detail = "the SDK did not accept it · " + detail };
    }
}
