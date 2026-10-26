import Foundation

enum GrovsUnityCodec {
    private static let isoFormatter = ISO8601DateFormatter()
    private static let maxDepth = 64

    static func json(_ value: Any?) -> String {
        let safe = sanitize(value, depth: 0)
        guard let data = try? JSONSerialization.data(withJSONObject: safe, options: [.fragmentsAllowed, .sortedKeys]),
              let text = String(data: data, encoding: .utf8) else {
            return "null"
        }
        return text
    }

    static func object(_ json: String?) -> [String: Any]? {
        guard let parsed = parse(json) else { return nil }
        return parsed as? [String: Any]
    }

    static func stringList(_ json: String?) -> [String]? {
        guard let array = parse(json) as? [Any] else { return nil }
        return array.map { element in
            if let string = element as? String { return string }
            if let number = element as? NSNumber, !isBool(number) { return number.stringValue }
            return "\(element)"
        }
    }

    static func errorJSON(code: String, message: String?) -> String {
        "{\"code\":\(json(code)),\"message\":\(json(message))}"
    }

    static func resultJSON(requestId: Int32, ok: Bool, value: Any?, errorCode: String?, errorMessage: String?) -> String {
        let error = errorCode.map { errorJSON(code: $0, message: errorMessage) } ?? "null"
        return "{\"requestId\":\(requestId),\"ok\":\(ok),\"value\":\(json(value)),\"error\":\(error)}"
    }

    private static func parse(_ json: String?) -> Any? {
        guard let json = json,
              !json.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty,
              let data = json.data(using: .utf8) else { return nil }
        return try? JSONSerialization.jsonObject(with: data, options: [.fragmentsAllowed])
    }

    private static func isBool(_ number: NSNumber) -> Bool {
        CFGetTypeID(number) == CFBooleanGetTypeID()
    }

    // Values nested deeper than maxDepth become null, so a hostile payload cannot overflow the stack.
    private static func sanitize(_ value: Any?, depth: Int) -> Any {
        switch value {
        case nil, is NSNull:
            return NSNull()
        case let string as String:
            return string
        case let number as NSNumber:
            if isBool(number) { return number.boolValue }
            let double = number.doubleValue
            return double.isNaN || double.isInfinite ? NSNull() : number
        case let bool as Bool:
            return bool
        case let double as Double:
            return double.isNaN || double.isInfinite ? NSNull() : double
        case let float as Float:
            return float.isNaN || float.isInfinite ? NSNull() : float
        case let int as Int:
            return int
        case let date as Date:
            return isoFormatter.string(from: date)
        case let url as URL:
            return url.absoluteString
        case let dict as [AnyHashable: Any]:
            if depth == maxDepth { return NSNull() }
            var result = [String: Any]()
            for (key, element) in dict { result["\(key)"] = sanitize(element, depth: depth + 1) }
            return result
        case let array as [Any]:
            if depth == maxDepth { return NSNull() }
            return array.map { sanitize($0, depth: depth + 1) }
        default:
            return "\(value!)"
        }
    }
}
