using System.Collections.Generic;
using System.Linq;

namespace GrovsSDK.Editor
{
    /// <summary>Settings resolved for one build, with values trimmed and the environment decided.</summary>
    internal sealed class GrovsBuildConfig
    {
        public string ApiKey { get; private set; }
        public bool UseTestEnvironment { get; private set; }
        public string BaseUrl { get; private set; }
        public bool StartEnabled { get; private set; }
        public List<string> ClipboardDomains { get; private set; }
        public string UriScheme { get; private set; }
        public List<string> LinkDomains { get; private set; }
        public bool PushEnabled { get; private set; }
        public GrovsAndroidPushMode AndroidPushMode { get; private set; }
        public bool IosOwnNotificationDelegate { get; private set; }
        public string AndroidSmallIcon { get; private set; }
        public string GoogleServicesJsonPath { get; private set; }
        public bool TrackScenes { get; private set; }
        public string LogLevelName { get; private set; }

        public static GrovsBuildConfig Resolve(GrovsSettings s, bool developmentBuild)
        {
            return new GrovsBuildConfig
            {
                ApiKey = Clean(s.apiKey),
                UseTestEnvironment = s.environment == GrovsEnvironment.Test ||
                                     (s.environment == GrovsEnvironment.TestForDevelopmentBuilds && developmentBuild),
                BaseUrl = Clean(s.baseUrl),
                StartEnabled = s.startEnabled,
                ClipboardDomains = CleanList(s.clipboardDomains),
                UriScheme = Clean(s.uriScheme),
                LinkDomains = CleanList(s.linkDomains),
                PushEnabled = s.pushEnabled,
                AndroidPushMode = s.androidPushMode,
                IosOwnNotificationDelegate = s.iosOwnNotificationDelegate,
                AndroidSmallIcon = Clean(s.androidSmallIcon),
                GoogleServicesJsonPath = Clean(s.googleServicesJsonPath),
                TrackScenes = s.trackScenes,
                LogLevelName = LogLevelNameOf(s.logLevel)
            };
        }

        public List<string> Validate()
        {
            var errors = new List<string>();
            if (UriScheme.Length == 0) errors.Add("Grovs: URI scheme is empty. iOS authentication fails without it.");
            return errors;
        }

        public List<string> Warnings()
        {
            var warnings = new List<string>();
            if (ApiKey.Length == 0) warnings.Add("Grovs: API key is empty (Project Settings > Grovs).");
            return warnings;
        }

        static string LogLevelNameOf(GrovsLogLevel level)
        {
            switch (level)
            {
                case GrovsLogLevel.Info: return "info";
                case GrovsLogLevel.Warn: return "warn";
                default: return "error";
            }
        }

        static string Clean(string value) => (value ?? "").Trim();

        static List<string> CleanList(IEnumerable<string> values) =>
            (values ?? Enumerable.Empty<string>()).Select(Clean).Where(v => v.Length > 0).Distinct().ToList();
    }
}
