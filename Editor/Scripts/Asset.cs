using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using UnityEditor.VersionControl;
using UnityEngine;

namespace Neural
{
    public enum AssetType
    {
        Unknown,
        Mesh,
        Material,
    }

    public abstract class Asset
    {
        public string Id;
        public bool IsTemp = false;
        public DateTime CreatedAt;
        public string AssetPath;
        public bool IsFavorite = false;

        public Asset()
        {
        }

        public void InitTemp()
        {
            Id = Guid.NewGuid().ToString();
            CreatedAt = DateTime.Now;

            string TempPath = Path.Combine(Context.GetAppDataPath(), "Temp");
            AssetPath = Path.Combine(TempPath, Id);

            if (!Directory.Exists(AssetPath))
            {
                Directory.CreateDirectory(AssetPath);
            }

            IsTemp = true;
        }

        public virtual AssetType GetAssetType() {
            return AssetType.Unknown;
        }

        public virtual bool AddFile(string path, string fileName)
        {
            string destPath = Path.Combine(AssetPath, fileName);

            if (!File.Exists(path))
            {
                return false;
            }

            File.Copy(path, destPath);

            return true;

        }

        static string TypeToString(AssetType type)
        {
            switch (type)
            {
                case AssetType.Mesh:
                    return "Mesh";
                case AssetType.Material:
                    return "Material";
                default:
                    return "Unknown";
            }
        }

        static AssetType StringToType(string type)
        {
            switch (type)
            {
                case "Mesh":
                    return AssetType.Mesh;
                case "Material":
                    return AssetType.Material;
                default:
                    return AssetType.Unknown;
            }
        }

        public virtual void SerializeAsset(JObject json)
        {
            json["Id"] = Id;
            json["Type"] = TypeToString(GetAssetType());
            json["CreatedAt"] = CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
            json["IsFavorite"] = IsFavorite;
        }

        public virtual bool DeserializeAsset(JObject json)
        {
            if (!json.TryGetValue("Id", out var idString))
            {
                Debug.LogError("Failed to deserialize Asset. Id not found.");
                return false;
            }

            Id = idString.Value<string>();

            if (!json.TryGetValue("CreatedAt", out var createdAtString))
            {
                Debug.LogError("Failed to deserialize Asset. CreatedAt not found.");
                return false;
            }

            CreatedAt = DateTime.Parse(createdAtString.Value<string>(), CultureInfo.InvariantCulture);

            if (!json.TryGetValue("IsFavorite", out var isFavorite))
            {
                Debug.LogError("Failed to deserialize Asset. IsFavorite not found.");
                return false;
            }

            IsFavorite = isFavorite.Value<bool>();

            return true;
        }

        public abstract Task<Mesh> LoadMesh();

        public abstract Material LoadMaterial();

        public abstract void ImportInScene();

        protected string GetFilePath(string fileName)
        {
            return Path.Combine(AssetPath, fileName);
        }
    }
}