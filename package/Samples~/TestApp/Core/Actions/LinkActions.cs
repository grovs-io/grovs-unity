using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using GrovsSDK;

namespace GrovsTestApp
{
    public sealed class LinkActions
    {
        readonly IGrovsApi _api;
        readonly Func<CallStyle> _style;

        public LinkActions(IGrovsApi api, Func<CallStyle> style)
        {
            _api = api;
            _style = style;
        }

        public TestAction Generate(LinkForm form)
        {
            var style = _style();
            var options = ToOptions(form);
            var code = CallCode.Format(style == CallStyle.Async ? "GenerateLinkAsync" : "GenerateLink",
                ("Title", options.Title),
                ("Subtitle", options.Subtitle),
                ("ImageUrl", options.ImageUrl),
                ("Data", options.Data),
                ("Tags", options.Tags),
                ("IosRedirect", Describe(options.IosRedirect)),
                ("AndroidRedirect", Describe(options.AndroidRedirect)),
                ("DesktopRedirect", Describe(options.DesktopRedirect)),
                ("ShowPreviewIos", options.ShowPreviewIos),
                ("ShowPreviewAndroid", options.ShowPreviewAndroid),
                ("CopyToClipboardIos", options.CopyToClipboardIos),
                ("CopyToClipboardAndroid", options.CopyToClipboardAndroid),
                ("TrackingSource", options.TrackingSource),
                ("TrackingMedium", options.TrackingMedium),
                ("TrackingCampaign", options.TrackingCampaign));

            return new TestAction
            {
                Type = LogType.Create,
                Title = "Generate link",
                Code = code,
                Run = async () =>
                {
                    var link = style == CallStyle.Async
                        ? await _api.GenerateLinkAsync(options)
                        : await Calls.FromCallback<string>(cb => _api.GenerateLink(options, cb));
                    return new ActionResult
                    {
                        Detail = Calls.StyleLabel(style),
                        Value = link,
                        Code = code + "\n\n-> \"" + link + "\""
                    };
                }
            };
        }

        public TestAction Inspect(string path)
        {
            var style = _style();
            var code = CallCode.Format(style == CallStyle.Async ? "GetLinkDetailsAsync" : "GetLinkDetails", ("path", path));
            return new TestAction
            {
                Type = LogType.Inspect,
                Title = "Link details " + path,
                Code = code,
                Run = async () =>
                {
                    var details = style == CallStyle.Async
                        ? await _api.GetLinkDetailsAsync(path)
                        : await Calls.FromCallback<IReadOnlyDictionary<string, object>>(cb => _api.GetLinkDetails(path, cb));
                    return new ActionResult
                    {
                        Detail = CountFields(details) + " fields · " + Calls.StyleLabel(style),
                        Code = code + "\n\n-> " + CallCode.Render(details)
                    };
                }
            };
        }

        static GenerateLinkOptions ToOptions(LinkForm form)
        {
            var options = new GenerateLinkOptions
            {
                Title = Calls.Blank(form.Title),
                Subtitle = Calls.Blank(form.Subtitle),
                ImageUrl = Calls.Blank(form.Image),
                Data = Calls.MapOrNull(form.Data),
                Tags = Calls.NullIfEmpty(form.Tags),
                ShowPreviewIos = form.PreviewIos,
                ShowPreviewAndroid = form.PreviewAndroid,
                CopyToClipboardIos = form.ClipboardIos,
                CopyToClipboardAndroid = form.ClipboardAndroid
            };
            if (form.Utm)
            {
                options.TrackingSource = Calls.Blank(form.UtmSource);
                options.TrackingMedium = Calls.Blank(form.UtmMedium);
                options.TrackingCampaign = Calls.Blank(form.UtmCampaign);
            }
            if (form.CustomRedirects)
            {
                options.IosRedirect = ToRedirect(form.Ios);
                options.AndroidRedirect = ToRedirect(form.Android);
                options.DesktopRedirect = ToRedirect(form.Desktop);
            }
            return options;
        }

        static GrovsRedirect ToRedirect(RedirectForm form) =>
            string.IsNullOrWhiteSpace(form.Url)
                ? null
                : new GrovsRedirect { Url = form.Url, OpenAppIfInstalled = form.OpenAppIfInstalled };

        static IDictionary<string, object> Describe(GrovsRedirect redirect) =>
            redirect == null
                ? null
                : new Dictionary<string, object> { { "url", redirect.Url }, { "openAppIfInstalled", redirect.OpenAppIfInstalled } };

        static int CountFields(object value)
        {
            if (value is IEnumerable<KeyValuePair<string, object>> map)
            {
                var count = 0;
                foreach (var pair in map) count += pair.Value is IEnumerable<KeyValuePair<string, object>> ? CountFields(pair.Value) : 1;
                return count;
            }
            return 1;
        }
    }
}
