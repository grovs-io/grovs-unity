package io.grovs.unity

import android.content.Context
import android.os.Bundle
import com.google.firebase.messaging.RemoteMessage
import org.junit.Assert.assertEquals
import org.junit.Assert.assertSame
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.Robolectric
import org.robolectric.RobolectricTestRunner

@RunWith(RobolectricTestRunner::class)
class GrovsFirebaseUnityServiceTest {
    private val calls = mutableListOf<String>()
    private var seenContext: Context? = null

    private fun message(vararg data: Pair<String, String>): RemoteMessage {
        val bundle = Bundle()
        data.forEach { (key, value) -> bundle.putString(key, value) }
        return RemoteMessage(bundle)
    }

    private fun service(grovsHandles: Boolean): GrovsFirebaseUnityService {
        val service = Robolectric.buildService(GrovsFirebaseUnityService::class.java).create().get()
        service.messaging = { ctx: Context, _: RemoteMessage -> seenContext = ctx; calls.add("grovs"); grovsHandles }
        service.newToken = { calls.add("grovsToken:$it") }
        service.superMessage = { calls.add("super") }
        service.superToken = { calls.add("superToken:$it") }
        return service
    }

    @Test
    fun onMessageReceived_callsGrovsThenSuperAlways() {
        val grovsService = service(grovsHandles = true)
        grovsService.onMessageReceived(message("grovs_campaign_id" to "c1"))
        assertSame(grovsService, seenContext)
        assertEquals(listOf("grovs", "super"), calls)

        calls.clear()
        service(grovsHandles = false).onMessageReceived(message("other" to "x"))
        assertEquals(listOf("grovs", "super"), calls)
    }

    @Test
    fun onNewToken_forwardsToGrovsAndSuper() {
        service(grovsHandles = false).onNewToken("tok")
        assertEquals(listOf("grovsToken:tok", "superToken:tok"), calls)
    }

    @Test
    fun onMessageReceived_grovsThrows_stillCallsSuper() {
        val service = service(grovsHandles = true)
        service.messaging = { _, _ -> throw IllegalStateException("boom") }
        service.onMessageReceived(message("a" to "b"))
        assertEquals(listOf("super"), calls)
    }

    @Test
    fun onNewToken_grovsThrows_stillCallsSuper() {
        val service = service(grovsHandles = true)
        service.newToken = { throw IllegalStateException("boom") }
        service.onNewToken("tok")
        assertEquals(listOf("superToken:tok"), calls)
    }
}
