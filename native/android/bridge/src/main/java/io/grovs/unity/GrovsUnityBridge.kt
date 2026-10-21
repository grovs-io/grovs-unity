package io.grovs.unity

import android.app.Activity
import android.app.Application
import android.content.Intent
import android.net.Uri
import android.os.Handler
import android.os.Looper
import android.util.Log
import io.grovs.GrovsInAppListener
import io.grovs.model.CustomLinkRedirect
import io.grovs.model.LogLevel
import io.grovs.model.events.PaymentEventType
import io.grovs.model.exceptions.GrovsErrorCode
import io.grovs.model.exceptions.GrovsException
import io.grovs.service.CustomRedirects
import io.grovs.service.TrackingParams
import io.grovs.utils.InstantCompat

/** Entry points called from Unity C#. Every method is safe to call from any thread. */
object GrovsUnityBridge {
    private const val TAG = "GrovsUnity"

    internal var facade: GrovsFacade = RealGrovsFacade()
    internal val queue = GrovsUnityMessageQueue()
    internal var tracker = GrovsUnityActivityTracker(::onActivityStarted)
        private set

    private val lock = Any()
    private val mainHandler by lazy { Handler(Looper.getMainLooper()) }

    @Volatile private var app: Application? = null
    @Volatile private var config: GrovsUnityConfig? = null
    @Volatile private var initialized = false
    @Volatile private var listener: GrovsUnityListener? = null
    private var enabled = true
    private var launchLinkPending = false

    internal fun start(app: Application, config: GrovsUnityConfig) {
        synchronized(lock) {
            if (initialized) return
            this.app = app
            this.config = config
            enabled = GrovsUnityConsent.stored(app) ?: config.startEnabled
            if (config.apiKey == null) {
                Log.e(TAG, "Missing grovs_api_key meta-data. Grovs stays inactive.")
                return
            }
            facade.configure(app, config, enabled)
            facade.setLogLevel(logLevel(config.logLevel))
            facade.setDeeplinkListener(null) { link, data, tracking ->
                queue.post("deeplink", GrovsUnityCodec.toJson(linkedMapOf("link" to link, "data" to data, "tracking" to tracking)))
            }
            facade.setInAppListener(inAppListener)
            app.registerActivityLifecycleCallbacks(tracker)
            initialized = true
        }
    }

    internal fun resetForTests(fake: GrovsFacade) {
        synchronized(lock) {
            facade = fake
            tracker = GrovsUnityActivityTracker(::onActivityStarted)
            app = null
            config = null
            initialized = false
            listener = null
            enabled = true
            launchLinkPending = false
        }
    }

    private fun onActivityStarted(activity: Activity) {
        if (!initialized) return
        synchronized(lock) {
            if (!enabled) launchLinkPending = true
        }
        facade.onStart(activity)
    }

    @JvmStatic
    fun setListener(listener: GrovsUnityListener) {
        this.listener = listener
        queue.setReady(listener)
    }

    @JvmStatic
    fun setEnabled(enabled: Boolean) {
        val ctx = app ?: return
        GrovsUnityConsent.store(ctx, enabled)
        var replay: Activity? = null
        synchronized(lock) {
            val changed = this.enabled != enabled
            this.enabled = enabled
            if (!initialized || !changed) return
            safely { facade.setEnabled(enabled) }
            if (enabled) {
                if (launchLinkPending) replay = tracker.current
                launchLinkPending = false
            }
        }
        replay?.let { activity -> onMain { facade.onStart(activity) } }
    }

    @JvmStatic
    fun isEnabled(): Boolean = synchronized(lock) { initialized && enabled }

    @JvmStatic
    fun isSceneTrackingEnabled(): Boolean = config?.trackScenes ?: false

    @JvmStatic
    fun setLogLevel(level: Int) = call { facade.setLogLevel(logLevel(level)) }

    @JvmStatic
    fun setUserIdentifier(id: String?) = call { facade.setIdentifier(id) }

    @JvmStatic
    fun setUserAttributes(json: String?) = call { facade.setAttributes(GrovsUnityCodec.parseObject(json)?.let(::withoutNulls)) }

    @JvmStatic
    fun setPushToken(token: String?) = call { facade.setPushToken(token) }

    @JvmStatic
    fun generateLink(requestId: Int, optionsJson: String) = request(requestId) {
        val options = parseOptions(optionsJson)
        if (options == null) {
            postResult(requestId, false, null, "invalid_argument", "Link options are not valid JSON.")
            return@request
        }
        facade.generateLink(
            title = options["title"] as? String,
            subtitle = options["subtitle"] as? String,
            imageUrl = options["imageUrl"] as? String,
            data = (options["data"] as? Map<*, *>)?.let { GrovsUnityCodec.toSerializableMap(stringKeys(it)) },
            tags = (options["tags"] as? List<*>)?.mapNotNull { it?.toString() },
            customRedirects = redirects(options),
            showPreviewIos = options["showPreviewIos"] as? Boolean,
            showPreviewAndroid = options["showPreviewAndroid"] as? Boolean,
            copyToClipboardIos = options["copyToClipboardIos"] as? Boolean,
            copyToClipboardAndroid = options["copyToClipboardAndroid"] as? Boolean,
            tracking = tracking(options)
        ) { link, error ->
            when {
                error != null -> postResult(requestId, false, null, errorCode(error.errorCode), error.message)
                link == null -> postResult(requestId, false, null, "link_generation_failed", "No link was generated.")
                else -> postResult(requestId, true, link, null, null)
            }
        }
    }

    @JvmStatic
    fun linkDetails(requestId: Int, path: String) = request(requestId) {
        facade.linkDetails(path) { details, error ->
            when {
                error != null -> postResult(requestId, false, null, errorCode(error.errorCode), error.message)
                details == null -> postResult(requestId, false, null, "link_details_failed", "No link details.")
                else -> postResult(requestId, true, details, null, null)
            }
        }
    }

    @JvmStatic
    fun handleUrl(url: String): Boolean {
        if (!isEnabled()) return false
        val uri = Uri.parse(url)
        if (uri.scheme.isNullOrEmpty() || uri.host.isNullOrEmpty()) return false
        val intent = Intent(Intent.ACTION_VIEW, uri)
        onMain { facade.onNewIntent(intent, tracker.current) }
        return true
    }

    @JvmStatic
    fun track(name: String, propsJson: String?, tagsJson: String?) = call {
        facade.track(name, GrovsUnityCodec.parseObject(propsJson)?.let(::withoutNulls), GrovsUnityCodec.parseStringList(tagsJson))
    }

    @JvmStatic
    fun trackScreenView(name: String, propsJson: String?) = call {
        facade.trackScreenView(name, GrovsUnityCodec.parseObject(propsJson)?.let(::withoutNulls))
    }

    @JvmStatic
    fun setGlobalTags(tagsJson: String?) = call { facade.setGlobalTags(GrovsUnityCodec.parseStringList(tagsJson)) }

    @JvmStatic
    fun setScreenAliases(json: String) = call {
        val aliases = GrovsUnityCodec.parseObject(json).orEmpty()
            .mapNotNull { (k, v) -> v?.let { k to it.toString() } }
            .toMap()
        facade.setScreenAliases(aliases)
    }

    @JvmStatic
    fun logInAppPurchase(requestId: Int, originalJson: String) = request(requestId) {
        facade.logInAppPurchase(originalJson)
        postResult(requestId, true, true, null, null)
    }

    @JvmStatic
    fun logCustomPurchase(requestId: Int, type: Int, priceInCents: Int, currency: String, productId: String, epochMs: Long) =
        request(requestId) {
            val eventType = when (type) {
                0 -> PaymentEventType.BUY
                1 -> PaymentEventType.CANCEL
                2 -> PaymentEventType.REFUND
                else -> {
                    postResult(requestId, false, null, "invalid_argument", "Unknown transaction type $type.")
                    return@request
                }
            }
            val date = if (epochMs == -1L) null else InstantCompat.ofEpochMilli(epochMs)
            facade.logCustomPurchase(eventType, priceInCents, currency, productId, date)
            postResult(requestId, true, true, null, null)
        }

    @JvmStatic
    fun isGrovsNotification(json: String): Boolean = safely(false) {
        facade.isGrovsNotification(stringValues(json))
    }

    @JvmStatic
    fun handleNotificationOpen(json: String): Boolean {
        if (!initialized) return false
        return safely(false) {
            val intent = Intent()
            stringValues(json).forEach { (k, v) -> intent.putExtra(k, v) }
            facade.handleNotificationOpen(intent)
        }
    }

    @JvmStatic
    fun pauseInApp() = call { facade.pauseInApp() }

    @JvmStatic
    fun resumeInApp() = call { facade.resumeInApp() }

    @JvmStatic
    fun discardInApp(dismissCurrent: Boolean) = call { facade.discardInApp(dismissCurrent) }

    @JvmStatic
    fun setInAppExcludedScreens(json: String) = call {
        facade.setInAppExcludedScreens(GrovsUnityCodec.parseStringList(json).orEmpty())
    }

    @JvmStatic
    fun setInAppDebugLogging(enabled: Boolean) = call { facade.setInAppDebugLogging(enabled) }

    internal fun errorCode(code: GrovsErrorCode?): String = when (code) {
        GrovsErrorCode.LINK_GENERATION_ERROR -> "link_generation_failed"
        GrovsErrorCode.SDK_NOT_INITIALIZED -> "sdk_not_initialized"
        GrovsErrorCode.NOTIFICATIONS_ERROR -> "notifications_error"
        GrovsErrorCode.LINK_DETAILS_ERROR -> "link_details_failed"
        GrovsErrorCode.EVENT_DISPATCH_ERROR -> "event_dispatch_failed"
        null -> "unknown"
    }

    private val inAppListener = object : GrovsInAppListener {
        override fun shouldShowInAppMessage(campaignId: String): Boolean {
            val target = listener ?: return true
            return try {
                target.shouldShowInAppMessage(campaignId)
            } catch (e: Exception) {
                Log.e(TAG, "shouldShowInAppMessage failed, showing the message.", e)
                true
            }
        }

        override fun onInAppMessageShown(campaignId: String) =
            postInApp("shown", campaignId)

        override fun onInAppMessageClicked(campaignId: String, buttonId: String?, payload: Map<String, Any?>?) =
            postInApp("clicked", campaignId, "buttonId" to buttonId, "payload" to payload)

        override fun onInAppMessageDismissed(campaignId: String, reason: String) =
            postInApp("dismissed", campaignId, "reason" to reason)

        override fun onInAppMessageAnswered(campaignId: String, value: Any, comment: String?) =
            postInApp("answered", campaignId, "value" to value, "comment" to comment)

        override fun onInAppMessageFailed(campaignId: String, message: String) =
            postInApp("failed", campaignId, "message" to message)
    }

    private fun postInApp(type: String, campaignId: String, vararg fields: Pair<String, Any?>) {
        val event = linkedMapOf<String, Any?>("type" to type, "campaignId" to campaignId)
        event.putAll(fields)
        queue.post("inapp_event", GrovsUnityCodec.toJson(event))
    }

    private fun postResult(requestId: Int, ok: Boolean, value: Any?, code: String?, message: String?) {
        queue.post("result", GrovsUnityCodec.resultJson(requestId, ok, value, code, message))
    }

    private fun postError(code: String, message: String?) {
        queue.post("error", GrovsUnityCodec.errorJson(code, message))
    }

    private inline fun request(requestId: Int, block: () -> Unit) {
        if (!initialized) {
            postResult(requestId, false, null, "sdk_not_initialized", "Grovs is not configured.")
            return
        }
        if (!synchronized(lock) { enabled }) {
            postResult(requestId, false, null, "sdk_disabled", "Grovs is disabled.")
            return
        }
        try {
            block()
        } catch (e: Exception) {
            val code = if (e is GrovsException) errorCode(e.errorCode) else "unknown"
            postResult(requestId, false, null, code, e.message)
        }
    }

    private inline fun call(block: () -> Unit) {
        if (!initialized) return
        safely(block)
    }

    private inline fun safely(block: () -> Unit) {
        try {
            block()
        } catch (e: Exception) {
            val code = if (e is GrovsException) errorCode(e.errorCode) else "unknown"
            postError(code, e.message)
        }
    }

    private inline fun <T> safely(fallback: T, block: () -> T): T =
        try {
            block()
        } catch (e: Exception) {
            Log.e(TAG, "Grovs call failed.", e)
            fallback
        }

    private fun onMain(block: () -> Unit) {
        val guarded = { safely(block) }
        if (Looper.myLooper() === Looper.getMainLooper()) guarded() else mainHandler.post(guarded)
    }

    private fun logLevel(level: Int): LogLevel = if (level >= 2) LogLevel.ERROR else LogLevel.INFO

    private fun parseOptions(json: String?): Map<String, Any?>? =
        if (json.isNullOrBlank()) emptyMap() else GrovsUnityCodec.parseObject(json)

    private fun redirects(options: Map<String, Any?>): CustomRedirects? {
        val ios = redirect(options["iosRedirect"])
        val android = redirect(options["androidRedirect"])
        val desktop = redirect(options["desktopRedirect"])
        if (ios == null && android == null && desktop == null) return null
        return CustomRedirects(ios = ios, android = android, desktop = desktop)
    }

    private fun redirect(value: Any?): CustomLinkRedirect? {
        val map = value as? Map<*, *> ?: return null
        val url = map["url"] as? String ?: return null
        return CustomLinkRedirect(url, map["openAppIfInstalled"] as? Boolean ?: true)
    }

    private fun tracking(options: Map<String, Any?>): TrackingParams? {
        val campaign = options["trackingCampaign"] as? String
        val source = options["trackingSource"] as? String
        val medium = options["trackingMedium"] as? String
        if (campaign == null && source == null && medium == null) return null
        return TrackingParams(utmCampaign = campaign, utmSource = source, utmMedium = medium)
    }

    private fun stringKeys(map: Map<*, *>): Map<String, Any?> =
        map.entries.associate { (k, v) -> k.toString() to v }

    private fun withoutNulls(map: Map<String, Any?>): Map<String, Any> =
        map.mapNotNull { (k, v) -> v?.let { k to it } }.toMap()

    private fun stringValues(json: String): Map<String, String> =
        GrovsUnityCodec.parseObject(json).orEmpty()
            .mapNotNull { (k, v) -> v?.let { k to it.toString() } }
            .toMap()
}
