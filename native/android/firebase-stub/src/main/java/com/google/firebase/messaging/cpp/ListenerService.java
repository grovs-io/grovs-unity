package com.google.firebase.messaging.cpp;

import androidx.annotation.NonNull;

import com.google.firebase.messaging.FirebaseMessagingService;
import com.google.firebase.messaging.RemoteMessage;

/**
 * Compile-time stand-in for the Firebase Unity class of the same name. Never packaged: the
 * real class comes from the Firebase Unity messaging plugin.
 */
public class ListenerService extends FirebaseMessagingService {
    @Override
    public void onMessageReceived(@NonNull RemoteMessage message) {
        super.onMessageReceived(message);
    }

    @Override
    public void onNewToken(@NonNull String token) {
        super.onNewToken(token);
    }
}
