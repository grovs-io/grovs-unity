using System.Collections.Generic;
using GrovsSDK.Internal;

namespace GrovsSDK
{
    public sealed class GrovsTracking
    {
        public string Campaign;
        public string Source;
        public string Medium;
    }

    public sealed class GrovsDeeplink
    {
        const string PushMarkerKey = "grovs_campaign_id";

        /// <summary>The tapped link. May be null.</summary>
        public string Link;
        /// <summary>The link payload. Never null.</summary>
        public IReadOnlyDictionary<string, object> Data;
        /// <summary>Campaign, source and medium. May be null.</summary>
        public GrovsTracking Tracking;
        /// <summary>True when the link came from a push notification.</summary>
        public bool FromPush;
        public string RawJson;

        internal static GrovsDeeplink FromJson(string json)
        {
            var root = GrovsJson.Parse(json) as Dictionary<string, object>;
            var data = Get(root, "data") as Dictionary<string, object> ?? new Dictionary<string, object>();
            var tracking = Get(root, "tracking") as Dictionary<string, object>;
            return new GrovsDeeplink
            {
                Link = Get(root, "link") as string,
                Data = data,
                Tracking = tracking == null ? null : new GrovsTracking
                {
                    Campaign = Get(tracking, "campaign") as string,
                    Source = Get(tracking, "source") as string,
                    Medium = Get(tracking, "medium") as string
                },
                FromPush = data.ContainsKey(PushMarkerKey),
                RawJson = json
            };
        }

        static object Get(Dictionary<string, object> map, string key)
        {
            object value = null;
            if (map != null) map.TryGetValue(key, out value);
            return value;
        }
    }
}
