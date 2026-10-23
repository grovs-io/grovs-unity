using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace GrovsTestApp
{
    [RequireComponent(typeof(UIDocument))]
    public class AppRoot : MonoBehaviour
    {
        const string LightClass = "theme-light";
        const string DarkClass = "theme-dark";

        public static AppRoot Instance { get; private set; }

        public VisualElement Root { get; private set; }
        public bool IsDark { get; private set; }

        public event Action<bool> DarkModeChanged;

        Rect _appliedSafeArea;
        Vector2Int _appliedScreen;

        void Awake()
        {
            Instance = this;
        }

        void OnEnable()
        {
            var document = GetComponent<UIDocument>();
            Root = document.rootVisualElement.Q("root");
            Root.RegisterCallback<GeometryChangedEvent>(_ => ApplySafeArea());
            SetDarkMode(NativeServices.Current.IsDarkMode());
            Build();
            ApplySafeArea();
        }

        void OnApplicationFocus(bool focus)
        {
            if (focus && Root != null) SetDarkMode(NativeServices.Current.IsDarkMode());
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (Screen.safeArea != _appliedSafeArea || Screen.width != _appliedScreen.x || Screen.height != _appliedScreen.y)
                ApplySafeArea();
        }

        public void SetDarkMode(bool dark)
        {
            var changed = dark != IsDark;
            IsDark = dark;
            Root.EnableInClassList(DarkClass, dark);
            Root.EnableInClassList(LightClass, !dark);
            if (changed) DarkModeChanged?.Invoke(dark);
        }

        void Build()
        {
            var title = new Label("Grovs Test");
            title.AddToClassList("t-title");
            title.style.marginTop = 16;
            Root.Add(title);

            var field = new TextField("Name") { value = "Grovs" };
            field.style.marginTop = 16;
            Root.Add(field);
        }

        void ApplySafeArea()
        {
            var safe = Screen.safeArea;
            _appliedSafeArea = safe;
            _appliedScreen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;

            var width = Root.layout.width;
            var scale = float.IsNaN(width) || width <= 0 ? 1f : width / Screen.width;

            Root.style.paddingLeft = safe.xMin * scale;
            Root.style.paddingRight = (Screen.width - safe.xMax) * scale;
            Root.style.paddingTop = (Screen.height - safe.yMax) * scale;
            Root.style.paddingBottom = safe.yMin * scale;
        }
    }
}
