package io.grovs.unity

import android.app.Activity
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.Robolectric
import org.robolectric.RobolectricTestRunner

@RunWith(RobolectricTestRunner::class)
class GrovsUnityActivityTrackerTest {
    @Test
    fun tracker_onActivityStarted_callsOnStart() {
        val fake = FakeGrovsFacade()
        val tracker = GrovsUnityActivityTracker { fake.onStart(it) }
        val activity = Robolectric.buildActivity(Activity::class.java).get()

        tracker.onActivityStarted(activity)

        assertEquals(listOf<Activity?>(activity), fake.onStarts)
        assertEquals(activity, tracker.current)
    }

    @Test
    fun tracker_destroyedActivity_clearsCurrent() {
        val tracker = GrovsUnityActivityTracker { }
        val activity = Robolectric.buildActivity(Activity::class.java).get()
        tracker.onActivityStarted(activity)
        tracker.onActivityDestroyed(activity)
        assertNull(tracker.current)
    }
}
