# About &lt;com.unity.backgrounddownload&gt;

Use the &lt;Background Download&gt; package to &lt;download large files in the background on mobile platforms&gt;. The intent of this package is to let you fetch files that aren't required immediately with caring less about application lifecycle. The download will continue if your application goes into background and even if Operating System decides to close your application (usually due to low memory for foreground tasks).

The &lt;Background Download&gt; package however &lt;is not a replacement for HTTP clients&gt;. It has a specific focus on fetching files in lower priority for future use. Downloads may also be slower, since it is assumed that these download are of lower priority.


## Preview package
This package is available as a preview, so it is not ready for production use. The features and documentation in this package might change before it is verified for release.


## Package contents

The following table describes the package folder structure:

|**Location**|**Description**|
|---|---|
|*Runtime*|Contains &lt;C# code and native plugins for mobile platforms&gt;.|
|*Samples*|Contains &lt;example C# scripts explaining how to use this package&gt;.|

<a name="Installation"></a>

## Installation

To install this package, follow the instructions in the [Package Manager documentation](https://docs.unity3d.com/Manual/upm-ui-install.html).


## Requirements

This version of &lt;package name&gt; is compatible with the following versions of the Unity Editor:

* 2020.1 and later


## Known limitations

&lt;Background Download&gt; version &lt;0.1.0&gt; includes the following known limitations:

* &lt;Does not work in Editor, only compiles.&gt;
* &lt;Only Android, iOS and Universal Windows Platform are supported.&gt;

# Examples

Download file during the same app session in a coroutine [(call `StartCoroutine(StartDownload())`)](https://docs.unity3d.com/ScriptReference/MonoBehaviour.StartCoroutine.html).

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

Pick download from previous app run and continue it until it finishes.

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
