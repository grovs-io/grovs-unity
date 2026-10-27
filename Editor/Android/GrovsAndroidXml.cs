using System.IO;
using System.Text;
using System.Xml;

namespace GrovsSDK.Editor.Android
{
    internal static class GrovsAndroidXml
    {
        public const string AndroidNs = "http://schemas.android.com/apk/res/android";
        public const string ToolsNs = "http://schemas.android.com/tools";

        public static string ToText(XmlDocument doc)
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "    ",
                NewLineChars = "\n",
                Encoding = new UTF8Encoding(false)
            };
            using (var stream = new MemoryStream())
            {
                using (var writer = XmlWriter.Create(stream, settings)) doc.Save(writer);
                return Encoding.UTF8.GetString(stream.ToArray()) + "\n";
            }
        }

        public static void Save(XmlDocument doc, string path) => File.WriteAllText(path, ToText(doc));

        public static XmlElement Element(XmlDocument doc, string name, params string[] androidAttributes)
        {
            var element = doc.CreateElement(name);
            for (var i = 0; i + 1 < androidAttributes.Length; i += 2)
                SetAndroid(element, androidAttributes[i], androidAttributes[i + 1]);
            return element;
        }

        public static void SetAndroid(XmlElement element, string name, string value) =>
            Set(element, "android", AndroidNs, name, value);

        public static void SetTools(XmlElement element, string name, string value) =>
            Set(element, "tools", ToolsNs, name, value);

        static void Set(XmlElement element, string prefix, string ns, string name, string value)
        {
            var attribute = element.OwnerDocument.CreateAttribute(prefix, name, ns);
            attribute.Value = value;
            element.SetAttributeNode(attribute);
        }

        public static string GetAndroid(XmlNode node, string name) =>
            (node as XmlElement)?.GetAttribute(name, AndroidNs) ?? "";
    }
}
