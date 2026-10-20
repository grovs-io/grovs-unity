import Foundation
import Grovs
import UserNotifications
@testable import GrovsUnityBridge

final class FakeGrovsFacade: GrovsFacade {
    struct Configure {
        let apiKey: String
        let useTestEnvironment: Bool
        let baseURL: String?
        let clipboardDomains: [String]?
        let enabled: Bool
    }

    struct CustomPurchase {
        let type: TransactionType
        let priceInCents: Int
        let currency: String
        let productID: String
        let startDate: Date?
    }

    var configured: [Configure] = []
    weak var delegate: GrovsDelegate?
    weak var inAppDelegate: GrovsInAppDelegate?
    var enabledCalls: [Bool] = []
    var logLevels: [LogLevel] = []
    var identifiers: [String?] = []
    var attributes: [[String: Any]?] = []
    var pushTokens: [String?] = []
    var deviceTokens: [Data] = []
    var linkRequests: [GrovsLinkRequest] = []
    var linkResult: URL? = URL(string: "https://x.sqd.link/a")
    var linkDetailsPaths: [String] = []
    var linkDetailsResult: (String) -> [String: Any]? = { ["path": $0] }
    var opened: [URL] = []
    var continued: [NSUserActivity] = []
    var tracked: [(name: String, properties: [String: Any]?, tags: [String]?)] = []
    var screenViews: [(name: String, properties: [String: Any]?)] = []
    var globalTags: [[String]?] = []
    var screenAliases: [[String: String]] = []
    var inAppPurchases: [UInt64] = []
    var inAppPurchaseResult = true
    var customPurchases: [CustomPurchase] = []
    var customPurchaseResult = true
    var notificationOpens: [[AnyHashable: Any]] = []
    var notificationResponses: [UNNotificationResponse] = []
    var inAppCalls: [String] = []
    var excludedScreens: [[String]] = []
    var debugLogging: [Bool] = []

    func configure(apiKey: String, useTestEnvironment: Bool, baseURL: String?, clipboardDomains: [String]?,
                   enabled: Bool, delegate: GrovsDelegate) {
        configured.append(Configure(apiKey: apiKey, useTestEnvironment: useTestEnvironment, baseURL: baseURL,
                                    clipboardDomains: clipboardDomains, enabled: enabled))
        self.delegate = delegate
    }

    func setInAppDelegate(_ delegate: GrovsInAppDelegate) { inAppDelegate = delegate }
    func setEnabled(_ enabled: Bool) { enabledCalls.append(enabled) }
    func setLogLevel(_ level: LogLevel) { logLevels.append(level) }
    func setUserIdentifier(_ identifier: String?) { identifiers.append(identifier) }
    func setUserAttributes(_ attributes: [String: Any]?) { self.attributes.append(attributes) }
    func setPushToken(_ token: String?) { pushTokens.append(token) }
    func setPushToken(deviceToken: Data) { deviceTokens.append(deviceToken) }

    func generateLink(_ request: GrovsLinkRequest, completion: @escaping (URL?) -> Void) {
        linkRequests.append(request)
        completion(linkResult)
    }

    func linkDetails(path: String, completion: @escaping ([String: Any]?) -> Void) {
        linkDetailsPaths.append(path)
        completion(linkDetailsResult(path))
    }

    func handleOpen(url: URL) -> Bool {
        opened.append(url)
        return true
    }

    func handleContinue(userActivity: NSUserActivity) -> Bool {
        continued.append(userActivity)
        return true
    }

    func track(_ name: String, properties: [String: Any]?, tags: [String]?) {
        tracked.append((name, properties, tags))
    }

    func trackScreenView(_ name: String, properties: [String: Any]?) { screenViews.append((name, properties)) }
    func setGlobalTags(_ tags: [String]?) { globalTags.append(tags) }
    func setScreenAliases(_ aliases: [String: String]) { screenAliases.append(aliases) }

    func logInAppPurchase(transactionID: UInt64, completion: @escaping (Bool) -> Void) {
        inAppPurchases.append(transactionID)
        completion(inAppPurchaseResult)
    }

    func logCustomPurchase(type: TransactionType, priceInCents: Int, currency: String, productID: String,
                           startDate: Date?, completion: @escaping (Bool) -> Void) {
        customPurchases.append(CustomPurchase(type: type, priceInCents: priceInCents, currency: currency,
                                              productID: productID, startDate: startDate))
        completion(customPurchaseResult)
    }

    func isGrovsNotification(userInfo: [AnyHashable: Any]) -> Bool { userInfo["grovs_campaign_id"] != nil }

    func handleNotificationOpen(userInfo: [AnyHashable: Any]) -> Bool {
        notificationOpens.append(userInfo)
        return userInfo["grovs_campaign_id"] != nil
    }

    func handleNotificationResponse(_ response: UNNotificationResponse) -> Bool {
        notificationResponses.append(response)
        return true
    }

    func pauseInApp() { inAppCalls.append("pause") }
    func resumeInApp() { inAppCalls.append("resume") }
    func discardInApp(dismissCurrent: Bool) { inAppCalls.append("discard:\(dismissCurrent)") }
    func setInAppExcludedScreens(_ screens: [String]) { excludedScreens.append(screens) }
    func setInAppDebugLogging(_ enabled: Bool) { debugLogging.append(enabled) }
}
