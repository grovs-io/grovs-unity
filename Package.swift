// swift-tools-version: 5.9
import PackageDescription

let package = Package(
    name: "GrovsUnityBridge",
    platforms: [.iOS(.v13)],
    products: [
        .library(name: "GrovsUnityBridge", targets: ["GrovsUnityBridge"]),
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
    ]
)
