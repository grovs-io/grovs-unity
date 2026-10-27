import Foundation
import UserNotifications

public typealias GrovsUnityMessageCallback = @convention(c) (UnsafePointer<CChar>?, UnsafePointer<CChar>?) -> Void
public typealias GrovsUnityShouldShowCallback = @convention(c) (UnsafePointer<CChar>?) -> Bool

private var bridge: GrovsUnityBridge { GrovsUnityBridge.shared }

private func text(_ pointer: UnsafePointer<CChar>?) -> String? {
    pointer.map { String(cString: $0) }
}

private func onMain(_ block: @escaping () -> Void) {
    if Thread.isMainThread {
        block()
    } else {
        DispatchQueue.main.async(execute: block)
    }
}

private func onMainSync<T>(_ block: () -> T) -> T {
    Thread.isMainThread ? block() : DispatchQueue.main.sync(execute: block)
}

// MARK: Called from C#

@_cdecl("GrovsUnity_RegisterCallbacks")
public func GrovsUnity_RegisterCallbacks(_ onMessage: GrovsUnityMessageCallback?,
                                         _ shouldShow: GrovsUnityShouldShowCallback?) {
    onMain {
        let sink: (String, String) -> Void = { type, json in
            type.withCString { typePointer in
                json.withCString { jsonPointer in onMessage?(typePointer, jsonPointer) }
            }
        }
        let veto = shouldShow.map { callback in
            { (campaignId: String) -> Bool in campaignId.withCString { callback($0) } }
        }
        bridge.register(onMessage: sink, shouldShow: veto)
    }
}

@_cdecl("GrovsUnity_SetEnabled")
public func GrovsUnity_SetEnabled(_ enabled: Bool) {
    onMain { bridge.setEnabled(enabled) }
}

@_cdecl("GrovsUnity_IsEnabled")
public func GrovsUnity_IsEnabled() -> Bool {
    onMainSync { bridge.isEnabled }
}

@_cdecl("GrovsUnity_IsSceneTrackingEnabled")
public func GrovsUnity_IsSceneTrackingEnabled() -> Bool {
    onMainSync { bridge.isSceneTrackingEnabled }
}

@_cdecl("GrovsUnity_SetLogLevel")
public func GrovsUnity_SetLogLevel(_ level: Int32) {
    onMain { bridge.setLogLevel(Int(level)) }
}

@_cdecl("GrovsUnity_SetUserIdentifier")
public func GrovsUnity_SetUserIdentifier(_ identifier: UnsafePointer<CChar>?) {
    let identifier = text(identifier)
    onMain { bridge.setUserIdentifier(identifier) }
}

@_cdecl("GrovsUnity_SetUserAttributes")
public func GrovsUnity_SetUserAttributes(_ json: UnsafePointer<CChar>?) {
    let json = text(json)
    onMain { bridge.setUserAttributes(json) }
}

@_cdecl("GrovsUnity_SetPushToken")
public func GrovsUnity_SetPushToken(_ token: UnsafePointer<CChar>?) {
    let token = text(token)
    onMain { bridge.setPushToken(token) }
}

@_cdecl("GrovsUnity_GenerateLink")
public func GrovsUnity_GenerateLink(_ requestId: Int32, _ optionsJSON: UnsafePointer<CChar>?) {
    let options = text(optionsJSON)
    onMain { bridge.generateLink(requestId: requestId, optionsJSON: options) }
}

@_cdecl("GrovsUnity_LinkDetails")
public func GrovsUnity_LinkDetails(_ requestId: Int32, _ path: UnsafePointer<CChar>?) {
    let path = text(path) ?? ""
    onMain { bridge.linkDetails(requestId: requestId, path: path) }
}

@_cdecl("GrovsUnity_HandleUrl")
public func GrovsUnity_HandleUrl(_ url: UnsafePointer<CChar>?) -> Bool {
    guard let url = text(url) else { return false }
    return onMainSync { bridge.handleUrl(url) }
}

@_cdecl("GrovsUnity_Track")
public func GrovsUnity_Track(_ name: UnsafePointer<CChar>?, _ propertiesJSON: UnsafePointer<CChar>?,
                             _ tagsJSON: UnsafePointer<CChar>?) {
    guard let name = text(name) else { return }
    let properties = text(propertiesJSON)
    let tags = text(tagsJSON)
    onMain { bridge.track(name, propertiesJSON: properties, tagsJSON: tags) }
}

@_cdecl("GrovsUnity_TrackScreenView")
public func GrovsUnity_TrackScreenView(_ name: UnsafePointer<CChar>?, _ propertiesJSON: UnsafePointer<CChar>?) {
    guard let name = text(name) else { return }
    let properties = text(propertiesJSON)
    onMain { bridge.trackScreenView(name, propertiesJSON: properties) }
}

@_cdecl("GrovsUnity_SetGlobalTags")
public func GrovsUnity_SetGlobalTags(_ json: UnsafePointer<CChar>?) {
    let json = text(json)
    onMain { bridge.setGlobalTags(json) }
}

@_cdecl("GrovsUnity_SetScreenAliases")
public func GrovsUnity_SetScreenAliases(_ json: UnsafePointer<CChar>?) {
    let json = text(json)
    onMain { bridge.setScreenAliases(json) }
}

@_cdecl("GrovsUnity_LogInAppPurchase")
public func GrovsUnity_LogInAppPurchase(_ requestId: Int32, _ transaction: UnsafePointer<CChar>?) {
    let transaction = text(transaction) ?? ""
    onMain { bridge.logInAppPurchase(requestId: requestId, transaction: transaction) }
}

@_cdecl("GrovsUnity_LogCustomPurchase")
public func GrovsUnity_LogCustomPurchase(_ requestId: Int32, _ type: Int32, _ priceInCents: Int32,
                                         _ currency: UnsafePointer<CChar>?, _ productId: UnsafePointer<CChar>?,
                                         _ epochMs: Int64) {
    let currency = text(currency) ?? ""
    let productId = text(productId) ?? ""
    onMain {
        bridge.logCustomPurchase(requestId: requestId, type: type, priceInCents: priceInCents, currency: currency,
                                 productID: productId, epochMs: epochMs)
    }
}

@_cdecl("GrovsUnity_IsGrovsNotification")
public func GrovsUnity_IsGrovsNotification(_ json: UnsafePointer<CChar>?) -> Bool {
    let json = text(json)
    return onMainSync { bridge.isGrovsNotification(json) }
}

@_cdecl("GrovsUnity_HandleNotificationOpen")
public func GrovsUnity_HandleNotificationOpen(_ json: UnsafePointer<CChar>?) -> Bool {
    let json = text(json)
    return onMainSync { bridge.handleNotificationOpen(json) }
}

@_cdecl("GrovsUnity_PauseInApp")
public func GrovsUnity_PauseInApp() {
    onMain { bridge.pauseInApp() }
}

@_cdecl("GrovsUnity_ResumeInApp")
public func GrovsUnity_ResumeInApp() {
    onMain { bridge.resumeInApp() }
}

@_cdecl("GrovsUnity_DiscardInApp")
public func GrovsUnity_DiscardInApp(_ dismissCurrent: Bool) {
    onMain { bridge.discardInApp(dismissCurrent: dismissCurrent) }
}

@_cdecl("GrovsUnity_SetInAppExcludedScreens")
public func GrovsUnity_SetInAppExcludedScreens(_ json: UnsafePointer<CChar>?) {
    let json = text(json)
    onMain { bridge.setInAppExcludedScreens(json) }
}

@_cdecl("GrovsUnity_SetInAppDebugLogging")
public func GrovsUnity_SetInAppDebugLogging(_ enabled: Bool) {
    onMain { bridge.setInAppDebugLogging(enabled) }
}

// MARK: Called from GrovsUnityAppHooks.mm

@_cdecl("GrovsUnity_Internal_AutoInit")
public func GrovsUnity_Internal_AutoInit() {
    onMain { bridge.start(config: .from(infoDictionary: Bundle.main.infoDictionary ?? [:])) }
}

@_cdecl("GrovsUnity_Internal_OpenURL")
public func GrovsUnity_Internal_OpenURL(_ nsurl: UnsafeMutableRawPointer?) -> Bool {
    guard let nsurl = nsurl else { return false }
    let url = Unmanaged<NSURL>.fromOpaque(nsurl).takeUnretainedValue() as URL
    return onMainSync { bridge.openURL(url) }
}

@_cdecl("GrovsUnity_Internal_ContinueUserActivity")
public func GrovsUnity_Internal_ContinueUserActivity(_ nsUserActivity: UnsafeMutableRawPointer?) -> Bool {
    guard let nsUserActivity = nsUserActivity else { return false }
    let activity = Unmanaged<NSUserActivity>.fromOpaque(nsUserActivity).takeUnretainedValue()
    return onMainSync { bridge.continueUserActivity(activity) }
}

@_cdecl("GrovsUnity_Internal_SetPushToken")
public func GrovsUnity_Internal_SetPushToken(_ nsdata: UnsafeMutableRawPointer?) {
    guard let nsdata = nsdata else { return }
    let token = Unmanaged<NSData>.fromOpaque(nsdata).takeUnretainedValue() as Data
    onMain { bridge.setPushToken(deviceToken: token) }
}

@_cdecl("GrovsUnity_Internal_HandleNotificationResponse")
public func GrovsUnity_Internal_HandleNotificationResponse(_ response: UnsafeMutableRawPointer?) -> Bool {
    guard let response = response else { return false }
    let notificationResponse = Unmanaged<UNNotificationResponse>.fromOpaque(response).takeUnretainedValue()
    return onMainSync { bridge.handleNotificationResponse(notificationResponse) }
}
