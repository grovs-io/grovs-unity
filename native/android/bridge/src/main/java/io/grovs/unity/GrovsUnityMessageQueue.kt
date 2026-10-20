package io.grovs.unity

/**
 * Holds messages until the listener is ready, then delivers in arrival order.
 * Types with a limit drop their oldest entry when full. Other types are never dropped.
 */
class GrovsUnityMessageQueue(
    private val limits: Map<String, Int> = mapOf("deeplink" to 20, "inapp_event" to 50, "error" to 20)
) {
    private class Message(val type: String, val json: String)

    private val lock = Any()
    private val pending = ArrayList<Message>()
    private var listener: GrovsUnityListener? = null

    val isReady: Boolean
        get() = synchronized(lock) { listener != null }

    fun post(type: String, json: String) {
        synchronized(lock) {
            val target = listener
            if (target != null) {
                target.onMessage(type, json)
                return
            }
            pending.add(Message(type, json))
            val limit = limits[type] ?: return
            if (pending.count { it.type == type } > limit) {
                pending.removeAt(pending.indexOfFirst { it.type == type })
            }
        }
    }

    fun setReady(listener: GrovsUnityListener) {
        synchronized(lock) {
            this.listener = listener
            val batch = ArrayList(pending)
            pending.clear()
            batch.forEach { listener.onMessage(it.type, it.json) }
        }
    }
}
