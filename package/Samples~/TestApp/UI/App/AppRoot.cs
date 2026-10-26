using UnityEngine;
using UnityEngine.UIElements;

namespace GrovsTestApp
{
    /// <summary>What the scene shows: the whole app, or only the excluded screen.</summary>
    public enum AppRootView { App, ExcludedScreen }

    [RequireComponent(typeof(UIDocument))]
    public class AppRoot : MonoBehaviour
    {
        const string LightClass = "theme-light";
        const string DarkClass = "theme-dark";
        const string RootName = "root";

        [SerializeField] AppRootView _view = AppRootView.App;

        public static AppRoot Instance { get; private set; }

        public VisualElement Root { get; private set; }
        public AppScaffold Scaffold { get; private set; }
        public bool IsDark { get; private set; }

        Rect _appliedSafeArea;
        Vector2Int _appliedScreen;
        bool _themeRead;
        bool _overlayWasOpen;
        ExcludedScreenView _excludedScreen;

        void Awake()
        {
            Instance = this;
        }

        void OnEnable()
        {
            var document = GetComponent<UIDocument>();
            Root = document.rootVisualElement.Q(RootName);
            if (Root == null)
            {
                Debug.LogError("AppRoot needs an element named \"" + RootName + "\" in its UXML (TestApp.uxml).", this);
                enabled = false;
                return;
            }
            Root.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);

            if (!_themeRead)
            {
                _themeRead = true;
                IsDark = NativeServices.Current.IsDarkMode();
            }
            ApplyTheme();
            Build();
            ApplySafeArea();
        }

        void OnDisable()
        {
            Root?.UnregisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
            if (Scaffold != null && AppContext.Current.Scaffold == Scaffold) AppContext.Current.Scaffold = null;
        }

        void OnApplicationFocus(bool focus)
        {
            if (!focus || Root == null) return;
            SetDarkMode(NativeServices.Current.IsDarkMode());
            AppContext.Current.OnResume();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (Root == null) return;
            if (Screen.safeArea != _appliedSafeArea || Screen.width != _appliedScreen.x || Screen.height != _appliedScreen.y)
                ApplySafeArea();

            AppContext.Current.FlushBlocked();
            Scaffold?.SetTyping(TouchScreenKeyboard.visible);

#if ENABLE_LEGACY_INPUT_MANAGER
            // A back press while a sheet or dialog was open belongs to that overlay, which closes itself.
            if (Input.GetKeyDown(KeyCode.Escape) && !_overlayWasOpen && Overlay.Newest == null) HandleBack();
#endif
        }

        void LateUpdate()
        {
            _overlayWasOpen = Overlay.Newest != null;
        }

        void HandleBack()
        {
            if (_excludedScreen != null) _excludedScreen.Back();
            else Scaffold.HandleBack();
        }

        public void SetDarkMode(bool dark)
        {
            IsDark = dark;
            ApplyTheme();
        }

        void ApplyTheme()
        {
            Root.EnableInClassList(DarkClass, IsDark);
            Root.EnableInClassList(LightClass, !IsDark);
        }

        void Build()
        {
            Root.Clear();
            var ctx = AppContext.Current;
            if (_view == AppRootView.ExcludedScreen)
            {
                _excludedScreen = new ExcludedScreenView(ctx);
                Root.Add(_excludedScreen);
                return;
            }
            Scaffold = new AppScaffold(ctx);
            Root.Add(Scaffold);
            ctx.Scaffold = Scaffold;
            ctx.ApplyScreens(Scaffold);
        }

        void OnRootGeometryChanged(GeometryChangedEvent _) => ApplySafeArea();

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
