package io.grovs.unity

import android.app.Activity
import android.app.Application
import android.content.Context
import android.content.Intent
import android.net.Uri
import androidx.test.core.app.ApplicationProvider
import io.grovs.model.LogLevel
import io.grovs.model.events.PaymentEventType
import io.grovs.model.exceptions.GrovsErrorCode
import io.grovs.model.exceptions.GrovsException
import org.json.JSONObject
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.Robolectric
import org.robolectric.RobolectricTestRunner

@RunWith(RobolectricTestRunner::class)
class GrovsUnityBridgeTest {
    private class Recorder : GrovsUnityListener {
        val messages = mutableListOf<Pair<String, String>>()
        var answer: (String) -> Boolean = { false }
        override fun onMessage(type: String, json: String) {
            messages.add(type to json)
        }
        override fun shouldShowInAppMessage(campaignId: String) = answer(campaignId)

        fun of(type: String) = messages.filter { it.first == type }.map { JSONObject(it.second) }
    }

    private lateinit var app: Application
    private lateinit var fake: FakeGrovsFacade
    private lateinit var listener: Recorder

    @Before
    fun setUp() {
        app = ApplicationProvider.getApplicationContext()
        app.getSharedPreferences("grovs_unity", Context.MODE_PRIVATE).edit().clear().commit()
        fake = FakeGrovsFacade()
        GrovsUnityBridge.resetForTests(fake)
        listener = Recorder()
        GrovsUnityBridge.setListener(listener)
    }

    private fun config(apiKey: String? = "key", startEnabled: Boolean = true) =
        GrovsUnityConfig(apiKey, false, null, startEnabled, null, 2, false)

    private fun activity(): Activity = Robolectric.buildActivity(Activity::class.java).get()

    @Test
    fun init_missingApiKey_staysInert_andCallsReportNotInitialized() {
        GrovsUnityBridge.start(app, config(apiKey = null))
        GrovsUnityBridge.generateLink(1, "{}")
        val result = listener.of("result").single()
        assertEquals(1, result.getInt("requestId"))
        assertFalse(result.getBoolean("ok"))
        assertEquals("sdk_not_initialized", result.getJSONObject("error").getString("code"))
        assertTrue(fake.configured.isEmpty())
    }

    @Test
    fun init_usesStoredConsentOverMetaData() {
        GrovsUnityConsent.store(app, true)
        GrovsUnityBridge.start(app, config(startEnabled = false))
        assertEquals(listOf(true), fake.configured)
        assertTrue(GrovsUnityBridge.isEnabled())
    }

    @Test
    fun generateLink_mapsAllOptions() {
        GrovsUnityBridge.start(app, config())
        val json = """
            {"title":"t","subtitle":"s","imageUrl":"https://img","data":{"level":3,"name":"n","nested":{"a":true}},
             "tags":["a","b"],
             "iosRedirect":{"url":"https://ios","openAppIfInstalled":false},
             "androidRedirect":{"url":"https://android","openAppIfInstalled":true},
             "desktopRedirect":{"url":"https://desktop"},
             "showPreviewIos":true,"showPreviewAndroid":false,
             "copyToClipboardIos":false,"copyToClipboardAndroid":true,
             "trackingCampaign":"c","trackingSource":"src","trackingMedium":"m"}
        """.trimIndent()
        GrovsUnityBridge.generateLink(5, json)

        val call = fake.linkCalls.single()
        assertEquals("t", call.title)
        assertEquals("s", call.subtitle)
        assertEquals("https://img", call.imageUrl)
        assertEquals(listOf("a", "b"), call.tags)
        val data: HashMap<String, java.io.Serializable> = call.data!!
        assertEquals(3, data["level"])
        assertEquals("n", data["name"])
        assertEquals(true, (data["nested"] as Map<*, *>)["a"])
        val redirects = call.customRedirects!!
        assertEquals("https://ios", redirects.ios!!.link)
        assertFalse(redirects.ios!!.openAppIfInstalled)
        assertEquals("https://android", redirects.android!!.link)
        assertTrue(redirects.android!!.openAppIfInstalled)
        assertEquals("https://desktop", redirects.desktop!!.link)
        assertTrue(redirects.desktop!!.openAppIfInstalled)
        assertEquals(true, call.showPreviewIos)
        assertEquals(false, call.showPreviewAndroid)
        assertEquals(false, call.copyToClipboardIos)
        assertEquals(true, call.copyToClipboardAndroid)
        assertEquals("c", call.tracking!!.utmCampaign)
        assertEquals("src", call.tracking.utmSource)
        assertEquals("m", call.tracking.utmMedium)

        val result = listener.of("result").single()
        assertEquals(5, result.getInt("requestId"))
        assertTrue(result.getBoolean("ok"))
        assertEquals("https://x.sqd.link/a", result.getString("value"))
    }

    @Test
    fun generateLink_emptyOptions_passesNulls() {
        GrovsUnityBridge.start(app, config())
        GrovsUnityBridge.generateLink(2, "{}")
        val call = fake.linkCalls.single()
        assertNull(call.title)
        assertNull(call.data)
        assertNull(call.customRedirects)
        assertNull(call.tracking)
        assertNull(call.showPreviewIos)
    }

    @Test
    fun generateLink_immediateError_postsResultNotThrow() {
        GrovsUnityBridge.start(app, config())
        fake.linkResult = null to GrovsException("boom", GrovsErrorCode.LINK_GENERATION_ERROR)
        GrovsUnityBridge.generateLink(3, "{}")
        val result = listener.of("result").single()
        assertFalse(result.getBoolean("ok"))
        assertEquals("link_generation_failed", result.getJSONObject("error").getString("code"))
        assertEquals("boom", result.getJSONObject("error").getString("message"))
    }

    private fun assertDisabledResult(requestId: Int) {
        val result = listener.of("result").single()
        assertEquals(requestId, result.getInt("requestId"))
        assertFalse(result.getBoolean("ok"))
        assertEquals("sdk_disabled", result.getJSONObject("error").getString("code"))
    }

    @Test
    fun generateLink_whileDisabled_failsWithSdkDisabled() {
        GrovsUnityBridge.start(app, config(startEnabled = false))
        GrovsUnityBridge.generateLink(20, "{}")
        assertDisabledResult(20)
        assertTrue(fake.linkCalls.isEmpty())
    }

    @Test
    fun linkDetails_whileDisabled_failsWithSdkDisabled() {
        GrovsUnityBridge.start(app, config())
        GrovsUnityBridge.setEnabled(false)
        GrovsUnityBridge.linkDetails(21, "abc")
        assertDisabledResult(21)
        assertTrue(fake.linkDetailsCalls.isEmpty())
    }

    @Test
    fun logInAppPurchase_whileDisabled_failsWithSdkDisabled() {
        GrovsUnityBridge.start(app, config(startEnabled = false))
        GrovsUnityBridge.logInAppPurchase(22, "{\"orderId\":\"o\"}")
        assertDisabledResult(22)
        assertTrue(fake.inAppPurchases.isEmpty())
    }

    @Test
    fun logCustomPurchase_whileDisabled_failsWithSdkDisabled() {
        GrovsUnityBridge.start(app, config())
        GrovsUnityBridge.setEnabled(false)
        GrovsUnityBridge.logCustomPurchase(23, 0, 499, "USD", "p1", -1)
        assertDisabledResult(23)
        assertTrue(fake.customPurchases.isEmpty())
    }

    @Test
    fun errorCodeMapping() {
        assertEquals("sdk_not_initialized", GrovsUnityBridge.errorCode(GrovsErrorCode.SDK_NOT_INITIALIZED))
        assertEquals("link_details_failed", GrovsUnityBridge.errorCode(GrovsErrorCode.LINK_DETAILS_ERROR))
        assertEquals("event_dispatch_failed", GrovsUnityBridge.errorCode(GrovsErrorCode.EVENT_DISPATCH_ERROR))
        assertEquals("notifications_error", GrovsUnityBridge.errorCode(GrovsErrorCode.NOTIFICATIONS_ERROR))
        assertEquals("link_generation_failed", GrovsUnityBridge.errorCode(GrovsErrorCode.LINK_GENERATION_ERROR))
        assertEquals("unknown", GrovsUnityBridge.errorCode(null))
    }

    @Test
    fun deeplink_postsMessage() {
        GrovsUnityBridge.start(app, config())
        fake.deeplinkCallback!!("https://x.sqd.link/a", mapOf("level" to 3.0), mapOf("campaign" to "c"))
        val msg = listener.of("deeplink").single()
        assertEquals("https://x.sqd.link/a", msg.getString("link"))
        assertEquals(3.0, msg.getJSONObject("data").getDouble("level"), 0.0)
        assertEquals("c", msg.getJSONObject("tracking").getString("campaign"))
    }

    @Test
    fun inApp_allSixCallbacks_mapToEvents() {
        GrovsUnityBridge.start(app, config())
        val l = fake.inApp!!
        listener.answer = { it == "c0" }
        assertTrue(l.shouldShowInAppMessage("c0"))
        assertFalse(l.shouldShowInAppMessage("other"))
        l.onInAppMessageShown("c1")
        l.onInAppMessageClicked("c2", "b1", mapOf("k" to "v"))
        l.onInAppMessageDismissed("c3", "backdrop")
        l.onInAppMessageAnswered("c4", 4, "ok")
        l.onInAppMessageFailed("c5", "no view")

        val events = listener.of("inapp_event")
        assertEquals(listOf("shown", "clicked", "dismissed", "answered", "failed"), events.map { it.getString("type") })
        assertEquals(listOf("c1", "c2", "c3", "c4", "c5"), events.map { it.getString("campaignId") })
        assertEquals("b1", events[1].getString("buttonId"))
        assertEquals("v", events[1].getJSONObject("payload").getString("k"))
        assertEquals("backdrop", events[2].getString("reason"))
        assertEquals(4, events[3].getInt("value"))
        assertEquals("ok", events[3].getString("comment"))
        assertEquals("no view", events[4].getString("message"))
        val error = listener.of("error").single()
        assertEquals("in_app_display_failed", error.getString("code"))
        assertEquals("no view", error.getString("message"))
        assertEquals("error", listener.messages.last().first)
    }

    @Test
    fun shouldShow_noListener_returnsTrue() {
        GrovsUnityBridge.resetForTests(fake)
        GrovsUnityBridge.start(app, config())
        assertTrue(fake.inApp!!.shouldShowInAppMessage("c"))
    }

    @Test
    fun shouldShow_listenerThrows_returnsTrue() {
        GrovsUnityBridge.start(app, config())
        listener.answer = { throw IllegalStateException("veto failed") }
        assertTrue(fake.inApp!!.shouldShowInAppMessage("c"))
    }

    @Test
    fun consent_enableTwice_replaysLaunchOnce() {
        GrovsUnityBridge.start(app, config(startEnabled = false))
        val a = activity()
        GrovsUnityBridge.tracker.onActivityStarted(a)
        val before = fake.onStarts.size

        GrovsUnityBridge.setEnabled(true)
        GrovsUnityBridge.setEnabled(true)

        assertEquals(before + 1, fake.onStarts.size)
        assertEquals(a, fake.onStarts.last())
        assertTrue(GrovsUnityBridge.isEnabled())
    }

    @Test
    fun consent_disableThenEnable_noActivity_noReplay() {
        GrovsUnityBridge.start(app, config())
        GrovsUnityBridge.setEnabled(false)
        GrovsUnityBridge.setEnabled(true)
        assertTrue(fake.onStarts.isEmpty())
        assertEquals(listOf(false, true), fake.enabledCalls)
    }

    @Test
    fun consent_isPersisted() {
        GrovsUnityBridge.start(app, config())
        GrovsUnityBridge.setEnabled(false)
        assertEquals(false, GrovsUnityConsent.stored(app))
        assertFalse(GrovsUnityBridge.isEnabled())
    }

    @Test
    fun handleUrl_https_callsOnNewIntentWithViewIntent() {
        GrovsUnityBridge.start(app, config())
        assertTrue(GrovsUnityBridge.handleUrl("https://x.sqd.link/a"))
        val intent = fake.newIntents.single()
        assertEquals(Intent.ACTION_VIEW, intent.action)
        assertEquals(Uri.parse("https://x.sqd.link/a"), intent.data)

        GrovsUnityBridge.setEnabled(false)
        assertFalse(GrovsUnityBridge.handleUrl("https://x.sqd.link/b"))
        assertEquals(1, fake.newIntents.size)
    }

    @Test
    fun logCustomPurchase_typeAndDate() {
        GrovsUnityBridge.start(app, config())
        GrovsUnityBridge.logCustomPurchase(9, 1, 499, "USD", "p1", -1)
        GrovsUnityBridge.logCustomPurchase(10, 2, 100, "EUR", "p2", 1767225600000L)
        val first = fake.customPurchases[0]
        assertEquals(PaymentEventType.CANCEL, first.type)
        assertEquals(499, first.cents)
        assertEquals("USD", first.currency)
        assertEquals("p1", first.productId)
        assertNull(first.date)
        assertEquals(PaymentEventType.REFUND, fake.customPurchases[1].type)
        assertEquals(1767225600000L, fake.customPurchases[1].date!!.epochMillis)
        val results = listener.of("result")
        assertTrue(results.all { it.getBoolean("ok") && it.getBoolean("value") })
    }

    @Test
    fun logInAppPurchase_resolvesTrueOnHandOff() {
        GrovsUnityBridge.start(app, config())
        GrovsUnityBridge.logInAppPurchase(7, "{\"orderId\":\"o\"}")
        assertEquals(listOf("{\"orderId\":\"o\"}"), fake.inAppPurchases)
        val result = listener.of("result").single()
        assertEquals(7, result.getInt("requestId"))
        assertTrue(result.getBoolean("ok"))
        assertTrue(result.getBoolean("value"))
    }

    @Test
    fun logLevel_warnMapsToInfo() {
        GrovsUnityBridge.start(app, config())
        GrovsUnityBridge.setLogLevel(1)
        assertEquals(LogLevel.INFO, fake.logLevels.last())
        GrovsUnityBridge.setLogLevel(2)
        assertEquals(LogLevel.ERROR, fake.logLevels.last())
    }

    @Test
    fun linkDetails_postsDetails() {
        GrovsUnityBridge.start(app, config())
        GrovsUnityBridge.linkDetails(4, "abc")
        val result = listener.of("result").single()
        assertTrue(result.getBoolean("ok"))
        assertEquals("abc", result.getJSONObject("value").getString("path"))
    }

    @Test
    fun pushHelpers_useDataAsMapAndExtras() {
        GrovsUnityBridge.start(app, config())
        assertTrue(GrovsUnityBridge.isGrovsNotification("{\"grovs_campaign_id\":\"c\"}"))
        assertFalse(GrovsUnityBridge.isGrovsNotification("{\"other\":\"x\"}"))
        assertTrue(GrovsUnityBridge.handleNotificationOpen("{\"grovs_campaign_id\":\"c\"}"))
    }
}
