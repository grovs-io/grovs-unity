// swift-tools-version: 5.9
import PackageDescription

let package = Package(
    name: "GrovsUnityBridge",
    platforms: [.iOS(.v13)],
    products: [
        .library(name: "GrovsUnityBridge", targets: ["GrovsUnityBridge"]),
        .library(name: "GrovsUnityHooks", targets: ["GrovsUnityHooks"]),
    ],
    dependencies: [
        .package(url: "https://github.com/grovs-io/grovs-iOS.git", from: "3.1.0"),
    ],
    targets: [
        .target(
            name: "GrovsUnityBridge",
            dependencies: [.product(name: "Grovs", package: "grovs-iOS")],
            path: "package/Runtime/Plugins/iOS/GrovsUnityBridge",
            // Unity writes a .meta file next to every source.
            exclude: [
                "GrovsFacade.swift.meta",
                "GrovsUnityBridge.swift.meta",
                "GrovsUnityCodec.swift.meta",
                "GrovsUnityConfig.swift.meta",
                "GrovsUnityExports.swift.meta",
                "GrovsUnityMessageQueue.swift.meta",
                "RealGrovsFacade.swift.meta",
            ]),
        .testTarget(
            name: "GrovsUnityBridgeTests",
            dependencies: ["GrovsUnityBridge"],
            path: "native/ios/Tests/GrovsUnityBridgeTests"),
        .target(
            name: "GrovsUnityHooks",
            path: "package/Runtime/Plugins/iOS/GrovsUnityHooks",
            exclude: [
                "GrovsUnityNotificationProxy.m.meta",
                "include.meta",
                "include/GrovsUnityNotificationProxy.h.meta",
            ],
            publicHeadersPath: "include"),
        .testTarget(
            name: "GrovsUnityHooksTests",
            dependencies: ["GrovsUnityHooks"],
            path: "native/ios/Tests/GrovsUnityHooksTests"),
    ]
)
