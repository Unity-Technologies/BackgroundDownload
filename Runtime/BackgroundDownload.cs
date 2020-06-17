using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using BackgroundDownloadimpl = Unity.Networking.BackgroundDownloadEditor;
#elif UNITY_ANDROID
using BackgroundDownloadimpl = Unity.Networking.BackgroundDownloadAndroid;
#elif UNITY_IOS
using BackgroundDownloadimpl = Unity.Networking.BackgroundDownloadiOS;
#elif UNITY_WSA_10_0
using BackgroundDownloadimpl = Unity.Networking.BackgroundDownloadUWP;
#endif

namespace Unity.Networking
{
    /// <summary>
    /// The policy for download, such as what type of network connection can be used.
    /// Not supported on iOS (does nothing).
    /// </summary>
    public enum BackgroundDownloadPolicy
    {
        /// <summary>
        /// Operating System recommended policy.
        /// </summary>
        Default = 0,
        /// <summary>
        /// Only download when using Wi-Fi or similar unlimited connection.
        /// </summary>
        UnrestrictedOnly = 1,
        /// <summary>
        /// Allows Mobile and other metered connections, that may involve additional charges.
        /// </summary>
        AllowMetered = 2,
        /// <summary>
        /// Allows to use any connection including expensive options, such as roaming.
        /// </summary>
        AlwaysAllow = 3,
    }

    /// <summary>
    /// All the settings to perform a download.
    /// This structure must contain the URL to file to download and a path to file to store.
    /// Destination file will be overwritten if exists. Destination path must relative and result will be placed inside Application.persistentDataPath, because directories an application is allowed to write to are not guaranteed to be the same across different app runs.
    /// Optionally can contain custom HTTP headers to send and network policy. These two settings are not guaranteed to persist across different app runs.
    /// </summary>
    public struct BackgroundDownloadConfig
    {
        /// <summary>
        /// URL to resource to download.
        /// </summary>
        public Uri url;
        /// <summary>
        /// A relative path to safe downloaded file to; will be saved under Application.persistentDataPath.
        /// </summary>
        public string filePath;
        /// <summary>
        /// A policy to limit this download to certain network types; does not persist across app runs.
        /// </summary>
        public BackgroundDownloadPolicy policy;
        /// <summary>
        /// Additional HTTP headers to send with the request. Key is HTTP header name, value is a list of values, when more than one, multiple headers with the same key will be sent.
        /// </summary>
        public Dictionary<string, List<string>> requestHeaders;

        /// <summary>
        /// A convenience helper to add a single HTTP header to requestHeaders. Fills the value list if called again with the same header name.
        /// </summary>
        /// <param name="name">HTTP header name.</param>
        /// <param name="value">HTTP header value.</param>
        /// <exception cref="ArgumentException">Thrown when name or value is not valid.</exception>
        /// <exception cref="ArgumentNullException">Thrown when header value is null.</exception>
        public void AddRequestHeader(string name, string value)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Header name cannot be empty");
            if (value == null)
                throw new ArgumentNullException("Header value cannot be null");
            if (requestHeaders == null)
                requestHeaders = new Dictionary<string, List<string>>();
            List<string> values;
            if (requestHeaders.TryGetValue(name, out values))
                values.Add(value);
            else
            {
                values = new List<string>();
                values.Add(value);
                requestHeaders[name] = values;
            }
        }
    }

    /// <summary>
    /// The current status of the download.
    /// </summary>
    public enum BackgroundDownloadStatus
    {
        /// <summary>
        /// The download is in progress.
        /// </summary>
        Downloading = 0,
        /// <summary>
        /// The download has finished successfully.
        /// </summary>
        Done = 1,
        /// <summary>
        /// The download has finished with an error.
        /// </summary>
        Failed = 2,
    }

    /// <summary>
    /// A class to perform file downloads that will continue if app goes into background or even gets shut down by OS.
    /// Note, that the download might not finish under specific conditions, for example Operating System can provide user with a feature to cancel such download.
    /// An instance of this class can be returned from the Coroutine to suspend it until download is finished.
    /// The object of this class must be disposed when no longer required by calling Dispose() or by placing the object in the using() block.
    /// If the background download is disposed before completion, it is also cancelled. The result file may or may not exist, contain old data, or contain partial data.
    /// The destination file must not be used before download completes. Otherwise it may prevent the download from writing to destination.
    /// If app is quit by the operating system, on next run background downloads can be picked up by accessing BackgroundDownload.backgroundDownloads.
    /// </summary>
    public abstract class BackgroundDownload
        : CustomYieldInstruction
        , IDisposable
    {
        protected static Dictionary<string, BackgroundDownload> _downloads;

        /// <summary>
        /// Returns an array of currently present download.
        /// After starting an application this should be queried to pick up the downloads from previous session.
        /// </summary>
        public static BackgroundDownload[] backgroundDownloads
        {
            get
            {
                lock (typeof(BackgroundDownload))
                {
                    LoadDownloads();
                    var ret = new BackgroundDownload[_downloads.Count];
                    int i = 0;
                    foreach (var download in _downloads)
                        ret[i++] = download.Value;
                    return ret;
                }
            }
        }

        /// <summary>Holds download configuration. For internal use only.</summary>
        protected BackgroundDownloadConfig _config;
        /// <summary>Hold download status. For internal use only.</summary>
        protected BackgroundDownloadStatus _status = BackgroundDownloadStatus.Downloading;
        /// <summary>Hold error message. For internal use only.</summary>
        protected string _error;

        /// <summary>
        /// Start download from given URL. Creates BackgroundDownloadConfig using given arguments.
        /// </summary>
        /// <param name="url">URL to download from.</param>
        /// <param name="filePath">A relative path to save to; will be saved under Application.persistentDataPath.</param>
        /// <returns>An instance for monitoring the progress.</returns>
        /// <exception cref="ArgumentException">Thrown if there already is a download with given destination file.</exception>
        public static BackgroundDownload Start(Uri url, String filePath)
        {
            var config = new BackgroundDownloadConfig();
            config.url = url;
            config.filePath = filePath;
            return Start(config);
        }

        /// <summary>
        /// Start download using given configuration.
        /// </summary>
        /// <param name="config">The configuration for the download.</param>
        /// <returns>An instance for monitoring the progress.</returns>
        /// <exception cref="ArgumentException">Thrown if there already is a download with given destination file.</exception>
        public static BackgroundDownload Start(BackgroundDownloadConfig config)
        {
            lock (typeof(BackgroundDownload))
            {
                LoadDownloads();
                if (_downloads.ContainsKey(config.filePath))
                    throw new ArgumentException("Download of this file is already present");
                var download = new BackgroundDownloadimpl(config);
                _downloads.Add(config.filePath, download);
                SaveDownloads();
                return download;
            }
        }

        /// <summary>For internal use.</summary>
        protected BackgroundDownload()
        {
        }

        /// <summary>For internal use.</summary>
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

        /// <summary>
        /// The configuration of this download.
        /// </summary>
        public BackgroundDownloadConfig config { get { return _config; } }

        /// <summary>
        /// The current status of this download.
        /// </summary>
        public virtual BackgroundDownloadStatus status { get { return _status; } }

        /// <summary>
        /// Error message for a failed download.
        /// </summary>
        public string error { get { return _error; } }

        /// <summary>
        /// How far the request has progressed (0 to 1), negative value if unknown. Accessing this field can be very expensive (in particular on Android).
        /// </summary>
        public float progress { get { return GetProgress(); } }

        /// <summary>For internal use.</summary>
        protected abstract float GetProgress();

        /// <summary>
        /// Disposes of this download, aborts the download if still in progress.
        /// All background download instances have to be disposed when no longer required.
        /// </summary>
        public virtual void Dispose()
        {
            lock (typeof(BackgroundDownload))
            {
                _downloads.Remove(_config.filePath);
                SaveDownloads();
                if (_status == BackgroundDownloadStatus.Downloading)
                {
                    _status = BackgroundDownloadStatus.Failed;
                    _error = "Aborted";
                }
            }
        }

        static void LoadDownloads()
        {
            if (_downloads == null)
                _downloads = BackgroundDownloadimpl.LoadDownloads();
        }

        static void SaveDownloads()
        {
            BackgroundDownloadimpl.SaveDownloads(_downloads);
        }
    }
}
