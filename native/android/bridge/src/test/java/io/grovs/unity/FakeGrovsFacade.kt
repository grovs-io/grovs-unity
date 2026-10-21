package io.grovs.unity

import android.app.Activity
import android.app.Application
import android.content.Intent
import io.grovs.GrovsInAppListener
import io.grovs.model.LogLevel
import io.grovs.model.events.PaymentEventType
import io.grovs.model.exceptions.GrovsException
import io.grovs.service.CustomRedirects
import io.grovs.service.TrackingParams
import io.grovs.utils.InstantCompat
import java.io.Serializable

class FakeGrovsFacade : GrovsFacade {
    class LinkCall(
        val title: String?,
        val subtitle: String?,
        val imageUrl: String?,
        val data: HashMap<String, Serializable>?,
        val tags: List<String>?,
        val customRedirects: CustomRedirects?,
        val showPreviewIos: Boolean?,
        val showPreviewAndroid: Boolean?,
        val copyToClipboardIos: Boolean?,
        val copyToClipboardAndroid: Boolean?,
        val tracking: TrackingParams?
    )

    class CustomPurchase(
        val type: PaymentEventType,
        val cents: Int,
        val currency: String,
        val productId: String,
        val date: InstantCompat?
    )

    val configured = mutableListOf<Boolean>()
    val enabledCalls = mutableListOf<Boolean>()
    val logLevels = mutableListOf<LogLevel>()
    val onStarts = mutableListOf<Activity?>()
    val newIntents = mutableListOf<Intent>()
    val linkCalls = mutableListOf<LinkCall>()
    val inAppPurchases = mutableListOf<String>()
    val customPurchases = mutableListOf<CustomPurchase>()
    val linkDetailsCalls = mutableListOf<String>()

    var deeplinkCallback: ((String?, Map<String, Any?>?, Map<String, Any?>?) -> Unit)? = null
    var inApp: GrovsInAppListener? = null

    var linkResult: Pair<String?, GrovsException?> = "https://x.sqd.link/a" to null

    override fun configure(app: Application, config: GrovsUnityConfig, enabled: Boolean) {
        configured.add(enabled)
    }

    override fun setEnabled(enabled: Boolean) {
        enabledCalls.add(enabled)
    }

    override fun setLogLevel(level: LogLevel) {
        logLevels.add(level)
    }

    override fun onStart(activity: Activity?) {
        onStarts.add(activity)
    }

    override fun onNewIntent(intent: Intent, activity: Activity?) {
        newIntents.add(intent)
    }

    override fun setDeeplinkListener(
        activity: Activity?,
        cb: (link: String?, data: Map<String, Any?>?, tracking: Map<String, Any?>?) -> Unit
    ) {
        deeplinkCallback = cb
    }

    override fun setInAppListener(listener: GrovsInAppListener) {
        inApp = listener
    }

    override fun setIdentifier(id: String?) {}
    override fun setAttributes(attributes: Map<String, Any>?) {}
    override fun setPushToken(token: String?) {}

    override fun generateLink(
        title: String?,
        subtitle: String?,
        imageUrl: String?,
        data: HashMap<String, Serializable>?,
        tags: List<String>?,
        customRedirects: CustomRedirects?,
        showPreviewIos: Boolean?,
        showPreviewAndroid: Boolean?,
        copyToClipboardIos: Boolean?,
        copyToClipboardAndroid: Boolean?,
        tracking: TrackingParams?,
        cb: (link: String?, error: GrovsException?) -> Unit
    ) {
        linkCalls.add(
            LinkCall(
                title, subtitle, imageUrl, data, tags, customRedirects,
                showPreviewIos, showPreviewAndroid, copyToClipboardIos, copyToClipboardAndroid, tracking
            )
        )
        cb(linkResult.first, linkResult.second)
    }

    override fun linkDetails(path: String, cb: (details: Map<String, Any>?, error: GrovsException?) -> Unit) {
        linkDetailsCalls.add(path)
        cb(mapOf("path" to path), null)
    }

    override fun track(name: String, properties: Map<String, Any>?, tags: List<String>?) {}
    override fun trackScreenView(name: String, properties: Map<String, Any>?) {}
    override fun setGlobalTags(tags: List<String>?) {}
    override fun setScreenAliases(aliases: Map<String, String>) {}

    override fun logInAppPurchase(originalJson: String) {
        inAppPurchases.add(originalJson)
    }

    override fun logCustomPurchase(
        type: PaymentEventType,
        priceInCents: Int,
        currency: String,
        productId: String,
        date: InstantCompat?
    ) {
        customPurchases.add(CustomPurchase(type, priceInCents, currency, productId, date))
    }

    override fun isGrovsNotification(data: Map<String, String>): Boolean = data.containsKey("grovs_campaign_id")
    override fun handleNotificationOpen(intent: Intent): Boolean = intent.hasExtra("grovs_campaign_id")

    override fun pauseInApp() {}
    override fun resumeInApp() {}
    override fun discardInApp(dismissCurrent: Boolean) {}
    override fun setInAppExcludedScreens(screens: List<String>) {}
    override fun setInAppDebugLogging(enabled: Boolean) {}
}
