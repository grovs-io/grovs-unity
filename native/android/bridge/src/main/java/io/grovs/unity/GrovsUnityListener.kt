package io.grovs.unity

/** Receives bridge messages on whatever thread produced them. */
interface GrovsUnityListener {
    fun onMessage(type: String, json: String)
    fun shouldShowInAppMessage(campaignId: String): Boolean
}
