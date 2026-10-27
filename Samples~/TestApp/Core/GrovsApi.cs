using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GrovsSDK;

namespace GrovsTestApp
{
    /// <summary>The SDK calls the test app makes, so actions can run against a fake.</summary>
    public interface IGrovsApi
    {
        bool IsEnabled { get; }
        void SetEnabled(bool enabled);
        Task<string> GenerateLinkAsync(GenerateLinkOptions options);
        void GenerateLink(GenerateLinkOptions options, Action<string, GrovsError> callback);
        Task<IReadOnlyDictionary<string, object>> GetLinkDetailsAsync(string path);
        void GetLinkDetails(string path, Action<IReadOnlyDictionary<string, object>, GrovsError> callback);
        void SetUserIdentifier(string id);
        void SetUserAttributes(IDictionary<string, object> attrs);
        void Track(string name, IDictionary<string, object> properties, IList<string> tags);
        void TrackScreenView(string screenName, IDictionary<string, object> properties);
        void SetGlobalTags(IList<string> tags);
        void SetScreenAliases(IDictionary<string, string> aliases);
        void PauseInAppMessages();
        void ResumeInAppMessages();
        void DiscardInAppMessages(bool dismissCurrent);
        void SetInAppExcludedScreens(IList<string> screens);
        void SetInAppDebugLogging(bool enabled);
        Task<bool> LogInAppPurchaseAsync(string storeTransaction);
        Task<bool> LogCustomPurchaseAsync(GrovsTransactionType type, int priceInCents, string currency, string productId, DateTime? date);
        void SetLogLevel(GrovsLogLevel level);
        bool HandleUrl(string url);
    }

    public sealed class GrovsApi : IGrovsApi
    {
        public bool IsEnabled => Grovs.IsEnabled;
        public void SetEnabled(bool enabled) => Grovs.SetEnabled(enabled);
        public Task<string> GenerateLinkAsync(GenerateLinkOptions options) => Grovs.GenerateLinkAsync(options);
        public void GenerateLink(GenerateLinkOptions options, Action<string, GrovsError> callback) => Grovs.GenerateLink(options, callback);
        public Task<IReadOnlyDictionary<string, object>> GetLinkDetailsAsync(string path) => Grovs.GetLinkDetailsAsync(path);

        public void GetLinkDetails(string path, Action<IReadOnlyDictionary<string, object>, GrovsError> callback) =>
            Grovs.GetLinkDetails(path, callback);

        public void SetUserIdentifier(string id) => Grovs.SetUserIdentifier(id);
        public void SetUserAttributes(IDictionary<string, object> attrs) => Grovs.SetUserAttributes(attrs);
        public void Track(string name, IDictionary<string, object> properties, IList<string> tags) => Grovs.Track(name, properties, tags);
        public void TrackScreenView(string screenName, IDictionary<string, object> properties) => Grovs.TrackScreenView(screenName, properties);
        public void SetGlobalTags(IList<string> tags) => Grovs.SetGlobalTags(tags);
        public void SetScreenAliases(IDictionary<string, string> aliases) => Grovs.SetScreenAliases(aliases);
        public void PauseInAppMessages() => Grovs.PauseInAppMessages();
        public void ResumeInAppMessages() => Grovs.ResumeInAppMessages();
        public void DiscardInAppMessages(bool dismissCurrent) => Grovs.DiscardInAppMessages(dismissCurrent);
        public void SetInAppExcludedScreens(IList<string> screens) => Grovs.SetInAppExcludedScreens(screens);
        public void SetInAppDebugLogging(bool enabled) => Grovs.SetInAppDebugLogging(enabled);
        public Task<bool> LogInAppPurchaseAsync(string storeTransaction) => Grovs.LogInAppPurchaseAsync(storeTransaction);

        public Task<bool> LogCustomPurchaseAsync(GrovsTransactionType type, int priceInCents, string currency, string productId, DateTime? date) =>
            Grovs.LogCustomPurchaseAsync(type, priceInCents, currency, productId, date);

        public void SetLogLevel(GrovsLogLevel level) => Grovs.SetLogLevel(level);
        public bool HandleUrl(string url) => Grovs.HandleUrl(url);
    }
}
