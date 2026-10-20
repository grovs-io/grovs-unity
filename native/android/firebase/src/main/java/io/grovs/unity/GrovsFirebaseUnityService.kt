package io.grovs.unity

import android.content.Context
import com.google.firebase.messaging.RemoteMessage
import com.google.firebase.messaging.cpp.ListenerService
import io.grovs.GrovsMessaging

/**
 * Receives FCM messages when the Firebase Unity messaging plugin is present. Grovs pushes are
 * shown here, and every message still reaches the Firebase Unity listener so the C# events fire.
 */
open class GrovsFirebaseUnityService : ListenerService() {

    internal var messaging: (Context, RemoteMessage) -> Boolean = GrovsMessaging::handleMessage
    internal var newToken: (String) -> Unit = GrovsMessaging::onNewToken
    internal var superMessage: (RemoteMessage) -> Unit = { super.onMessageReceived(it) }
    internal var superToken: (String) -> Unit = { super.onNewToken(it) }

    override fun onMessageReceived(message: RemoteMessage) {
        messaging(this, message)
        superMessage(message)
    }

    override fun onNewToken(token: String) {
        newToken(token)
        superToken(token)
    }
}
