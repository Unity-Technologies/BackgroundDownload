#if UNITY_ANDROID

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Unity.Networking
{

    class BackgroundDownloadAndroid : BackgroundDownload
    {
        static AndroidJavaClass _playerClass;
        static AndroidJavaClass _backgroundDownloadClass;

        class Callback : AndroidJavaProxy
        {
            public Callback()
                : base("com.unity3d.backgrounddownload.CompletionReceiver$Callback")
            {}

            void downloadCompleted()
            {
                foreach (var download in _downloads.Values)
                    ((BackgroundDownloadAndroid)download).CheckFinished();
            }
        }

        static Callback _finishedCallback;

        AndroidJavaObject _download;
        long _id = 0;

        static void SetupBackendStatics()
        {
            if (_backgroundDownloadClass == null)
                _backgroundDownloadClass = new AndroidJavaClass("com.unity3d.backgrounddownload.BackgroundDownload");
            if (_finishedCallback == null)
            {
                _finishedCallback = new Callback();
                var receiver = new AndroidJavaClass("com.unity3d.backgrounddownload.CompletionReceiver");
                receiver.CallStatic("setCallback", _finishedCallback);
            }
            if (_playerClass == null)
                _playerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        }

        internal BackgroundDownloadAndroid(BackgroundDownloadConfig config)
            : base(config)
        {
            SetupBackendStatics();
            string filePath = Path.Combine(Application.persistentDataPath, config.filePath);
            if (File.Exists(filePath))
                File.Delete(filePath);
            string fileUri = "file://" + filePath;
            bool allowMetered = false;
            bool allowRoaming = false;
            switch (_config.policy)
            {
                case BackgroundDownloadPolicy.AllowMetered:
                    allowMetered = true;
                    break;
                case BackgroundDownloadPolicy.AlwaysAllow:
                    allowMetered = true;
                    allowRoaming = true;
                    break;
                default:
                    break;
            }
            _download = _backgroundDownloadClass.CallStatic<AndroidJavaObject>("create", config.url.AbsoluteUri, fileUri);

            _download.Call("setAllowMetered", allowMetered);
            _download.Call("setAllowRoaming", allowRoaming);
            var activity = _playerClass.GetStatic<AndroidJavaObject>("currentActivity");
            _id = _download.Call<long>("start", activity);
        }

        BackgroundDownloadAndroid(long id, AndroidJavaObject download)
        {
            _id = id;
            _download = download;
            _config.url = QueryDownloadUri();
            _config.filePath = QueryDestinationPath();
            CheckFinished();
        }

        static BackgroundDownloadAndroid Recreate(long id)
        {
            try
            {
                SetupBackendStatics();
                var activity = _playerClass.GetStatic<AndroidJavaObject>("currentActivity");
                var download = _backgroundDownloadClass.CallStatic<AndroidJavaObject>("recreate", activity, id);
                if (download != null)
                    return new BackgroundDownloadAndroid(id, download);
            }
            catch (Exception e)
            {
                Debug.LogError(string.Format("Failed to recreate background download with id {0}: {1}", id, e.Message));
            }

            return null;
        }

        Uri QueryDownloadUri()
        {
            return new Uri(_download.Call<string>("getDownloadUrl"));
        }

        string QueryDestinationPath()
        {
            string uri = _download.Call<string>("getDestinationUri");
            string basePath = Application.persistentDataPath;
            var pos = uri.IndexOf(basePath);
            pos += basePath.Length;
            if (uri[pos] == '/')
                ++pos;
            return uri.Substring(pos);
        }

        string GetError()
        {
            return _download.Call<string>("getError");
        }

        void CheckFinished()
        {
            if (_status == BackgroundDownloadStatus.Downloading)
            {
                int status = _download.Call<int>("checkFinished");
                if (status == 1)
                    _status = BackgroundDownloadStatus.Done;
                else if (status < 0)
                {
                    _status = BackgroundDownloadStatus.Failed;
                    _error = GetError();
                }
            }
        }

        void RemoveDownload()
        {
            _download.Call("remove");
        }

        public override bool keepWaiting { get { return _status == BackgroundDownloadStatus.Downloading; } }

        public override void Dispose()
        {
            RemoveDownload();
            base.Dispose();
        }

        internal static Dictionary<string, BackgroundDownload> LoadDownloads()
        {
            var downloads = new Dictionary<string, BackgroundDownload>();
            var file = Path.Combine(Application.persistentDataPath, "unity_background_downloads.dl");
            if (File.Exists(file))
            {
                foreach (var line in File.ReadAllLines(file))
                    if (!string.IsNullOrEmpty(line))
                    {
                        long id = long.Parse(line);
                        var dl = Recreate(id);
                        if (dl != null)
                            downloads[dl.config.filePath] = dl;
                    }
            }

            // some loads might have failed, save the actual state
            SaveDownloads(downloads);
            return downloads;
        }

        internal static void SaveDownloads(Dictionary<string, BackgroundDownload> downloads)
        {
            var file = Path.Combine(Application.persistentDataPath, "unity_background_downloads.dl");
            if (downloads.Count > 0)
            {
                var ids = new string[downloads.Count];
                int i = 0;
                foreach (var dl in downloads)
                    ids[i++] = ((BackgroundDownloadAndroid)dl.Value)._id.ToString();
                File.WriteAllLines(file, ids);
            }
            else if (File.Exists(file))
                File.Delete(file);
        }
    }

}

#endif
