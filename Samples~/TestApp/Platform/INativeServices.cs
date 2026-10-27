using System;
using UnityEngine;

namespace GrovsTestApp
{
    public enum NotificationStatus
    {
        Allowed,
        NotAllowed,
        Unknown
    }

    public interface INativeServices
    {
        string Platform { get; }
        bool IsDarkMode();
        void Share(string text);
        NotificationStatus GetNotificationStatus();
        void RequestNotifications(Action<NotificationStatus> done);
        void OpenAppSettings();
        BakedConfig ReadBakedConfig();
        void GetPushToken(Action<string> done);
        string ShareCommand(string link);
    }

    /// <summary>Runs callbacks that arrive on native threads back on the Unity main thread.</summary>
    static class MainThread
    {
        public static Action<T> Wrap<T>(Action<T> action)
        {
            if (action == null) return null;
            var context = System.Threading.SynchronizationContext.Current;
            if (context == null) return action;
            return value => context.Post(_ => action(value), null);
        }
    }
}
