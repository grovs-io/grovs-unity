using System;
using UnityEngine;

namespace GrovsTestApp
{
    public class EditorNativeServices : INativeServices
    {
        public string Platform => "Editor";
        public bool CanRestart => false;

        public bool IsDarkMode()
        {
#if UNITY_EDITOR
            return UnityEditor.EditorGUIUtility.isProSkin;
#else
            return false;
#endif
        }

        public void Share(string text)
        {
            GUIUtility.systemCopyBuffer = text;
        }

        public NotificationStatus GetNotificationStatus() => NotificationStatus.Unknown;

        public void RequestNotifications(Action<NotificationStatus> done) => done?.Invoke(NotificationStatus.Unknown);

        public void OpenAppSettings() { }

        public void Restart() { }

        public BakedConfig ReadBakedConfig() => ReadBakedConfigStatic();

        public static BakedConfig ReadBakedConfigStatic()
        {
            return new BakedConfig("", false, null, Array.Empty<string>(), false, true, null);
        }

        public void GetPushToken(Action<string> done) => done?.Invoke(null);

        public string ShareCommand(string link) => ShareCommands.Android(link, Application.identifier);
    }
}
