#import <UIKit/UIKit.h>
#import <UserNotifications/UserNotifications.h>

extern "C" UIViewController* UnityGetGLViewController(void);

typedef void (*GrovsTestAppPermissionCallback)(int status);

// Returned strings are malloc'd; the C# side frees them.
static char* GrovsTestAppCopy(NSString* text)
{
    return text == nil ? NULL : strdup(text.UTF8String);
}

static int GrovsTestAppStatusCode(UNAuthorizationStatus status)
{
    switch (status)
    {
        case UNAuthorizationStatusAuthorized:
        case UNAuthorizationStatusProvisional:
        case UNAuthorizationStatusEphemeral:
            return 0;
        case UNAuthorizationStatusDenied:
            return 1;
        default:
            return 2;
    }
}

extern "C" {

bool GrovsTestApp_IsDarkMode(void)
{
    return UIScreen.mainScreen.traitCollection.userInterfaceStyle == UIUserInterfaceStyleDark;
}

void GrovsTestApp_Share(const char* text)
{
    NSString* value = text ? [NSString stringWithUTF8String:text] : @"";
    dispatch_async(dispatch_get_main_queue(), ^{
        UIViewController* presenter = UnityGetGLViewController();
        UIActivityViewController* sheet = [[UIActivityViewController alloc] initWithActivityItems:@[ value ] applicationActivities:nil];
        UIPopoverPresentationController* popover = sheet.popoverPresentationController;
        if (popover != nil)
        {
            popover.sourceView = presenter.view;
            popover.sourceRect = CGRectMake(CGRectGetMidX(presenter.view.bounds), CGRectGetMaxY(presenter.view.bounds) - 1, 1, 1);
            popover.permittedArrowDirections = UIPopoverArrowDirectionDown;
        }
        [presenter presentViewController:sheet animated:YES completion:nil];
    });
}

// 0 allowed, 1 not allowed, 2 unknown. The completion runs on a background queue, so waiting here is safe.
int GrovsTestApp_NotificationStatus(void)
{
    __block int result = 2;
    dispatch_semaphore_t done = dispatch_semaphore_create(0);
    [UNUserNotificationCenter.currentNotificationCenter getNotificationSettingsWithCompletionHandler:^(UNNotificationSettings* settings) {
        result = GrovsTestAppStatusCode(settings.authorizationStatus);
        dispatch_semaphore_signal(done);
    }];
    dispatch_semaphore_wait(done, dispatch_time(DISPATCH_TIME_NOW, NSEC_PER_SEC));
    return result;
}

void GrovsTestApp_RequestNotifications(GrovsTestAppPermissionCallback callback)
{
    UNAuthorizationOptions options = UNAuthorizationOptionAlert | UNAuthorizationOptionBadge | UNAuthorizationOptionSound;
    [UNUserNotificationCenter.currentNotificationCenter requestAuthorizationWithOptions:options completionHandler:^(BOOL granted, NSError* error) {
        if (callback != NULL) callback(granted ? 0 : 1);
    }];
}

void GrovsTestApp_OpenSettings(void)
{
    dispatch_async(dispatch_get_main_queue(), ^{
        NSURL* url = [NSURL URLWithString:UIApplicationOpenSettingsURLString];
        [UIApplication.sharedApplication openURL:url options:@{} completionHandler:nil];
    });
}

// Booleans become "true"/"false" and arrays become comma separated text.
char* GrovsTestApp_InfoValue(const char* key)
{
    id value = [NSBundle.mainBundle objectForInfoDictionaryKey:[NSString stringWithUTF8String:key]];
    if (value == nil) return NULL;
    if ([value isKindOfClass:[NSArray class]])
        return GrovsTestAppCopy([(NSArray*)value componentsJoinedByString:@","]);
    if ([value isKindOfClass:[NSNumber class]] && CFGetTypeID((__bridge CFTypeRef)value) == CFBooleanGetTypeID())
        return GrovsTestAppCopy([value boolValue] ? @"true" : @"false");
    return GrovsTestAppCopy([NSString stringWithFormat:@"%@", value]);
}

}
