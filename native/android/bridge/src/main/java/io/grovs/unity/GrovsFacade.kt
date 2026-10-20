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

/** Thin seam over the native Grovs SDK so the bridge can be tested without it. */
interface GrovsFacade {
    fun configure(app: Application, config: GrovsUnityConfig, enabled: Boolean)
    fun setEnabled(enabled: Boolean)
    fun setLogLevel(level: LogLevel)

    fun onStart(activity: Activity?)
    fun onNewIntent(intent: Intent, activity: Activity?)
    fun setDeeplinkListener(
        activity: Activity?,
        cb: (link: String?, data: Map<String, Any?>?, tracking: Map<String, Any?>?) -> Unit
    )
    fun setInAppListener(listener: GrovsInAppListener)

    fun setIdentifier(id: String?)
    fun setAttributes(attributes: Map<String, Any>?)
    fun setPushToken(token: String?)

    fun generateLink(
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
    )
    fun linkDetails(path: String, cb: (details: Map<String, Any>?, error: GrovsException?) -> Unit)

    fun track(name: String, properties: Map<String, Any>?, tags: List<String>?)
    fun trackScreenView(name: String, properties: Map<String, Any>?)
    fun setGlobalTags(tags: List<String>?)
    fun setScreenAliases(aliases: Map<String, String>)

    fun logInAppPurchase(originalJson: String)
    /** A null [date] means now. */
    fun logCustomPurchase(type: PaymentEventType, priceInCents: Int, currency: String, productId: String, date: InstantCompat?)

    fun isGrovsNotification(data: Map<String, String>): Boolean
    fun handleNotificationOpen(intent: Intent): Boolean

    fun pauseInApp()
    fun resumeInApp()
    fun discardInApp(dismissCurrent: Boolean)
    fun setInAppExcludedScreens(screens: List<String>)
    fun setInAppDebugLogging(enabled: Boolean)
}
