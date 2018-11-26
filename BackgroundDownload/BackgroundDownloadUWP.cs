#if UNITY_WSA

using System.Collections.Generic;
#if ENABLE_WINMD_SUPPORT
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using UnityEngine;
#endif

namespace Unity.Networking
{
    class BackgroundDownloadUWP : BackgroundDownload
    {
#if ENABLE_WINMD_SUPPORT
        static BackgroundTransferGroup s_BackgroundDownloadGroup;

        CancellationTokenSource _cancelSource;
        IAsyncOperationWithProgress<DownloadOperation, DownloadOperation> _downloadOperation;
        DownloadOperation _download;
#endif

        internal BackgroundDownloadUWP(BackgroundDownloadConfig config)
            : base(config)
        {
#if ENABLE_WINMD_SUPPORT
            CreateBackgroundDownloadGroup();
            string filePath = Path.Combine(Application.persistentDataPath, config.filePath);
            string directory = Path.GetDirectoryName(filePath).Replace('/', '\\');
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            _cancelSource = new CancellationTokenSource();
            StartDownload(filePath, directory);
#endif
        }

#if ENABLE_WINMD_SUPPORT
        // constructor for recreating download from existing one in OS
        internal BackgroundDownloadUWP(Uri url, string filePath)
        {
            _config.url = url;
            _config.filePath = filePath;
            _cancelSource = new CancellationTokenSource();
        }

        async void StartDownload(string filePath, string directory)
        {
            try
            {
                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(directory).AsTask(_cancelSource.Token);
                var fileName = Path.GetFileName(filePath);
                StorageFile resultFile = await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting).AsTask(_cancelSource.Token);

                var downloader = new BackgroundDownloader();
                downloader.TransferGroup = s_BackgroundDownloadGroup;
                if (config.requestHeaders != null)
                    foreach (var header in config.requestHeaders)
                        if (header.Value != null)
                            foreach (var value in header.Value)
                                downloader.SetRequestHeader(header.Key, value);
                switch (config.policy)
                {
                    case BackgroundDownloadPolicy.AlwaysAllow:
                        downloader.CostPolicy = BackgroundTransferCostPolicy.Always;
                        break;
                    case BackgroundDownloadPolicy.AllowMetered:
                    case BackgroundDownloadPolicy.Default:
                        downloader.CostPolicy = BackgroundTransferCostPolicy.Default;
                        break;
                    case BackgroundDownloadPolicy.UnrestrictedOnly:
                        downloader.CostPolicy = BackgroundTransferCostPolicy.UnrestrictedOnly;
                        break;
                }
                _download = downloader.CreateDownload(config.url, resultFile);
                _downloadOperation = _download.StartAsync();
                var downloadTask = _downloadOperation.AsTask();
                downloadTask.ContinueWith(DownloadTaskFinished, _cancelSource.Token);
            }
            catch (OperationCanceledException)
            {
                _error = "Download aborted";
                _status = BackgroundDownloadStatus.Failed;
            }
            catch (Exception e)
            {
                _error = "Download: " + e.Message;
                _status = BackgroundDownloadStatus.Failed;
            }
        }
#endif

        public override bool keepWaiting { get { return _status == BackgroundDownloadStatus.Downloading; } }

        internal static Dictionary<string, BackgroundDownload> LoadDownloads()
        {
            var downloads = new Dictionary<string, BackgroundDownload>();

#if ENABLE_WINMD_SUPPORT
            CreateBackgroundDownloadGroup();
            var downloadsTask = BackgroundDownloader.GetCurrentDownloadsForTransferGroupAsync(s_BackgroundDownloadGroup).AsTask();
            downloadsTask.Wait();
            foreach (var download in downloadsTask.Result)
            {
                var uri = download.RequestedUri;
                var filePath = GetDownloadPath(download.ResultFile.Path);
                if (filePath != null)
                {
                    var dl = new BackgroundDownloadUWP(uri, filePath);
                    dl._download = download;
                    switch (download.Progress.Status)
                    {
                        case BackgroundTransferStatus.Completed:
                            dl._status = BackgroundDownloadStatus.Done;
                            break;
                        case BackgroundTransferStatus.Error:
                            dl._status = BackgroundDownloadStatus.Failed;
                            dl._error = download.CurrentWebErrorStatus.ToString();
                            break;
                    }

                    downloads[filePath] = dl;
                    dl._downloadOperation = download.AttachAsync();
                    dl._downloadOperation.AsTask().ContinueWith(dl.DownloadTaskFinished, dl._cancelSource.Token);
                }
            }
#endif

            return downloads;
        }

        internal static void SaveDownloads(Dictionary<string, BackgroundDownload> downloads)
        {
        }

        protected override float GetProgress()
        {
#if ENABLE_WINMD_SUPPORT
            if (_status != BackgroundDownloadStatus.Downloading)
                return 1.0f;
            if (_download != null)
            {
                var progress = _download.Progress;
                double received = progress.BytesReceived;
                double total = progress.TotalBytesToReceive;
                float ret = total > 0 ? (float)(received / total) : -1.0f;
                return ret > 1 ? 1.0f : ret;
            }
#endif
            return 0.0f;
        }

#if ENABLE_WINMD_SUPPORT
        public override void Dispose()
        {
            if (_status == BackgroundDownloadStatus.Downloading)
            {
                _cancelSource.Cancel();
                if (_downloadOperation != null)
                    _downloadOperation.Cancel();
            }
            base.Dispose();
        }

        static void CreateBackgroundDownloadGroup()
        {
            if (s_BackgroundDownloadGroup == null)
                s_BackgroundDownloadGroup = BackgroundTransferGroup.CreateGroup("UnityBackgroundDownloads");
        }

        string GetErrorMessage(Task task, string prefix)
        {
            switch (task.Status)
            {
                case TaskStatus.Canceled:
                    return "Aborted";
                case TaskStatus.Faulted:
                    return prefix + task.Exception.GetBaseException().Message;
                default:
                    return "Unknown error";
            }
        }

        void DownloadTaskFinished(Task<DownloadOperation> downloadT)
        {
            if (downloadT.Status == TaskStatus.RanToCompletion)
                _status = BackgroundDownloadStatus.Done;
            else
            {
                _error = GetErrorMessage(downloadT, "Download failed: ");
                _status = BackgroundDownloadStatus.Failed;
            }
        }

        static string GetDownloadPath(string absPath)
        {
            string basePath = Application.persistentDataPath.Replace('/', '\\');
            if (!absPath.StartsWith(basePath)) // not something started by Unity
                return null;
            int idx = basePath.Length;
            if (absPath[idx] == '\\')
                idx++;
            return absPath.Substring(idx);
        }
#endif
    }
}

#endif
