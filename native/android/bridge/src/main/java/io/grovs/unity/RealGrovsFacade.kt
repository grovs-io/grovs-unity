package io.grovs.unity

import android.app.Activity
import android.app.Application
import android.content.Intent
import io.grovs.Grovs
import io.grovs.GrovsInAppListener
import io.grovs.model.LogLevel
import io.grovs.model.events.PaymentEventType
import io.grovs.model.exceptions.GrovsException
import io.grovs.service.CustomRedirects
import io.grovs.service.TrackingParams
import io.grovs.utils.InstantCompat
import java.io.Serializable

class RealGrovsFacade : GrovsFacade {
    override fun configure(app: Application, config: GrovsUnityConfig, enabled: Boolean) {
        Grovs.configure(
            application = app,
            apiKey = config.apiKey.orEmpty(),
            useTestEnvironment = config.useTestEnvironment,
            baseURL = config.baseUrl,
            autoTrackScreenViews = false,
            clipboardDomains = config.clipboardDomains,
            enabled = enabled
        )
    }

    override fun setEnabled(enabled: Boolean) = Grovs.setSDK(enabled)

    override fun setLogLevel(level: LogLevel) = Grovs.setDebug(level)

    override fun onStart(activity: Activity?) = Grovs.onStart(activity)

    override fun onNewIntent(intent: Intent, activity: Activity?) = Grovs.onNewIntent(intent, activity)

    override fun setDeeplinkListener(
        activity: Activity?,
        cb: (link: String?, data: Map<String, Any?>?, tracking: Map<String, Any?>?) -> Unit
    ) {
        Grovs.setOnDeeplinkReceivedListener(activity) { details ->
            cb(details.link, details.data, details.tracking)
        }
    }

    override fun setInAppListener(listener: GrovsInAppListener) = Grovs.setInAppListener(listener)

    override fun setIdentifier(id: String?) {
        Grovs.identifier = id
    }

    override fun setAttributes(attributes: Map<String, Any>?) {
        Grovs.attributes = attributes
    }

    override fun setPushToken(token: String?) {
        Grovs.pushToken = token
    }

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
        Grovs.generateLink(
            title = title,
            subtitle = subtitle,
            imageURL = imageUrl,
            data = data,
            tags = tags,
            customRedirects = customRedirects,
            showPreviewIos = showPreviewIos,
            showPreviewAndroid = showPreviewAndroid,
            copyToClipboardIos = copyToClipboardIos,
            copyToClipboardAndroid = copyToClipboardAndroid,
            tracking = tracking,
            lifecycleOwner = null,
            listener = { link, error -> cb(link, error) }
        )
    }

    override fun linkDetails(path: String, cb: (details: Map<String, Any>?, error: GrovsException?) -> Unit) {
        Grovs.linkDetails(path = path, lifecycleOwner = null, listener = { details, error -> cb(details, error) })
    }

    override fun track(name: String, properties: Map<String, Any>?, tags: List<String>?) =
        Grovs.track(name, properties, tags)

    override fun trackScreenView(name: String, properties: Map<String, Any>?) =
        Grovs.trackScreenView(name, properties)

    override fun setGlobalTags(tags: List<String>?) = Grovs.setGlobalTags(tags)

    override fun setScreenAliases(aliases: Map<String, String>) = Grovs.setScreenAliases(aliases)

    override fun logInAppPurchase(originalJson: String) = Grovs.logInAppPurchase(originalJson)

    override fun logCustomPurchase(
        type: PaymentEventType,
        priceInCents: Int,
        currency: String,
        productId: String,
        date: InstantCompat?
    ) = Grovs.logCustomPurchase(type, priceInCents, currency, productId, date ?: InstantCompat.now())

    override fun isGrovsNotification(data: Map<String, String>): Boolean = Grovs.isGrovsNotification(data)

    override fun handleNotificationOpen(intent: Intent): Boolean = Grovs.handleNotificationOpen(intent)

    override fun pauseInApp() = Grovs.pauseInAppMessages()

    override fun resumeInApp() = Grovs.resumeInAppMessages()

    override fun discardInApp(dismissCurrent: Boolean) = Grovs.discardInAppMessages(dismissCurrent)

    override fun setInAppExcludedScreens(screens: List<String>) = Grovs.setInAppExcludedScreens(screens)

    override fun setInAppDebugLogging(enabled: Boolean) = Grovs.setInAppDebugLogging(enabled)
}
