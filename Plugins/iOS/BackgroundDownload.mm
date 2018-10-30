#include "UnityAppController.h"

static NSString* _Nonnull kUnityBackgroungDownloadSessionID = @"UnityBackgroundDownload";
static NSURLSession* gUnityBackgroundDownloadSession = nil;

static NSURLSession* UnityBackgroundDownloadSession();
static NSURL* GetDestinationUri(NSString* dest, NSFileManager** fileManager)
{
    NSFileManager* manager = [NSFileManager defaultManager];
    NSURL* documents = [[manager URLsForDirectory: NSDocumentDirectory inDomains: NSUserDomainMask] lastObject];
    NSURL* destUri = [documents URLByAppendingPathComponent: dest];
    if (fileManager != NULL)
        *fileManager = manager;
    return destUri;
}

enum
{
    kStatusDownloading = 0,
    kStatusDone = 1,
    kStatusFailed = 2,
};

@interface UnityBackgroundDownload : NSObject
{
}

@property BOOL isAttached;
@property BOOL status;

@end

@implementation UnityBackgroundDownload
{
	BOOL _isAttached;
    BOOL _status;
}

@synthesize isAttached = _isAttached;
@synthesize status = _status;

- (id)init
{
	_isAttached = NO;
    _status = kStatusDownloading;
    return self;
}

@end


@interface UnityBackgroundDownloadDelegate : NSObject<NSURLSessionDownloadDelegate>
{
}

@property (nullable) UnityHandleEventsForBackgroundURLSession finishEventsHandler;

+ (void)setFinishEventsHandler:(nonnull UnityHandleEventsForBackgroundURLSession)handler;

@end


@implementation UnityBackgroundDownloadDelegate
{
    NSMutableDictionary<NSURLSessionDownloadTask*, UnityBackgroundDownload*>* backgroundDownloads;
    UnityHandleEventsForBackgroundURLSession _finishEventsHandler;
}

@synthesize finishEventsHandler = _finishEventsHandler;

+ (void)setFinishEventsHandler:(nonnull UnityHandleEventsForBackgroundURLSession)handler
{
    NSURLSession* session = UnityBackgroundDownloadSession();
    UnityBackgroundDownloadDelegate* delegate = (UnityBackgroundDownloadDelegate*)session.delegate;
    delegate.finishEventsHandler = handler;
}

- (id)init
{
    backgroundDownloads = [[NSMutableDictionary<NSURLSessionDownloadTask*, UnityBackgroundDownload*> alloc] init];
    return self;
}

- (void)URLSession:(NSURLSession *)session downloadTask:(NSURLSessionDownloadTask *)downloadTask didFinishDownloadingToURL:(NSURL *)location
{
    NSFileManager* fileManager;
    NSURL* destUri = GetDestinationUri(downloadTask.taskDescription, &fileManager);
    [fileManager replaceItemAtURL: destUri withItemAtURL: location backupItemName: nil options: NSFileManagerItemReplacementUsingNewMetadataOnly resultingItemURL: nil error: nil];
    UnityBackgroundDownload* download = [backgroundDownloads objectForKey: downloadTask];
    download.status = kStatusDone;
}

- (void)URLSessionDidFinishEventsForBackgroundURLSession:(NSURLSession *)session
{
    if (self.finishEventsHandler != nil)
    {
        dispatch_async(dispatch_get_main_queue(), self.finishEventsHandler);
        self.finishEventsHandler = nil;
    }
}

- (NSURLSessionDownloadTask*)newSessionTask:(NSURLSession*)session withRequest:(NSURLRequest*)request forDestination:(NSString*)dest
{
    NSURLSessionDownloadTask *task = [session downloadTaskWithRequest: request];
    task.taskDescription = dest;
    UnityBackgroundDownload* download = [[UnityBackgroundDownload alloc] init];
	download.isAttached = YES;
    [backgroundDownloads setObject: download forKey: task];
    return task;
}

- (void)collectTasksForSession:(NSURLSession*)session
{
    [session getTasksWithCompletionHandler:^(NSArray<NSURLSessionDataTask *> * _Nonnull dataTasks, NSArray<NSURLSessionUploadTask *> * _Nonnull uploadTasks, NSArray<NSURLSessionDownloadTask *> * _Nonnull downloadTasks) {
        for (NSUInteger i = 0; i < downloadTasks.count; ++i)
        {
            UnityBackgroundDownload* download = [[UnityBackgroundDownload alloc] init];
            [backgroundDownloads setObject: download forKey: downloadTasks[i]];
        }
    }];
}

- (NSURLSessionDownloadTask*)firstUnattachedTask
{
	NSEnumerator<NSURLSessionDownloadTask*>* tasks = backgroundDownloads.keyEnumerator;
	NSURLSessionDownloadTask* task = [tasks nextObject];
	while (task != nil)
	{
		UnityBackgroundDownload* download = [backgroundDownloads objectForKey:task];
		if (download.isAttached == NO)
		{
			download.isAttached = YES;
			return task;
		}
		
		task = [tasks nextObject];
	}
	
	return nil;
}

- (int)taskStatus:(NSURLSessionDownloadTask*)task
{
	UnityBackgroundDownload* download = [backgroundDownloads objectForKey:task];
	if (download == nil)
		return YES;
	return download.status;
}

- (void)removeTask:(NSURLSessionDownloadTask*)task
{
    [task cancel];
    [backgroundDownloads removeObjectForKey: task];
}

@end


static NSURLSession* UnityBackgroundDownloadSession()
{
	if (gUnityBackgroundDownloadSession == nil)
	{
		NSURLSessionConfiguration* config = [NSURLSessionConfiguration backgroundSessionConfigurationWithIdentifier: kUnityBackgroungDownloadSessionID];
		UnityBackgroundDownloadDelegate* delegate = [[UnityBackgroundDownloadDelegate alloc] init];
		gUnityBackgroundDownloadSession = [NSURLSession sessionWithConfiguration: config delegate: delegate delegateQueue: nil];
		[delegate collectTasksForSession: gUnityBackgroundDownloadSession];
	}
	
	return gUnityBackgroundDownloadSession;
}

static void UnityBackgroundDownloadCreate()
{
	UnityBackgroundDownloadSession();
}

static void UnityBackgroundDownloadHandleEventsForBackgroundURLSession(NSString* identifier, UnityHandleEventsForBackgroundURLSession completionHandler)
{
	if ([identifier isEqualToString: kUnityBackgroungDownloadSessionID])
		[UnityBackgroundDownloadDelegate setFinishEventsHandler: completionHandler];
}

class UnityBackgroundDownloadRegistrator
{
public:
	UnityBackgroundDownloadRegistrator()
	{
		UnityBackgroundDownloadCreateFunc = UnityBackgroundDownloadCreate;
		UnityHandleEventsForBackgroundURLSessionFunc = UnityBackgroundDownloadHandleEventsForBackgroundURLSession;
	}
};

static UnityBackgroundDownloadRegistrator gRegistrator;


extern "C" void* UnityBackgroundDownloadCreateRequest(const char* url)
{
    NSURL* downloadUrl = [NSURL URLWithString: [NSString stringWithUTF8String: url]];
    NSMutableURLRequest* request = [[NSMutableURLRequest alloc] init];
    request.HTTPMethod = @"GET";
    request.URL = downloadUrl;
    return (__bridge_retained void*)request;
}

extern "C" void UnityBackgroundDownloadAddRequestHeader(void* req, const char* header, const char* value)
{
    NSMutableURLRequest* request = (__bridge NSMutableURLRequest*)req;
    [request setValue:[NSString stringWithUTF8String:value] forHTTPHeaderField:[NSString stringWithUTF8String:header]];
}

extern "C" void* UnityBackgroundDownloadStart(void* req, const char* dest)
{
    NSMutableURLRequest* request = (__bridge_transfer NSMutableURLRequest*)req;
    NSString* destPath = [NSString stringWithUTF8String: dest];
    NSURLSession* session = UnityBackgroundDownloadSession();
    UnityBackgroundDownloadDelegate* delegate = (UnityBackgroundDownloadDelegate*)session.delegate;
    NSURLSessionDownloadTask *task = [delegate newSessionTask: session withRequest: request forDestination: destPath];
    [task resume];
    return (__bridge void*)task;
}

extern "C" void* UnityBackgroundDownloadAttach()
{
	NSURLSession* session = UnityBackgroundDownloadSession();
	UnityBackgroundDownloadDelegate* delegate = (UnityBackgroundDownloadDelegate*)session.delegate;
	NSURLSessionDownloadTask* task = [delegate firstUnattachedTask];
	return (__bridge void*)task;
}

extern "C" int32_t UnityBackgroundDownloadGetUrl(void* download, char* buffer)
{
	NSURLSessionDownloadTask* task = (__bridge NSURLSessionDownloadTask*)download;
	NSString* url = task.originalRequest.URL.absoluteString;
	const char* cstr = [url UTF8String];
	strncpy(buffer, cstr, 2048);
	return (int32_t)strlen(cstr);
}

extern "C" int32_t UnityBackgroundDownloadGetFilePath(void* download, char* buffer)
{
	NSURLSessionDownloadTask* task = (__bridge NSURLSessionDownloadTask*)download;
	NSString* dest = task.taskDescription;
	const char* cstr = [dest UTF8String];
	strncpy(buffer, cstr, 2048);
	return (int32_t)strlen(cstr);
}

extern "C" int32_t UnityBackgroundDownloadGetStatus(void* download)
{
	NSURLSession* session = UnityBackgroundDownloadSession();
	UnityBackgroundDownloadDelegate* delegate = (UnityBackgroundDownloadDelegate*)session.delegate;
	NSURLSessionDownloadTask* task = (__bridge NSURLSessionDownloadTask*)download;
	return (int)[delegate taskStatus:task];
}

extern "C" float UnityBackgroundDownloadGetProgress(void* download)
{
    if (UnityBackgroundDownloadGetStatus(download) != kStatusDownloading)
        return 1.0f;
    if ([[NSProcessInfo processInfo] isOperatingSystemAtLeastVersion:{11,0,0}])
    {
        NSURLSessionDownloadTask* task = (__bridge NSURLSessionDownloadTask*)download;
        return (float)task.progress.fractionCompleted;
    }
    return -1.0f;
}

extern "C" void UnityBackgroundDownloadDestroy(void* download)
{
    NSURLSessionDownloadTask* task = (__bridge NSURLSessionDownloadTask*)download;
    NSURLSession* session = UnityBackgroundDownloadSession();
    UnityBackgroundDownloadDelegate* delegate = (UnityBackgroundDownloadDelegate*)session.delegate;
    [delegate removeTask: task];
}
