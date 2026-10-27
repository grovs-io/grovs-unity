using UnityEngine;

namespace GrovsTestApp
{
    public static class NativeServices
    {
        static INativeServices _current;

        public static INativeServices Current => _current ??= Create();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _current = null;

        static INativeServices Create()
        {
            if (Application.isEditor) return new EditorNativeServices();
            switch (Application.platform)
            {
                case RuntimePlatform.Android: return new AndroidNativeServices();
                case RuntimePlatform.IPhonePlayer: return new IosNativeServices();
                default: return new EditorNativeServices();
            }
        }
    }
}
