using System;

namespace GrovsSDK.Internal
{
    /// <summary>One native bridge. Complex values cross as JSON strings.</summary>
    internal interface IGrovsPlatform
    {
        bool IsSupported { get; }

        /// <summary>Registers the callbacks, then tells native that C# is ready.</summary>
        void Start(Action<string, string> onMessage, Func<string, bool> shouldShowInApp);

        void SetEnabled(bool enabled);
        bool IsEnabled();
        bool IsSceneTrackingEnabled();
        void SetLogLevel(int level);

        void SetUserIdentifier(string id);
        void SetUserAttributes(string json);
        void SetPushToken(string token);

        void GenerateLink(int requestId, string optionsJson);
        void LinkDetails(int requestId, string path);
        bool HandleUrl(string url);

        void Track(string name, string propsJson, string tagsJson);
        void TrackScreenView(string name, string propsJson);
        void SetGlobalTags(string tagsJson);
        void SetScreenAliases(string json);

        void LogInAppPurchase(int requestId, string storeTransaction);
        void LogCustomPurchase(int requestId, int type, int cents, string currency, string productId, long epochMs);

        bool IsGrovsNotification(string json);
        bool HandleNotificationOpen(string json);

        void PauseInApp();
        void ResumeInApp();
        void DiscardInApp(bool dismissCurrent);
        void SetInAppExcludedScreens(string json);
        void SetInAppDebugLogging(bool enabled);
    }
}
