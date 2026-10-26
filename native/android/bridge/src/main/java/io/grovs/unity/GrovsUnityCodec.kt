package io.grovs.unity

import org.json.JSONArray
import org.json.JSONObject
import java.io.Serializable

object GrovsUnityCodec {
    private const val MAX_DEPTH = 64

    fun toJson(value: Any?): String = StringBuilder().also { write(it, value, 0) }.toString()

    // Values nested deeper than MAX_DEPTH are written as null, so a hostile payload cannot overflow the stack.
    private fun write(out: StringBuilder, value: Any?, depth: Int) {
        when (value) {
            null -> out.append("null")
            is String -> out.append(JSONObject.quote(value))
            is Boolean -> out.append(value.toString())
            is Double -> if (value.isNaN() || value.isInfinite()) out.append("null") else out.append(value.toString())
            is Float -> if (value.isNaN() || value.isInfinite()) out.append("null") else out.append(value.toString())
            is Number -> out.append(value.toString())
            is Map<*, *>, is Iterable<*> -> if (depth == MAX_DEPTH) out.append("null") else writeContainer(out, value, depth + 1)
            else -> out.append(JSONObject.quote(value.toString()))
        }
    }

    private fun writeContainer(out: StringBuilder, value: Any, depth: Int) {
        when (value) {
            is Map<*, *> -> {
                out.append('{')
                var first = true
                for ((k, v) in value) {
                    if (!first) out.append(',')
                    first = false
                    out.append(JSONObject.quote(k.toString())).append(':')
                    write(out, v, depth)
                }
                out.append('}')
            }
            is Iterable<*> -> {
                out.append('[')
                var first = true
                for (v in value) {
                    if (!first) out.append(',')
                    first = false
                    write(out, v, depth)
                }
                out.append(']')
            }
        }
    }

    fun parseObject(json: String?): Map<String, Any?>? {
        if (json.isNullOrBlank()) return null
        return try {
            @Suppress("UNCHECKED_CAST")
            unwrap(JSONObject(json), 1) as Map<String, Any?>
        } catch (e: Exception) {
            null
        }
    }

    fun parseStringList(json: String?): List<String>? {
        if (json.isNullOrBlank()) return null
        return try {
            val array = JSONArray(json)
            (0 until array.length()).map { array.get(it).toString() }
        } catch (e: Exception) {
            null
        }
    }

    private fun unwrap(value: Any?, depth: Int): Any? = when (value) {
        null, JSONObject.NULL -> null
        is JSONObject -> {
            require(depth <= MAX_DEPTH) { "JSON is nested deeper than $MAX_DEPTH levels" }
            val map = LinkedHashMap<String, Any?>()
            for (key in value.keys()) map[key] = unwrap(value.get(key), depth + 1)
            map
        }
        is JSONArray -> {
            require(depth <= MAX_DEPTH) { "JSON is nested deeper than $MAX_DEPTH levels" }
            (0 until value.length()).map { unwrap(value.get(it), depth + 1) }
        }
        else -> value
    }

    fun toSerializableMap(map: Map<String, Any?>): HashMap<String, Serializable> {
        val result = HashMap<String, Serializable>()
        for ((k, v) in map) {
            toSerializable(v)?.let { result[k] = it }
        }
        return result
    }

    private fun toSerializable(value: Any?): Serializable? = when (value) {
        null -> null
        is Map<*, *> -> {
            val m = HashMap<String, Serializable>()
            for ((k, v) in value) toSerializable(v)?.let { m[k.toString()] = it }
            m
        }
        is Iterable<*> -> {
            val l = ArrayList<Serializable>()
            for (v in value) toSerializable(v)?.let { l.add(it) }
            l
        }
        is Serializable -> value
        else -> value.toString()
    }

    fun errorJson(code: String, message: String?): String =
        toJson(linkedMapOf("code" to code, "message" to message))

    fun resultJson(requestId: Int, ok: Boolean, value: Any?, errorCode: String?, errorMessage: String?): String =
        toJson(
            linkedMapOf(
                "requestId" to requestId,
                "ok" to ok,
                "value" to value,
                "error" to errorCode?.let { linkedMapOf("code" to it, "message" to errorMessage) }
            )
        )
}
