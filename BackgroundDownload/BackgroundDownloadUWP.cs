#if UNITY_WSA_10_0

using System.Collections.Generic;
#if !UNITY_EDITOR
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using UnityEngine;
#endif

namespace Unity.Networking
{
    class BackgroundDownloadUWP : BackgroundDownload
    {
#if !UNITY_EDITOR
        static BackgroundTransferGroup s_BackgroundDownloadGroup;

        CancellationTokenSource _cancelSource;
#endif

        internal BackgroundDownloadUWP(BackgroundDownloadConfig config)
            : base(config)
        {
#if !UNITY_EDITOR
            CreateBackgroundDownloadGroup();
            string filePath = Path.Combine(Application.persistentDataPath, config.filePath);
            string directory = Path.GetDirectoryName(filePath).Replace('/', '\\');
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            _cancelSource = new CancellationTokenSource();
            var folderTask = StorageFolder.GetFolderFromPathAsync(directory).AsTask();
            folderTask.ContinueWith((folderT) => {
                if (folderT.Status == TaskStatus.RanToCompletion)
                {
                    var fileName = Path.GetFileName(filePath);
                    var fileTask = folderT.Result.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting).AsTask();
                    fileTask.ContinueWith((fileT) => {
                        if (fileT.Status == TaskStatus.RanToCompletion)
                        {
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
                            var download = downloader.CreateDownload(config.url, fileT.Result);
                            var downloadTask = download.StartAsync().AsTask();
                            downloadTask.ContinueWith(DownloadTaskFinished, _cancelSource.Token);
                        }
                        else
                        {
                            _error = GetErrorMessage(fileT, "Failed to create file: ");
                            _status = BackgroundDownloadStatus.Failed;
                        }
                    }, _cancelSource.Token);
                }
                else
                {
                    _error = GetErrorMessage(folderT, "Failed to get/create directory: ");
                    _status = BackgroundDownloadStatus.Failed;
                }
            }, _cancelSource.Token);
#endif
        }

#if !UNITY_EDITOR
        internal BackgroundDownloadUWP(Uri url, string filePath)
        {
            _config.url = url;
            _config.filePath = filePath;
            _cancelSource = new CancellationTokenSource();
        }
#endif

        public override bool keepWaiting { get { return _status == BackgroundDownloadStatus.Downloading; } }

        internal static Dictionary<string, BackgroundDownload> LoadDownloads()
        {
            var downloads = new Dictionary<string, BackgroundDownload>();

#if !UNITY_EDITOR
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
                    download.AttachAsync().AsTask().ContinueWith(dl.DownloadTaskFinished, dl._cancelSource.Token);
                }
            }
#endif

            return downloads;
        }

        internal static void SaveDownloads(Dictionary<string, BackgroundDownload> downloads)
        {
        }

#if !UNITY_EDITOR
        public override void Dispose()
        {
            if (_status == BackgroundDownloadStatus.Downloading)
                _cancelSource.Cancel();
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
