using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace GrovsSDK.Internal
{
    /// <summary>
    /// Queues native messages from any thread and hands them to <see cref="Grovs"/> on the main thread.
    /// </summary>
    internal sealed class NativeMessageRouter
    {
        readonly ConcurrentQueue<(string Type, string Json)> _queue = new ConcurrentQueue<(string, string)>();
        readonly Dictionary<int, Action<object, GrovsError>> _requests = new Dictionary<int, Action<object, GrovsError>>();
        int _lastRequestId;

        /// <summary>Thread-safe. Native code may call this while holding its own locks, so it only enqueues.</summary>
        public void Enqueue(string type, string json)
        {
            _queue.Enqueue((type, json));
        }

        /// <summary>Main thread only.</summary>
        public int NewRequest(Action<object, GrovsError> onDone)
        {
            var id = ++_lastRequestId;
            _requests[id] = onDone;
            return id;
        }

        /// <summary>Main thread only.</summary>
        public void Complete(int requestId, object value, GrovsError error)
        {
            if (!_requests.TryGetValue(requestId, out var onDone))
            {
                Debug.LogWarning("[Grovs] Result for unknown request " + requestId + ".");
                return;
            }
            _requests.Remove(requestId);
            try
            {
                onDone(value, error);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>Main thread only.</summary>
        public void Drain()
        {
            while (_queue.TryDequeue(out var message))
            {
                try
                {
                    Route(message.Type, message.Json);
                }
                catch (Exception e)
                {
                    Debug.LogError("[Grovs] Could not handle native '" + message.Type + "' message: " + e.Message);
                }
            }
        }

        void Route(string type, string json)
        {
            switch (type)
            {
                case "deeplink":
                    Grovs.RaiseDeeplink(GrovsDeeplink.FromJson(json));
                    break;
                case "error":
                    Grovs.RaiseError(GrovsError.FromJson(GrovsJson.Parse(json)));
                    break;
                case "inapp_event":
                    Grovs.RaiseInAppEvent(InAppMessageEvent.FromJson(json));
                    break;
                case "result":
                    RouteResult(json);
                    break;
                case "log":
                    RouteLog(json);
                    break;
                default:
                    Debug.LogWarning("[Grovs] Unknown native message type '" + type + "'.");
                    break;
            }
        }

        void RouteResult(string json)
        {
            var map = (Dictionary<string, object>)GrovsJson.Parse(json);
            var requestId = Convert.ToInt32(map["requestId"]);
            map.TryGetValue("ok", out var ok);
            map.TryGetValue("value", out var value);
            map.TryGetValue("error", out var errorObj);

            if (ok is bool success && success)
            {
                Complete(requestId, value, null);
                return;
            }
            var error = GrovsError.FromJson(errorObj)
                        ?? new GrovsError { Code = GrovsErrorCode.Unknown, Message = "Native call failed." };
            Complete(requestId, null, error);
        }

        static void RouteLog(string json)
        {
            var map = (Dictionary<string, object>)GrovsJson.Parse(json);
            map.TryGetValue("level", out var level);
            map.TryGetValue("message", out var message);
            var text = "[Grovs] " + message;
            switch (level as string)
            {
                case "error":
                    Debug.LogError(text);
                    break;
                case "warn":
                    Debug.LogWarning(text);
                    break;
                default:
                    Debug.Log(text);
                    break;
            }
        }
    }
}
