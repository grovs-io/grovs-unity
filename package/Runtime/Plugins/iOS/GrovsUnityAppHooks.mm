#import <UIKit/UIKit.h>
#import <UserNotifications/UserNotifications.h>
#import <objc/runtime.h>
#import "AppDelegateListener.h"
#import "GrovsUnityNotificationProxy.h"
#import "UnityAppController.h"

extern "C" {
void GrovsUnity_Internal_AutoInit(void);
bool GrovsUnity_Internal_OpenURL(void* nsurl);
bool GrovsUnity_Internal_ContinueUserActivity(void* nsUserActivity);
void GrovsUnity_Internal_SetPushToken(void* nsdata);
bool GrovsUnity_Internal_HandleNotificationResponse(void* unNotificationResponse);
}

static BOOL GrovsUnityOwnsNotificationDelegate(void)
{
    id value = NSBundle.mainBundle.infoDictionary[@"GrovsUnityOwnNotificationDelegate"];
    if ([value isKindOfClass:[NSNumber class]])
        return [value boolValue];
    if ([value isKindOfClass:[NSString class]])
    {
        NSString* text = [[value stringByTrimmingCharactersInSet:NSCharacterSet.whitespaceAndNewlineCharacterSet] lowercaseString];
        if ([text isEqualToString:@"false"] || [text isEqualToString:@"no"] || [text isEqualToString:@"0"])
            return NO;
    }
    return YES;
}

static void GrovsUnityInstallNotificationProxy(void)
{
    if (!GrovsUnityOwnsNotificationDelegate())
        return;
    [GrovsUnityNotificationProxy installOn:[UNUserNotificationCenter currentNotificationCenter]
                                onResponse:^BOOL(UNNotificationResponse* response) {
        return GrovsUnity_Internal_HandleNotificationResponse((__bridge void*)response);
    }];
}

typedef void (^GrovsUnityRestorationHandler)(NSArray<id<UIUserActivityRestoring>>* _Nullable);
typedef BOOL (*GrovsUnityContinueIMP)(id, SEL, UIApplication*, NSUserActivity*, GrovsUnityRestorationHandler);

static GrovsUnityContinueIMP GrovsUnityOriginalContinue = NULL;

static BOOL GrovsUnityContinueUserActivity(id self, SEL _cmd, UIApplication* application, NSUserActivity* userActivity,
                                           GrovsUnityRestorationHandler restorationHandler)
{
    BOOL handled = NO;
    if (GrovsUnityOriginalContinue != NULL)
        handled = GrovsUnityOriginalContinue(self, _cmd, application, userActivity, restorationHandler);
    BOOL grovsHandled = userActivity != nil && GrovsUnity_Internal_ContinueUserActivity((__bridge void*)userActivity);
    return handled || grovsHandled;
}

static void GrovsUnityHookContinueUserActivity(void)
{
    Class cls = [UnityAppController class];
    SEL selector = @selector(application:continueUserActivity:restorationHandler:);
    IMP replacement = (IMP)GrovsUnityContinueUserActivity;
    Method existing = class_getInstanceMethod(cls, selector);

    if (existing == NULL)
    {
        struct objc_method_description description =
            protocol_getMethodDescription(@protocol(UIApplicationDelegate), selector, NO, YES);
        class_addMethod(cls, selector, replacement, description.types != NULL ? description.types : "B@:@@@?");
        return;
    }

    IMP original = method_getImplementation(existing);
    if (class_addMethod(cls, selector, replacement, method_getTypeEncoding(existing)))
        GrovsUnityOriginalContinue = (GrovsUnityContinueIMP)original;  // inherited from a superclass
    else
        GrovsUnityOriginalContinue = (GrovsUnityContinueIMP)method_setImplementation(existing, replacement);
}

@interface GrovsUnityAppHooks : NSObject<AppDelegateListener>
@end

@implementation GrovsUnityAppHooks

+ (void)load
{
    static GrovsUnityAppHooks* listener = nil;
    static dispatch_once_t once;
    dispatch_once(&once, ^{
        // Posted by UnityAppController from willFinishLaunchingWithOptions, before didFinishLaunching returns.
        [[NSNotificationCenter defaultCenter] addObserverForName:kUnityWillFinishLaunchingWithOptions
                                                          object:nil
                                                           queue:nil
                                                      usingBlock:^(__unused NSNotification* notification) {
            GrovsUnityInstallNotificationProxy();
        }];
        [[NSNotificationCenter defaultCenter] addObserverForName:UIApplicationDidFinishLaunchingNotification
                                                          object:nil
                                                           queue:nil
                                                      usingBlock:^(__unused NSNotification* notification) {
            GrovsUnity_Internal_AutoInit();
        }];
        listener = [[GrovsUnityAppHooks alloc] init];
        UnityRegisterAppDelegateListener(listener);
        GrovsUnityHookContinueUserActivity();
    });
}

- (void)onOpenURL:(NSNotification*)notification
{
    id url = notification.userInfo[@"url"];
    if ([url isKindOfClass:[NSURL class]])
        GrovsUnity_Internal_OpenURL((__bridge void*)url);
}

- (void)didRegisterForRemoteNotificationsWithDeviceToken:(NSNotification*)notification
{
    // Unity posts the token NSData itself as the notification's userInfo.
    id token = notification.userInfo;
    if ([token isKindOfClass:[NSData class]])
        GrovsUnity_Internal_SetPushToken((__bridge void*)token);
}

@end
