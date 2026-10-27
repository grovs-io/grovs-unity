#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;

namespace GrovsSDK.Internal
{
    /// <summary>Calls the Kotlin bridge <c>io.grovs.unity.GrovsUnityBridge</c>.</summary>
    internal sealed class GrovsAndroid : IGrovsPlatform
    {
        const string BridgeClass = "io.grovs.unity.GrovsUnityBridge";

        // Java holds the proxy, so C# must keep it alive too.
        static ListenerProxy _listener;

        static readonly AndroidJavaClass Bridge = new AndroidJavaClass(BridgeClass);
        static readonly Dictionary<string, IntPtr> StringMethods = new Dictionary<string, IntPtr>();

        public bool IsSupported => true;

        public void Start(Action<string, string> onMessage, Func<string, bool> shouldShowInApp)
        {
            _listener = new ListenerProxy(onMessage, shouldShowInApp);
            Bridge.CallStatic("setListener", _listener);
        }

        public void SetEnabled(bool enabled) => Bridge.CallStatic("setEnabled", enabled);
        public bool IsEnabled() => Bridge.CallStatic<bool>("isEnabled");
        public bool IsSceneTrackingEnabled() => Bridge.CallStatic<bool>("isSceneTrackingEnabled");
        public void SetLogLevel(int level) => Bridge.CallStatic("setLogLevel", level);

        public void SetUserIdentifier(string id) => CallWithStrings("setUserIdentifier", id);
        public void SetUserAttributes(string json) => CallWithStrings("setUserAttributes", json);
        public void SetPushToken(string token) => CallWithStrings("setPushToken", token);

        public void GenerateLink(int requestId, string optionsJson) =>
            Bridge.CallStatic("generateLink", requestId, optionsJson ?? "{}");

        public void LinkDetails(int requestId, string path) => Bridge.CallStatic("linkDetails", requestId, path);
        public bool HandleUrl(string url) => Bridge.CallStatic<bool>("handleUrl", url);

        public void Track(string name, string propsJson, string tagsJson) =>
            CallWithStrings("track", name, propsJson, tagsJson);

        public void TrackScreenView(string name, string propsJson) =>
            CallWithStrings("trackScreenView", name, propsJson);

        public void SetGlobalTags(string tagsJson) => CallWithStrings("setGlobalTags", tagsJson);
        public void SetScreenAliases(string json) => CallWithStrings("setScreenAliases", json ?? "{}");

        public void LogInAppPurchase(int requestId, string storeTransaction) =>
            Bridge.CallStatic("logInAppPurchase", requestId, storeTransaction);

        public void LogCustomPurchase(int requestId, int type, int cents, string currency, string productId, long epochMs) =>
            Bridge.CallStatic("logCustomPurchase", requestId, type, cents, currency, productId, epochMs);

        public bool IsGrovsNotification(string json) => Bridge.CallStatic<bool>("isGrovsNotification", json);
        public bool HandleNotificationOpen(string json) => Bridge.CallStatic<bool>("handleNotificationOpen", json);

        public void PauseInApp() => Bridge.CallStatic("pauseInApp");
        public void ResumeInApp() => Bridge.CallStatic("resumeInApp");
        public void DiscardInApp(bool dismissCurrent) => Bridge.CallStatic("discardInApp", dismissCurrent);
        public void SetInAppExcludedScreens(string json) => CallWithStrings("setInAppExcludedScreens", json ?? "[]");
        public void SetInAppDebugLogging(bool enabled) => Bridge.CallStatic("setInAppDebugLogging", enabled);

        /// <summary>
        /// Calls a static void method that takes only strings, any of which may be null.
        /// CallStatic cannot do this: it types a null argument as Object, so the method is not found.
        /// </summary>
        static void CallWithStrings(string method, params string[] values)
        {
            var bridgeClass = Bridge.GetRawClass();
            var methodId = StringMethodId(bridgeClass, method, values.Length);
            var args = new jvalue[values.Length];
            try
            {
                for (var i = 0; i < values.Length; i++)
                    args[i].l = values[i] == null ? IntPtr.Zero : AndroidJNI.NewString(values[i]);
                AndroidJNI.CallStaticVoidMethod(bridgeClass, methodId, args);
                ThrowPendingJavaException(method);
            }
            finally
            {
                foreach (var arg in args)
                    if (arg.l != IntPtr.Zero) AndroidJNI.DeleteLocalRef(arg.l);
            }
        }

        static IntPtr StringMethodId(IntPtr bridgeClass, string method, int argCount)
        {
            if (StringMethods.TryGetValue(method, out var cached)) return cached;
            var signature = "(" + string.Concat(Enumerable.Repeat("Ljava/lang/String;", argCount)) + ")V";
            var methodId = AndroidJNI.GetStaticMethodID(bridgeClass, method, signature);
            ThrowPendingJavaException(method);
            if (methodId == IntPtr.Zero) throw new MissingMethodException(BridgeClass, method + signature);
            StringMethods[method] = methodId;
            return methodId;
        }

        static void ThrowPendingJavaException(string method)
        {
            var throwable = AndroidJNI.ExceptionOccurred();
            if (throwable == IntPtr.Zero) return;
            AndroidJNI.ExceptionClear();
            string description;
            using (var error = new AndroidJavaObject(throwable)) description = error.Call<string>("toString");
            AndroidJNI.DeleteLocalRef(throwable);
            throw new InvalidOperationException("[Grovs] " + method + " failed: " + description);
        }

        sealed class ListenerProxy : AndroidJavaProxy
        {
            readonly Action<string, string> _onMessage;
            readonly Func<string, bool> _shouldShowInApp;

            public ListenerProxy(Action<string, string> onMessage, Func<string, bool> shouldShowInApp)
                : base("io.grovs.unity.GrovsUnityListener")
            {
                _onMessage = onMessage;
                _shouldShowInApp = shouldShowInApp;
            }

            // Kotlin may call this while holding its queue lock, so it only enqueues.
            [Preserve]
            public void onMessage(string type, string json) => _onMessage(type, json);

            // Runs on the Android UI thread, not Unity's main thread.
            [Preserve]
            public bool shouldShowInAppMessage(string campaignId) => _shouldShowInApp(campaignId);
        }
    }
}
#endif
