import Foundation

/// Holds messages until the sink is ready, then delivers them in arrival order.
/// Types with a limit drop their oldest entry when full. Other types are never dropped.
final class GrovsUnityMessageQueue {
    private struct Message {
        let type: String
        let json: String
    }

    private let limits: [String: Int]
    private let lock = DispatchQueue(label: "io.grovs.unity.message-queue")
    private var pending: [Message] = []
    private var sink: ((String, String) -> Void)?

    init(limits: [String: Int] = ["deeplink": 20, "inapp_event": 50, "error": 20]) {
        self.limits = limits
    }

    var isReady: Bool {
        lock.sync { sink != nil }
    }

    func post(_ type: String, _ json: String) {
        let target: ((String, String) -> Void)? = lock.sync {
            if let sink = sink { return sink }
            pending.append(Message(type: type, json: json))
            if let limit = limits[type], pending.filter({ $0.type == type }).count > limit,
               let oldest = pending.firstIndex(where: { $0.type == type }) {
                pending.remove(at: oldest)
            }
            return nil
        }
        target?(type, json)
    }

    /// The sink is published only once the buffer is empty, so messages posted while
    /// flushing queue up behind older ones and order is kept.
    func setReady(_ sink: @escaping (String, String) -> Void) {
        while true {
            let batch: [Message] = lock.sync {
                if pending.isEmpty {
                    self.sink = sink
                    return []
                }
                let drained = pending
                pending.removeAll()
                return drained
            }
            if batch.isEmpty { return }
            batch.forEach { sink($0.type, $0.json) }
        }
    }
}
