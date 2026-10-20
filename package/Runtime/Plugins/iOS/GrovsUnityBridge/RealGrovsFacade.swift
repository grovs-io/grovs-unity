import Foundation
import Grovs
import UserNotifications

final class RealGrovsFacade: GrovsFacade {
    func configure(apiKey: String, useTestEnvironment: Bool, baseURL: String?, clipboardDomains: [String]?,
                   enabled: Bool, delegate: GrovsDelegate) {
        Grovs.configure(APIKey: apiKey,
                        useTestEnvironment: useTestEnvironment,
                        baseURL: baseURL,
                        autoTrackScreenViews: false,
                        clipboardDomains: clipboardDomains,
                        enabled: enabled,
                        delegate: delegate)
    }

    func setInAppDelegate(_ delegate: GrovsInAppDelegate) { Grovs.inAppDelegate = delegate }

    func setEnabled(_ enabled: Bool) { Grovs.setSDK(enabled: enabled) }

    func setLogLevel(_ level: LogLevel) { Grovs.setDebug(level: level) }

    func setUserIdentifier(_ identifier: String?) { Grovs.userIdentifier = identifier }

    func setUserAttributes(_ attributes: [String: Any]?) { Grovs.userAttributes = attributes }

    func setPushToken(_ token: String?) { Grovs.pushToken = token }

    func setPushToken(deviceToken: Data) { Grovs.setPushToken(deviceToken: deviceToken) }

    func generateLink(_ request: GrovsLinkRequest, completion: @escaping (URL?) -> Void) {
        Grovs.generateLink(title: request.title,
                           subtitle: request.subtitle,
                           imageURL: request.imageURL,
                           data: request.data,
                           tags: request.tags,
                           customRedirects: request.customRedirects,
                           showPreviewiOS: request.showPreviewiOS,
                           showPreviewAndroid: request.showPreviewAndroid,
                           copyToClipboardiOS: request.copyToClipboardiOS,
                           copyToClipboardAndroid: request.copyToClipboardAndroid,
                           trackingCampaign: request.trackingCampaign,
                           trackingSource: request.trackingSource,
                           trackingMedium: request.trackingMedium,
                           completion: completion)
    }

    func linkDetails(path: String, completion: @escaping ([String: Any]?) -> Void) {
        Grovs.linkDetails(path: path, completion: completion)
    }

    func handleOpen(url: URL) -> Bool { Grovs.handleAppDelegate(open: url) }

    func handleContinue(userActivity: NSUserActivity) -> Bool {
        Grovs.handleAppDelegate(continue: userActivity, restorationHandler: { _ in })
    }

    func track(_ name: String, properties: [String: Any]?, tags: [String]?) {
        Grovs.track(name, properties: properties, tags: tags)
    }

    func trackScreenView(_ name: String, properties: [String: Any]?) {
        Grovs.trackScreenView(name, properties: properties)
    }

    func setGlobalTags(_ tags: [String]?) { Grovs.setGlobalTags(tags) }

    func setScreenAliases(_ aliases: [String: String]) { Grovs.setScreenAliases(aliases) }

    func logInAppPurchase(transactionID: UInt64, completion: @escaping (Bool) -> Void) {
        Grovs.logInAppPurchase(transactionID: transactionID, completion: completion)
    }

    func logCustomPurchase(type: TransactionType, priceInCents: Int, currency: String, productID: String,
                           startDate: Date?, completion: @escaping (Bool) -> Void) {
        Grovs.logCustomPurchase(type: type, priceInCents: priceInCents, currency: currency, productID: productID,
                                startDate: startDate, completion: completion)
    }

    func isGrovsNotification(userInfo: [AnyHashable: Any]) -> Bool { Grovs.isGrovsNotification(userInfo: userInfo) }

    func handleNotificationOpen(userInfo: [AnyHashable: Any]) -> Bool { Grovs.handleNotificationOpen(userInfo: userInfo) }

    func handleNotificationResponse(_ response: UNNotificationResponse) -> Bool {
        Grovs.handleNotificationResponse(response)
    }

    func pauseInApp() { Grovs.pauseInAppMessages() }

    func resumeInApp() { Grovs.resumeInAppMessages() }

    func discardInApp(dismissCurrent: Bool) { Grovs.discardInAppMessages(dismissCurrent: dismissCurrent) }

    func setInAppExcludedScreens(_ screens: [String]) { Grovs.setInAppExcludedScreens(screens) }

    func setInAppDebugLogging(_ enabled: Bool) { Grovs.setInAppDebugLogging(enabled) }
}
