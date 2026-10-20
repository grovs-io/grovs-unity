#if UNITY_IOS && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

namespace GrovsSDK.Internal
{
    /// <summary>Calls the Swift bridge through its <c>GrovsUnity_</c> C functions.</summary>
    internal sealed class GrovsIOS : IGrovsPlatform
    {
        const string Lib = "__Internal";

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void MessageCallback(IntPtr type, IntPtr json);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        delegate bool ShouldShowCallback(IntPtr campaignId);

        // Native holds these function pointers, so the delegates must never be collected.
        static readonly MessageCallback MessageCallbackDelegate = OnNativeMessage;
        static readonly ShouldShowCallback ShouldShowCallbackDelegate = OnNativeShouldShow;

        static Action<string, string> _onMessage;
        static Func<string, bool> _shouldShowInApp;

        public bool IsSupported => true;

        public void Start(Action<string, string> onMessage, Func<string, bool> shouldShowInApp)
        {
            _onMessage = onMessage;
            _shouldShowInApp = shouldShowInApp;
            GrovsUnity_RegisterCallbacks(MessageCallbackDelegate, ShouldShowCallbackDelegate);
        }

        // Native may call this from any thread, so it only enqueues.
        [MonoPInvokeCallback(typeof(MessageCallback))]
        static void OnNativeMessage(IntPtr type, IntPtr json)
        {
            try
            {
                _onMessage?.Invoke(Marshal.PtrToStringUTF8(type), Marshal.PtrToStringUTF8(json));
            }
            catch (Exception e)
            {
                // An exception must not cross into native code.
                Debug.LogException(e);
            }
        }

        [MonoPInvokeCallback(typeof(ShouldShowCallback))]
        static bool OnNativeShouldShow(IntPtr campaignId)
        {
            try
            {
                var shouldShow = _shouldShowInApp;
                return shouldShow == null || shouldShow(Marshal.PtrToStringUTF8(campaignId));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return true;
            }
        }

        public void SetEnabled(bool enabled) => GrovsUnity_SetEnabled(enabled);
        public bool IsEnabled() => GrovsUnity_IsEnabled();
        public bool IsSceneTrackingEnabled() => GrovsUnity_IsSceneTrackingEnabled();
        public void SetLogLevel(int level) => GrovsUnity_SetLogLevel(level);

        public void SetUserIdentifier(string id) => GrovsUnity_SetUserIdentifier(id);
        public void SetUserAttributes(string json) => GrovsUnity_SetUserAttributes(json);
        public void SetPushToken(string token) => GrovsUnity_SetPushToken(token);

        public void GenerateLink(int requestId, string optionsJson) =>
            GrovsUnity_GenerateLink(requestId, optionsJson ?? "{}");

        public void LinkDetails(int requestId, string path) => GrovsUnity_LinkDetails(requestId, path);
        public bool HandleUrl(string url) => GrovsUnity_HandleUrl(url);

        public void Track(string name, string propsJson, string tagsJson) => GrovsUnity_Track(name, propsJson, tagsJson);
        public void TrackScreenView(string name, string propsJson) => GrovsUnity_TrackScreenView(name, propsJson);
        public void SetGlobalTags(string tagsJson) => GrovsUnity_SetGlobalTags(tagsJson);
        public void SetScreenAliases(string json) => GrovsUnity_SetScreenAliases(json ?? "{}");

        public void LogInAppPurchase(int requestId, string storeTransaction) =>
            GrovsUnity_LogInAppPurchase(requestId, storeTransaction);

        public void LogCustomPurchase(int requestId, int type, int cents, string currency, string productId, long epochMs) =>
            GrovsUnity_LogCustomPurchase(requestId, type, cents, currency, productId, epochMs);

        public bool IsGrovsNotification(string json) => GrovsUnity_IsGrovsNotification(json);
        public bool HandleNotificationOpen(string json) => GrovsUnity_HandleNotificationOpen(json);

        public void PauseInApp() => GrovsUnity_PauseInApp();
        public void ResumeInApp() => GrovsUnity_ResumeInApp();
        public void DiscardInApp(bool dismissCurrent) => GrovsUnity_DiscardInApp(dismissCurrent);
        public void SetInAppExcludedScreens(string json) => GrovsUnity_SetInAppExcludedScreens(json ?? "[]");
        public void SetInAppDebugLogging(bool enabled) => GrovsUnity_SetInAppDebugLogging(enabled);

        // Swift Bool is one byte, so every bool crosses as U1. Strings cross as UTF-8 and may be null.

        [DllImport(Lib)]
        static extern void GrovsUnity_RegisterCallbacks(MessageCallback onMessage, ShouldShowCallback shouldShow);

        [DllImport(Lib)]
        static extern void GrovsUnity_SetEnabled([MarshalAs(UnmanagedType.U1)] bool enabled);

        [DllImport(Lib)]
        [return: MarshalAs(UnmanagedType.U1)]
        static extern bool GrovsUnity_IsEnabled();

        [DllImport(Lib)]
        [return: MarshalAs(UnmanagedType.U1)]
        static extern bool GrovsUnity_IsSceneTrackingEnabled();

        [DllImport(Lib)]
        static extern void GrovsUnity_SetLogLevel(int level);

        [DllImport(Lib)]
        static extern void GrovsUnity_SetUserIdentifier(string identifier);

        [DllImport(Lib)]
        static extern void GrovsUnity_SetUserAttributes(string json);

        [DllImport(Lib)]
        static extern void GrovsUnity_SetPushToken(string token);

        [DllImport(Lib)]
        static extern void GrovsUnity_GenerateLink(int requestId, string optionsJson);

        [DllImport(Lib)]
        static extern void GrovsUnity_LinkDetails(int requestId, string path);

        [DllImport(Lib)]
        [return: MarshalAs(UnmanagedType.U1)]
        static extern bool GrovsUnity_HandleUrl(string url);

        [DllImport(Lib)]
        static extern void GrovsUnity_Track(string name, string propertiesJson, string tagsJson);

        [DllImport(Lib)]
        static extern void GrovsUnity_TrackScreenView(string name, string propertiesJson);

        [DllImport(Lib)]
        static extern void GrovsUnity_SetGlobalTags(string json);

        [DllImport(Lib)]
        static extern void GrovsUnity_SetScreenAliases(string json);

        [DllImport(Lib)]
        static extern void GrovsUnity_LogInAppPurchase(int requestId, string transaction);

        [DllImport(Lib)]
        static extern void GrovsUnity_LogCustomPurchase(int requestId, int type, int priceInCents, string currency,
            string productId, long epochMs);

        [DllImport(Lib)]
        [return: MarshalAs(UnmanagedType.U1)]
        static extern bool GrovsUnity_IsGrovsNotification(string json);

        [DllImport(Lib)]
        [return: MarshalAs(UnmanagedType.U1)]
        static extern bool GrovsUnity_HandleNotificationOpen(string json);

        [DllImport(Lib)]
        static extern void GrovsUnity_PauseInApp();

        [DllImport(Lib)]
        static extern void GrovsUnity_ResumeInApp();

        [DllImport(Lib)]
        static extern void GrovsUnity_DiscardInApp([MarshalAs(UnmanagedType.U1)] bool dismissCurrent);

        [DllImport(Lib)]
        static extern void GrovsUnity_SetInAppExcludedScreens(string json);

        [DllImport(Lib)]
        static extern void GrovsUnity_SetInAppDebugLogging([MarshalAs(UnmanagedType.U1)] bool enabled);
    }
}
#endif
