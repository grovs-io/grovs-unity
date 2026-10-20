package io.grovs.unity

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

class GrovsUnityMessageQueueTest {
    private class Recorder : GrovsUnityListener {
        val messages = mutableListOf<Pair<String, String>>()
        override fun onMessage(type: String, json: String) {
            messages.add(type to json)
        }
        override fun shouldShowInAppMessage(campaignId: String) = true
    }

    @Test
    fun queue_buffersUntilReady_thenFlushesInOrder() {
        val q = GrovsUnityMessageQueue()
        val l = Recorder()
        q.post("deeplink", "1")
        q.post("log", "2")
        q.post("error", "3")
        assertFalse(q.isReady)
        assertTrue(l.messages.isEmpty())
        q.setReady(l)
        assertTrue(q.isReady)
        assertEquals(listOf("deeplink" to "1", "log" to "2", "error" to "3"), l.messages)
    }

    @Test
    fun queue_dropsOldestDeeplinkPast20() {
        val q = GrovsUnityMessageQueue()
        val l = Recorder()
        for (i in 1..25) q.post("deeplink", i.toString())
        q.setReady(l)
        assertEquals((6..25).map { it.toString() }, l.messages.map { it.second })
    }

    @Test
    fun queue_dropsOldestInAppEventPast50AndErrorPast20() {
        val q = GrovsUnityMessageQueue()
        val l = Recorder()
        for (i in 1..60) q.post("inapp_event", i.toString())
        for (i in 1..30) q.post("error", i.toString())
        q.setReady(l)
        assertEquals((11..60).map { it.toString() }, l.messages.filter { it.first == "inapp_event" }.map { it.second })
        assertEquals((11..30).map { it.toString() }, l.messages.filter { it.first == "error" }.map { it.second })
    }

    @Test
    fun queue_dropKeepsOtherTypesInOrder() {
        val q = GrovsUnityMessageQueue(mapOf("deeplink" to 2))
        val l = Recorder()
        q.post("deeplink", "a")
        q.post("result", "r")
        q.post("deeplink", "b")
        q.post("deeplink", "c")
        q.setReady(l)
        assertEquals(listOf("result" to "r", "deeplink" to "b", "deeplink" to "c"), l.messages)
    }

    @Test
    fun queue_neverDropsResults() {
        val q = GrovsUnityMessageQueue()
        val l = Recorder()
        for (i in 1..100) q.post("result", i.toString())
        q.setReady(l)
        assertEquals(100, l.messages.size)
    }

    @Test
    fun queue_afterReady_deliversImmediately() {
        val q = GrovsUnityMessageQueue()
        val l = Recorder()
        q.setReady(l)
        q.post("deeplink", "x")
        assertEquals(listOf("deeplink" to "x"), l.messages)
    }
}
