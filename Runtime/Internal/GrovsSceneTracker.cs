using System;
using UnityEngine.SceneManagement;

namespace GrovsSDK.Internal
{
    /// <summary>Reports each newly active scene as a screen view, once per change.</summary>
    internal sealed class GrovsSceneTracker
    {
        readonly bool _enabled;
        readonly Action<string> _trackScreenView;
        string _lastScene;

        public GrovsSceneTracker(bool enabled, Action<string> trackScreenView)
        {
            _enabled = enabled;
            _trackScreenView = trackScreenView;
        }

        public void OnActiveSceneChanged(string newSceneName)
        {
            if (!_enabled || string.IsNullOrEmpty(newSceneName) || newSceneName == _lastScene) return;
            _lastScene = newSceneName;
            _trackScreenView(newSceneName);
        }

        public void Attach()
        {
            SceneManager.activeSceneChanged += HandleSceneChanged;
        }

        public void Detach()
        {
            SceneManager.activeSceneChanged -= HandleSceneChanged;
        }

        // activeSceneChanged does not fire for every first scene, so startup reports it directly.
        public void TrackCurrentScene()
        {
            OnActiveSceneChanged(SceneManager.GetActiveScene().name);
        }

        void HandleSceneChanged(Scene previous, Scene next) => OnActiveSceneChanged(next.name);
    }
}
