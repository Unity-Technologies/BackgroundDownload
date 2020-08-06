using System;
using System.Collections;
using System.IO;
using UnityEngine;
using Unity.Networking;

/*
 * Example for downloading a single file.
 */
public class SingleFileDownload : MonoBehaviour
{
    void Start()
    {
        string fileName = "success.jpg";
        string destinationFile = Path.Combine(Application.persistentDataPath, fileName);

        if (File.Exists(destinationFile))
        {
            Debug.Log("File already downloaded");
            return;
        }
        
        var downloads = BackgroundDownload.backgroundDownloads;
        if (downloads.Length > 0)
            StartCoroutine(WaitForDownload(downloads[0]));
        else
        {
            Uri url = new Uri("https://memegenerator.net/img/images/1154731/success-baby.jpg");
            StartCoroutine(WaitForDownload(BackgroundDownload.Start(url, fileName)));
        }
    }

    IEnumerator WaitForDownload(BackgroundDownload download)
    {
        yield return download;
        if (download.status == BackgroundDownloadStatus.Done)
            Debug.Log("File successfully downloaded");
        else
            Debug.Log("File download failed with error: " + download.error);
    }
}
