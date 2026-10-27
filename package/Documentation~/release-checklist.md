# Release checklist

Run on a real iOS device and a real Android device for every release. Write the device, OS version and build next to each result.

## Publishing

- [ ] The OpenUPM package entry uses `gitTagPrefix: upm/`, so it builds only from `upm/X.Y.Z` tags. Plain `X.Y.Z` tags have no AARs and keep the `package/` folder layout, so add `gitTagIgnore` for them if OpenUPM picks them up.
- [ ] The OpenUPM page lists the new `upm/X.Y.Z` version, and that version has both AARs in `Runtime/Plugins/Android`.

## iOS

- [ ] Cold start from a universal link
- [ ] Cold start from the custom scheme
- [ ] Cold start from a push tap
- [ ] Warm start from a universal link
- [ ] Warm start from the custom scheme
- [ ] Warm start from a push tap
- [ ] Deferred link after a fresh install (fingerprint)
- [ ] Deferred link after a fresh install (clipboard)
- [ ] `HandleUrl` from the game UI
- [ ] Consent: start disabled, enable, and the launch link is replayed
- [ ] Link generation
- [ ] Link details
- [ ] Events and screen views visible in the dashboard
- [ ] Revenue via Unity IAP (sandbox), IAP 4 `Product` path
- [ ] Revenue via Unity IAP (sandbox), IAP 5 `Product` path
- [ ] Revenue via Unity IAP (sandbox), IAP 5 `Order` path
- [ ] In-app messages: each template over a running game
- [ ] In-app messages: the veto
- [ ] In-app messages: key_value payload
- [ ] Firebase Unity coexistence: the game's `MessageReceived` still fires and the Grovs push open is attributed
- [ ] Unity Mobile Notifications coexistence (delegate chain)

## Android

- [ ] Cold start from an App Link
- [ ] Cold start from the custom scheme
- [ ] Cold start from a push tap
- [ ] Warm start from an App Link
- [ ] Warm start from the custom scheme
- [ ] Warm start from a push tap
- [ ] Deferred link after a fresh install (fingerprint)
- [ ] `HandleUrl` from the game UI
- [ ] Consent: start disabled, enable, and the launch link is replayed
- [ ] Link generation
- [ ] Link details
- [ ] Events and screen views visible in the dashboard
- [ ] Revenue via Unity IAP (sandbox), IAP 4 `Product` path
- [ ] Revenue via Unity IAP (sandbox), IAP 5 `Product` path
- [ ] Revenue via Unity IAP (sandbox), IAP 5 `Order` path
- [ ] In-app messages: each template over a running game
- [ ] In-app messages: the veto
- [ ] In-app messages: key_value payload
- [ ] Firebase Unity coexistence: the game's `MessageReceived` still fires and the Grovs push open is attributed
