using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GrovsSDK.Internal;
using UnityEngine;

namespace GrovsSDK
{
    /// <summary>
    /// Grovs deep links, attribution, events, revenue and in-app messages.
    /// Events, callbacks and awaited results run on the Unity main thread.
    /// </summary>
    public static class Grovs
    {
        const int MaxPendingDeeplinks = 20;
        // Screen names are not event names, so only the size limits apply to them.
        const string ScreenViewCheckName = "screen";

        static IGrovsPlatform _platform;
        static NativeMessageRouter _router;
        static Action<GrovsDeeplink> _deeplinkHandlers;
        static readonly List<GrovsDeeplink> PendingDeeplinks = new List<GrovsDeeplink>();

        public static string SdkVersion => "1.0.0";

        /// <summary>True on iOS and Android players.</summary>
        public static bool IsSupportedPlatform => Platform.IsSupported;

        /// <summary>Errors that have no other place to be reported.</summary>
        public static event Action<GrovsError> ErrorOccurred;

        /// <summary>Android has no warn level, so Warn maps to Info there.</summary>
        public static void SetLogLevel(GrovsLogLevel level) => Platform.SetLogLevel((int)level);

        #region Consent

        /// <summary>Stored natively and used again on the next launch.</summary>
        public static void SetEnabled(bool enabled) => Platform.SetEnabled(enabled);

        public static bool IsEnabled => Platform.IsEnabled();

        #endregion

        #region Deep links

        /// <summary>
        /// Links that arrive before anyone subscribes are kept and delivered, in order,
        /// to the first subscriber.
        /// </summary>
        public static event Action<GrovsDeeplink> DeeplinkReceived
        {
            add
            {
                if (value == null) return;
                var hadHandlers = _deeplinkHandlers != null;
                _deeplinkHandlers += value;
                if (!hadHandlers) FlushPendingDeeplinks(value);
            }
            remove { _deeplinkHandlers -= value; }
        }

        public static GrovsDeeplink LastDeeplink { get; private set; }

        /// <summary>
        /// Handles a link tapped inside the game. The URL must be absolute with a scheme and a host.
        /// Returns false when the SDK is disabled.
        /// </summary>
        public static bool HandleUrl(string url)
        {
            if (url == null) throw new ArgumentNullException(nameof(url));
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Host))
                throw new ArgumentException("URL must be absolute with a scheme and a host: " + url, nameof(url));
            return Platform.HandleUrl(url);
        }

        #endregion

        #region Links

        public static Task<string> GenerateLinkAsync(GenerateLinkOptions options)
        {
            options = options ?? new GenerateLinkOptions();
            GrovsValidation.EnsureSerializable(options.Data, nameof(options));
            var json = options.ToJson();
            return Request(value => value as string, id => Platform.GenerateLink(id, json));
        }

        public static void GenerateLink(GenerateLinkOptions options, Action<string, GrovsError> callback)
        {
            Forward(GenerateLinkAsync(options), callback);
        }

        public static Task<IReadOnlyDictionary<string, object>> GetLinkDetailsAsync(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            return Request<IReadOnlyDictionary<string, object>>(
                value => value as Dictionary<string, object> ?? new Dictionary<string, object>(),
                id => Platform.LinkDetails(id, path));
        }

        public static void GetLinkDetails(string path, Action<IReadOnlyDictionary<string, object>, GrovsError> callback)
        {
            Forward(GetLinkDetailsAsync(path), callback);
        }

        #endregion

        #region Identity

        /// <summary>Null clears the identifier.</summary>
        public static void SetUserIdentifier(string id) => Platform.SetUserIdentifier(id);

        /// <summary>Values must be JSON-serializable.</summary>
        public static void SetUserAttributes(IDictionary<string, object> attrs)
        {
            GrovsValidation.EnsureSerializable(attrs, nameof(attrs));
            Platform.SetUserAttributes(attrs == null ? null : GrovsJson.Serialize(attrs));
        }

        /// <summary>Only needed when the game gets push tokens itself.</summary>
        public static void SetPushToken(string token) => Platform.SetPushToken(token);

        #endregion

        #region Events

        public static void Track(string name, IDictionary<string, object> properties = null, IList<string> tags = null)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            GrovsValidation.EnsureSerializable(properties, nameof(properties));
            WarnIfInvalid(GrovsValidation.CheckEvent(name, properties, tags));
            Platform.Track(name, JsonOrNull(properties), JsonOrNull(tags));
        }

        public static void TrackScreenView(string screenName, IDictionary<string, object> properties = null)
        {
            if (screenName == null) throw new ArgumentNullException(nameof(screenName));
            GrovsValidation.EnsureSerializable(properties, nameof(properties));
            WarnIfInvalid(GrovsValidation.CheckEvent(ScreenViewCheckName, properties, null));
            Platform.TrackScreenView(screenName, JsonOrNull(properties));
        }

        /// <summary>Null clears the tags.</summary>
        public static void SetGlobalTags(IList<string> tags) => Platform.SetGlobalTags(JsonOrNull(tags));

        public static void SetScreenAliases(IDictionary<string, string> aliases)
        {
            Platform.SetScreenAliases(GrovsJson.Serialize(ToObjectMap(aliases)));
        }

        #endregion

        #region In-app messages

        public static event Action<InAppMessageEvent> InAppMessageEvent;

        /// <summary>
        /// Return false to hold a message back. Null shows every message.
        /// Called synchronously on a native thread (the Android UI thread is not Unity's),
        /// so it must be fast and must not touch Unity APIs.
        /// </summary>
        public static Func<string, bool> ShouldShowInAppMessage { get; set; }

        public static void PauseInAppMessages() => Platform.PauseInApp();

        public static void ResumeInAppMessages() => Platform.ResumeInApp();

        public static void DiscardInAppMessages(bool dismissCurrent = false) => Platform.DiscardInApp(dismissCurrent);

        public static void SetInAppExcludedScreens(IList<string> screens)
        {
            Platform.SetInAppExcludedScreens(GrovsJson.Serialize(screens ?? new List<string>()));
        }

        public static void SetInAppDebugLogging(bool enabled) => Platform.SetInAppDebugLogging(enabled);

        #endregion

        #region Revenue

        /// <summary>
        /// iOS: the StoreKit transaction id. Android: Play <c>Purchase.originalJson</c>,
        /// where the result is true once the call is handed over.
        /// </summary>
        public static Task<bool> LogInAppPurchaseAsync(string storeTransaction)
        {
            if (string.IsNullOrEmpty(storeTransaction))
                throw new ArgumentException("Store transaction is required.", nameof(storeTransaction));
            return Request(ToBool, id => Platform.LogInAppPurchase(id, storeTransaction));
        }

        /// <summary>A null date means now.</summary>
        public static Task<bool> LogCustomPurchaseAsync(GrovsTransactionType type, int priceInCents,
            string currency, string productId, DateTime? date = null)
        {
            if (currency == null) throw new ArgumentNullException(nameof(currency));
            if (productId == null) throw new ArgumentNullException(nameof(productId));
            var epochMs = date.HasValue ? new DateTimeOffset(date.Value.ToUniversalTime()).ToUnixTimeMilliseconds() : -1L;
            return Request(ToBool,
                id => Platform.LogCustomPurchase(id, (int)type, priceInCents, currency, productId, epochMs));
        }

        #endregion

        #region Push helpers

        public static bool IsGrovsNotification(IDictionary<string, string> data)
        {
            if (data == null) return false;
            return Platform.IsGrovsNotification(GrovsJson.Serialize(ToObjectMap(data)));
        }

        public static bool HandleNotificationOpen(IDictionary<string, object> data)
        {
            if (data == null) return false;
            GrovsValidation.EnsureSerializable(data, nameof(data));
            return Platform.HandleNotificationOpen(GrovsJson.Serialize(data));
        }

        #endregion

        #region Internal

        internal static bool IsSceneTrackingEnabled => Platform.IsSceneTrackingEnabled();

#if UNITY_EDITOR
        /// <summary>Delivers a fake incoming link exactly like a native one. Editor only.</summary>
        internal static void SimulateDeeplink(string link, string dataJson)
        {
            var data = string.IsNullOrWhiteSpace(dataJson)
                ? new Dictionary<string, object>()
                : GrovsJson.Parse(dataJson);
            if (!(data is Dictionary<string, object>))
                throw new ArgumentException("Data must be a JSON object.", nameof(dataJson));
            var message = new Dictionary<string, object> { { "link", link }, { "data", data }, { "tracking", null } };
            Router.Enqueue("deeplink", GrovsJson.Serialize(message));
        }
#endif

        internal static NativeMessageRouter Router => EnsureStarted();

        static IGrovsPlatform Platform
        {
            get
            {
                EnsureStarted();
                return _platform;
            }
        }

        internal static NativeMessageRouter EnsureStarted()
        {
            if (_platform == null) Start(GrovsBootstrap.CreatePlatform);
            return _router;
        }

        internal static void ResetForTests(IGrovsPlatform platform)
        {
            ResetState();
            Start(() => platform);
        }

        internal static void ResetState()
        {
            _platform = null;
            _router = null;
            _deeplinkHandlers = null;
            PendingDeeplinks.Clear();
            LastDeeplink = null;
            ErrorOccurred = null;
            InAppMessageEvent = null;
            ShouldShowInAppMessage = null;
        }

        // A broken native bridge must not break the game, so it falls back to the stub.
        static void Start(Func<IGrovsPlatform> createPlatform)
        {
            _router = new NativeMessageRouter();
            try
            {
                _platform = createPlatform();
                _platform.Start(_router.Enqueue, InvokeShouldShow);
            }
            catch (Exception e)
            {
                Debug.LogError("[Grovs] The native bridge failed to start, so Grovs is disabled: " + e);
                _platform = new GrovsStub();
                _platform.Start(_router.Enqueue, InvokeShouldShow);
            }
        }

        /// <summary>Runs on the native thread, so it calls the delegate directly.</summary>
        internal static bool InvokeShouldShow(string campaignId)
        {
            var shouldShow = ShouldShowInAppMessage;
            if (shouldShow == null) return true;
            try
            {
                return shouldShow(campaignId);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return true;
            }
        }

        internal static void RaiseDeeplink(GrovsDeeplink deeplink)
        {
            LastDeeplink = deeplink;
            var handlers = _deeplinkHandlers;
            if (handlers == null)
            {
                if (PendingDeeplinks.Count == MaxPendingDeeplinks) PendingDeeplinks.RemoveAt(0);
                PendingDeeplinks.Add(deeplink);
                return;
            }
            InvokeEach(handlers, deeplink);
        }

        internal static void RaiseError(GrovsError error) => InvokeEach(ErrorOccurred, error);

        internal static void RaiseInAppEvent(InAppMessageEvent inAppEvent) => InvokeEach(InAppMessageEvent, inAppEvent);

        static void FlushPendingDeeplinks(Action<GrovsDeeplink> handler)
        {
            var pending = PendingDeeplinks.ToArray();
            PendingDeeplinks.Clear();
            foreach (var deeplink in pending) InvokeEach(handler, deeplink);
        }

        static void InvokeEach<T>(Action<T> handlers, T arg)
        {
            if (handlers == null) return;
            foreach (var handler in handlers.GetInvocationList())
            {
                try
                {
                    ((Action<T>)handler)(arg);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        static Task<T> Request<T>(Func<object, T> convert, Action<int> send)
        {
            var router = Router;
            var tcs = new TaskCompletionSource<T>();
            var id = router.NewRequest((value, error) =>
            {
                if (error != null) tcs.TrySetException(new GrovsException(error));
                else tcs.TrySetResult(convert(value));
            });
            try
            {
                send(id);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                router.Complete(id, null, new GrovsError { Code = GrovsErrorCode.Unknown, Message = e.Message });
            }
            return tcs.Task;
        }

        // Runs the callback inline on the thread that completes the task, which is the main thread.
        static void Forward<T>(Task<T> task, Action<T, GrovsError> callback)
        {
            if (callback == null) return;
            task.ContinueWith(t =>
            {
                var value = t.Status == TaskStatus.RanToCompletion ? t.Result : default;
                var error = t.IsFaulted ? ToError(t.Exception?.InnerException) : null;
                try
                {
                    callback(value, error);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }, TaskContinuationOptions.ExecuteSynchronously);
        }

        static GrovsError ToError(Exception e)
        {
            return (e as GrovsException)?.Error
                   ?? new GrovsError { Code = GrovsErrorCode.Unknown, Message = e?.Message };
        }

        static bool ToBool(object value) => value is bool b && b;

        static string JsonOrNull(object value) => value == null ? null : GrovsJson.Serialize(value);

        static Dictionary<string, object> ToObjectMap<TValue>(IDictionary<string, TValue> source)
        {
            var map = new Dictionary<string, object>();
            if (source == null) return map;
            foreach (var pair in source) map[pair.Key] = pair.Value;
            return map;
        }

        static void WarnIfInvalid(string warning)
        {
            if (warning != null) Debug.LogWarning("[Grovs] " + warning);
        }

        #endregion
    }
}
