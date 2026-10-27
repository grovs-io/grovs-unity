import Foundation
import Grovs
import UserNotifications

/// Holds the bridge state and receives the Grovs delegate callbacks. Main thread only;
/// the C exports hop to main before calling in.
final class GrovsUnityBridge: NSObject {
    static let shared = GrovsUnityBridge(facade: RealGrovsFacade(), defaults: .standard)

    private enum LaunchLink {
        case open(URL)
        case continueActivity(URL)
    }

    private let facade: GrovsFacade
    private let defaults: UserDefaults
    private let queue = GrovsUnityMessageQueue()
    private var shouldShow: ((String) -> Bool)?
    private var config: GrovsUnityConfig?
    private var started = false
    private var initialized = false
    private var enabled = true
    private var launchLink: LaunchLink?

    init(facade: GrovsFacade, defaults: UserDefaults) {
        self.facade = facade
        self.defaults = defaults
    }

    /// Before start, links still go to Grovs, which holds them until configure.
    private var nativeReachable: Bool { !started || initialized }

    var isEnabled: Bool { initialized && enabled }

    var isSceneTrackingEnabled: Bool { config?.trackScenes ?? false }

    func start(config: GrovsUnityConfig) {
        guard !started else { return }
        started = true
        self.config = config
        enabled = GrovsUnityConsent.stored(defaults) ?? config.startEnabled
        guard let apiKey = config.apiKey else {
            NSLog("[GrovsUnity] Missing GrovsApiKey in Info.plist. Grovs stays inactive.")
            return
        }
        // Grovs replays a link held from before configure only when enabled.
        if enabled { launchLink = nil }
        facade.configure(apiKey: apiKey,
                         useTestEnvironment: config.useTestEnvironment,
                         baseURL: config.baseURL,
                         clipboardDomains: config.clipboardDomains,
                         enabled: enabled,
                         delegate: self)
        facade.setInAppDelegate(self)
        facade.setLogLevel(Self.logLevel(config.logLevel))
        initialized = true
    }

    func register(onMessage: @escaping (String, String) -> Void, shouldShow: ((String) -> Bool)?) {
        self.shouldShow = shouldShow
        queue.setReady(onMessage)
    }

    // MARK: Consent

    func setEnabled(_ value: Bool) {
        GrovsUnityConsent.store(defaults, value)
        let changed = enabled != value
        enabled = value
        guard initialized, changed else { return }
        facade.setEnabled(value)
        guard value, let link = launchLink else { return }
        launchLink = nil
        switch link {
        case .open(let url): _ = facade.handleOpen(url: url)
        case .continueActivity(let url): _ = facade.handleContinue(userActivity: Self.browsingActivity(url))
        }
    }

    // MARK: App delegate hooks

    func openURL(_ url: URL) -> Bool {
        guard nativeReachable else { return false }
        if !isEnabled { launchLink = .open(url) }
        return facade.handleOpen(url: url)
    }

    func continueUserActivity(_ activity: NSUserActivity) -> Bool {
        guard nativeReachable else { return false }
        if !isEnabled, activity.activityType == NSUserActivityTypeBrowsingWeb, let url = activity.webpageURL {
            launchLink = .continueActivity(url)
        }
        return facade.handleContinue(userActivity: activity)
    }

    func setPushToken(deviceToken: Data) {
        guard nativeReachable else { return }
        facade.setPushToken(deviceToken: deviceToken)
    }

    func handleNotificationResponse(_ response: UNNotificationResponse) -> Bool {
        guard nativeReachable else { return false }
        return facade.handleNotificationResponse(response)
    }

    // MARK: Calls from C#

    func setLogLevel(_ level: Int) {
        call { facade.setLogLevel(Self.logLevel(level)) }
    }

    func setUserIdentifier(_ identifier: String?) {
        call { facade.setUserIdentifier(identifier) }
    }

    func setUserAttributes(_ json: String?) {
        call { facade.setUserAttributes(GrovsUnityCodec.object(json).map(Self.withoutNulls)) }
    }

    func setPushToken(_ token: String?) {
        call { facade.setPushToken(token) }
    }

    func generateLink(requestId: Int32, optionsJSON: String?) {
        request(requestId) {
            guard let options = Self.linkOptions(optionsJSON) else {
                postResult(requestId, failure: "invalid_argument", "Link options are not valid JSON.")
                return
            }
            facade.generateLink(Self.linkRequest(options)) { [weak self] url in
                if let url = url {
                    self?.postResult(requestId, value: url.absoluteString)
                } else {
                    self?.postResult(requestId, failure: "link_generation_failed", "No link was generated.")
                }
            }
        }
    }

    func linkDetails(requestId: Int32, path: String) {
        request(requestId) {
            facade.linkDetails(path: path) { [weak self] details in
                if let details = details {
                    self?.postResult(requestId, value: details)
                } else {
                    self?.postResult(requestId, failure: "link_details_failed", "No link details.")
                }
            }
        }
    }

    func handleUrl(_ text: String) -> Bool {
        guard isEnabled, let url = URL(string: text),
              let scheme = url.scheme?.lowercased(), !scheme.isEmpty,
              let host = url.host, !host.isEmpty else { return false }
        if scheme == "https" {
            return facade.handleContinue(userActivity: Self.browsingActivity(url))
        }
        return facade.handleOpen(url: url)
    }

    func track(_ name: String, propertiesJSON: String?, tagsJSON: String?) {
        call {
            facade.track(name,
                         properties: GrovsUnityCodec.object(propertiesJSON).map(Self.withoutNulls),
                         tags: GrovsUnityCodec.stringList(tagsJSON))
        }
    }

    func trackScreenView(_ name: String, propertiesJSON: String?) {
        call { facade.trackScreenView(name, properties: GrovsUnityCodec.object(propertiesJSON).map(Self.withoutNulls)) }
    }

    func setGlobalTags(_ json: String?) {
        call { facade.setGlobalTags(GrovsUnityCodec.stringList(json)) }
    }

    func setScreenAliases(_ json: String?) {
        call {
            let aliases = Self.withoutNulls(GrovsUnityCodec.object(json) ?? [:]).mapValues { "\($0)" }
            facade.setScreenAliases(aliases)
        }
    }

    func logInAppPurchase(requestId: Int32, transaction: String) {
        request(requestId) {
            guard let transactionID = UInt64(transaction.trimmingCharacters(in: .whitespacesAndNewlines)) else {
                postResult(requestId, failure: "invalid_argument", "Transaction id must be a decimal number.")
                return
            }
            facade.logInAppPurchase(transactionID: transactionID) { [weak self] ok in
                self?.postResult(requestId, value: ok)
            }
        }
    }

    func logCustomPurchase(requestId: Int32, type: Int32, priceInCents: Int32, currency: String, productID: String,
                           epochMs: Int64) {
        request(requestId) {
            guard let transactionType = Self.transactionType(type) else {
                postResult(requestId, failure: "invalid_argument", "Unknown transaction type \(type).")
                return
            }
            let date = epochMs == -1 ? nil : Date(timeIntervalSince1970: TimeInterval(epochMs) / 1000)
            facade.logCustomPurchase(type: transactionType, priceInCents: Int(priceInCents), currency: currency,
                                     productID: productID, startDate: date) { [weak self] ok in
                self?.postResult(requestId, value: ok)
            }
        }
    }

    func isGrovsNotification(_ json: String?) -> Bool {
        guard let userInfo = GrovsUnityCodec.object(json) else { return false }
        return facade.isGrovsNotification(userInfo: userInfo)
    }

    func handleNotificationOpen(_ json: String?) -> Bool {
        guard initialized, let userInfo = GrovsUnityCodec.object(json) else { return false }
        return facade.handleNotificationOpen(userInfo: userInfo)
    }

    func pauseInApp() { call { facade.pauseInApp() } }

    func resumeInApp() { call { facade.resumeInApp() } }

    func discardInApp(dismissCurrent: Bool) { call { facade.discardInApp(dismissCurrent: dismissCurrent) } }

    func setInAppExcludedScreens(_ json: String?) {
        call { facade.setInAppExcludedScreens(GrovsUnityCodec.stringList(json) ?? []) }
    }

    func setInAppDebugLogging(_ enabled: Bool) { call { facade.setInAppDebugLogging(enabled) } }

    // MARK: Mapping

    static func errorCode(rawValue: Int) -> String {
        switch GrovsError(rawValue: rawValue) {
        case .authenticationFailed: return "authentication_failed"
        case .networkRequestFailed: return "network_request_failed"
        case .eventDispatchFailed: return "event_dispatch_failed"
        case .linkGenerationFailed: return "link_generation_failed"
        case .inAppDisplayFailed: return "in_app_display_failed"
        case nil: return "unknown"
        }
    }

    static func logLevel(_ level: Int) -> LogLevel {
        LogLevel(rawValue: min(max(level, 0), 2)) ?? .error
    }

    private static func transactionType(_ type: Int32) -> TransactionType? {
        switch type {
        case 0: return .buy
        case 1: return .cancel
        case 2: return .refund
        default: return nil
        }
    }

    private static func browsingActivity(_ url: URL) -> NSUserActivity {
        let activity = NSUserActivity(activityType: NSUserActivityTypeBrowsingWeb)
        activity.webpageURL = url
        return activity
    }

    private static func linkOptions(_ json: String?) -> [String: Any]? {
        guard let json = json, !json.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty else { return [:] }
        return GrovsUnityCodec.object(json)
    }

    private static func linkRequest(_ options: [String: Any]) -> GrovsLinkRequest {
        GrovsLinkRequest(title: options["title"] as? String,
                         subtitle: options["subtitle"] as? String,
                         imageURL: options["imageUrl"] as? String,
                         data: options["data"] as? [String: Any],
                         tags: (options["tags"] as? [Any])?.map { "\($0)" },
                         customRedirects: redirects(options),
                         showPreviewiOS: options["showPreviewIos"] as? Bool,
                         showPreviewAndroid: options["showPreviewAndroid"] as? Bool,
                         copyToClipboardiOS: options["copyToClipboardIos"] as? Bool,
                         copyToClipboardAndroid: options["copyToClipboardAndroid"] as? Bool,
                         trackingCampaign: options["trackingCampaign"] as? String,
                         trackingSource: options["trackingSource"] as? String,
                         trackingMedium: options["trackingMedium"] as? String)
    }

    private static func redirects(_ options: [String: Any]) -> CustomRedirects? {
        let ios = redirect(options["iosRedirect"])
        let android = redirect(options["androidRedirect"])
        let desktop = redirect(options["desktopRedirect"])
        if ios == nil && android == nil && desktop == nil { return nil }
        return CustomRedirects(ios: ios, android: android, desktop: desktop)
    }

    private static func redirect(_ value: Any?) -> CustomLinkRedirect? {
        guard let map = value as? [String: Any], let url = map["url"] as? String else { return nil }
        return CustomLinkRedirect(link: url, openAppIfInstalled: map["openAppIfInstalled"] as? Bool ?? true)
    }

    private static func withoutNulls(_ map: [String: Any]) -> [String: Any] {
        map.filter { !($0.value is NSNull) }
    }

    // MARK: Messages

    private func call(_ block: () -> Void) {
        guard initialized else { return }
        block()
    }

    private func request(_ requestId: Int32, _ block: () -> Void) {
        guard initialized else {
            postResult(requestId, failure: "sdk_not_initialized", "Grovs is not configured.")
            return
        }
        guard enabled else {
            postResult(requestId, failure: "sdk_disabled", "Grovs is disabled.")
            return
        }
        block()
    }

    private func postResult(_ requestId: Int32, value: Any) {
        queue.post("result", GrovsUnityCodec.resultJSON(requestId: requestId, ok: true, value: value,
                                                        errorCode: nil, errorMessage: nil))
    }

    private func postResult(_ requestId: Int32, failure code: String, _ message: String) {
        queue.post("result", GrovsUnityCodec.resultJSON(requestId: requestId, ok: false, value: nil,
                                                        errorCode: code, errorMessage: message))
    }

    private func postInApp(_ type: String, _ campaignId: String?, _ fields: [String: Any?] = [:]) {
        var event: [String: Any] = ["type": type, "campaignId": campaignId ?? NSNull()]
        for (key, value) in fields { event[key] = value ?? NSNull() }
        queue.post("inapp_event", GrovsUnityCodec.json(event))
    }
}

extension GrovsUnityBridge: GrovsDelegate {
    func grovsReceivedPayloadFromDeeplink(link: String?, payload: [String: Any]?, tracking: [String: Any]?) {
        let message: [String: Any] = ["link": link ?? NSNull(), "data": payload ?? NSNull(), "tracking": tracking ?? NSNull()]
        queue.post("deeplink", GrovsUnityCodec.json(message))
    }

    func grovsDidEncounterError(_ error: GrovsError, message: String) {
        if error == .inAppDisplayFailed {
            postInApp("failed", nil, ["message": message])
        }
        queue.post("error", GrovsUnityCodec.errorJSON(code: Self.errorCode(rawValue: error.rawValue), message: message))
    }
}

extension GrovsUnityBridge: GrovsInAppDelegate {
    func grovsShouldShowInAppMessage(campaignID: String) -> Bool {
        shouldShow?(campaignID) ?? true
    }

    func grovsDidShowInAppMessage(campaignID: String) {
        postInApp("shown", campaignID)
    }

    func grovsDidTapInAppMessage(campaignID: String, buttonID: String?, payload: [String: Any]?) {
        postInApp("clicked", campaignID, ["buttonId": buttonID, "payload": payload])
    }

    func grovsDidDismissInAppMessage(campaignID: String, reason: String) {
        postInApp("dismissed", campaignID, ["reason": reason])
    }

    func grovsDidAnswerInAppMessage(campaignID: String, value: Any, comment: String?) {
        postInApp("answered", campaignID, ["value": value, "comment": comment])
    }
}
