import Foundation
import Grovs
import UserNotifications

struct GrovsLinkRequest {
    var title: String?
    var subtitle: String?
    var imageURL: String?
    var data: [String: Any]?
    var tags: [String]?
    var customRedirects: CustomRedirects?
    var showPreviewiOS: Bool?
    var showPreviewAndroid: Bool?
    var copyToClipboardiOS: Bool?
    var copyToClipboardAndroid: Bool?
    var trackingCampaign: String?
    var trackingSource: String?
    var trackingMedium: String?
}

/// Thin seam over the static Grovs API so the bridge can be tested without it. Main thread only.
protocol GrovsFacade: AnyObject {
    func configure(apiKey: String, useTestEnvironment: Bool, baseURL: String?, clipboardDomains: [String]?,
                   enabled: Bool, delegate: GrovsDelegate)
    func setInAppDelegate(_ delegate: GrovsInAppDelegate)
    func setEnabled(_ enabled: Bool)
    func setLogLevel(_ level: LogLevel)

    func setUserIdentifier(_ identifier: String?)
    func setUserAttributes(_ attributes: [String: Any]?)
    func setPushToken(_ token: String?)
    func setPushToken(deviceToken: Data)

    func generateLink(_ request: GrovsLinkRequest, completion: @escaping (URL?) -> Void)
    func linkDetails(path: String, completion: @escaping ([String: Any]?) -> Void)
    func handleOpen(url: URL) -> Bool
    func handleContinue(userActivity: NSUserActivity) -> Bool

    func track(_ name: String, properties: [String: Any]?, tags: [String]?)
    func trackScreenView(_ name: String, properties: [String: Any]?)
    func setGlobalTags(_ tags: [String]?)
    func setScreenAliases(_ aliases: [String: String])

    func logInAppPurchase(transactionID: UInt64, completion: @escaping (Bool) -> Void)
    /// A nil `startDate` means now.
    func logCustomPurchase(type: TransactionType, priceInCents: Int, currency: String, productID: String,
                           startDate: Date?, completion: @escaping (Bool) -> Void)

    func isGrovsNotification(userInfo: [AnyHashable: Any]) -> Bool
    func handleNotificationOpen(userInfo: [AnyHashable: Any]) -> Bool
    func handleNotificationResponse(_ response: UNNotificationResponse) -> Bool

    func pauseInApp()
    func resumeInApp()
    func discardInApp(dismissCurrent: Bool)
    func setInAppExcludedScreens(_ screens: [String])
    func setInAppDebugLogging(_ enabled: Bool)
}
