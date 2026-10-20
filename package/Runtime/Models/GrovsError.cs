using System;
using System.Collections.Generic;

namespace GrovsSDK
{
    public enum GrovsErrorCode
    {
        AuthenticationFailed,
        NetworkRequestFailed,
        EventDispatchFailed,
        LinkGenerationFailed,
        InAppDisplayFailed,
        LinkDetailsFailed,
        NotificationsError,
        SdkNotInitialized,
        SdkDisabled,
        InvalidArgument,
        UnsupportedPlatform,
        Unknown
    }

    public sealed class GrovsError
    {
        public GrovsErrorCode Code;
        public string Message;

        public override string ToString()
        {
            return Code + ": " + Message;
        }

        internal static GrovsError FromJson(object errorObj)
        {
            var map = errorObj as Dictionary<string, object>;
            if (map == null) return null;
            map.TryGetValue("code", out var codeValue);
            map.TryGetValue("message", out var messageValue);
            var raw = codeValue as string;
            var message = messageValue as string;
            if (!Codes.TryGetValue(raw ?? "", out var code))
            {
                code = GrovsErrorCode.Unknown;
                message = string.IsNullOrEmpty(message) ? raw : raw + ": " + message;
            }
            return new GrovsError { Code = code, Message = message };
        }

        static readonly Dictionary<string, GrovsErrorCode> Codes = new Dictionary<string, GrovsErrorCode>
        {
            { "authentication_failed", GrovsErrorCode.AuthenticationFailed },
            { "network_request_failed", GrovsErrorCode.NetworkRequestFailed },
            { "event_dispatch_failed", GrovsErrorCode.EventDispatchFailed },
            { "link_generation_failed", GrovsErrorCode.LinkGenerationFailed },
            { "in_app_display_failed", GrovsErrorCode.InAppDisplayFailed },
            { "link_details_failed", GrovsErrorCode.LinkDetailsFailed },
            { "notifications_error", GrovsErrorCode.NotificationsError },
            { "sdk_not_initialized", GrovsErrorCode.SdkNotInitialized },
            { "sdk_disabled", GrovsErrorCode.SdkDisabled },
            { "invalid_argument", GrovsErrorCode.InvalidArgument },
            { "unsupported_platform", GrovsErrorCode.UnsupportedPlatform },
            { "unknown", GrovsErrorCode.Unknown }
        };
    }

    public sealed class GrovsException : Exception
    {
        public GrovsError Error;

        public GrovsException(GrovsError error) : base(error != null ? error.Message : null)
        {
            Error = error;
        }
    }
}
