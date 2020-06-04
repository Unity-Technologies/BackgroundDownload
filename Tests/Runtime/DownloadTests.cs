using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Networking;

namespace Tests
{
    public class DownloadTests
    {
        const string TEST_URL = "https://memegenerator.net/img/images/1154731/success-baby.jpg";
        const string TEST_FILE = "success.jpg";
        const string TEST_FILE_IN_DIR = "tests/success.jpg";

        string FilePath { get { return Path.Combine(Application.persistentDataPath, TEST_FILE); } }
        string FileInDirPath { get { return Path.Combine(Application.persistentDataPath, TEST_FILE_IN_DIR); } }

        [SetUp]
        public void RemoveTestFile()
        {
            if (File.Exists(FilePath))
                File.Delete(FilePath);
            if (File.Exists(FileInDirPath))
                File.Delete(FileInDirPath);
        }

        [UnityTest]
        public IEnumerator DownloadFile()
        {
#if UNITY_EDITOR
            yield break;
#else
            return DownloadFileTest(TEST_FILE);
#endif
        }

        [UnityTest]
        public IEnumerator DownloadFileToSubdir()
        {
#if UNITY_EDITOR
            yield break;
#else
            return DownloadFileTest(TEST_FILE_IN_DIR);
#endif
        }

        IEnumerator DownloadFileTest(string filePath)
        {
            using (var download = BackgroundDownload.Start(new Uri(TEST_URL), filePath))
            {
                yield return download;
                Assert.AreEqual(BackgroundDownloadStatus.Done, download.status);
                Assert.IsTrue(File.Exists(Path.Combine(Application.persistentDataPath, filePath)));
            }
        }
    }
}
