#if UNITY_IOS

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using UnityEngine;


namespace Unity.Networking
{

    class BackgroundDownloadiOS : BackgroundDownload
    {
        IntPtr _backend;

        internal BackgroundDownloadiOS(BackgroundDownloadConfig config)
            : base(config)
        {
            var destDir = Path.GetDirectoryName(Path.Combine(Application.persistentDataPath, config.filePath));
            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);
            IntPtr request = UnityBackgroundDownloadCreateRequest(config.url.AbsoluteUri);
            if (config.requestHeaders != null)
                foreach (var header in config.requestHeaders)
                    if (header.Value != null)
                        foreach (var val in header.Value)
                            UnityBackgroundDownloadAddRequestHeader(request, header.Key, val);
            _backend = UnityBackgroundDownloadStart(request, config.filePath);
        }

        BackgroundDownloadiOS(IntPtr backend, BackgroundDownloadConfig config)
            : base(config)
        {
            _backend = backend;
        }

        public override BackgroundDownloadStatus status { get { UpdateStatus(); return base.status; } }

        public override bool keepWaiting
        {
            get
            {
                UpdateStatus();
                return _status == BackgroundDownloadStatus.Downloading;
            }
        }

        internal static Dictionary<string, BackgroundDownload> LoadDownloads()
        {
            var downloads = new Dictionary<string, BackgroundDownload>();
            int numDownloads = UnityBackgroundDownloadGetCount();
            if (numDownloads > 0)
            {
                IntPtr[] loadedDownloads = new IntPtr[numDownloads];
                UnityBackgroundDownloadGetAll(loadedDownloads);
                byte[] buffer = new byte[2048];

                for (int i = 0; i < numDownloads; ++i)
                {
                    IntPtr backend = loadedDownloads[i];
                    BackgroundDownloadConfig config = new BackgroundDownloadConfig();
                    int length = UnityBackgroundDownloadGetUrl(backend, buffer);
                    config.url = new Uri(MarshalObjCString(buffer, length));
                    length = UnityBackgroundDownloadGetFilePath(backend, buffer);
                    config.filePath = MarshalObjCString(buffer, length);
                    var dl = new BackgroundDownloadiOS(backend, config);
                    downloads[config.filePath] = dl;
                }
            }

            return downloads;
        }

        internal static void SaveDownloads(Dictionary<string, BackgroundDownload> downloads)
        {
        }

        protected override float GetProgress()
        {
            if (_backend == IntPtr.Zero)
                return 1.0f;
            if (_status != BackgroundDownloadStatus.Downloading)
                return 1.0f;
            return UnityBackgroundDownloadGetProgress(_backend);
        }

        public override void Dispose()
        {
            if (_backend != IntPtr.Zero)
                UnityBackgroundDownloadDestroy(_backend);
            base.Dispose();
        }

        private void UpdateStatus()
        {
            if (_backend == IntPtr.Zero)
                return;
            if (_status != BackgroundDownloadStatus.Downloading)
                return;
            _status = (BackgroundDownloadStatus)UnityBackgroundDownloadGetStatus(_backend);
            if (_status == BackgroundDownloadStatus.Failed)
                _error = GetError();
        }

        private string GetError()
        {
            if (_backend == IntPtr.Zero)
                return "";
            byte[] buffer = new byte[2048];
            int length = UnityBackgroundDownloadGetError(_backend, buffer);
            return MarshalObjCString(buffer, length);
        }

        private static string MarshalObjCString(byte[] buffer, int length)
        {
            return Encoding.Unicode.GetString(buffer, 0, length);
        }

        [DllImport("__Internal")]
        static extern IntPtr UnityBackgroundDownloadCreateRequest([MarshalAs(UnmanagedType.LPWStr)] string url);

        [DllImport("__Internal")]
        static extern void UnityBackgroundDownloadAddRequestHeader(IntPtr headers,
            [MarshalAs(UnmanagedType.LPWStr)] string name, [MarshalAs(UnmanagedType.LPWStr)] string value);

        [DllImport("__Internal")]
        static extern IntPtr UnityBackgroundDownloadStart(IntPtr request, [MarshalAs(UnmanagedType.LPWStr)] string dest);

        [DllImport("__Internal")]
        static extern int UnityBackgroundDownloadGetStatus(IntPtr backend);

        [DllImport("__Internal")]
        static extern float UnityBackgroundDownloadGetProgress(IntPtr backend);

        [DllImport("__Internal")]
        static extern void UnityBackgroundDownloadDestroy(IntPtr backend);

        [DllImport("__Internal")]
        static extern int UnityBackgroundDownloadGetCount();

        [DllImport("__Internal")]
        static extern void UnityBackgroundDownloadGetAll([MarshalAs(UnmanagedType.LPArray)] IntPtr[] downloads);

        [DllImport("__Internal")]
        static extern int UnityBackgroundDownloadGetUrl(IntPtr backend, [MarshalAs(UnmanagedType.LPArray)] byte[] buffer);

        [DllImport("__Internal")]
        static extern int UnityBackgroundDownloadGetFilePath(IntPtr backend, [MarshalAs(UnmanagedType.LPArray)] byte[] buffer);

        [DllImport("__Internal")]
        static extern int UnityBackgroundDownloadGetError(IntPtr backend, [MarshalAs(UnmanagedType.LPArray)] byte[] buffer);
    }

}

#endif
