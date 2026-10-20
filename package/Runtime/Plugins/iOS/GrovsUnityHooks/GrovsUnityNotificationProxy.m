#import "GrovsUnityNotificationProxy.h"
#import <objc/runtime.h>

static char GrovsUnityProxyKey;

typedef void (*GrovsUnitySetDelegateIMP)(id, SEL, id);

static void (^GrovsUnityRunOnce(void (^block)(void)))(void)
{
    __block void (^pending)(void) = [block copy];
    NSObject *lock = [NSObject new];
    return ^{
        void (^run)(void);
        @synchronized(lock)
        {
            run = pending;
            pending = nil;
        }
        if (run != nil)
            run();
    };
}

@interface GrovsUnityNotificationProxy ()
@property (nonatomic, copy) GrovsUnityNotificationResponseHandler onResponse;
@end

@implementation GrovsUnityNotificationProxy

+ (instancetype)installOn:(UNUserNotificationCenter *)center onResponse:(GrovsUnityNotificationResponseHandler)onResponse
{
    [self swizzleSetDelegate];
    return [self installOnCenter:(id<GrovsUnityNotificationCenter>)center onResponse:onResponse];
}

+ (instancetype)installOnCenter:(id<GrovsUnityNotificationCenter>)center
                     onResponse:(GrovsUnityNotificationResponseHandler)onResponse
{
    @synchronized(self)
    {
        GrovsUnityNotificationProxy *existing = objc_getAssociatedObject(center, &GrovsUnityProxyKey);
        if (existing != nil)
            return existing;

        GrovsUnityNotificationProxy *proxy = [[self alloc] init];
        proxy.onResponse = onResponse;
        proxy.inner = center.delegate;
        // The center holds its delegate weakly, so it keeps the proxy alive this way.
        objc_setAssociatedObject(center, &GrovsUnityProxyKey, proxy, OBJC_ASSOCIATION_RETAIN_NONATOMIC);
        center.delegate = proxy;
        return proxy;
    }
}

+ (void)swizzleSetDelegate
{
    static dispatch_once_t once;
    dispatch_once(&once, ^{
        [self swizzleSetDelegateOnClass:[UNUserNotificationCenter class]];
    });
}

+ (void)swizzleSetDelegateOnClass:(Class)centerClass
{
    static NSMutableSet<Class> *swizzled;
    @synchronized(self)
    {
        if (swizzled == nil)
            swizzled = [NSMutableSet set];
        if ([swizzled containsObject:centerClass])
            return;

        SEL selector = @selector(setDelegate:);
        Method method = class_getInstanceMethod(centerClass, selector);
        if (method == NULL)
            return;
        [swizzled addObject:centerClass];

        __block GrovsUnitySetDelegateIMP original = (GrovsUnitySetDelegateIMP)method_getImplementation(method);
        IMP replacement = imp_implementationWithBlock(^(id center, id delegate) {
            GrovsUnityNotificationProxy *proxy = objc_getAssociatedObject(center, &GrovsUnityProxyKey);
            if (proxy != nil && delegate != proxy)
            {
                proxy.inner = delegate;
                // Set the proxy again so the center re-reads which optional methods it answers.
                delegate = proxy;
            }
            original(center, selector, delegate);
        });

        // An inherited setter gets an override on this class, so the superclass stays untouched.
        if (!class_addMethod(centerClass, selector, replacement, method_getTypeEncoding(method)))
            original = (GrovsUnitySetDelegateIMP)method_setImplementation(method, replacement);
    }
}

- (void)userNotificationCenter:(UNUserNotificationCenter *)center
    didReceiveNotificationResponse:(UNNotificationResponse *)response
             withCompletionHandler:(void (^)(void))completionHandler
{
    void (^complete)(void) = GrovsUnityRunOnce(completionHandler);

    GrovsUnityNotificationResponseHandler onResponse = self.onResponse;
    if (onResponse != nil)
        onResponse(response);

    id<UNUserNotificationCenterDelegate> inner = self.inner;
    SEL selector = @selector(userNotificationCenter:didReceiveNotificationResponse:withCompletionHandler:);
    if ([inner respondsToSelector:selector])
        [inner userNotificationCenter:center didReceiveNotificationResponse:response withCompletionHandler:complete];
    else
        complete();
}

- (void)userNotificationCenter:(UNUserNotificationCenter *)center
       willPresentNotification:(UNNotification *)notification
         withCompletionHandler:(void (^)(UNNotificationPresentationOptions))completionHandler
{
    id<UNUserNotificationCenterDelegate> inner = self.inner;
    SEL selector = @selector(userNotificationCenter:willPresentNotification:withCompletionHandler:);
    if ([inner respondsToSelector:selector])
    {
        [inner userNotificationCenter:center willPresentNotification:notification withCompletionHandler:completionHandler];
        return;
    }

    if (@available(iOS 14.0, *))
        completionHandler(UNNotificationPresentationOptionBanner | UNNotificationPresentationOptionSound |
                          UNNotificationPresentationOptionList);
    else
        completionHandler(UNNotificationPresentationOptionAlert | UNNotificationPresentationOptionSound);
}

- (BOOL)respondsToSelector:(SEL)aSelector
{
    return [super respondsToSelector:aSelector] || [self.inner respondsToSelector:aSelector];
}

- (id)forwardingTargetForSelector:(SEL)aSelector
{
    id inner = self.inner;
    if ([inner respondsToSelector:aSelector])
        return inner;
    return [super forwardingTargetForSelector:aSelector];
}

// The inner delegate can go away between respondsToSelector: and the call. Drop the call then.
- (NSMethodSignature *)methodSignatureForSelector:(SEL)aSelector
{
    NSMethodSignature *signature = [super methodSignatureForSelector:aSelector];
    if (signature != nil)
        return signature;
    struct objc_method_description description =
        protocol_getMethodDescription(@protocol(UNUserNotificationCenterDelegate), aSelector, NO, YES);
    return description.types != NULL ? [NSMethodSignature signatureWithObjCTypes:description.types] : nil;
}

- (void)forwardInvocation:(NSInvocation *)invocation
{
}

@end
