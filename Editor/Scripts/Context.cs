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
        private const string DefaultFaceLimitKey = "Neural_DefaultFaceLimit";
        private const string DefaultPbrKey = "Neural_DefaultPbr";

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

        public static int DefaultFaceLimit
        {
            get => EditorPrefs.GetInt(DefaultFaceLimitKey, 100000);
            set => EditorPrefs.SetInt(DefaultFaceLimitKey, value);
        }

        public static bool DefaultPbr
        {
            get => EditorPrefs.GetBool(DefaultPbrKey, true);
            set => EditorPrefs.SetBool(DefaultPbrKey, value);
        }

        public static string GetAppDataPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Neural", "Unity");
        }

        public static JobController JobController { get; } = new JobController();

        public static AssetDatabase AssetDatabase { get; } = new AssetDatabase();

        public static Billing Billing { get; } = new Billing();
    }
}