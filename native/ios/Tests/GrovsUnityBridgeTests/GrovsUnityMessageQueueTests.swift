import XCTest
@testable import GrovsUnityBridge

final class GrovsUnityMessageQueueTests: XCTestCase {
    private final class Recorder {
        var messages: [(String, String)] = []
        func sink(_ type: String, _ json: String) { messages.append((type, json)) }
        func values(_ type: String? = nil) -> [String] {
            messages.filter { type == nil || $0.0 == type }.map { $0.1 }
        }
    }

    private func range(_ a: Int, _ b: Int) -> [String] { (a...b).map(String.init) }

    func testBuffersUntilReadyThenFlushesInOrder() {
        let q = GrovsUnityMessageQueue()
        let r = Recorder()
        q.post("deeplink", "1")
        q.post("log", "2")
        q.post("error", "3")
        XCTAssertFalse(q.isReady)
        XCTAssertTrue(r.messages.isEmpty)
        q.setReady(r.sink)
        XCTAssertTrue(q.isReady)
        XCTAssertEqual(r.messages.map { "\($0.0):\($0.1)" }, ["deeplink:1", "log:2", "error:3"])
    }

    func testDropsOldestDeeplinkPast20() {
        let q = GrovsUnityMessageQueue()
        let r = Recorder()
        for i in 1...25 { q.post("deeplink", String(i)) }
        q.setReady(r.sink)
        XCTAssertEqual(r.values(), range(6, 25))
    }

    func testDropsOldestInAppEventPast50AndErrorPast20() {
        let q = GrovsUnityMessageQueue()
        let r = Recorder()
        for i in 1...60 { q.post("inapp_event", String(i)) }
        for i in 1...30 { q.post("error", String(i)) }
        q.setReady(r.sink)
        XCTAssertEqual(r.values("inapp_event"), range(11, 60))
        XCTAssertEqual(r.values("error"), range(11, 30))
    }

    func testDropKeepsOtherTypesInOrder() {
        let q = GrovsUnityMessageQueue(limits: ["deeplink": 2])
        let r = Recorder()
        q.post("deeplink", "a")
        q.post("result", "r")
        q.post("deeplink", "b")
        q.post("deeplink", "c")
        q.setReady(r.sink)
        XCTAssertEqual(r.messages.map { "\($0.0):\($0.1)" }, ["result:r", "deeplink:b", "deeplink:c"])
    }

    func testNeverDropsResults() {
        let q = GrovsUnityMessageQueue()
        let r = Recorder()
        for i in 1...100 { q.post("result", String(i)) }
        q.setReady(r.sink)
        XCTAssertEqual(r.messages.count, 100)
    }

    func testAfterReadyDeliversImmediately() {
        let q = GrovsUnityMessageQueue()
        let r = Recorder()
        q.setReady(r.sink)
        q.post("deeplink", "x")
        XCTAssertEqual(r.messages.map { "\($0.0):\($0.1)" }, ["deeplink:x"])
    }
}
