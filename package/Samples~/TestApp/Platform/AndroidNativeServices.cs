using System;
using UnityEngine;
using UnityEngine.Android;

namespace GrovsTestApp
{
    public class AndroidNativeServices : INativeServices
    {
        const string PostNotifications = "android.permission.POST_NOTIFICATIONS";
        const int NotificationPermissionApi = 33;

        public string Platform => "Android";

        static AndroidJavaObject Activity
        {
            get
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    return player.GetStatic<AndroidJavaObject>("currentActivity");
            }
        }

        static int SdkInt
        {
            get
            {
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                    return version.GetStatic<int>("SDK_INT");
            }
        }

        public bool IsDarkMode()
        {
            using (var activity = Activity)
            using (var resources = activity.Call<AndroidJavaObject>("getResources"))
            using (var configuration = resources.Call<AndroidJavaObject>("getConfiguration"))
                return (configuration.Get<int>("uiMode") & 0x30) == 0x20;
        }

        public void Share(string text)
        {
            using (var activity = Activity)
            using (var intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.SEND"))
            using (var chooserClass = new AndroidJavaClass("android.content.Intent"))
            {
                intent.Call<AndroidJavaObject>("setType", "text/plain");
                intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.TEXT", text);
                using (var chooser = chooserClass.CallStatic<AndroidJavaObject>("createChooser", intent, "Share"))
                    activity.Call("startActivity", chooser);
            }
        }

        public NotificationStatus GetNotificationStatus()
        {
            if (SdkInt >= NotificationPermissionApi)
                return Permission.HasUserAuthorizedPermission(PostNotifications)
                    ? NotificationStatus.Allowed
                    : NotificationStatus.NotAllowed;
            return AreNotificationsEnabled() ? NotificationStatus.Allowed : NotificationStatus.NotAllowed;
        }

        public void RequestNotifications(Action<NotificationStatus> done)
        {
            var callback = MainThread.Wrap(done);
            if (SdkInt < NotificationPermissionApi)
            {
                OpenAppSettings();
                callback?.Invoke(GetNotificationStatus());
                return;
            }

            var finished = false;
            Action<NotificationStatus> finish = status =>
            {
                if (finished) return;
                finished = true;
                callback?.Invoke(status);
            };
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += _ => finish(NotificationStatus.Allowed);
            callbacks.PermissionDenied += _ => finish(NotificationStatus.NotAllowed);
            callbacks.PermissionDeniedAndDontAskAgain += _ => finish(NotificationStatus.NotAllowed);
            Permission.RequestUserPermission(PostNotifications, callbacks);
        }

        public void OpenAppSettings()
        {
            using (var activity = Activity)
            using (var uriClass = new AndroidJavaClass("android.net.Uri"))
            using (var uri = uriClass.CallStatic<AndroidJavaObject>("parse", "package:" + Application.identifier))
            using (var intent = new AndroidJavaObject("android.content.Intent", "android.settings.APPLICATION_DETAILS_SETTINGS", uri))
            {
                intent.Call<AndroidJavaObject>("addFlags", 0x10000000);
                activity.Call("startActivity", intent);
            }
        }

        public BakedConfig ReadBakedConfig()
        {
            using (var activity = Activity)
            using (var manager = activity.Call<AndroidJavaObject>("getPackageManager"))
            using (var info = manager.Call<AndroidJavaObject>("getApplicationInfo", Application.identifier, 128))
            using (var bundle = info.Get<AndroidJavaObject>("metaData"))
            {
                return BakedConfig.FromRaw(name => ReadMetaData(bundle, name switch
                {
                    "apiKey" => "grovs_api_key",
                    "useTestEnvironment" => "grovs_use_test_environment",
                    "baseUrl" => "grovs_base_url",
                    "clipboardDomains" => "grovs_clipboard_domains",
                    "trackScenes" => "grovs_unity_track_scenes",
                    "startEnabled" => "grovs_enabled",
                    _ => "grovs_log_level"
                }));
            }
        }

        public void GetPushToken(Action<string> done)
        {
            var callback = MainThread.Wrap(done);
            try
            {
                using (var messaging = new AndroidJavaClass("com.google.firebase.messaging.FirebaseMessaging"))
                using (var instance = messaging.CallStatic<AndroidJavaObject>("getInstance"))
                using (var task = instance.Call<AndroidJavaObject>("getToken"))
                    task.Call<AndroidJavaObject>("addOnCompleteListener", new TokenListener(callback));
            }
            catch (Exception)
            {
                callback?.Invoke(null);
            }
        }

        public string ShareCommand(string link) => ShareCommands.Android(link, Application.identifier);

        static string ReadMetaData(AndroidJavaObject bundle, string key)
        {
            if (bundle == null) return null;
            using (var value = bundle.Call<AndroidJavaObject>("get", key))
                return value?.Call<string>("toString");
        }

        static bool AreNotificationsEnabled()
        {
            using (var activity = Activity)
            {
                try
                {
                    using (var compat = new AndroidJavaClass("androidx.core.app.NotificationManagerCompat"))
                    using (var manager = compat.CallStatic<AndroidJavaObject>("from", activity))
                        return manager.Call<bool>("areNotificationsEnabled");
                }
                catch (AndroidJavaException)
                {
                    if (SdkInt < 24) return true;
                    using (var manager = activity.Call<AndroidJavaObject>("getSystemService", "notification"))
                        return manager.Call<bool>("areNotificationsEnabled");
                }
            }
        }

        class TokenListener : AndroidJavaProxy
        {
            readonly Action<string> _done;

            public TokenListener(Action<string> done) : base("com.google.android.gms.tasks.OnCompleteListener")
            {
                _done = done;
            }

            public void onComplete(AndroidJavaObject task)
            {
                string token = null;
                try
                {
                    if (task.Call<bool>("isSuccessful"))
                    {
                        using (var result = task.Call<AndroidJavaObject>("getResult"))
                            token = result?.Call<string>("toString");
                    }
                }
                catch (Exception)
                {
                }
                _done?.Invoke(token);
            }
        }
    }
}
