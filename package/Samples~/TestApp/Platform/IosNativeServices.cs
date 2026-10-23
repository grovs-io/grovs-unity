using System;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

namespace GrovsTestApp
{
    public class IosNativeServices : INativeServices
    {
        delegate void PermissionCallback(int status);

        static Action<NotificationStatus> _pendingRequest;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _pendingRequest = null;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        [return: MarshalAs(UnmanagedType.U1)]
        static extern bool GrovsTestApp_IsDarkMode();
        [DllImport("__Internal")] static extern void GrovsTestApp_Share(string text);
        [DllImport("__Internal")] static extern int GrovsTestApp_NotificationStatus();
        [DllImport("__Internal")] static extern void GrovsTestApp_RequestNotifications(PermissionCallback callback);
        [DllImport("__Internal")] static extern void GrovsTestApp_OpenSettings();
        [DllImport("__Internal")] static extern IntPtr GrovsTestApp_InfoValue(string key);
#else
        static bool GrovsTestApp_IsDarkMode() => false;
        static void GrovsTestApp_Share(string text) { }
        static int GrovsTestApp_NotificationStatus() => 2;
        static void GrovsTestApp_RequestNotifications(PermissionCallback callback) { }
        static void GrovsTestApp_OpenSettings() { }
        static IntPtr GrovsTestApp_InfoValue(string key) => IntPtr.Zero;
#endif

        public string Platform => "iOS";
        public bool CanRestart => false;

        public bool IsDarkMode() => GrovsTestApp_IsDarkMode();

        public void Share(string text) => GrovsTestApp_Share(text);

        public NotificationStatus GetNotificationStatus() => ToStatus(GrovsTestApp_NotificationStatus());

        public void RequestNotifications(Action<NotificationStatus> done)
        {
            _pendingRequest = MainThread.Wrap(done);
            GrovsTestApp_RequestNotifications(OnPermissionResult);
        }

        [MonoPInvokeCallback(typeof(PermissionCallback))]
        static void OnPermissionResult(int status)
        {
            var callback = _pendingRequest;
            _pendingRequest = null;
            callback?.Invoke(ToStatus(status));
        }

        public void OpenAppSettings() => GrovsTestApp_OpenSettings();

        public void Restart() => Application.Quit();

        public BakedConfig ReadBakedConfig()
        {
            return BakedConfig.FromRaw(name => ReadInfo(name switch
            {
                "apiKey" => "GrovsApiKey",
                "useTestEnvironment" => "GrovsUseTestEnvironment",
                "baseUrl" => "GrovsBaseURL",
                "clipboardDomains" => "GrovsClipboardDomains",
                "trackScenes" => "GrovsUnityTrackScenes",
                "startEnabled" => "GrovsEnabled",
                _ => "GrovsLogLevel"
            }));
        }

        public void GetPushToken(Action<string> done) => done?.Invoke(null);

        public string ShareCommand(string link) => ShareCommands.Ios(link);

        static string ReadInfo(string key)
        {
            var pointer = GrovsTestApp_InfoValue(key);
            if (pointer == IntPtr.Zero) return null;
            var text = Marshal.PtrToStringUTF8(pointer);
            Marshal.FreeHGlobal(pointer);
            return text;
        }

        static NotificationStatus ToStatus(int value)
        {
            switch (value)
            {
                case 0: return NotificationStatus.Allowed;
                case 1: return NotificationStatus.NotAllowed;
                default: return NotificationStatus.Unknown;
            }
        }
    }
}
