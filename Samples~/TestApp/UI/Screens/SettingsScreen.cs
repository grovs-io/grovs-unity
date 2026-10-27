using System.Collections.Generic;
using System.Threading.Tasks;
using GrovsSDK;
using UnityEngine;
using UnityEngine.UIElements;

namespace GrovsTestApp
{
    /// <summary>What the Settings rows show, kept apart from the views so it can be tested.</summary>
    public static class SettingsRules
    {
        public const int KeyEllipsisAbove = 24;
        public const int KeyEllipsisKeep = 8;
        public const string NotSet = "Not set";

        public static readonly GrovsLogLevel[] DebugLevels = { GrovsLogLevel.Info, GrovsLogLevel.Error };

        public static string ApiKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return NotSet;
            return key.Length > KeyEllipsisAbove ? TextFormat.MiddleEllipsis(key, KeyEllipsisKeep) : key;
        }

        public static string Environment(BakedConfig config) => HomeRules.Environment(config);

        public static string BaseUrl(BakedConfig config) => config.BaseUrl ?? "SDK default";

        public static string ClipboardDomains(BakedConfig config) =>
            config.ClipboardDomains.Count == 0 ? "none" : string.Join(", ", config.ClipboardDomains);

        public static string OnOff(bool on) => on ? "On" : "Off";

        /// <summary>The control has no Warn option, so a saved Warn shows as Error.</summary>
        public static GrovsLogLevel DisplayedLevel(GrovsLogLevel saved) =>
            saved == GrovsLogLevel.Info ? GrovsLogLevel.Info : GrovsLogLevel.Error;

        public static string DebugLevelTitle(GrovsLogLevel level) => "Debug level " + level.ToString().ToUpperInvariant();
    }

    /// <summary>Settings, opened from Home: the project config baked into the build, and the developer options.</summary>
    public class SettingsScreen : VisualElement
    {
        public const string ProjectHelp =
            "These come from Grovs settings at build time. To test another project, change the keys in Grovs settings (Project Settings) and rebuild.";

        readonly AppContext _ctx;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => AppContext.RegisterSettings(ctx => new SettingsScreen(ctx));

        public SettingsScreen(AppContext ctx)
        {
            _ctx = ctx;
            AddToClassList("settings");
            var scroll = AppScaffold.NewScreen();
            scroll.Add(AppScaffold.SettingsHeader(() => ctx.Scaffold?.CloseSettings()));
            BuildProject(scroll, ctx.Native.ReadBakedConfig());
            BuildDeveloper(scroll);
            Add(scroll);
        }

        static void BuildProject(VisualElement body, BakedConfig config)
        {
            body.Add(new SectionHeader("Project"));
            body.Add(ApiKeyRow(config.ApiKey));
            body.Add(ValueRow("Environment", SettingsRules.Environment(config), Help.Get("environment")));
            body.Add(ValueRow("Base URL", SettingsRules.BaseUrl(config), Help.Get("baseUrl")));
            body.Add(ValueRow("Clipboard domains", SettingsRules.ClipboardDomains(config), Help.Get("clipboardDomains")));
            body.Add(ValueRow("Auto track screens", SettingsRules.OnOff(config.TrackScenes), Help.Get("autoTrack")));
            body.Add(ValueRow("Start with SDK enabled", SettingsRules.OnOff(config.StartEnabled), Help.Get("startEnabled")));
            body.Add(new HelpText(ProjectHelp));
        }

        static SettingRow ValueRow(string label, string value, (string title, string body)? help)
        {
            var row = new SettingRow(label, help: help);
            row.Trailing.AddToClassList("settings__trailing");
            row.Trailing.Add(Ui.Text(value, "t-body", "settings__value"));
            return row;
        }

        static SettingRow ApiKeyRow(string key)
        {
            var row = new SettingRow("API key", help: Help.Get("apiKey"));
            var set = !string.IsNullOrEmpty(key);
            var value = Ui.Text(SettingsRules.ApiKey(key), "t-mono", "settings__value");
            value.EnableInClassList("settings__value--error", !set);
            row.Trailing.Add(value);
            if (!set) return row;

            row.Trailing.Add(Ui.Text("Copy", "t-label", "copy-row__action"));
            row.MakeTappable(() =>
            {
                GUIUtility.systemCopyBuffer = key;
                Toast.Show(Overlay.HostFor(row), "Copied");
            });
            return row;
        }

        void BuildDeveloper(VisualElement body)
        {
            body.Add(new SectionHeader("Developer"));
            var prefs = _ctx.Developer;

            var level = new SettingRow("Debug level", help: Help.Get("debugLevel"));
            var levelSegment = new Segment<GrovsLogLevel>(SettingsRules.DebugLevels, SettingsRules.DisplayedLevel(prefs.LogLevel),
                SetDebugLevel, compact: true);
            levelSegment.AddToClassList("settings__segment--level");
            level.Trailing.Add(levelSegment);
            body.Add(level);

            var style = new SettingRow("Call style", divider: false, help: Help.Get("callStyle"));
            var styleSegment = new Segment<CallStyle>(new[] { CallStyle.Async, CallStyle.Callback }, prefs.CallStyle,
                SetCallStyle, compact: true);
            styleSegment.AddToClassList("settings__segment--style");
            style.Trailing.Add(styleSegment);
            body.Add(style);
        }

        void SetDebugLevel(GrovsLogLevel level)
        {
            _ctx.Developer.LogLevel = level;
            _ctx.Store.Save(FormKeys.Developer, _ctx.Developer);
            _ = _ctx.Runner.Run(new TestAction
            {
                Type = LogType.Sdk,
                Title = SettingsRules.DebugLevelTitle(level),
                Code = CallCode.Format("SetLogLevel", ("level", level)),
                Repeatable = false,
                Run = () =>
                {
                    _ctx.Api.SetLogLevel(level);
                    return Task.FromResult(new ActionResult { Detail = "applied" });
                }
            });
        }

        void SetCallStyle(CallStyle style)
        {
            _ctx.Developer.CallStyle = style;
            _ctx.Store.Save(FormKeys.Developer, _ctx.Developer);
        }
    }
}
