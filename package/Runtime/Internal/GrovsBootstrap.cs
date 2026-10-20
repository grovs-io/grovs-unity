using UnityEngine;

namespace GrovsSDK.Internal
{
    internal static class GrovsBootstrap
    {
        static GrovsSceneTracker _sceneTracker;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            GrovsDispatcher.Create(Grovs.EnsureStarted());
            if (!Grovs.IsSceneTrackingEnabled) return;
            _sceneTracker = new GrovsSceneTracker(true, name => Grovs.TrackScreenView(name));
            _sceneTracker.Attach();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void TrackFirstScene()
        {
            _sceneTracker?.TrackCurrentScene();
        }

        // Clears state left over from the last Play Mode session when domain reload is off.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _sceneTracker?.Detach();
            _sceneTracker = null;
            Grovs.ResetState();
        }

        public static IGrovsPlatform CreatePlatform()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return new GrovsIOS();
#elif UNITY_ANDROID && !UNITY_EDITOR
            return new GrovsAndroid();
#else
            return new GrovsStub();
#endif
        }
    }
}
