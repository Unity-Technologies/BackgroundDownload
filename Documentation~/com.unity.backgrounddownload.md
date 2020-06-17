# About com.unity.backgrounddownload

Use the Background Download package to download large files in the background on mobile platforms. This lackage lets you fetch files that aren't required immediately while caring less about application lifecycle. Downloads will continue even if your application goes into background or the Operating System closes it (usually due to low memory for foreground tasks).

The Background Download package is not a replacement for HTTP clients. It has a specific focus on fetching lower-priority files for future use. Because the app assumes that these downloads have lower priority, download speeds can also be slower.


## Preview package
This is a preview package and is not ready for production use. The features and documentation in this package might change before it is verified for release.


## Package contents

The following table describes the package folder structure:

|**Location**|**Description**|
|---|---|
|`Runtime`|Contains C# code and native plugins for mobile platforms.|
|`Samples`|Contains example C# scripts explaining how to use this package.|

<a name="Installation"></a>

## Installation

To install this package, follow the instructions in the [Package Manager documentation](https://docs.unity3d.com/Manual/upm-ui-install.html).


## Requirements

This version of Background Download is compatible with the following versions of the Unity Editor:

* 2020.1 and later


## Known limitations

Background Download version 0.1.0 includes the following known limitations:

* Does not work in the Unity Editor, only compiles.
* Only supports Android, iOS, and Universal Windows Platform.

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
