# About com.unity.backgrounddownload

## Note: Unsupported feature
This package is not an officially supported feature, and is provided "as is".

## Feature scope
Use Background Download to download large files in the background on mobile platforms. It lets you fetch files that aren't required immediately while caring less about application lifecycle. Downloads will continue even if your application goes into background or the Operating System closes it (usually due to low memory for foreground tasks).
The Background Download package is not a replacement for HTTP clients. It has a specific focus on fetching lower-priority files for future use. Because the app assumes that these downloads have lower priority, download speeds can also be slower.

**The Background Download is not integrated with the Addressable System or Asset Bundles** and will require you write additional code to use in this context.


## Limited platform support
Background Download only works on Android, iOS and Universal Windows Platform.
It does not work in the Unity Editor, it only compiles.

## How to install

### For 2019.4 or newer 
This feature is built as a package. To install the package, follow the instructions in the Package Manager documentation [from a local folder](https://docs.unity3d.com/Manual/upm-ui-local.html) or [from a GIT URL](https://docs.unity3d.com/Manual/upm-ui-giturl.html). 

For 2019.3 or older:  
Clone/download this project from the 2019-3-and-older branch. Drop BackgroundDownload and Plugins folders into Assets in your Unity project. If you are building for Android, you have to set Write Permission to External in Player Settings. If you are building for Universal Windows Platform, you need to enable one of the Internet permissions.

## Package contents

The following table describes the package folder structure:

|**Location**|**Description**|
|---|---|
|`Runtime`|Contains C# code and native plugins for mobile platforms.|
|`Samples`|Contains example C# scripts explaining how to use this package.|


# Examples

The example below shows how to call [`StartCoroutine(StartDownload())`](https://docs.unity3d.com/ScriptReference/MonoBehaviour.StartCoroutine.html) to download a file during the same app session in a coroutine.

```
IEnumerator StartDownload()
{
    using (var download = BackgroundDownload.Start(new Uri("https://mysite.com/file"), "files/file.data"))
    {
        yield return download;
        if (download.status == BackgroundDownloadStatus.Failed)
            Debug.Log(download.error);
        else
            Debug.Log("DONE downloading file");
    }
}
```

The example below shows how to pick up a download from a previous app run and continue it until it finishes.

```
IEnumerator ResumeDownload()
{
    if (BackgroundDownload.backgroundDownloads.Length == 0)
        yield break;
    var download = BackgroundDownload.backgroundDownloads[0];
    yield return download;
    // deal with results here
    // dispose download
    download.Dispose();
}
```

# API

## BackgroundDownloadPolicy

Enum that lets control the network types over which the downloads are allowed to happen. Not supported on iOS.

Possible values:
* `UnrestrictedOnly` - downloads using unlimited connection, such as Wi-Fi.
* `AllowMetered` - allows downloads using metered connections, such as mobile data (default).
* `AlwaysAllow` - allows downloads using all network types, including potentially expensive ones, such as roaming.


## BackgroundDownloadConfig

Structure containing all the data required to start background download.
This structure must contain the URL to file to download and a path to file to store. Destination file will be overwritten if exists. Destination path must relative and result will be placed inside `Application.persistentDataPath`, because directories an application is allowed to write to are not guaranteed to be the same across different app runs.
Optionally can contain custom HTTP headers to send and network policy. These two settings are not guaranteed to persist across different app runs.

Fields:
* `System.Uri url` - the URL to the file to download.
* `string filePath` -  a **relative** file path that must be relative (will be inside `Application.persistentDataPath`).
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
