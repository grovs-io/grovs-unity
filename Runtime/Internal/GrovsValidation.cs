using System;
using System.Collections.Generic;
using System.Text;

namespace GrovsSDK.Internal
{
    internal static class GrovsValidation
    {
        const int MaxPropertiesBytes = 8192;
        const int MaxTags = 20;
        const int MaxTagLength = 255;

        static readonly HashSet<string> Reserved = new HashSet<string>
        {
            "view", "open", "install", "reinstall", "app_open", "time_spent", "reactivation",
            "user_referred", "custom", "screen_view", "push_open", "push_delivered", "push_held",
            "in_app_viewed", "in_app_clicked", "in_app_dismissed", "in_app_answered",
            "in_app_commented", "in_app_held", "journey_entered", "journey_held", "journey_path"
        };

        public static bool IsReservedEventName(string name)
        {
            return string.IsNullOrEmpty(name) || Reserved.Contains(name);
        }

        /// <summary>Returns a warning for the first limit that is broken, or null.</summary>
        public static string CheckEvent(string name, IDictionary<string, object> props, IList<string> tags)
        {
            if (IsReservedEventName(name))
                return "Event name '" + name + "' is empty or reserved.";

            if (tags != null)
            {
                if (tags.Count > MaxTags)
                    return "Events allow at most " + MaxTags + " tags, got " + tags.Count + ".";
                foreach (var tag in tags)
                {
                    if (tag != null && tag.Length > MaxTagLength)
                        return "Tags can be at most " + MaxTagLength + " characters.";
                }
            }

            if (props != null)
            {
                var bytes = Encoding.UTF8.GetByteCount(GrovsJson.Serialize(props));
                if (bytes > MaxPropertiesBytes)
                    return "Event properties are " + bytes + " bytes, the limit is " + MaxPropertiesBytes + ".";
            }

            return null;
        }

        public static void EnsureSerializable(IDictionary<string, object> data, string paramName)
        {
            if (data == null) return;
            foreach (var pair in data)
            {
                try
                {
                    GrovsJson.Serialize(pair.Value);
                }
                catch (ArgumentException e)
                {
                    throw new ArgumentException(
                        "Value for key '" + pair.Key + "' is not JSON serializable: " + e.Message, paramName);
                }
            }
        }
    }
}
