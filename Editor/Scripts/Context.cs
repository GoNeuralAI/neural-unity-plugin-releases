using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;

namespace Neural
{
    public static class Context
    {
        private const string BaseUrlKey = "Neural_BaseUrl";
        private const string ApiKeyKey = "Neural_ApiKey";

        public static string BaseUrl
        {
            get => EditorPrefs.GetString(BaseUrlKey, "https://api.goneural.ai/v1");
            set => EditorPrefs.SetString(BaseUrlKey, value);
        }

        public static string ApiKey
        {
            get => EditorPrefs.GetString(ApiKeyKey, "");
            set => EditorPrefs.SetString(ApiKeyKey, value);
        }

        public static string GetAppDataPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Neural");
        }

        public static JobController JobController { get; } = new JobController();

        public static AssetDatabase AssetDatabase { get; } = new AssetDatabase();

        public static Billing Billing { get; } = new Billing();
    }
}