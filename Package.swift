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
        .package(name: "Grovs", path: "../grovs-iOS"),
    ],
    targets: [
        .target(
            name: "GrovsUnityBridge",
            dependencies: [.product(name: "Grovs", package: "Grovs")],
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
