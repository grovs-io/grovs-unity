#import <Foundation/Foundation.h>
#import <UserNotifications/UserNotifications.h>

NS_ASSUME_NONNULL_BEGIN

typedef BOOL (^GrovsUnityNotificationResponseHandler)(UNNotificationResponse *response);

/// Anything with a notification-center delegate. UNUserNotificationCenter conforms.
@protocol GrovsUnityNotificationCenter <NSObject>
@property (nonatomic, weak, nullable) id<UNUserNotificationCenterDelegate> delegate;
@end

/// Stays the notification-center delegate, gives taps to Grovs first, then forwards
/// everything to the delegate the app or another SDK set (`inner`).
@interface GrovsUnityNotificationProxy : NSObject <UNUserNotificationCenterDelegate>

@property (weak, nullable) id<UNUserNotificationCenterDelegate> inner;

/// Swizzles the real center's setDelegate:, then installs the proxy. Returns the existing
/// proxy when the center already has one.
+ (instancetype)installOn:(UNUserNotificationCenter *)center onResponse:(GrovsUnityNotificationResponseHandler)onResponse;

/// After this runs, `center.delegate = X` sets `proxy.inner = X` and the proxy stays the delegate.
+ (void)swizzleSetDelegate;

/// Same as installOn:onResponse: without swizzling, for any center-like object.
+ (instancetype)installOnCenter:(id<GrovsUnityNotificationCenter>)center
                     onResponse:(GrovsUnityNotificationResponseHandler)onResponse;

/// Same as swizzleSetDelegate for any center-like class. Safe to call more than once.
+ (void)swizzleSetDelegateOnClass:(Class)centerClass;

@end

NS_ASSUME_NONNULL_END
