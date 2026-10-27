using System;
using System.Collections.Generic;
using UnityEngine;

namespace GrovsSDK.Internal
{
    /// <summary>Used in the Editor and on every target without a native bridge.</summary>
    internal sealed class GrovsStub : IGrovsPlatform
    {
        readonly HashSet<string> _logged = new HashSet<string>();
        Action<string, string> _onMessage;

        public bool IsSupported => false;

        public void Start(Action<string, string> onMessage, Func<string, bool> shouldShowInApp)
        {
            _onMessage = onMessage;
        }

        public void SetEnabled(bool enabled) => LogOnce(nameof(SetEnabled));
        public bool IsEnabled() => LogOnce(nameof(IsEnabled), false);
        public bool IsSceneTrackingEnabled() => false;
        public void SetLogLevel(int level) => LogOnce(nameof(SetLogLevel));

        public void SetUserIdentifier(string id) => LogOnce(nameof(SetUserIdentifier));
        public void SetUserAttributes(string json) => LogOnce(nameof(SetUserAttributes));
        public void SetPushToken(string token) => LogOnce(nameof(SetPushToken));

        public void GenerateLink(int requestId, string optionsJson)
        {
            LogOnce(nameof(GenerateLink));
            FailUnsupported(requestId);
        }

        public void LinkDetails(int requestId, string path)
        {
            LogOnce(nameof(LinkDetails));
            FailUnsupported(requestId);
        }

        public bool HandleUrl(string url) => LogOnce(nameof(HandleUrl), false);

        public void Track(string name, string propsJson, string tagsJson) => LogOnce(nameof(Track));
        public void TrackScreenView(string name, string propsJson) => LogOnce(nameof(TrackScreenView));
        public void SetGlobalTags(string tagsJson) => LogOnce(nameof(SetGlobalTags));
        public void SetScreenAliases(string json) => LogOnce(nameof(SetScreenAliases));

        public void LogInAppPurchase(int requestId, string storeTransaction)
        {
            LogOnce(nameof(LogInAppPurchase));
            Succeed(requestId, false);
        }

        public void LogCustomPurchase(int requestId, int type, int cents, string currency, string productId, long epochMs)
        {
            LogOnce(nameof(LogCustomPurchase));
            Succeed(requestId, false);
        }

        public bool IsGrovsNotification(string json) => LogOnce(nameof(IsGrovsNotification), false);
        public bool HandleNotificationOpen(string json) => LogOnce(nameof(HandleNotificationOpen), false);

        public void PauseInApp() => LogOnce(nameof(PauseInApp));
        public void ResumeInApp() => LogOnce(nameof(ResumeInApp));
        public void DiscardInApp(bool dismissCurrent) => LogOnce(nameof(DiscardInApp));
        public void SetInAppExcludedScreens(string json) => LogOnce(nameof(SetInAppExcludedScreens));
        public void SetInAppDebugLogging(bool enabled) => LogOnce(nameof(SetInAppDebugLogging));

        void LogOnce(string method)
        {
            if (_logged.Add(method))
                Debug.Log("[Grovs] " + method + " does nothing on this platform. Grovs runs on iOS and Android players.");
        }

        bool LogOnce(string method, bool result)
        {
            LogOnce(method);
            return result;
        }

        void Succeed(int requestId, object value)
        {
            PostResult(requestId, true, value, null);
        }

        void FailUnsupported(int requestId)
        {
            PostResult(requestId, false, null, new Dictionary<string, object>
            {
                { "code", "unsupported_platform" },
                { "message", "Grovs is not supported on this platform." }
            });
        }

        void PostResult(int requestId, bool ok, object value, Dictionary<string, object> error)
        {
            _onMessage?.Invoke("result", GrovsJson.Serialize(new Dictionary<string, object>
            {
                { "requestId", requestId },
                { "ok", ok },
                { "value", value },
                { "error", error }
            }));
        }
    }
}
