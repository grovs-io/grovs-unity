import Foundation

struct GrovsUnityConfig {
    let apiKey: String?
    let useTestEnvironment: Bool
    let baseURL: String?
    let startEnabled: Bool
    let clipboardDomains: [String]?
    let logLevel: Int
    let trackScenes: Bool
    let ownNotificationDelegate: Bool

    private static let logError = 2

    static func from(infoDictionary info: [String: Any]) -> GrovsUnityConfig {
        GrovsUnityConfig(
            apiKey: text(info["GrovsApiKey"]),
            useTestEnvironment: flag(info["GrovsUseTestEnvironment"], default: false),
            baseURL: text(info["GrovsBaseURL"]),
            startEnabled: flag(info["GrovsEnabled"], default: true),
            clipboardDomains: domains(info["GrovsClipboardDomains"]),
            logLevel: logLevel(info["GrovsLogLevel"]),
            trackScenes: flag(info["GrovsUnityTrackScenes"], default: false),
            ownNotificationDelegate: flag(info["GrovsUnityOwnNotificationDelegate"], default: true))
    }

    private static func text(_ value: Any?) -> String? {
        guard let value = value else { return nil }
        let trimmed = "\(value)".trimmingCharacters(in: .whitespacesAndNewlines)
        return trimmed.isEmpty ? nil : trimmed
    }

    private static func flag(_ value: Any?, default fallback: Bool) -> Bool {
        if let number = value as? NSNumber { return number.boolValue }
        switch text(value)?.lowercased() {
        case "true", "yes", "1": return true
        case "false", "no", "0": return false
        default: return fallback
        }
    }

    private static func domains(_ value: Any?) -> [String]? {
        let items: [String]
        if let array = value as? [Any] {
            items = array.map { "\($0)" }
        } else if let string = value as? String {
            items = string.components(separatedBy: ",")
        } else {
            return nil
        }
        let cleaned = items
            .map { $0.trimmingCharacters(in: .whitespacesAndNewlines) }
            .filter { !$0.isEmpty }
        return cleaned.isEmpty ? nil : cleaned
    }

    private static func logLevel(_ value: Any?) -> Int {
        guard let raw = text(value)?.lowercased() else { return logError }
        switch raw {
        case "info", "0": return 0
        case "warn", "warning", "1": return 1
        case "error", "2": return 2
        default: return Int(raw).map { min(max($0, 0), 2) } ?? logError
        }
    }
}

enum GrovsUnityConsent {
    private static let key = "grovs_unity_enabled"

    static func stored(_ defaults: UserDefaults) -> Bool? {
        defaults.object(forKey: key) == nil ? nil : defaults.bool(forKey: key)
    }

    static func store(_ defaults: UserDefaults, _ enabled: Bool) {
        defaults.set(enabled, forKey: key)
    }
}
