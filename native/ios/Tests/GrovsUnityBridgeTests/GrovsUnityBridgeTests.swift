import XCTest
import Grovs
@testable import GrovsUnityBridge

final class GrovsUnityBridgeTests: XCTestCase {
    private let suiteName = "GrovsUnityBridgeTests"
    private var defaults: UserDefaults!
    private var fake: FakeGrovsFacade!
    private var bridge: GrovsUnityBridge!
    private var messages: [(type: String, json: String)] = []
    private var veto: ((String) -> Bool)?

    override func setUp() {
        super.setUp()
        defaults = UserDefaults(suiteName: suiteName)
        defaults.removePersistentDomain(forName: suiteName)
        fake = FakeGrovsFacade()
        bridge = GrovsUnityBridge(facade: fake, defaults: defaults)
        messages = []
        veto = nil
        bridge.register(onMessage: { [unowned self] type, json in messages.append((type, json)) },
                        shouldShow: { [unowned self] id in veto?(id) ?? false })
    }

    override func tearDown() {
        defaults.removePersistentDomain(forName: suiteName)
        super.tearDown()
    }

    private func config(apiKey: String? = "key", startEnabled: Bool = true, trackScenes: Bool = false) -> GrovsUnityConfig {
        GrovsUnityConfig(apiKey: apiKey, useTestEnvironment: true, baseURL: "https://b.test", startEnabled: startEnabled,
                         clipboardDomains: ["c.test"], logLevel: 1, trackScenes: trackScenes, ownNotificationDelegate: true)
    }

    private func of(_ type: String) -> [[String: Any]] {
        messages.filter { $0.type == type }.compactMap { GrovsUnityCodec.object($0.json) }
    }

    private func errorCode(_ message: [String: Any]) -> String? {
        (message["error"] as? [String: Any])?["code"] as? String
    }

    func testMissingApiKeyInert() {
        bridge.start(config: config(apiKey: nil))
        bridge.generateLink(requestId: 1, optionsJSON: "{}")
        bridge.logInAppPurchase(requestId: 2, transaction: "1")
        bridge.track("e", propertiesJSON: nil, tagsJSON: nil)

        let results = of("result")
        XCTAssertEqual(results.map { $0["requestId"] as? Int }, [1, 2])
        XCTAssertTrue(results.allSatisfy { $0["ok"] as? Bool == false && errorCode($0) == "sdk_not_initialized" })
        XCTAssertTrue(fake.configured.isEmpty)
        XCTAssertTrue(fake.tracked.isEmpty)
        XCTAssertFalse(bridge.isEnabled)
        XCTAssertFalse(bridge.handleUrl("https://x.sqd.link/a"))
        XCTAssertFalse(bridge.openURL(URL(string: "app://x")!))
        XCTAssertTrue(fake.opened.isEmpty)
    }

    func testConfigurePassesSettings() {
        bridge.start(config: config(trackScenes: true))
        let call = fake.configured.first
        XCTAssertEqual(fake.configured.count, 1)
        XCTAssertEqual(call?.apiKey, "key")
        XCTAssertEqual(call?.useTestEnvironment, true)
        XCTAssertEqual(call?.baseURL, "https://b.test")
        XCTAssertEqual(call?.clipboardDomains, ["c.test"])
        XCTAssertTrue(fake.delegate === bridge)
        XCTAssertTrue(fake.inAppDelegate === bridge)
        XCTAssertEqual(fake.logLevels, [.warn])
        XCTAssertTrue(bridge.isSceneTrackingEnabled)

        bridge.start(config: config())
        XCTAssertEqual(fake.configured.count, 1)
    }

    func testStoredConsentWins() {
        GrovsUnityConsent.store(defaults, true)
        bridge.start(config: config(startEnabled: false))
        XCTAssertEqual(fake.configured.map(\.enabled), [true])
        XCTAssertTrue(bridge.isEnabled)

        let other = GrovsUnityBridge(facade: FakeGrovsFacade(), defaults: defaults)
        GrovsUnityConsent.store(defaults, false)
        other.start(config: config(startEnabled: true))
        XCTAssertFalse(other.isEnabled)
    }

    func testGenerateLinkMapsOptions() {
        bridge.start(config: config())
        let json = """
        {"title":"t","subtitle":"s","imageUrl":"https://img","data":{"level":3,"name":"n"},
         "tags":["a","b"],
         "iosRedirect":{"url":"https://ios","openAppIfInstalled":false},
         "androidRedirect":{"url":"https://android","openAppIfInstalled":true},
         "desktopRedirect":{"url":"https://desktop"},
         "showPreviewIos":true,"showPreviewAndroid":false,
         "copyToClipboardIos":false,"copyToClipboardAndroid":true,
         "trackingCampaign":"c","trackingSource":"src","trackingMedium":"m"}
        """
        bridge.generateLink(requestId: 5, optionsJSON: json)

        let call = fake.linkRequests.first
        XCTAssertEqual(fake.linkRequests.count, 1)
        XCTAssertEqual(call?.title, "t")
        XCTAssertEqual(call?.subtitle, "s")
        XCTAssertEqual(call?.imageURL, "https://img")
        XCTAssertEqual(call?.data?["level"] as? Int, 3)
        XCTAssertEqual(call?.data?["name"] as? String, "n")
        XCTAssertEqual(call?.tags, ["a", "b"])
        XCTAssertEqual(call?.customRedirects?.ios?.link, "https://ios")
        XCTAssertEqual(call?.customRedirects?.ios?.openAppIfInstalled, false)
        XCTAssertEqual(call?.customRedirects?.android?.link, "https://android")
        XCTAssertEqual(call?.customRedirects?.android?.openAppIfInstalled, true)
        XCTAssertEqual(call?.customRedirects?.desktop?.link, "https://desktop")
        XCTAssertEqual(call?.customRedirects?.desktop?.openAppIfInstalled, true)
        XCTAssertEqual(call?.showPreviewiOS, true)
        XCTAssertEqual(call?.showPreviewAndroid, false)
        XCTAssertEqual(call?.copyToClipboardiOS, false)
        XCTAssertEqual(call?.copyToClipboardAndroid, true)
        XCTAssertEqual(call?.trackingCampaign, "c")
        XCTAssertEqual(call?.trackingSource, "src")
        XCTAssertEqual(call?.trackingMedium, "m")

        let result = of("result").first
        XCTAssertEqual(result?["requestId"] as? Int, 5)
        XCTAssertEqual(result?["ok"] as? Bool, true)
        XCTAssertEqual(result?["value"] as? String, "https://x.sqd.link/a")
    }

    func testGenerateLinkEmptyOptionsPassesNils() {
        bridge.start(config: config())
        bridge.generateLink(requestId: 2, optionsJSON: "{}")
        let call = fake.linkRequests.first
        XCTAssertNil(call?.title)
        XCTAssertNil(call?.data)
        XCTAssertNil(call?.customRedirects)
        XCTAssertNil(call?.showPreviewiOS)
        XCTAssertNil(call?.trackingCampaign)
    }

    func testGenerateLinkBadJSONGivesInvalidArgument() {
        bridge.start(config: config())
        bridge.generateLink(requestId: 3, optionsJSON: "[1")
        XCTAssertTrue(fake.linkRequests.isEmpty)
        XCTAssertEqual(of("result").first.flatMap(errorCode), "invalid_argument")
    }

    func testGenerateLinkNilURLGivesLinkGenerationFailed() {
        bridge.start(config: config())
        fake.linkResult = nil
        bridge.generateLink(requestId: 3, optionsJSON: "{}")
        let result = of("result").first
        XCTAssertEqual(result?["ok"] as? Bool, false)
        XCTAssertEqual(result.flatMap(errorCode), "link_generation_failed")
    }

    func testLinkDetailsNilGivesLinkDetailsFailed() {
        bridge.start(config: config())
        bridge.linkDetails(requestId: 4, path: "abc")
        fake.linkDetailsResult = { _ in nil }
        bridge.linkDetails(requestId: 5, path: "def")

        let results = of("result")
        XCTAssertEqual(fake.linkDetailsPaths, ["abc", "def"])
        XCTAssertEqual(results[0]["ok"] as? Bool, true)
        XCTAssertEqual((results[0]["value"] as? [String: Any])?["path"] as? String, "abc")
        XCTAssertEqual(results[1]["ok"] as? Bool, false)
        XCTAssertEqual(errorCode(results[1]), "link_details_failed")
    }

    func testDisabledAsyncRequestsFailWithSdkDisabled() {
        bridge.start(config: config(startEnabled: false))
        bridge.generateLink(requestId: 1, optionsJSON: "{}")
        bridge.linkDetails(requestId: 2, path: "p")
        bridge.logInAppPurchase(requestId: 3, transaction: "7")
        bridge.logCustomPurchase(requestId: 4, type: 0, priceInCents: 1, currency: "USD", productID: "p", epochMs: -1)

        let results = of("result")
        XCTAssertEqual(results.map { $0["requestId"] as? Int }, [1, 2, 3, 4])
        XCTAssertTrue(results.allSatisfy { $0["ok"] as? Bool == false && errorCode($0) == "sdk_disabled" })
        XCTAssertTrue(fake.linkRequests.isEmpty)
        XCTAssertTrue(fake.linkDetailsPaths.isEmpty)
        XCTAssertTrue(fake.inAppPurchases.isEmpty)
        XCTAssertTrue(fake.customPurchases.isEmpty)
    }

    func testErrorCodeMapping() {
        XCTAssertEqual(GrovsUnityBridge.errorCode(rawValue: 1), "authentication_failed")
        XCTAssertEqual(GrovsUnityBridge.errorCode(rawValue: 2), "network_request_failed")
        XCTAssertEqual(GrovsUnityBridge.errorCode(rawValue: 3), "event_dispatch_failed")
        XCTAssertEqual(GrovsUnityBridge.errorCode(rawValue: 4), "link_generation_failed")
        XCTAssertEqual(GrovsUnityBridge.errorCode(rawValue: 5), "in_app_display_failed")
        XCTAssertEqual(GrovsUnityBridge.errorCode(rawValue: 99), "unknown")

        bridge.start(config: config())
        bridge.grovsDidEncounterError(.authenticationFailed, message: "auth")
        bridge.grovsDidEncounterError(.inAppDisplayFailed, message: "no view")

        let errors = of("error")
        XCTAssertEqual(errors.map { $0["code"] as? String }, ["authentication_failed", "in_app_display_failed"])
        XCTAssertEqual(errors.map { $0["message"] as? String }, ["auth", "no view"])
        let events = of("inapp_event")
        XCTAssertEqual(events.count, 1)
        XCTAssertEqual(events.first?["type"] as? String, "failed")
        XCTAssertEqual(events.first?["message"] as? String, "no view")
    }

    func testDeeplinkPostsMessage() {
        bridge.start(config: config())
        bridge.grovsReceivedPayloadFromDeeplink(link: "https://x.sqd.link/a", payload: ["level": 3], tracking: ["campaign": "c"])
        let message = of("deeplink").first
        XCTAssertEqual(message?["link"] as? String, "https://x.sqd.link/a")
        XCTAssertEqual((message?["data"] as? [String: Any])?["level"] as? Int, 3)
        XCTAssertEqual((message?["tracking"] as? [String: Any])?["campaign"] as? String, "c")
    }

    func testInAppDelegateMapping() {
        bridge.start(config: config())
        veto = { $0 == "c0" }
        XCTAssertTrue(bridge.grovsShouldShowInAppMessage(campaignID: "c0"))
        XCTAssertFalse(bridge.grovsShouldShowInAppMessage(campaignID: "other"))
        bridge.grovsDidShowInAppMessage(campaignID: "c1")
        bridge.grovsDidTapInAppMessage(campaignID: "c2", buttonID: "b1", payload: ["k": "v"])
        bridge.grovsDidDismissInAppMessage(campaignID: "c3", reason: "backdrop")
        bridge.grovsDidAnswerInAppMessage(campaignID: "c4", value: 4, comment: "ok")

        let events = of("inapp_event")
        XCTAssertEqual(events.map { $0["type"] as? String }, ["shown", "clicked", "dismissed", "answered"])
        XCTAssertEqual(events.map { $0["campaignId"] as? String }, ["c1", "c2", "c3", "c4"])
        XCTAssertEqual(events[1]["buttonId"] as? String, "b1")
        XCTAssertEqual((events[1]["payload"] as? [String: Any])?["k"] as? String, "v")
        XCTAssertEqual(events[2]["reason"] as? String, "backdrop")
        XCTAssertEqual(events[3]["value"] as? Int, 4)
        XCTAssertEqual(events[3]["comment"] as? String, "ok")
        XCTAssertTrue(of("error").isEmpty)
    }

    func testShouldShowDefaultsTrue() {
        let quiet = GrovsUnityBridge(facade: FakeGrovsFacade(), defaults: defaults)
        quiet.start(config: config())
        XCTAssertTrue(quiet.grovsShouldShowInAppMessage(campaignID: "c"))
    }

    func testConsentEnableTwiceReplaysLaunchOnce() {
        bridge.start(config: config(startEnabled: false))
        let scheme = URL(string: "myapp://open/a")!
        XCTAssertTrue(bridge.openURL(scheme))
        XCTAssertEqual(fake.opened, [scheme])

        bridge.setEnabled(true)
        bridge.setEnabled(true)
        XCTAssertEqual(fake.opened, [scheme, scheme])
        XCTAssertEqual(fake.enabledCalls, [true])
        XCTAssertTrue(bridge.isEnabled)

        bridge.setEnabled(false)
        let universal = NSUserActivity(activityType: NSUserActivityTypeBrowsingWeb)
        universal.webpageURL = URL(string: "https://x.sqd.link/b")
        XCTAssertTrue(bridge.continueUserActivity(universal))
        XCTAssertEqual(fake.continued.count, 1)

        bridge.setEnabled(true)
        bridge.setEnabled(true)
        XCTAssertEqual(fake.continued.count, 2)
        XCTAssertEqual(fake.continued.last?.activityType, NSUserActivityTypeBrowsingWeb)
        XCTAssertEqual(fake.continued.last?.webpageURL, URL(string: "https://x.sqd.link/b"))
        XCTAssertEqual(fake.opened.count, 2)
    }

    func testEnableWithoutLaunchLinkReplaysNothing() {
        bridge.start(config: config())
        bridge.setEnabled(false)
        bridge.setEnabled(true)
        XCTAssertTrue(fake.opened.isEmpty)
        XCTAssertTrue(fake.continued.isEmpty)
        XCTAssertEqual(fake.enabledCalls, [false, true])
    }

    func testLinkBeforeStartIsForwardedAndKeptForConsent() {
        let scheme = URL(string: "myapp://early")!
        XCTAssertTrue(bridge.openURL(scheme))
        XCTAssertEqual(fake.opened, [scheme])

        bridge.start(config: config(startEnabled: false))
        bridge.setEnabled(true)
        XCTAssertEqual(fake.opened, [scheme, scheme])
    }

    func testLinkBeforeEnabledStartIsNotReplayedLater() {
        _ = bridge.openURL(URL(string: "myapp://early")!)
        bridge.start(config: config())
        bridge.setEnabled(false)
        bridge.setEnabled(true)
        XCTAssertEqual(fake.opened.count, 1)
    }

    func testConsentPersisted() {
        bridge.start(config: config())
        bridge.setEnabled(false)
        XCTAssertEqual(GrovsUnityConsent.stored(defaults), false)
        XCTAssertFalse(bridge.isEnabled)
        XCTAssertEqual(fake.enabledCalls, [false])
    }

    func testHandleUrlHttpsUsesContinueUserActivity() {
        bridge.start(config: config())
        XCTAssertTrue(bridge.handleUrl("https://x.sqd.link/a"))
        XCTAssertEqual(fake.continued.count, 1)
        XCTAssertEqual(fake.continued.first?.activityType, NSUserActivityTypeBrowsingWeb)
        XCTAssertEqual(fake.continued.first?.webpageURL, URL(string: "https://x.sqd.link/a"))
        XCTAssertTrue(fake.opened.isEmpty)

        XCTAssertTrue(bridge.handleUrl("myapp://open/a"))
        XCTAssertEqual(fake.opened, [URL(string: "myapp://open/a")!])

        XCTAssertFalse(bridge.handleUrl("not a url"))
        XCTAssertFalse(bridge.handleUrl("/relative/path"))

        bridge.setEnabled(false)
        XCTAssertFalse(bridge.handleUrl("https://x.sqd.link/b"))
        XCTAssertFalse(bridge.handleUrl("myapp://open/b"))
        XCTAssertEqual(fake.continued.count, 1)
        XCTAssertEqual(fake.opened.count, 1)
    }

    func testLogInAppPurchaseNonNumericFailsInvalidArgument() {
        bridge.start(config: config())
        bridge.logInAppPurchase(requestId: 6, transaction: "abc")
        bridge.logInAppPurchase(requestId: 7, transaction: "2000000123456789")
        fake.inAppPurchaseResult = false
        bridge.logInAppPurchase(requestId: 8, transaction: "5")

        let results = of("result")
        XCTAssertEqual(results[0]["ok"] as? Bool, false)
        XCTAssertEqual(errorCode(results[0]), "invalid_argument")
        XCTAssertEqual(fake.inAppPurchases, [2000000123456789, 5])
        XCTAssertEqual(results[1]["ok"] as? Bool, true)
        XCTAssertEqual(results[1]["value"] as? Bool, true)
        XCTAssertEqual(results[2]["ok"] as? Bool, true)
        XCTAssertEqual(results[2]["value"] as? Bool, false)
    }

    func testLogCustomPurchaseTypeAndDate() {
        bridge.start(config: config())
        bridge.logCustomPurchase(requestId: 9, type: 1, priceInCents: 499, currency: "USD", productID: "p1", epochMs: -1)
        bridge.logCustomPurchase(requestId: 10, type: 2, priceInCents: 100, currency: "EUR", productID: "p2", epochMs: 1767225600000)
        bridge.logCustomPurchase(requestId: 11, type: 0, priceInCents: 1, currency: "USD", productID: "p3", epochMs: -1)
        bridge.logCustomPurchase(requestId: 12, type: 7, priceInCents: 1, currency: "USD", productID: "p4", epochMs: -1)

        let first = fake.customPurchases[0]
        XCTAssertEqual(first.type, .cancel)
        XCTAssertEqual(first.priceInCents, 499)
        XCTAssertEqual(first.currency, "USD")
        XCTAssertEqual(first.productID, "p1")
        XCTAssertNil(first.startDate)
        XCTAssertEqual(fake.customPurchases[1].type, .refund)
        XCTAssertEqual(fake.customPurchases[1].startDate, Date(timeIntervalSince1970: 1767225600))
        XCTAssertEqual(fake.customPurchases[2].type, .buy)
        XCTAssertEqual(fake.customPurchases.count, 3)

        let results = of("result")
        XCTAssertTrue(results.prefix(3).allSatisfy { $0["ok"] as? Bool == true && $0["value"] as? Bool == true })
        XCTAssertEqual(errorCode(results[3]), "invalid_argument")
    }

    func testLogLevelMapping() {
        XCTAssertEqual(GrovsUnityBridge.logLevel(0), .info)
        XCTAssertEqual(GrovsUnityBridge.logLevel(1), .warn)
        XCTAssertEqual(GrovsUnityBridge.logLevel(2), .error)
        bridge.start(config: config())
        bridge.setLogLevel(0)
        XCTAssertEqual(fake.logLevels.last, .info)
        bridge.setLogLevel(2)
        XCTAssertEqual(fake.logLevels.last, .error)
    }

    func testSyncCallsPassThrough() {
        bridge.start(config: config())
        bridge.setUserIdentifier("u1")
        bridge.setUserIdentifier(nil)
        bridge.setUserAttributes("{\"a\":1,\"b\":null}")
        bridge.setPushToken("tok")
        bridge.track("e", propertiesJSON: "{\"p\":\"v\",\"n\":null}", tagsJSON: "[\"t\"]")
        bridge.trackScreenView("menu", propertiesJSON: nil)
        bridge.setGlobalTags("[\"g\"]")
        bridge.setGlobalTags(nil)
        bridge.setScreenAliases("{\"A\":\"Home\",\"B\":null}")
        bridge.pauseInApp()
        bridge.resumeInApp()
        bridge.discardInApp(dismissCurrent: true)
        bridge.setInAppExcludedScreens("[\"menu\"]")
        bridge.setInAppDebugLogging(true)

        XCTAssertEqual(fake.identifiers, ["u1", nil])
        XCTAssertEqual(fake.attributes.first??.count, 1)
        XCTAssertEqual(fake.attributes.first??["a"] as? Int, 1)
        XCTAssertEqual(fake.pushTokens, ["tok"])
        XCTAssertEqual(fake.tracked.first?.name, "e")
        XCTAssertEqual(fake.tracked.first?.properties?.count, 1)
        XCTAssertEqual(fake.tracked.first?.tags, ["t"])
        XCTAssertEqual(fake.screenViews.first?.name, "menu")
        XCTAssertNil(fake.screenViews.first?.properties)
        XCTAssertEqual(fake.globalTags, [["g"], nil])
        XCTAssertEqual(fake.screenAliases, [["A": "Home"]])
        XCTAssertEqual(fake.inAppCalls, ["pause", "resume", "discard:true"])
        XCTAssertEqual(fake.excludedScreens, [["menu"]])
        XCTAssertEqual(fake.debugLogging, [true])
    }

    func testHookEntryForwardsDeviceToken() {
        bridge.start(config: config())
        let token = Data([0xab, 0xcd])
        bridge.setPushToken(deviceToken: token)
        XCTAssertEqual(fake.deviceTokens, [token])
    }

    func testPushHelpers() {
        bridge.start(config: config())
        XCTAssertTrue(bridge.isGrovsNotification("{\"grovs_campaign_id\":\"c\"}"))
        XCTAssertFalse(bridge.isGrovsNotification("{\"other\":\"x\"}"))
        XCTAssertFalse(bridge.isGrovsNotification("nope"))
        XCTAssertTrue(bridge.handleNotificationOpen("{\"grovs_campaign_id\":\"c\"}"))
        XCTAssertEqual(fake.notificationOpens.count, 1)

        let inert = GrovsUnityBridge(facade: fake, defaults: defaults)
        inert.start(config: config(apiKey: nil))
        XCTAssertFalse(inert.handleNotificationOpen("{\"grovs_campaign_id\":\"c\"}"))
        XCTAssertEqual(fake.notificationOpens.count, 1)
    }

    func testMessagesBufferedUntilRegistered() {
        let late = GrovsUnityBridge(facade: FakeGrovsFacade(), defaults: defaults)
        late.start(config: config())
        late.grovsReceivedPayloadFromDeeplink(link: "https://x.sqd.link/a", payload: nil, tracking: nil)
        var received: [String] = []
        late.register(onMessage: { type, _ in received.append(type) }, shouldShow: nil)
        XCTAssertEqual(received, ["deeplink"])
    }
}
