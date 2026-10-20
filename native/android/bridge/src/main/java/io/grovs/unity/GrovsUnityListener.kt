package io.grovs.unity

/**
 * Receives bridge messages on whatever thread produced them.
 * Implementations must return quickly and must not block: the message queue calls them while holding its lock.
 */
interface GrovsUnityListener {
    fun onMessage(type: String, json: String)
    fun shouldShowInAppMessage(campaignId: String): Boolean
}
