using System;
using System.Collections.Generic;
using GrovsSDK;

namespace GrovsTestApp
{
    public enum CallStyle { Async, Callback }

    [Serializable]
    public sealed class RedirectForm
    {
        public string Url = "";
        public bool OpenAppIfInstalled;
    }

    [Serializable]
    public sealed class LinkForm
    {
        public string Title = "Test title";
        public string Subtitle = "Test subtitle";
        public List<KeyValueRow> Data = new List<KeyValueRow> { new KeyValueRow { Key = "param1", Value = "Test value" } };
        public string Image = "";
        public List<string> Tags = new List<string>();
        public bool Utm;
        public string UtmSource = "test_source";
        public string UtmMedium = "test_medium";
        public string UtmCampaign = "test_campaign";
        public bool CustomRedirects;
        public RedirectForm Android = new RedirectForm { Url = "https://www.youtube.com" };
        public RedirectForm Ios = new RedirectForm { Url = "https://www.google.com" };
        public RedirectForm Desktop = new RedirectForm();

        // JsonUtility cannot store bool?, so each tri-state is an int: -1 default, 0 off, 1 on.
        public int PreviewAndroidState = -1;
        public int PreviewIosState = -1;
        public int ClipboardAndroidState = -1;
        public int ClipboardIosState = -1;

        public bool? PreviewAndroid { get => ToBool(PreviewAndroidState); set => PreviewAndroidState = FromBool(value); }
        public bool? PreviewIos { get => ToBool(PreviewIosState); set => PreviewIosState = FromBool(value); }
        public bool? ClipboardAndroid { get => ToBool(ClipboardAndroidState); set => ClipboardAndroidState = FromBool(value); }
        public bool? ClipboardIos { get => ToBool(ClipboardIosState); set => ClipboardIosState = FromBool(value); }

        public string Summary() => string.Join(" · ", new[]
        {
            Utm ? "UTM on" : "UTM off",
            Tags.Count == 0 ? "no tags" : Tags.Count + " tags",
            string.IsNullOrWhiteSpace(Image) ? "no image" : "image",
            CustomRedirects ? "redirects on" : "no redirects"
        });

        static bool? ToBool(int state) => state < 0 ? (bool?)null : state > 0;
        static int FromBool(bool? value) => value.HasValue ? (value.Value ? 1 : 0) : -1;
    }

    [Serializable]
    public sealed class EventForm
    {
        public string Name = "demo_tapped";
        public List<KeyValueRow> Properties = new List<KeyValueRow>();
        public List<string> Tags = new List<string>();
        public List<string> Recent = new List<string>();
    }

    [Serializable]
    public sealed class ScreenForm
    {
        public string Name = "Checkout";
        public List<KeyValueRow> Properties = new List<KeyValueRow>();
        public List<string> Recent = new List<string>();
    }

    [Serializable]
    public sealed class PurchaseForm
    {
        public GrovsTransactionType Type = GrovsTransactionType.Buy;
        public string Amount = "4.99";
        public string Currency = "USD";
        public string Product = "pro_monthly";
        public int DaysAgo;
        public string StoreReceipt = "";

        public string ShortLabel()
        {
            var cents = Money.ParseCents(Amount);
            if (cents == null) return "Log purchase";
            return "Log " + Money.Format(cents.Value, Currency) + " " + Type.ToString().ToLowerInvariant();
        }

        public string Label() => ShortLabel() + " · " + Currency + " · " + Product;
    }

    [Serializable]
    public sealed class IdentityForm
    {
        public string Identifier = DeviceInfo.Name;
        public List<KeyValueRow> Attributes = new List<KeyValueRow>
        {
            new KeyValueRow { Key = "param1", Value = "value1", Kind = ValueKind.Text },
            new KeyValueRow { Key = "param2", Value = "123", Kind = ValueKind.Number },
            new KeyValueRow { Key = "param3", Value = "true", Kind = ValueKind.Bool }
        };
    }

    [Serializable]
    public sealed class SetupForm
    {
        public List<string> GlobalTags = new List<string>();
        public List<KeyValueRow> Aliases = new List<KeyValueRow> { new KeyValueRow { Key = "TestApp", Value = "Home" } };
    }

    [Serializable]
    public sealed class InAppPrefs
    {
        public string Trigger = "demo_tapped";
        public List<string> RecentTriggers = new List<string>();
        public bool DebugLogging = true;
        public List<string> Excluded = new List<string> { "ExcludedScreen" };
    }

    [Serializable]
    public sealed class DeveloperPrefs
    {
        public GrovsLogLevel LogLevel = GrovsLogLevel.Info;
        public CallStyle CallStyle = CallStyle.Async;
    }

    public static class Recents
    {
        public const int Max = 5;

        public static void Remember(List<string> recent, string name)
        {
            recent.Remove(name);
            recent.Insert(0, name);
            if (recent.Count > Max) recent.RemoveRange(Max, recent.Count - Max);
        }
    }
}
