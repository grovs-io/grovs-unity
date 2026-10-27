using System;
using System.Linq;
using System.Security;
using System.Text;
using UnityEngine;

namespace GrovsSDK.Editor.Android
{
    /// <summary>Turns google-services.json into the values resource the Google Services Gradle plugin would write.</summary>
    internal static class GrovsGoogleServicesConverter
    {
        public static string Convert(string json, string packageName)
        {
            var file = JsonUtility.FromJson<GoogleServicesFile>(json ?? "");
            var client = file?.client?.FirstOrDefault(c =>
                c?.client_info?.android_client_info?.package_name == packageName);
            if (client == null)
                throw new InvalidOperationException(
                    "Grovs: google-services.json has no Android client for package " + packageName + ".");

            var xml = new StringBuilder("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<resources>\n");
            AddString(xml, "google_app_id", client.client_info.mobilesdk_app_id);
            AddString(xml, "gcm_defaultSenderId", file.project_info?.project_number);
            AddString(xml, "google_api_key", client.api_key?.FirstOrDefault()?.current_key);
            AddString(xml, "project_id", file.project_info?.project_id);
            AddString(xml, "google_storage_bucket", file.project_info?.storage_bucket);
            return xml.Append("</resources>\n").ToString();
        }

        static void AddString(StringBuilder xml, string name, string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            xml.Append("    <string name=\"").Append(name).Append("\" translatable=\"false\">")
                .Append(SecurityElement.Escape(value)).Append("</string>\n");
        }

#pragma warning disable 0649
        [Serializable] class GoogleServicesFile { public ProjectInfo project_info; public Client[] client; }
        [Serializable] class ProjectInfo { public string project_number; public string project_id; public string storage_bucket; }
        [Serializable] class Client { public ClientInfo client_info; public ApiKey[] api_key; }
        [Serializable] class ClientInfo { public string mobilesdk_app_id; public AndroidClientInfo android_client_info; }
        [Serializable] class AndroidClientInfo { public string package_name; }
        [Serializable] class ApiKey { public string current_key; }
#pragma warning restore 0649
    }
}
