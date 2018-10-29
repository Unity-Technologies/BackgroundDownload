#if UNITY_IOS

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;


namespace Unity.Networking
{

    class BackgroundDownloadiOS : BackgroundDownload
    {
        IntPtr _backend;

        internal BackgroundDownloadiOS(BackgroundDownloadConfig config)
            : base(config)
        {
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

        public override bool keepWaiting
        {
            get
            {
                if (_backend == IntPtr.Zero)
                    return false;
                return UnityBackgroundDownloadIsDone(_backend) == 0;
            }
        }

        internal static Dictionary<string, BackgroundDownload> LoadDownloads()
        {
            var downloads = new Dictionary<string, BackgroundDownload>();
            IntPtr backend = UnityBackgroundDownloadAttach();
            byte[] buffer = null;
            while (backend != IntPtr.Zero)
            {
                if (buffer == null)
                    buffer = new byte[2048];
                BackgroundDownloadConfig config = new BackgroundDownloadConfig();
                int length = UnityBackgroundDownloadGetUrl(backend, buffer);
                config.url = new Uri(Encoding.UTF8.GetString(buffer, 0, length));
                length = UnityBackgroundDownloadGetFilePath(backend, buffer);
                config.filePath = Encoding.UTF8.GetString(buffer, 0, length);
                var dl = new BackgroundDownloadiOS(backend, config);
                downloads[config.filePath] = dl;
                backend = UnityBackgroundDownloadAttach();
            }
            return downloads;
        }

        internal static void SaveDownloads(Dictionary<string, BackgroundDownload> downloads)
        {
        }

        public override void Dispose()
        {
            if (_backend != IntPtr.Zero)
                UnityBackgroundDownloadDestroy(_backend);
            base.Dispose();
        }

        [DllImport("__Internal")]
        static extern IntPtr UnityBackgroundDownloadCreateRequest([MarshalAs(UnmanagedType.LPStr)] string url);

        [DllImport("__Internal")]
        static extern void UnityBackgroundDownloadAddRequestHeader(IntPtr headers,
            [MarshalAs(UnmanagedType.LPStr)] string name, [MarshalAs(UnmanagedType.LPStr)] string value);

        [DllImport("__Internal")]
        static extern IntPtr UnityBackgroundDownloadStart(IntPtr request, [MarshalAs(UnmanagedType.LPStr)] string dest);

        [DllImport("__Internal")]
        static extern int UnityBackgroundDownloadIsDone(IntPtr backend);

        [DllImport("__Internal")]
        static extern void UnityBackgroundDownloadDestroy(IntPtr backend);

        [DllImport("__Internal")]
        static extern IntPtr UnityBackgroundDownloadAttach();

        [DllImport("__Internal")]
        static extern int UnityBackgroundDownloadGetUrl(IntPtr backend, [MarshalAs(UnmanagedType.LPArray)] byte[] buffer);

        [DllImport("__Internal")]
        static extern int UnityBackgroundDownloadGetFilePath(IntPtr backend, [MarshalAs(UnmanagedType.LPArray)] byte[] buffer);
    }

}

#endif
