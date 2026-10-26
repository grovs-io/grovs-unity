using System;
using UnityEngine;

namespace GrovsTestApp
{
    public class EditorNativeServices : INativeServices
    {
#if UNITY_EDITOR
        const string SettingsAssetPath = "Assets/Grovs/GrovsSettings.asset";
        const int ProductionEnvironment = 1;
        static readonly string[] LogLevelNames = { "info", "warn", "error" };
#endif

        public string Platform => "Editor";

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

        /// <summary>
        /// The Editor has no native build config, so this reads Grovs settings (Project Settings) the way a
        /// development build would bake them.
        /// </summary>
        public BakedConfig ReadBakedConfig()
        {
#if UNITY_EDITOR
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObject>(SettingsAssetPath);
            if (asset != null)
            {
                using (var settings = new UnityEditor.SerializedObject(asset))
                    return FromSettings(settings);
            }
#endif
            return new BakedConfig("", false, null, Array.Empty<string>(), false, true, null);
        }

#if UNITY_EDITOR
        static BakedConfig FromSettings(UnityEditor.SerializedObject settings)
        {
            string Text(string name) => settings.FindProperty(name)?.stringValue;
            bool Flag(string name, bool fallback) => settings.FindProperty(name)?.boolValue ?? fallback;
            int Number(string name, int fallback) => settings.FindProperty(name)?.intValue ?? fallback;

            var domains = settings.FindProperty("clipboardDomains");
            var clipboardDomains = new string[domains?.arraySize ?? 0];
            for (var i = 0; i < clipboardDomains.Length; i++)
                clipboardDomains[i] = domains.GetArrayElementAtIndex(i).stringValue;

            var logLevel = Number("logLevel", LogLevelNames.Length - 1);
            return new BakedConfig(
                BakedConfig.Text(Text("apiKey")),
                Number("environment", 0) != ProductionEnvironment,
                BakedConfig.Text(Text("baseUrl")),
                BakedConfig.Domains(string.Join(",", clipboardDomains)),
                Flag("trackScenes", false),
                Flag("startEnabled", true),
                logLevel >= 0 && logLevel < LogLevelNames.Length ? LogLevelNames[logLevel] : null);
        }
#endif

        public void GetPushToken(Action<string> done) => done?.Invoke(null);

        public string ShareCommand(string link) => ShareCommands.Android(link, Application.identifier);
    }
}
