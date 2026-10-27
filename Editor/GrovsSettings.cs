using UnityEditor;
using UnityEngine;

namespace GrovsSDK.Editor
{
    public enum GrovsEnvironment
    {
        TestForDevelopmentBuilds,
        Production,
        Test
    }

    public enum GrovsAndroidPushMode
    {
        Auto,
        FirebaseUnity,
        Standalone
    }

    /// <summary>Build-time Grovs configuration. Read by the build post-processes only, never at runtime.</summary>
    public class GrovsSettings : ScriptableObject
    {
        public const string AssetPath = "Assets/Grovs/GrovsSettings.asset";

        [Tooltip("API key from the Grovs dashboard.")]
        public string apiKey = "";

        [Tooltip("Test environment for development builds and Production otherwise, or a fixed choice.")]
        public GrovsEnvironment environment = GrovsEnvironment.TestForDevelopmentBuilds;

        [Tooltip("Optional custom API base URL. Leave empty for the default.")]
        public string baseUrl = "";

        [Tooltip("Start the SDK enabled. A consent choice saved on the device wins over this.")]
        public bool startEnabled = true;

        [Tooltip("Domains whose links are read from the clipboard on first open.")]
        public string[] clipboardDomains = new string[0];

        [Tooltip("URI scheme from the Grovs dashboard. Required: iOS authentication fails without it.")]
        public string uriScheme = "";

        [Tooltip("Link hosts, for example x.sqd.link and x.test-sqd.link, plus any custom domain.")]
        public string[] linkDomains = new string[0];

        [Tooltip("Enable Grovs push notifications.")]
        public bool pushEnabled;

        [Tooltip("Auto uses Firebase Unity when it is in the project, else Standalone.")]
        public GrovsAndroidPushMode androidPushMode = GrovsAndroidPushMode.Auto;

        [Tooltip("iOS: Grovs installs a forwarding notification-center delegate to see push taps.")]
        public bool iosOwnNotificationDelegate = true;

        [Tooltip("Android: drawable resource name for the small notification icon.")]
        public string androidSmallIcon = "";

        [Tooltip("Android Standalone push: path to google-services.json.")]
        public string googleServicesJsonPath = "";

        [Tooltip("Report scene loads as screen views.")]
        public bool trackScenes;

        [Tooltip("Native SDK log level.")]
        public GrovsLogLevel logLevel = GrovsLogLevel.Error;

        public static GrovsSettings LoadOrCreate()
        {
            var settings = AssetDatabase.LoadAssetAtPath<GrovsSettings>(AssetPath);
            if (settings != null) return settings;

            if (!AssetDatabase.IsValidFolder("Assets/Grovs")) AssetDatabase.CreateFolder("Assets", "Grovs");
            settings = CreateInstance<GrovsSettings>();
            AssetDatabase.CreateAsset(settings, AssetPath);
            AssetDatabase.SaveAssets();
            return settings;
        }
    }

    static class GrovsSettingsProvider
    {
        [SettingsProvider]
        static SettingsProvider Create()
        {
            SerializedObject serialized = null;
            return new SettingsProvider("Project/Grovs", SettingsScope.Project)
            {
                label = "Grovs",
                activateHandler = (_, __) => serialized = new SerializedObject(GrovsSettings.LoadOrCreate()),
                guiHandler = _ =>
                {
                    if (serialized == null || serialized.targetObject == null)
                        serialized = new SerializedObject(GrovsSettings.LoadOrCreate());
                    serialized.Update();
                    var labelWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = 260;
                    var property = serialized.GetIterator();
                    property.NextVisible(true);
                    while (property.NextVisible(false))
                        EditorGUILayout.PropertyField(property, true);
                    EditorGUIUtility.labelWidth = labelWidth;
                    if (serialized.ApplyModifiedProperties())
                        AssetDatabase.SaveAssetIfDirty(serialized.targetObject);
                },
                keywords = new[] { "Grovs", "deep link", "API key", "URI scheme", "push" }
            };
        }
    }
}
