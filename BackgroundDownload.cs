using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Networking
{
    public enum BackgroundDownloadPolicy
    {
        Default = 0,
        UnrestrictedOnly = 1,  // Only Wi-Fi
        AllowMetered = 2,      // Allow Mobile
        AlwaysAllow = 3,       // Allow Roaming
    }

    public struct BackgroundDownloadConfig
    {
        public Uri url;
        public string filePath;
        public BackgroundDownloadPolicy policy;
    }

    public enum BackgroundDownloadStatus
    {
        Downloading = 0,
        Done = 1,
        Failed = 2,
    }

    public abstract class BackgroundDownload
        : CustomYieldInstruction
        , IDisposable
    {
        static Dictionary<string, BackgroundDownload> _downloads;

        public static BackgroundDownload[] backgroundDownloads
        {
            get
            {
                LoadDownloads();
                var ret = new BackgroundDownload[_downloads.Count];
                int i = 0;
                foreach (var download in _downloads)
                    ret[i++] = download.Value;
                return ret;
            }
        }

        protected BackgroundDownloadConfig _config;
        protected BackgroundDownloadStatus _status = BackgroundDownloadStatus.Downloading;
        protected string _error;

        public static BackgroundDownload Start(Uri url, String filePath)
        {
            var config = new BackgroundDownloadConfig();
            config.url = url;
            config.filePath = filePath;
            return Start(config);
        }

        public static BackgroundDownload Start(BackgroundDownloadConfig config)
        {
            var download = new BackgroundDownloadAndroid(config);
            LoadDownloads();
            _downloads.Add(config.filePath, download);
            SaveDownloads();
            return download;
        }

        protected BackgroundDownload()
        {
        }

        protected BackgroundDownload(BackgroundDownloadConfig config)
        {
            _config = config;
            switch (_config.policy)
            {
                case BackgroundDownloadPolicy.UnrestrictedOnly:
                case BackgroundDownloadPolicy.AllowMetered:
                case BackgroundDownloadPolicy.AlwaysAllow:
                    break;
                case BackgroundDownloadPolicy.Default:
                default:
                    _config.policy = BackgroundDownloadPolicy.AllowMetered;
                    break;
            }
        }

        public BackgroundDownloadConfig config { get { return _config; } }

        public BackgroundDownloadStatus status { get { return _status; } }

        public string error { get { return _error; } }

        public virtual void Dispose()
        {
            _downloads.Remove(_config.filePath);
            SaveDownloads();
        }

        static void LoadDownloads()
        {
            if (_downloads == null)
                _downloads = BackgroundDownloadAndroid.LoadDownloads();
        }

        static void SaveDownloads()
        {
            BackgroundDownloadAndroid.SaveDownloads(_downloads);
        }
    }

}
