using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GrovsSDK;

namespace GrovsTestApp.Tests
{
    public sealed class FakeGrovsApi : IGrovsApi
    {
        public readonly List<string> Calls = new List<string>();

        public GenerateLinkOptions LastOptions;
        public string LinkResult = "https://x.grovs.link/abc";
        public GrovsError CallbackError;
        public IReadOnlyDictionary<string, object> DetailsResult = new Dictionary<string, object>();
        public string LastDetailsPath;
        public string LastTrackName;
        public IDictionary<string, object> LastTrackProperties;
        public IList<string> LastTrackTags;
        public string LastScreenName;
        public IDictionary<string, object> LastScreenProperties;
        public IList<string> LastGlobalTags;
        public IDictionary<string, string> LastAliases;
        public string LastIdentifier;
        public IDictionary<string, object> LastAttributes;
        public bool? LastEnabled;
        public bool? LastDiscardDismiss;
        public bool? LastDebug;
        public IList<string> LastExcluded;
        public string LastReceipt;
        public (GrovsTransactionType type, int cents, string currency, string product, DateTime? date) LastPurchase;

        public bool IsEnabled => LastEnabled ?? true;
        public void SetEnabled(bool enabled) { Calls.Add("SetEnabled"); LastEnabled = enabled; }

        public Task<string> GenerateLinkAsync(GenerateLinkOptions options)
        {
            Calls.Add("GenerateLinkAsync");
            LastOptions = options;
            return Task.FromResult(LinkResult);
        }

        public void GenerateLink(GenerateLinkOptions options, Action<string, GrovsError> callback)
        {
            Calls.Add("GenerateLink");
            LastOptions = options;
            if (CallbackError != null) callback(null, CallbackError);
            else callback(LinkResult, null);
        }

        public Task<IReadOnlyDictionary<string, object>> GetLinkDetailsAsync(string path)
        {
            Calls.Add("GetLinkDetailsAsync");
            LastDetailsPath = path;
            return Task.FromResult(DetailsResult);
        }

        public void GetLinkDetails(string path, Action<IReadOnlyDictionary<string, object>, GrovsError> callback)
        {
            Calls.Add("GetLinkDetails");
            LastDetailsPath = path;
            if (CallbackError != null) callback(null, CallbackError);
            else callback(DetailsResult, null);
        }

        public void SetUserIdentifier(string id) { Calls.Add("SetUserIdentifier"); LastIdentifier = id; }
        public void SetUserAttributes(IDictionary<string, object> attrs) { Calls.Add("SetUserAttributes"); LastAttributes = attrs; }

        public void Track(string name, IDictionary<string, object> properties, IList<string> tags)
        {
            Calls.Add("Track");
            LastTrackName = name;
            LastTrackProperties = properties;
            LastTrackTags = tags;
        }

        public void TrackScreenView(string screenName, IDictionary<string, object> properties)
        {
            Calls.Add("TrackScreenView");
            LastScreenName = screenName;
            LastScreenProperties = properties;
        }

        public void SetGlobalTags(IList<string> tags) { Calls.Add("SetGlobalTags"); LastGlobalTags = tags; }
        public void SetScreenAliases(IDictionary<string, string> aliases) { Calls.Add("SetScreenAliases"); LastAliases = aliases; }
        public void PauseInAppMessages() => Calls.Add("PauseInAppMessages");
        public void ResumeInAppMessages() => Calls.Add("ResumeInAppMessages");
        public void DiscardInAppMessages(bool dismissCurrent) { Calls.Add("DiscardInAppMessages"); LastDiscardDismiss = dismissCurrent; }
        public void SetInAppExcludedScreens(IList<string> screens) { Calls.Add("SetInAppExcludedScreens"); LastExcluded = screens; }
        public void SetInAppDebugLogging(bool enabled) { Calls.Add("SetInAppDebugLogging"); LastDebug = enabled; }

        public Task<bool> LogInAppPurchaseAsync(string storeTransaction)
        {
            Calls.Add("LogInAppPurchaseAsync");
            LastReceipt = storeTransaction;
            return Task.FromResult(true);
        }

        public Task<bool> LogCustomPurchaseAsync(GrovsTransactionType type, int priceInCents, string currency, string productId, DateTime? date)
        {
            Calls.Add("LogCustomPurchaseAsync");
            LastPurchase = (type, priceInCents, currency, productId, date);
            return Task.FromResult(true);
        }

        public void SetLogLevel(GrovsLogLevel level) => Calls.Add("SetLogLevel");
        public bool HandleUrl(string url) { Calls.Add("HandleUrl"); return true; }
    }
}
