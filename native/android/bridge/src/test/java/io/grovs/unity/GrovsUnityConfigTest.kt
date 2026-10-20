package io.grovs.unity

import android.content.Context
import android.os.Bundle
import androidx.test.core.app.ApplicationProvider
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner

@RunWith(RobolectricTestRunner::class)
class GrovsUnityConfigTest {
    @Test
    fun config_fromMetaData_parsesAll() {
        val b = Bundle().apply {
            putString("grovs_api_key", "key")
            putBoolean("grovs_use_test_environment", true)
            putString("grovs_base_url", "https://x.test")
            putBoolean("grovs_enabled", false)
            putString("grovs_clipboard_domains", "a.com, b.com")
            putString("grovs_log_level", "warn")
            putBoolean("grovs_unity_track_scenes", true)
        }
        val c = GrovsUnityConfig.fromMetaData(b)
        assertEquals("key", c.apiKey)
        assertTrue(c.useTestEnvironment)
        assertEquals("https://x.test", c.baseUrl)
        assertFalse(c.startEnabled)
        assertEquals(listOf("a.com", "b.com"), c.clipboardDomains)
        assertEquals(1, c.logLevel)
        assertTrue(c.trackScenes)
    }

    @Test
    fun config_defaults() {
        val c = GrovsUnityConfig.fromMetaData(Bundle())
        assertTrue(c.startEnabled)
        assertFalse(c.useTestEnvironment)
        assertNull(c.baseUrl)
        assertNull(c.clipboardDomains)
        assertEquals(2, c.logLevel)
        assertFalse(c.trackScenes)
    }

    @Test
    fun config_missingApiKey_isNull() {
        assertNull(GrovsUnityConfig.fromMetaData(Bundle()).apiKey)
        assertNull(GrovsUnityConfig.fromMetaData(null).apiKey)
    }

    @Test
    fun consent_storeAndRead() {
        val ctx = ApplicationProvider.getApplicationContext<Context>()
        assertNull(GrovsUnityConsent.stored(ctx))
        GrovsUnityConsent.store(ctx, false)
        assertEquals(false, GrovsUnityConsent.stored(ctx))
        GrovsUnityConsent.store(ctx, true)
        assertEquals(true, GrovsUnityConsent.stored(ctx))
    }
}
