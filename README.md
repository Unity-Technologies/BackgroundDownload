# BackgroundDownload
Plugins for mobile platforms to enable file downloads in background

Allows to launch file downloads that will continue even if the app goes into background or gets quit by the operating system. The downloads can be picked the next time the app is started.
Supported platforms are: Android, iOS and Universal Windows Platform.

# API

## BackgroundDownloadPolicy

Enum that lets control the network types over which the downloads are allowed to happen. Not supported on iOS.

Possible values:
* `UnrestrictedOnly` - downloads using unlimited connection, such as Wi-Fi.
* `AllowMetered` - allows downloads using metered connections, such as mobile data (default).
* `AlwaysAllow` - allows downloads using all network types, including potentially expensive ones, such as roaming.


## BackgroundDownloadConfig

Structure containing all the data required to start background download.
This structure must contain the URL to file to download and a path to file to store. Destination file will be overwritten if exists. Destination path must relative and inside `Application.persistentDataPath`, because directories an application is allowed to write to are not guaranteed to be the same across different app runs.
Optionally can contain custom HTTP headers to send and network policy. These two settings are not guaranteed to persist across different app runs.

Fields:
* `System.Uri url` - the URL to the file to download.
* `string filePath` -  a **relative** file path that must be inside `Application.persistentDataPath`.
* `BackgroundDownloadPolicy policy` - policy to limit downloads to certain network types. Does not persist across app runs.
* `float progress` - how far the request has progressed (0 to 1), negative value if unkown. Accessing this field can be very expensive (in particular on Android).
* `Dictionary<string, List<string>> requestHeaders` - custom HTTP headers to send. Does not persist across app runs.

Methods:
* `void AddRequestHeader(string name, string value)` - convenience method to add custom HTTP header to `requestHeaders`.

## BackgroundDownloadStatus

The state of the background download

Values:
* `Downloading` - the download is in progress.
* `Done` - the download has finished successfully.
* `Failed` - the download operation has failed.

## BackgroundDownload

A class for launching and picking up downloads in background.
Every background download can be returned from the coroutine to wait for it's completion. To free system resources, after completion each background download must be disposed by calling `Dispose()` or by placing the object in a `using` block. If background download is disposed before completion, it is also cancelled, the existence and contents of result file is undefined in such case.
**The destination file must not be used before download completes.** Otherwise it may prevent the download from writing to destination.
If app is quit by the operating system, on next run background downloads can be picked up by accessing `BackgroundDownload.backgroundDownloads`.

Properties:
* `static BackgroundDownload[] backgroundDownloads` - an array containing all current downloads.
* `BackgroundDownloadConfig config` - the configuration of download. Read only.
* `BackgroundDownloadStatus status` - the status of download. Read only.
* `string error` - contains error message for a failed download. Read only.

Methods:
* `static BackgroundDownload Start(BackgroundDownloadConfig config)` - start download using given configuration.
* `static BackgroundDownload Start(Uri url, String filePath)` - convenience method to start download when no additional settings are required.
* `void Dispose()` - release the resources and remove the download. Cancels download if incomplete.
