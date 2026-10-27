# Changelog

## [1.0.0]

First release.

- Deep links with buffering until the first subscriber, `HandleUrl` for links inside the game, and an Editor deep link simulator.
- Link generation and link details, with Task and callback versions.
- User identifier and attributes, events, screen views, global tags, screen aliases and optional scene tracking.
- In-app messages: events, a veto callback, pause, resume, discard and excluded screens.
- Revenue: store purchases, custom purchases and a Unity IAP helper for IAP 4 and 5.
- Consent stored on the device and read before Unity starts.
- Push on iOS and Android, with Firebase Unity coexistence (tested with Firebase Unity 12.10.1) or a standalone Firebase setup.
- Build-time setup from Project Settings: Info.plist, entitlements, Xcode settings, Android meta-data and intent filters.
- Test app sample: a screen for every feature, with a log of every call.
- Native SDKs: CocoaPods `Grovs ~> 3.1`, Maven `io.grovs:Grovs:[3.1.0,3.2)`.
