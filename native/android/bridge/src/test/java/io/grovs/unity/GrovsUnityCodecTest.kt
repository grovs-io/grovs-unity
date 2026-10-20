package io.grovs.unity

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test
import java.util.ArrayList
import java.util.HashMap

class GrovsUnityCodecTest {
    @Test
    fun codec_doubleWholeStaysParseableAsLong() {
        assertEquals("{\"level\":3.0}", GrovsUnityCodec.toJson(mapOf("level" to 3.0)))
    }

    @Test
    fun codec_nestedRoundTrip() {
        val json = "{\"id\": 9007199254740993, \"n\": 1.5, \"nested\": {\"a\": [1, \"x\", null]}, \"emoji\": \"🎮\"}"
        val parsed = GrovsUnityCodec.parseObject(json)!!
        val again = GrovsUnityCodec.parseObject(GrovsUnityCodec.toJson(parsed))!!
        assertEquals(9007199254740993L, (again["id"] as Number).toLong())
        assertEquals(1.5, (again["n"] as Number).toDouble(), 0.0)
        assertEquals("🎮", again["emoji"])
        val a = (again["nested"] as Map<*, *>)["a"] as List<*>
        assertEquals(3, a.size)
        assertEquals(1, (a[0] as Number).toInt())
        assertEquals("x", a[1])
        assertEquals(null, a[2])
    }

    @Test
    fun codec_toSerializableMap_nestedBecomesHashMapAndArrayList() {
        val result = GrovsUnityCodec.toSerializableMap(
            mapOf("m" to mapOf("k" to listOf(1, mapOf("z" to "y"))), "s" to "v", "n" to null)
        )
        assertTrue(result["m"] is HashMap<*, *>)
        val list = (result["m"] as HashMap<*, *>)["k"]
        assertTrue(list is ArrayList<*>)
        assertTrue((list as ArrayList<*>)[1] is HashMap<*, *>)
        assertEquals("v", result["s"])
        assertFalse(result.containsKey("n"))
    }

    @Test
    fun codec_resultJson_shape() {
        assertEquals(
            "{\"requestId\":7,\"ok\":false,\"value\":null,\"error\":{\"code\":\"link_generation_failed\",\"message\":\"boom\"}}",
            GrovsUnityCodec.resultJson(7, false, null, "link_generation_failed", "boom")
        )
    }

    @Test
    fun codec_resultJson_okHasNullError() {
        assertEquals(
            "{\"requestId\":1,\"ok\":true,\"value\":\"x\",\"error\":null}",
            GrovsUnityCodec.resultJson(1, true, "x", null, null)
        )
    }

    @Test
    fun codec_parseStringList() {
        assertEquals(listOf("a", "b"), GrovsUnityCodec.parseStringList("[\"a\",\"b\"]"))
        assertEquals(null, GrovsUnityCodec.parseStringList(null))
        assertEquals(null, GrovsUnityCodec.parseStringList("nope"))
    }

    @Test
    fun codec_parseObject_invalidIsNull() {
        assertEquals(null, GrovsUnityCodec.parseObject(""))
        assertEquals(null, GrovsUnityCodec.parseObject("[1]"))
    }

    @Test
    fun codec_errorJson() {
        assertEquals("{\"code\":\"unknown\",\"message\":null}", GrovsUnityCodec.errorJson("unknown", null))
    }
}
