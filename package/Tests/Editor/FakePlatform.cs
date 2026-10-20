using System;
using System.Collections.Generic;
using System.Linq;
using GrovsSDK.Internal;

namespace GrovsSDK.Tests
{
    internal sealed class FakePlatform : IGrovsPlatform
    {
        public readonly List<(string Method, object[] Args)> Calls = new List<(string, object[])>();
        public Action<string, string> OnMessage;
        public Func<string, bool> ShouldShow;
        public bool Supported = true;
        public bool HandleUrlResult = true;
        public bool ThrowOnStart;

        public void Emit(string type, string json) => OnMessage(type, json);

        public object[] Last(string method) => Calls.Last(c => c.Method == method).Args;

        public int Count(string method) => Calls.Count(c => c.Method == method);

        T Returns<T>(T result, string method, params object[] args)
        {
            Calls.Add((method, args));
            return result;
        }

        void Record(string method, params object[] args) => Calls.Add((method, args));

        public bool IsSupported => Supported;

        public void Start(Action<string, string> onMessage, Func<string, bool> shouldShowInApp)
        {
            Record(nameof(Start));
            if (ThrowOnStart) throw new InvalidOperationException("bridge missing");
            OnMessage = onMessage;
            ShouldShow = shouldShowInApp;
        }

        public void SetEnabled(bool enabled) => Record(nameof(SetEnabled), enabled);
        public bool IsEnabled() => Returns(true, nameof(IsEnabled));
        public bool IsSceneTrackingEnabled() => Returns(false, nameof(IsSceneTrackingEnabled));
        public void SetLogLevel(int level) => Record(nameof(SetLogLevel), level);
        public void SetUserIdentifier(string id) => Record(nameof(SetUserIdentifier), id);
        public void SetUserAttributes(string json) => Record(nameof(SetUserAttributes), json);
        public void SetPushToken(string token) => Record(nameof(SetPushToken), token);
        public void GenerateLink(int requestId, string optionsJson) => Record(nameof(GenerateLink), requestId, optionsJson);
        public void LinkDetails(int requestId, string path) => Record(nameof(LinkDetails), requestId, path);
        public bool HandleUrl(string url) => Returns(HandleUrlResult, nameof(HandleUrl), url);
        public void Track(string name, string propsJson, string tagsJson) => Record(nameof(Track), name, propsJson, tagsJson);
        public void TrackScreenView(string name, string propsJson) => Record(nameof(TrackScreenView), name, propsJson);
        public void SetGlobalTags(string tagsJson) => Record(nameof(SetGlobalTags), tagsJson);
        public void SetScreenAliases(string json) => Record(nameof(SetScreenAliases), json);
        public void LogInAppPurchase(int requestId, string storeTransaction) =>
            Record(nameof(LogInAppPurchase), requestId, storeTransaction);
        public void LogCustomPurchase(int requestId, int type, int cents, string currency, string productId, long epochMs) =>
            Record(nameof(LogCustomPurchase), requestId, type, cents, currency, productId, epochMs);
        public bool IsGrovsNotification(string json) => Returns(true, nameof(IsGrovsNotification), json);
        public bool HandleNotificationOpen(string json) => Returns(true, nameof(HandleNotificationOpen), json);
        public void PauseInApp() => Record(nameof(PauseInApp));
        public void ResumeInApp() => Record(nameof(ResumeInApp));
        public void DiscardInApp(bool dismissCurrent) => Record(nameof(DiscardInApp), dismissCurrent);
        public void SetInAppExcludedScreens(string json) => Record(nameof(SetInAppExcludedScreens), json);
        public void SetInAppDebugLogging(bool enabled) => Record(nameof(SetInAppDebugLogging), enabled);
    }
}
