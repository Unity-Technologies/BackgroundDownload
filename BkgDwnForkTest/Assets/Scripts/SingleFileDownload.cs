using System;
using System.Collections;
using System.IO;
using UnityEngine;
using Unity.Networking;
using UnityEngine.UI;
using TMPro;

/*
 * Example for downloading a single file.
 */
public class SingleFileDownload : MonoBehaviour
{
    string fileName = "100MB.bin";
    string remoteFile = "https://speed.hetzner.de/100MB.bin";
    string destinationFile;
    BackgroundDownload currentDownload;
    
    [SerializeField]
    Button startDownloadButton;
    
    [SerializeField]
    TextMeshProUGUI status;

    [SerializeField]
    TextMeshProUGUI receivedBytes;

    [SerializeField]
    Image progress;

    void Start()
    {
        destinationFile = Path.Combine(Application.persistentDataPath, fileName);
        startDownloadButton.onClick.AddListener(OnStartDownloadTap);

    }

    void OnStartDownloadTap()
    {
        startDownloadButton.enabled = false;
        progress.transform.localScale = new Vector3(0.00001f, 1f, 1f);
        if (File.Exists(destinationFile))
        {
            Debug.Log("File already downloaded");
            File.Delete(destinationFile);
        }

        var downloads = BackgroundDownload.backgroundDownloads;
        if (downloads.Length > 0)
            StartCoroutine(WaitForDownload(downloads[0]));
        else
        {
            Uri url = new Uri(remoteFile);
            StartCoroutine(WaitForDownload(currentDownload = BackgroundDownload.Start(url, fileName)));
        }
    }

    void Update()
    {
        if (currentDownload != null)
        {
            status.text = currentDownload.status.ToString();
            receivedBytes.text = currentDownload.receivedBytes.ToString();
            progress.transform.localScale = new Vector3(Mathf.Max(0.00001f, currentDownload.progress), 1f, 1f);
        }
    }

    IEnumerator WaitForDownload(BackgroundDownload download)
    {
        yield return download;
        if (download.status == BackgroundDownloadStatus.Done)
            Debug.Log("File successfully downloaded");
        else
            Debug.Log("File download failed with error: " + download.error);
        
        startDownloadButton.enabled = true;
        currentDownload.Dispose();
        currentDownload = null;
        progress.transform.localScale = new Vector3(0.00001f, 1f, 1f);
    }
}
