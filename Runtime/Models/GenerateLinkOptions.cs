using System.Collections.Generic;
using GrovsSDK.Internal;

namespace GrovsSDK
{
    public sealed class GrovsRedirect
    {
        public string Url;
        public bool OpenAppIfInstalled = true;

        internal Dictionary<string, object> ToMap()
        {
            return new Dictionary<string, object>
            {
                { "url", Url },
                { "openAppIfInstalled", OpenAppIfInstalled }
            };
        }
    }

    public sealed class GenerateLinkOptions
    {
        public string Title, Subtitle, ImageUrl;
        /// <summary>JSON-serializable values only.</summary>
        public IDictionary<string, object> Data;
        public IList<string> Tags;
        public GrovsRedirect IosRedirect, AndroidRedirect, DesktopRedirect;
        public bool? ShowPreviewIos, ShowPreviewAndroid;
        /// <summary>Null uses the project setting.</summary>
        public bool? CopyToClipboardIos, CopyToClipboardAndroid;
        public string TrackingCampaign, TrackingSource, TrackingMedium;

        internal string ToJson()
        {
            var map = new Dictionary<string, object>();
            Put(map, "title", Title);
            Put(map, "subtitle", Subtitle);
            Put(map, "imageUrl", ImageUrl);
            Put(map, "data", Data);
            Put(map, "tags", Tags);
            Put(map, "iosRedirect", IosRedirect?.ToMap());
            Put(map, "androidRedirect", AndroidRedirect?.ToMap());
            Put(map, "desktopRedirect", DesktopRedirect?.ToMap());
            Put(map, "showPreviewIos", ShowPreviewIos);
            Put(map, "showPreviewAndroid", ShowPreviewAndroid);
            Put(map, "copyToClipboardIos", CopyToClipboardIos);
            Put(map, "copyToClipboardAndroid", CopyToClipboardAndroid);
            Put(map, "trackingCampaign", TrackingCampaign);
            Put(map, "trackingSource", TrackingSource);
            Put(map, "trackingMedium", TrackingMedium);
            return GrovsJson.Serialize(map);
        }

        static void Put(Dictionary<string, object> map, string key, object value)
        {
            if (value != null) map[key] = value;
        }
    }
}
