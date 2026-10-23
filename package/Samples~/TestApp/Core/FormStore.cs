using System;
using UnityEngine;

namespace GrovsTestApp
{
    public interface IPrefs
    {
        string Get(string key);
        void Set(string key, string value);
        void Delete(string key);
    }

    public sealed class PlayerPrefsStore : IPrefs
    {
        public string Get(string key) => PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null;

        public void Set(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
        }

        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }

    public static class FormKeys
    {
        public const string Link = "grovs_test_app.link_form";
        public const string Event = "grovs_test_app.event_form";
        public const string Screen = "grovs_test_app.screen_form";
        public const string Purchase = "grovs_test_app.purchase_form";
        public const string Identity = "grovs_test_app.identity_form";
        public const string Setup = "grovs_test_app.setup_form";
        public const string InApp = "grovs_test_app.in_app_prefs";
        public const string Developer = "grovs_test_app.developer_prefs";
    }

    /// <summary>Keeps each form as JSON under its own key. A missing or unreadable value gives the defaults.</summary>
    public sealed class FormStore
    {
        readonly IPrefs _prefs;

        public FormStore(IPrefs prefs)
        {
            _prefs = prefs;
        }

        public T Load<T>(string key) where T : new()
        {
            var json = _prefs.Get(key);
            if (string.IsNullOrEmpty(json)) return new T();
            try
            {
                var form = JsonUtility.FromJson<T>(json);
                return form != null ? form : new T();
            }
            catch (ArgumentException)
            {
                return new T();
            }
        }

        public void Save<T>(string key, T form) => _prefs.Set(key, JsonUtility.ToJson(form));

        public T Reset<T>(string key) where T : new()
        {
            _prefs.Delete(key);
            return new T();
        }
    }
}
