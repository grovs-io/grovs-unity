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
            path: "package/Runtime/Plugins/iOS/GrovsUnityBridge"),
        .testTarget(
            name: "GrovsUnityBridgeTests",
            dependencies: ["GrovsUnityBridge"],
            path: "native/ios/Tests/GrovsUnityBridgeTests"),
        .target(
            name: "GrovsUnityHooks",
            path: "package/Runtime/Plugins/iOS/GrovsUnityHooks",
            publicHeadersPath: "include"),
        .testTarget(
            name: "GrovsUnityHooksTests",
            dependencies: ["GrovsUnityHooks"],
            path: "native/ios/Tests/GrovsUnityHooksTests"),
    ]
)
