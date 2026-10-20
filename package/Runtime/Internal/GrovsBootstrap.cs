using UnityEngine;

namespace GrovsSDK.Internal
{
    internal static class GrovsBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            GrovsDispatcher.Create(Grovs.EnsureStarted());
        }

        // Clears state left over from the last Play Mode session when domain reload is off.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
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
