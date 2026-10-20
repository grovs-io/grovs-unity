using System;
using System.Linq;
using System.Xml;

namespace GrovsSDK.Editor.Android
{
    /// <summary>Adds the URI scheme and link domain intent filters to the launcher activity.</summary>
    internal static class GrovsLauncherIntentFilters
    {
        const string ViewAction = "android.intent.action.VIEW";

        public static void Apply(XmlDocument manifest, GrovsBuildConfig c)
        {
            var activity = FindLauncher(manifest);
            if (activity == null)
                throw new InvalidOperationException("Grovs: no MAIN/LAUNCHER activity found in unityLibrary manifest.");

            if (c.UriScheme.Length > 0) AddFilter(activity, c.UriScheme, null);
            foreach (var domain in c.LinkDomains) AddFilter(activity, "https", domain);
        }

        static XmlElement FindLauncher(XmlDocument manifest) =>
            manifest.SelectNodes("/manifest/application/activity|/manifest/application/activity-alias")
                .Cast<XmlElement>()
                .FirstOrDefault(a => Filters(a).Any(f =>
                    HasChild(f, "action", "android.intent.action.MAIN") &&
                    HasChild(f, "category", "android.intent.category.LAUNCHER")));

        static XmlElement[] Filters(XmlElement activity) =>
            activity.ChildNodes.OfType<XmlElement>().Where(e => e.Name == "intent-filter").ToArray();

        static bool HasChild(XmlElement parent, string name, string androidName) =>
            parent.ChildNodes.OfType<XmlElement>()
                .Any(e => e.Name == name && GrovsAndroidXml.GetAndroid(e, "name") == androidName);

        static void AddFilter(XmlElement activity, string scheme, string host)
        {
            var filters = Filters(activity);
            if (filters.Any(f => HasData(f, scheme, host))) return;

            var doc = activity.OwnerDocument;
            var filter = doc.CreateElement("intent-filter");
            if (host != null) GrovsAndroidXml.SetAndroid(filter, "autoVerify", "true");
            filter.AppendChild(GrovsAndroidXml.Element(doc, "action", "name", ViewAction));
            filter.AppendChild(GrovsAndroidXml.Element(doc, "category", "name", "android.intent.category.DEFAULT"));
            filter.AppendChild(GrovsAndroidXml.Element(doc, "category", "name", "android.intent.category.BROWSABLE"));
            filter.AppendChild(host == null
                ? GrovsAndroidXml.Element(doc, "data", "scheme", scheme)
                : GrovsAndroidXml.Element(doc, "data", "scheme", scheme, "host", host));

            activity.InsertAfter(filter, filters.Last());
        }

        static bool HasData(XmlElement filter, string scheme, string host) =>
            filter.ChildNodes.OfType<XmlElement>().Any(d =>
                d.Name == "data" &&
                GrovsAndroidXml.GetAndroid(d, "scheme") == scheme &&
                GrovsAndroidXml.GetAndroid(d, "host") == (host ?? ""));
    }
}
