using System.Collections.Generic;
using GrovsSDK.Internal;

namespace GrovsSDK
{
    public enum InAppEventType
    {
        Shown,
        Clicked,
        Dismissed,
        Answered,
        Failed
    }

    public sealed class InAppMessageEvent
    {
        public InAppEventType Type;
        public string CampaignId;
        /// <summary>Clicked only. Null when the body was tapped.</summary>
        public string ButtonId;
        /// <summary>Clicked with a key_value action.</summary>
        public IReadOnlyDictionary<string, object> Payload;
        /// <summary>close_button, backdrop, swipe, app or orientation.</summary>
        public string DismissReason;
        /// <summary>A long (stars) or a string (option id).</summary>
        public object AnswerValue;
        public string Comment;
        /// <summary>Failed only.</summary>
        public string FailureMessage;

        internal static InAppMessageEvent FromJson(string json)
        {
            var map = GrovsJson.Parse(json) as Dictionary<string, object>
                      ?? new Dictionary<string, object>();
            return new InAppMessageEvent
            {
                Type = ParseType(Str(map, "type")),
                CampaignId = Str(map, "campaignId"),
                ButtonId = Str(map, "buttonId"),
                Payload = Obj(map, "payload") as Dictionary<string, object>,
                DismissReason = Str(map, "reason"),
                AnswerValue = Obj(map, "value"),
                Comment = Str(map, "comment"),
                FailureMessage = Str(map, "message")
            };
        }

        static InAppEventType ParseType(string type)
        {
            switch (type)
            {
                case "shown": return InAppEventType.Shown;
                case "clicked": return InAppEventType.Clicked;
                case "dismissed": return InAppEventType.Dismissed;
                case "answered": return InAppEventType.Answered;
                default: return InAppEventType.Failed;
            }
        }

        static object Obj(Dictionary<string, object> map, string key)
        {
            map.TryGetValue(key, out var value);
            return value;
        }

        static string Str(Dictionary<string, object> map, string key)
        {
            return Obj(map, key) as string;
        }
    }
}
