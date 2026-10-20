# Called from Unity C# through JNI by name.
-keep class io.grovs.unity.** { *; }

# Native Grovs classes the bridge uses that the native SDK does not keep itself.
-keep class io.grovs.Grovs { *; }
-keep class io.grovs.Grovs$Companion { *; }
-keep class io.grovs.GrovsMessaging { *; }
-keep interface io.grovs.GrovsInAppListener { *; }
-keep interface io.grovs.GrovsDeeplinkListener { *; }
-keep interface io.grovs.GrovsLinkGenerationListener { *; }
-keep interface io.grovs.GrovsLinkDetailsListener { *; }
-keep class io.grovs.handlers.MessagingService { *; }
-keep class io.grovs.service.CustomRedirects { *; }
-keep class io.grovs.service.TrackingParams { *; }
-keep class io.grovs.model.CustomLinkRedirect { *; }
-keep class io.grovs.model.DeeplinkDetails { *; }
-keep class io.grovs.model.LogLevel { *; }
-keep class io.grovs.model.events.PaymentEventType { *; }
-keep class io.grovs.model.exceptions.** { *; }
-keep class io.grovs.utils.InstantCompat { *; }
