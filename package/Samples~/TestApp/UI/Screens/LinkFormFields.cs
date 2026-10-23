using System;
using System.Linq;
using UnityEngine.UIElements;

namespace GrovsTestApp
{
    /// <summary>The fields of a link form. Edits go straight into the form, then changed is called.</summary>
    public static class LinkFormFields
    {
        public const string SampleImage1 = "https://picsum.photos/seed/grovs1/1200/630";
        public const string SampleImage2 = "https://picsum.photos/seed/grovs2/1200/630";

        static readonly string[] SampleRedirects = { "https://www.youtube.com", "https://www.google.com", "https://grovs.io" };
        static readonly string[] SuggestedDataKeys = { "promo", "user_id", "screen" };

        public static VisualElement Build(LinkForm form, Action changed)
        {
            var root = Ui.Box("link-form");
            FoldRow more = null;
            Action edited = () =>
            {
                more?.RefreshSummary();
                changed?.Invoke();
            };

            var title = new SuggestField(new string[0], "Title");
            title.SetValueWithoutNotify(form.Title);
            title.Changed += v =>
            {
                form.Title = v;
                edited();
            };
            root.Add(title);

            var subtitle = new SuggestField(new string[0], "Subtitle");
            subtitle.AddToClassList("link-form__gap");
            subtitle.SetValueWithoutNotify(form.Subtitle);
            subtitle.Changed += v =>
            {
                form.Subtitle = v;
                edited();
            };
            root.Add(subtitle);

            var data = new KeyValueRows(form.Data, "Data", SuggestedDataKeys, help: Help.Get("linkData"));
            data.AddToClassList("link-form__gap");
            data.Changed += edited;
            root.Add(data);

            more = new FoldRow("More options", form.Summary, MoreOptions(form, edited));
            root.Add(more);
            return root;
        }

        static VisualElement MoreOptions(LinkForm form, Action edited)
        {
            var content = Ui.Box("link-form__more");

            var image = new ChipPicker(new[] { "", SampleImage1, SampleImage2 }, form.Image, v =>
                {
                    form.Image = v;
                    edited();
                }, "Image", "https://", "URL…", ImageName);
            content.Add(image);

            var tags = new ChipInput(form.Tags, "Tags", "Add tag", help: Help.Get("linkTags"));
            tags.AddToClassList("link-form__gap");
            tags.Changed += edited;
            content.Add(tags);

            content.Add(Utm(form, edited));
            content.Add(Redirects(form, edited));

            FoldRow overrides = null;
            overrides = new FoldRow("Platform overrides", () => OverridesSummary(form), Overrides(form, () =>
            {
                overrides?.RefreshSummary();
                edited();
            }), help: Help.Get("platformOverrides"));
            content.Add(overrides);
            return content;
        }

        static VisualElement Utm(LinkForm form, Action edited)
        {
            var group = Ui.Box();
            var fields = Ui.Box("utm-row");
            fields.Add(UtmField("source", form.UtmSource, v => form.UtmSource = v, edited));
            fields.Add(UtmField("medium", form.UtmMedium, v => form.UtmMedium = v, edited));
            fields.Add(UtmField("campaign", form.UtmCampaign, v => form.UtmCampaign = v, edited));
            fields.style.display = form.Utm ? DisplayStyle.Flex : DisplayStyle.None;

            group.Add(new SwitchRow("UTM tracking", form.Utm, on =>
            {
                form.Utm = on;
                fields.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
                edited();
            }, help: Help.Get("utm")));
            group.Add(fields);
            return group;
        }

        static AppTextField UtmField(string placeholder, string value, Action<string> set, Action edited)
        {
            var field = new AppTextField(placeholder);
            field.SetValueWithoutNotify(value);
            field.Changed += v =>
            {
                set(v);
                edited();
            };
            return field;
        }

        static VisualElement Redirects(LinkForm form, Action edited)
        {
            var group = Ui.Box();
            var editors = Ui.Box();
            editors.Add(RedirectEditor("Android", form.Android, edited));
            editors.Add(RedirectEditor("iOS", form.Ios, edited));
            editors.Add(RedirectEditor("Desktop", form.Desktop, edited));

            var row = new SwitchRow("Custom redirects", form.CustomRedirects, null, "uses project", help: Help.Get("customRedirects"));
            var hint = row.Trailing.Q<Label>();
            void Show(bool on)
            {
                editors.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
                hint.style.display = on ? DisplayStyle.None : DisplayStyle.Flex;
            }
            Show(form.CustomRedirects);
            row.Changed += on =>
            {
                form.CustomRedirects = on;
                Show(on);
                edited();
            };
            group.Add(row);
            group.Add(editors);
            return group;
        }

        static VisualElement RedirectEditor(string platform, RedirectForm redirect, Action edited)
        {
            var editor = Ui.Box("redirect-editor");
            var url = new LinkField(platform);
            url.SetValueWithoutNotify(redirect.Url);
            var samples = new ChipRow(SampleRedirects, redirect.Url, v => url.Value = v);
            samples.AddToClassList("redirect-editor__samples");
            url.Changed += v =>
            {
                redirect.Url = v;
                samples.Selected = v;
                edited();
            };
            editor.Add(url);
            editor.Add(samples);
            editor.Add(new SwitchRow("Open app if installed", redirect.OpenAppIfInstalled, on =>
            {
                redirect.OpenAppIfInstalled = on;
                edited();
            }, divider: false, help: Help.Get("openAppIfInstalled")));
            return editor;
        }

        static VisualElement Overrides(LinkForm form, Action edited)
        {
            var content = Ui.Box();
            content.Add(new TriStateRow("Preview on Android", form.PreviewAndroid, v => { form.PreviewAndroid = v; edited(); }));
            content.Add(new TriStateRow("Preview on iOS", form.PreviewIos, v => { form.PreviewIos = v; edited(); }));
            content.Add(new TriStateRow("Clipboard on Android", form.ClipboardAndroid, v => { form.ClipboardAndroid = v; edited(); }));
            content.Add(new TriStateRow("Clipboard on iOS", form.ClipboardIos, v => { form.ClipboardIos = v; edited(); }));
            return content;
        }

        public static string OverridesSummary(LinkForm form)
        {
            var set = new[] { form.PreviewAndroid, form.PreviewIos, form.ClipboardAndroid, form.ClipboardIos }.Count(v => v.HasValue);
            return set == 0 ? "all default" : set + " set";
        }

        static string ImageName(string url)
        {
            switch (url)
            {
                case "": return "None";
                case SampleImage1: return "Sample 1";
                case SampleImage2: return "Sample 2";
                default: return url;
            }
        }
    }
}
