using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace GrovsTestApp
{
    public static class LinkPath
    {
        /// <summary>Takes a full link, host/path or a bare path and returns the last path segment.</summary>
        public static string Extract(string input)
        {
            var clean = (input ?? "").Trim();
            clean = Before(clean, '#');
            clean = Before(clean, '?');
            if (clean.Length == 0) return null;

            var schemeEnd = clean.IndexOf("://", StringComparison.Ordinal);
            var afterScheme = schemeEnd >= 0 ? clean.Substring(schemeEnd + 3) : clean;
            var hasHost = schemeEnd >= 0 || afterScheme.Contains("/");
            var path = afterScheme;
            if (hasHost)
            {
                var slash = afterScheme.IndexOf('/');
                path = slash >= 0 ? afterScheme.Substring(slash + 1) : "";
            }

            var segments = path.Split('/');
            for (var i = segments.Length - 1; i >= 0; i--)
            {
                if (!string.IsNullOrWhiteSpace(segments[i])) return segments[i];
            }
            return null;
        }

        static string Before(string s, char c)
        {
            var i = s.IndexOf(c);
            return i >= 0 ? s.Substring(0, i) : s;
        }
    }

    public static class Money
    {
        static readonly Regex AmountPattern = new Regex(@"^[0-9]+([.,][0-9]{1,2})?$");

        public static int? ParseCents(string input)
        {
            var v = (input ?? "").Trim();
            if (!AmountPattern.IsMatch(v)) return null;

            var parts = v.Replace(',', '.').Split('.');
            var fraction = parts.Length > 1 ? parts[1].PadRight(2, '0') : "00";
            if (!long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var whole)) return null;

            if (whole > int.MaxValue / 100) return null;

            var cents = whole * 100 + int.Parse(fraction, CultureInfo.InvariantCulture);
            return cents > int.MaxValue ? (int?)null : (int)cents;
        }

        public static string Format(int cents, string currency)
        {
            var amount = (cents / 100).ToString(CultureInfo.InvariantCulture) + "." +
                         (cents % 100).ToString("00", CultureInfo.InvariantCulture);
            switch (currency)
            {
                case "USD": return "$" + amount;
                case "EUR": return "€" + amount;
                case "GBP": return "£" + amount;
                default: return amount + " " + currency;
            }
        }
    }

    public static class EventRules
    {
        public const int MaxTags = 20;
        public const int MaxTagLength = 255;

        static readonly HashSet<string> ReservedNames = new HashSet<string>
        {
            "app_open", "custom", "in_app_answered", "in_app_clicked", "in_app_commented", "in_app_dismissed",
            "in_app_held", "in_app_viewed", "install", "journey_entered", "journey_held", "journey_path", "open",
            "push_delivered", "push_held", "push_open", "reactivation", "reinstall", "screen_view", "time_spent",
            "user_referred", "view"
        };

        public static IReadOnlyCollection<string> Reserved => ReservedNames;

        public static string NameError(string name)
        {
            var trimmed = (name ?? "").Trim();
            if (trimmed.Length == 0) return "Enter an event name";
            if (ReservedNames.Contains(trimmed.ToLowerInvariant()))
                return "\"" + trimmed + "\" is reserved by the SDK";
            return null;
        }

        public static string TagsError(IList<string> tags)
        {
            if (tags == null) return null;
            if (tags.Count > MaxTags) return "Up to 20 tags";
            foreach (var tag in tags)
            {
                if (tag != null && tag.Length > MaxTagLength) return "Each tag can have up to 255 characters";
            }
            return null;
        }
    }

    public static class TextFormat
    {
        public static string MiddleEllipsis(string s, int keep = 5)
        {
            if (s == null) return "";
            if (s.Length <= 2 * keep + 1) return s;
            return s.Substring(0, keep) + "…" + s.Substring(s.Length - keep);
        }

        public static bool Url(string input)
        {
            return Uri.TryCreate((input ?? "").Trim(), UriKind.Absolute, out var uri)
                   && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                   && !string.IsNullOrEmpty(uri.Host);
        }
    }

    public static class RelativeTime
    {
        public static string Format(TimeSpan age)
        {
            if (age.TotalMinutes < 1) return "just now";
            if (age.TotalHours < 1) return (int)age.TotalMinutes + " min ago";
            if (age.TotalHours < 24) return (int)age.TotalHours + " h ago";
            if (age.TotalHours < 48) return "yesterday";
            return (int)age.TotalDays + " days ago";
        }
    }

    public static class DeviceInfo
    {
        public static string Name => "@" + SystemInfo.deviceModel;
    }

    public static class RandomId
    {
        static readonly System.Random Random = new System.Random();

        public static string Qa() => "qa_" + Random.Next(0, 1000000).ToString("000000", CultureInfo.InvariantCulture);
    }
}
