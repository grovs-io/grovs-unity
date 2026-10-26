import XCTest
@testable import GrovsUnityBridge

final class GrovsUnityCodecTests: XCTestCase {
    func testDoubleWholeStaysNumeric() {
        let parsed = GrovsUnityCodec.object(GrovsUnityCodec.json(["level": 3.0]))!
        XCTAssertEqual((parsed["level"] as? NSNumber)?.intValue, 3)
    }

    func testNestedRoundTrip() {
        let json = "{\"id\": 9007199254740993, \"n\": 1.5, \"nested\": {\"a\": [1, \"x\", null]}, \"emoji\": \"🎮\"}"
        let parsed = GrovsUnityCodec.object(json)!
        let again = GrovsUnityCodec.object(GrovsUnityCodec.json(parsed))!
        XCTAssertEqual((again["id"] as? NSNumber)?.int64Value, 9007199254740993)
        XCTAssertEqual((again["n"] as? NSNumber)?.doubleValue, 1.5)
        XCTAssertEqual(again["emoji"] as? String, "🎮")
        let a = (again["nested"] as! [String: Any])["a"] as! [Any]
        XCTAssertEqual(a.count, 3)
        XCTAssertEqual((a[0] as? NSNumber)?.intValue, 1)
        XCTAssertEqual(a[1] as? String, "x")
        XCTAssertTrue(a[2] is NSNull)
    }

    func testBoolNotEncodedAsNumber() {
        XCTAssertEqual(GrovsUnityCodec.json(["a": true]), "{\"a\":true}")
        XCTAssertEqual(GrovsUnityCodec.json(["a": NSNumber(value: false)]), "{\"a\":false}")
        XCTAssertEqual(GrovsUnityCodec.json(["a": NSNumber(value: 1)]), "{\"a\":1}")
    }

    func testDateAndURLConverted() {
        let date = Date(timeIntervalSince1970: 0)
        let url = URL(string: "https://x.test/p")!
        let parsed = GrovsUnityCodec.object(GrovsUnityCodec.json(["d": date, "u": url]))!
        XCTAssertEqual(parsed["d"] as? String, "1970-01-01T00:00:00Z")
        XCTAssertEqual(parsed["u"] as? String, "https://x.test/p")
    }

    func testNaNBecomesNull() {
        XCTAssertEqual(GrovsUnityCodec.json(["a": Double.nan]), "{\"a\":null}")
    }

    func testNilIsNull() {
        XCTAssertEqual(GrovsUnityCodec.json(nil), "null")
    }

    func testFragmentString() {
        XCTAssertEqual(GrovsUnityCodec.json("x"), "\"x\"")
    }

    func testResultJSONShape() {
        XCTAssertEqual(
            GrovsUnityCodec.resultJSON(requestId: 7, ok: false, value: nil, errorCode: "link_generation_failed", errorMessage: "boom"),
            "{\"requestId\":7,\"ok\":false,\"value\":null,\"error\":{\"code\":\"link_generation_failed\",\"message\":\"boom\"}}")
    }

    func testResultJSONOkHasNullError() {
        XCTAssertEqual(
            GrovsUnityCodec.resultJSON(requestId: 1, ok: true, value: "x", errorCode: nil, errorMessage: nil),
            "{\"requestId\":1,\"ok\":true,\"value\":\"x\",\"error\":null}")
    }

    func testStringList() {
        XCTAssertEqual(GrovsUnityCodec.stringList("[\"a\",\"b\"]"), ["a", "b"])
        XCTAssertNil(GrovsUnityCodec.stringList(nil))
        XCTAssertNil(GrovsUnityCodec.stringList("nope"))
    }

    func testObjectInvalidIsNil() {
        XCTAssertNil(GrovsUnityCodec.object(""))
        XCTAssertNil(GrovsUnityCodec.object(nil))
        XCTAssertNil(GrovsUnityCodec.object("[1]"))
    }

    func testErrorJSON() {
        XCTAssertEqual(GrovsUnityCodec.errorJSON(code: "unknown", message: nil), "{\"code\":\"unknown\",\"message\":null}")
    }

    private func nestedArrays(_ depth: Int) -> [Any] {
        depth == 1 ? [] : [nestedArrays(depth - 1)]
    }

    func testWritesUpToMaxDepth() {
        XCTAssertEqual(GrovsUnityCodec.json(nestedArrays(64)), String(repeating: "[", count: 64) + String(repeating: "]", count: 64))
    }

    func testWritesNullBeyondMaxDepth() {
        let truncated = String(repeating: "[", count: 64) + "null" + String(repeating: "]", count: 64)
        XCTAssertEqual(GrovsUnityCodec.json(nestedArrays(65)), truncated)
        XCTAssertEqual(GrovsUnityCodec.json(nestedArrays(500)), truncated)
    }
}
