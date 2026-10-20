#import <XCTest/XCTest.h>
#import <UserNotifications/UserNotifications.h>
#import <objc/runtime.h>
#import "GrovsUnityNotificationProxy.h"

// UNUserNotificationCenter needs a real app bundle, and UNNotificationResponse / UNNotification
// have no public initializers, so the tests use plain stand-in objects in their place.

@interface GrovsFakeCenter : NSObject <GrovsUnityNotificationCenter>
@property (nonatomic, weak, nullable) id<UNUserNotificationCenterDelegate> delegate;
@end

@implementation GrovsFakeCenter
@end

@interface GrovsFakeDelegate : NSObject <UNUserNotificationCenterDelegate>
@property (nonatomic, strong) NSMutableArray<NSString *> *calls;
@property (nonatomic) UNNotificationPresentationOptions options;
@end

@implementation GrovsFakeDelegate

- (instancetype)init
{
    if ((self = [super init]))
        _calls = [NSMutableArray array];
    return self;
}

- (void)userNotificationCenter:(UNUserNotificationCenter *)center
    didReceiveNotificationResponse:(UNNotificationResponse *)response
             withCompletionHandler:(void (^)(void))completionHandler
{
    [self.calls addObject:@"inner.didReceive"];
    completionHandler();
}

- (void)userNotificationCenter:(UNUserNotificationCenter *)center
       willPresentNotification:(UNNotification *)notification
         withCompletionHandler:(void (^)(UNNotificationPresentationOptions))completionHandler
{
    [self.calls addObject:@"inner.willPresent"];
    completionHandler(self.options);
}

@end

@interface GrovsFakeSettingsDelegate : GrovsFakeDelegate
@end

@implementation GrovsFakeSettingsDelegate

- (void)userNotificationCenter:(UNUserNotificationCenter *)center
    openSettingsForNotification:(UNNotification *)notification
{
    [self.calls addObject:@"inner.openSettings"];
}

@end

@interface GrovsFakeDoubleCompletionDelegate : NSObject <UNUserNotificationCenterDelegate>
@end

@implementation GrovsFakeDoubleCompletionDelegate

- (void)userNotificationCenter:(UNUserNotificationCenter *)center
    didReceiveNotificationResponse:(UNNotificationResponse *)response
             withCompletionHandler:(void (^)(void))completionHandler
{
    completionHandler();
    completionHandler();
}

@end

@interface GrovsUnityNotificationProxyTests : XCTestCase
@end

@implementation GrovsUnityNotificationProxyTests

- (void)setUp
{
    [GrovsUnityNotificationProxy swizzleSetDelegateOnClass:[GrovsFakeCenter class]];
}

- (GrovsUnityNotificationProxy *)installOn:(GrovsFakeCenter *)center log:(NSMutableArray<NSString *> *)log
{
    return [GrovsUnityNotificationProxy installOnCenter:center
                                             onResponse:^BOOL(UNNotificationResponse *response) {
        [log addObject:@"grovs"];
        return YES;
    }];
}

- (UNUserNotificationCenter *)asCenter:(GrovsFakeCenter *)center
{
    return (UNUserNotificationCenter *)(id)center;
}

- (void)testInstallWrapsExistingDelegate
{
    GrovsFakeCenter *center = [GrovsFakeCenter new];
    GrovsFakeDelegate *existing = [GrovsFakeDelegate new];
    center.delegate = existing;

    GrovsUnityNotificationProxy *proxy = [self installOn:center log:[NSMutableArray array]];

    XCTAssertEqual(proxy.inner, existing);
    XCTAssertEqual(center.delegate, proxy);
}

- (void)testInstallTwiceReturnsSameProxy
{
    GrovsFakeCenter *center = [GrovsFakeCenter new];
    GrovsUnityNotificationProxy *first = [self installOn:center log:[NSMutableArray array]];
    GrovsUnityNotificationProxy *second = [self installOn:center log:[NSMutableArray array]];

    XCTAssertEqual(first, second);
    XCTAssertEqual(center.delegate, first);
    XCTAssertNil(first.inner);
}

- (void)testLaterSetDelegateBecomesInner
{
    GrovsFakeCenter *center = [GrovsFakeCenter new];
    GrovsUnityNotificationProxy *proxy = [self installOn:center log:[NSMutableArray array]];
    GrovsFakeDelegate *later = [GrovsFakeDelegate new];

    center.delegate = later;

    XCTAssertEqual(proxy.inner, later);
    XCTAssertEqual(center.delegate, proxy);

    center.delegate = proxy;
    XCTAssertEqual(proxy.inner, later);
    XCTAssertEqual(center.delegate, proxy);
}

- (void)testSetDelegateWithoutProxyIsUnchanged
{
    GrovsFakeCenter *center = [GrovsFakeCenter new];
    GrovsFakeDelegate *delegate = [GrovsFakeDelegate new];

    center.delegate = delegate;

    XCTAssertEqual(center.delegate, delegate);
}

- (void)testSwizzleIsIdempotent
{
    [GrovsUnityNotificationProxy swizzleSetDelegateOnClass:[GrovsFakeCenter class]];
    [GrovsUnityNotificationProxy swizzleSetDelegateOnClass:[GrovsFakeCenter class]];
    GrovsFakeCenter *center = [GrovsFakeCenter new];
    GrovsUnityNotificationProxy *proxy = [self installOn:center log:[NSMutableArray array]];
    GrovsFakeDelegate *later = [GrovsFakeDelegate new];

    center.delegate = later;

    XCTAssertEqual(proxy.inner, later);
    XCTAssertEqual(center.delegate, proxy);
}

- (void)testRealCenterHasSetDelegateToSwizzle
{
    [GrovsUnityNotificationProxy swizzleSetDelegate];
    [GrovsUnityNotificationProxy swizzleSetDelegate];

    XCTAssertTrue(class_getInstanceMethod([UNUserNotificationCenter class], @selector(setDelegate:)) != NULL);
}

- (void)testDidReceiveCallsGrovsThenInner
{
    GrovsFakeCenter *center = [GrovsFakeCenter new];
    GrovsFakeDelegate *inner = [GrovsFakeDelegate new];
    center.delegate = inner;
    GrovsUnityNotificationProxy *proxy = [self installOn:center log:inner.calls];
    __block NSInteger completions = 0;

    [proxy userNotificationCenter:[self asCenter:center]
        didReceiveNotificationResponse:(UNNotificationResponse *)[NSObject new]
                 withCompletionHandler:^{ completions++; }];

    XCTAssertEqualObjects(inner.calls, (@[ @"grovs", @"inner.didReceive" ]));
    XCTAssertEqual(completions, 1);
}

- (void)testDidReceiveNoInnerCallsCompletion
{
    GrovsFakeCenter *center = [GrovsFakeCenter new];
    NSMutableArray<NSString *> *log = [NSMutableArray array];
    GrovsUnityNotificationProxy *proxy = [self installOn:center log:log];
    __block NSInteger completions = 0;

    [proxy userNotificationCenter:[self asCenter:center]
        didReceiveNotificationResponse:(UNNotificationResponse *)[NSObject new]
                 withCompletionHandler:^{ completions++; }];

    XCTAssertEqualObjects(log, @[ @"grovs" ]);
    XCTAssertEqual(completions, 1);
}

- (void)testDidReceiveInnerCallingTwiceCompletesOnce
{
    GrovsFakeCenter *center = [GrovsFakeCenter new];
    GrovsFakeDoubleCompletionDelegate *inner = [GrovsFakeDoubleCompletionDelegate new];
    center.delegate = inner;
    GrovsUnityNotificationProxy *proxy = [self installOn:center log:[NSMutableArray array]];
    __block NSInteger completions = 0;

    [proxy userNotificationCenter:[self asCenter:center]
        didReceiveNotificationResponse:(UNNotificationResponse *)[NSObject new]
                 withCompletionHandler:^{ completions++; }];

    XCTAssertEqual(completions, 1);
}

- (void)testWillPresentForwardsOrDefaults
{
    GrovsFakeCenter *center = [GrovsFakeCenter new];
    GrovsFakeDelegate *inner = [GrovsFakeDelegate new];
    inner.options = UNNotificationPresentationOptionBadge;
    center.delegate = inner;
    GrovsUnityNotificationProxy *proxy = [self installOn:center log:[NSMutableArray array]];
    UNNotification *notification = (UNNotification *)[NSObject new];

    __block NSInteger forwardedCount = 0;
    __block UNNotificationPresentationOptions forwarded = 0;
    [proxy userNotificationCenter:[self asCenter:center]
          willPresentNotification:notification
            withCompletionHandler:^(UNNotificationPresentationOptions options) {
        forwardedCount++;
        forwarded = options;
    }];
    XCTAssertEqual(forwardedCount, 1);
    XCTAssertEqual(forwarded, UNNotificationPresentationOptionBadge);
    XCTAssertEqualObjects(inner.calls, @[ @"inner.willPresent" ]);

    proxy.inner = nil;
    __block NSInteger defaultCount = 0;
    __block UNNotificationPresentationOptions defaults = 0;
    [proxy userNotificationCenter:[self asCenter:center]
          willPresentNotification:notification
            withCompletionHandler:^(UNNotificationPresentationOptions options) {
        defaultCount++;
        defaults = options;
    }];

    UNNotificationPresentationOptions expected;
    if (@available(iOS 14.0, *))
        expected = UNNotificationPresentationOptionBanner | UNNotificationPresentationOptionSound |
                   UNNotificationPresentationOptionList;
    else
        expected = UNNotificationPresentationOptionAlert | UNNotificationPresentationOptionSound;
    XCTAssertEqual(defaultCount, 1);
    XCTAssertEqual(defaults, expected);
}

- (void)testRespondsToSelectorForwarding
{
    GrovsFakeCenter *center = [GrovsFakeCenter new];
    GrovsUnityNotificationProxy *proxy = [self installOn:center log:[NSMutableArray array]];
    SEL openSettings = @selector(userNotificationCenter:openSettingsForNotification:);

    XCTAssertFalse([proxy respondsToSelector:openSettings]);

    GrovsFakeDelegate *plain = [GrovsFakeDelegate new];
    center.delegate = plain;
    XCTAssertFalse([proxy respondsToSelector:openSettings]);

    GrovsFakeSettingsDelegate *settings = [GrovsFakeSettingsDelegate new];
    center.delegate = settings;
    XCTAssertTrue([proxy respondsToSelector:openSettings]);
    XCTAssertEqual([proxy forwardingTargetForSelector:openSettings], settings);

    [(id<UNUserNotificationCenterDelegate>)proxy userNotificationCenter:[self asCenter:center]
                                            openSettingsForNotification:nil];
    XCTAssertEqualObjects(settings.calls, @[ @"inner.openSettings" ]);
}

@end
