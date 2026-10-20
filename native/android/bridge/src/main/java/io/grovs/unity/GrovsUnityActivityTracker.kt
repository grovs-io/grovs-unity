package io.grovs.unity

import android.app.Activity
import android.app.Application
import android.os.Bundle
import java.lang.ref.WeakReference

/** Remembers the visible activity and reports every activity start. */
class GrovsUnityActivityTracker(
    private val onStarted: (Activity) -> Unit
) : Application.ActivityLifecycleCallbacks {
    @Volatile
    private var currentRef: WeakReference<Activity>? = null

    val current: Activity?
        get() = currentRef?.get()

    override fun onActivityStarted(activity: Activity) {
        currentRef = WeakReference(activity)
        onStarted(activity)
    }

    override fun onActivityResumed(activity: Activity) {
        currentRef = WeakReference(activity)
    }

    override fun onActivityDestroyed(activity: Activity) {
        if (current === activity) currentRef = null
    }

    override fun onActivityCreated(activity: Activity, savedInstanceState: Bundle?) {}
    override fun onActivityPaused(activity: Activity) {}
    override fun onActivityStopped(activity: Activity) {}
    override fun onActivitySaveInstanceState(activity: Activity, outState: Bundle) {}
}
