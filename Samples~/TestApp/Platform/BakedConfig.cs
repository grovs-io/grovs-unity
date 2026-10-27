using System;
using System.Collections.Generic;

namespace GrovsTestApp
{
    /// <summary>The Grovs config baked into the build (Android meta-data or iOS Info.plist).</summary>
    public class BakedConfig
    {
        public string ApiKey { get; }
        public bool UseTestEnvironment { get; }
        public string BaseUrl { get; }
        public IReadOnlyList<string> ClipboardDomains { get; }
        public bool TrackScenes { get; }
        public bool StartEnabled { get; }
        public string LogLevel { get; }

        public BakedConfig(string apiKey, bool useTestEnvironment, string baseUrl, IReadOnlyList<string> clipboardDomains,
            bool trackScenes, bool startEnabled, string logLevel)
        {
            ApiKey = apiKey ?? "";
            UseTestEnvironment = useTestEnvironment;
            BaseUrl = baseUrl;
            ClipboardDomains = clipboardDomains ?? Array.Empty<string>();
            TrackScenes = trackScenes;
            StartEnabled = startEnabled;
            LogLevel = logLevel;
        }

        /// <summary>Builds a config from raw text values as the native layers store them.</summary>
        public static BakedConfig FromRaw(Func<string, string> read)
        {
            return new BakedConfig(
                Text(read("apiKey")) ?? "",
                Flag(read("useTestEnvironment"), false),
                Text(read("baseUrl")),
                Domains(read("clipboardDomains")),
                Flag(read("trackScenes"), false),
                Flag(read("startEnabled"), true),
                Text(read("logLevel")));
        }

        public static string Text(string value)
        {
            var trimmed = value?.Trim();
            return string.IsNullOrEmpty(trimmed) ? null : trimmed;
        }

        public static bool Flag(string value, bool fallback)
        {
            switch (Text(value)?.ToLowerInvariant())
            {
                case "true":
                case "yes":
                case "1":
                    return true;
                case "false":
                case "no":
                case "0":
                    return false;
                default:
                    return fallback;
            }
        }

        public static IReadOnlyList<string> Domains(string value)
        {
            var result = new List<string>();
            if (value == null) return result;
            foreach (var part in value.Split(','))
            {
                var trimmed = part.Trim();
                if (trimmed.Length > 0) result.Add(trimmed);
            }
            return result;
        }
    }
}
