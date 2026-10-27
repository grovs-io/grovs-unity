using UnityEngine;

namespace GrovsSDK.Internal
{
    /// <summary>Drains native messages once per frame on the main thread.</summary>
    internal sealed class GrovsDispatcher : MonoBehaviour
    {
        NativeMessageRouter _router;

        public static GrovsDispatcher Create(NativeMessageRouter router)
        {
            var go = new GameObject("[Grovs]") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(go);
            var dispatcher = go.AddComponent<GrovsDispatcher>();
            dispatcher._router = router;
            return dispatcher;
        }

        void Update()
        {
            _router.Drain();
        }

        // HideAndDontSave objects outlive Play Mode in the Editor.
        void OnApplicationQuit()
        {
            Destroy(gameObject);
        }
    }
}
