using System;
using UnityEditor;
using UnityEngine;

namespace GrovsSDK.Editor
{
    /// <summary>Fires a fake incoming deep link in Play Mode.</summary>
    internal sealed class GrovsDeeplinkSimulator : EditorWindow
    {
        string _link = "https://example.sqd.link/path";
        string _dataJson = "{}";
        Vector2 _scroll;

        [MenuItem("Grovs/Simulate Deep Link")]
        static void Open()
        {
            GetWindow<GrovsDeeplinkSimulator>("Grovs Deep Link");
        }

        void OnGUI()
        {
            _link = EditorGUILayout.TextField("Link", _link);
            EditorGUILayout.LabelField("Data (JSON object)");
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(120));
            _dataJson = EditorGUILayout.TextArea(_dataJson, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            var jsonError = ValidateJson(_dataJson);
            if (jsonError != null) EditorGUILayout.HelpBox(jsonError, MessageType.Error);
            if (!Application.isPlaying) EditorGUILayout.HelpBox("Enter Play Mode to send a link.", MessageType.Info);

            using (new EditorGUI.DisabledScope(!Application.isPlaying || jsonError != null || string.IsNullOrEmpty(_link)))
            {
                if (GUILayout.Button("Send")) Grovs.SimulateDeeplink(_link, _dataJson);
            }
        }

        static string ValidateJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                if (GrovsSDK.Internal.GrovsJson.Parse(json) is System.Collections.Generic.Dictionary<string, object>)
                    return null;
                return "Data must be a JSON object.";
            }
            catch (Exception e)
            {
                return "Invalid JSON: " + e.Message;
            }
        }
    }
}
