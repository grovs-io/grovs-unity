import XCTest
@testable import GrovsUnityBridge

final class GrovsUnityConfigTests: XCTestCase {
    func testParsesAll() {
        let c = GrovsUnityConfig.from(infoDictionary: [
            "GrovsApiKey": "key",
            "GrovsUseTestEnvironment": true,
            "GrovsBaseURL": "https://x.test",
            "GrovsEnabled": false,
            "GrovsClipboardDomains": ["a.com", "b.com"],
            "GrovsLogLevel": "warn",
            "GrovsUnityTrackScenes": true,
            "GrovsUnityOwnNotificationDelegate": false,
        ])
        XCTAssertEqual(c.apiKey, "key")
        XCTAssertTrue(c.useTestEnvironment)
        XCTAssertEqual(c.baseURL, "https://x.test")
        XCTAssertFalse(c.startEnabled)
        XCTAssertEqual(c.clipboardDomains, ["a.com", "b.com"])
        XCTAssertEqual(c.logLevel, 1)
        XCTAssertTrue(c.trackScenes)
        XCTAssertFalse(c.ownNotificationDelegate)
    }

    func testDefaults() {
        let c = GrovsUnityConfig.from(infoDictionary: [:])
        XCTAssertTrue(c.startEnabled)
        XCTAssertFalse(c.useTestEnvironment)
        XCTAssertNil(c.baseURL)
        XCTAssertNil(c.clipboardDomains)
        XCTAssertEqual(c.logLevel, 2)
        XCTAssertFalse(c.trackScenes)
        XCTAssertTrue(c.ownNotificationDelegate)
    }

    func testMissingApiKeyIsNil() {
        XCTAssertNil(GrovsUnityConfig.from(infoDictionary: [:]).apiKey)
        XCTAssertNil(GrovsUnityConfig.from(infoDictionary: ["GrovsApiKey": "  "]).apiKey)
    }

    func testClipboardDomainsArray() {
        let c = GrovsUnityConfig.from(infoDictionary: ["GrovsClipboardDomains": ["a.com"]])
        XCTAssertEqual(c.clipboardDomains, ["a.com"])
    }

    func testClipboardDomainsCommaString() {
        let c = GrovsUnityConfig.from(infoDictionary: ["GrovsClipboardDomains": "a.com, b.com"])
        XCTAssertEqual(c.clipboardDomains, ["a.com", "b.com"])
    }

    func testStringBooleans() {
        let c = GrovsUnityConfig.from(infoDictionary: ["GrovsEnabled": "false", "GrovsUnityTrackScenes": "true"])
        XCTAssertFalse(c.startEnabled)
        XCTAssertTrue(c.trackScenes)
    }

    func testLogLevelValues() {
        func level(_ v: Any) -> Int { GrovsUnityConfig.from(infoDictionary: ["GrovsLogLevel": v]).logLevel }
        XCTAssertEqual(level("info"), 0)
        XCTAssertEqual(level("error"), 2)
        XCTAssertEqual(level("bogus"), 2)
    }

    func testConsentStoreAndRead() {
        let d = UserDefaults(suiteName: "grovs_unity_tests")!
        d.removePersistentDomain(forName: "grovs_unity_tests")
        XCTAssertNil(GrovsUnityConsent.stored(d))
        GrovsUnityConsent.store(d, false)
        XCTAssertEqual(GrovsUnityConsent.stored(d), false)
        GrovsUnityConsent.store(d, true)
        XCTAssertEqual(GrovsUnityConsent.stored(d), true)
        d.removePersistentDomain(forName: "grovs_unity_tests")
    }
}
