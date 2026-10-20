package io.grovs.unity

import org.json.JSONArray
import org.json.JSONObject
import java.io.Serializable

object GrovsUnityCodec {
    fun toJson(value: Any?): String = StringBuilder().also { write(it, value) }.toString()

    private fun write(out: StringBuilder, value: Any?) {
        when (value) {
            null -> out.append("null")
            is String -> out.append(JSONObject.quote(value))
            is Boolean -> out.append(value.toString())
            is Double -> if (value.isNaN() || value.isInfinite()) out.append("null") else out.append(value.toString())
            is Float -> if (value.isNaN() || value.isInfinite()) out.append("null") else out.append(value.toString())
            is Number -> out.append(value.toString())
            is Map<*, *> -> {
                out.append('{')
                var first = true
                for ((k, v) in value) {
                    if (!first) out.append(',')
                    first = false
                    out.append(JSONObject.quote(k.toString())).append(':')
                    write(out, v)
                }
                out.append('}')
            }
            is Iterable<*> -> {
                out.append('[')
                var first = true
                for (v in value) {
                    if (!first) out.append(',')
                    first = false
                    write(out, v)
                }
                out.append(']')
            }
            else -> out.append(JSONObject.quote(value.toString()))
        }
    }

    fun parseObject(json: String?): Map<String, Any?>? {
        if (json.isNullOrBlank()) return null
        return try {
            @Suppress("UNCHECKED_CAST")
            unwrap(JSONObject(json)) as Map<String, Any?>
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

    private fun unwrap(value: Any?): Any? = when (value) {
        null, JSONObject.NULL -> null
        is JSONObject -> {
            val map = LinkedHashMap<String, Any?>()
            for (key in value.keys()) map[key] = unwrap(value.get(key))
            map
        }
        is JSONArray -> (0 until value.length()).map { unwrap(value.get(it)) }
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
