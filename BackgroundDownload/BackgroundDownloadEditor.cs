#if UNITY_EDITOR

using System.Collections.Generic;

namespace Unity.Networking
{
    class BackgroundDownloadEditor : BackgroundDownload
    {
        public BackgroundDownloadEditor(BackgroundDownloadConfig config)
            : base(config)
        {
        }

        public override bool keepWaiting { get { return false; } }

        internal static Dictionary<string, BackgroundDownload> LoadDownloads()
        {
            return new Dictionary<string, BackgroundDownload>();
        }

        internal static void SaveDownloads(Dictionary<string, BackgroundDownload> downloads)
        {
        }
    }
}

#endif
