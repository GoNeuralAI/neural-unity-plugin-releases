using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Neural
{
    /// <summary>
    /// Persistent storage for generated assets.
    /// </summary>
    public class AssetDatabase
    {
        private readonly Dictionary<string, Asset> Assets = new();
        private readonly Dictionary<AssetType, HashSet<string>> AssetsByType = new();

        public Asset GetAsset(string id)
        {
            return Assets.TryGetValue(id, out var asset) ? asset : null;
        }

        public List<Asset> GetAssetsOfType(AssetType type)
        {
            if (AssetsByType.TryGetValue(type, out var assetIds))
            {
                return assetIds
                    .Select(id => GetAsset(id))
                    .Where(asset => asset != null)
                    .OrderByDescending(asset => asset.CreatedAt)
                    .ToList();
            }
            return new List<Asset>();
        }

        public List<Asset> GetAllAssets()
        {
            return Assets.Values
                .OrderByDescending(asset => asset.CreatedAt)
                .ToList();
        }

        public bool SaveAsset(Asset asset)
        {
            string metadataPath = Path.Combine(asset.AssetPath, "metadata.json");

            JObject json = new();
            asset.SerializeAsset(json);

            string jsonString = JsonConvert.SerializeObject(json, Formatting.Indented);

            try
            {
                File.WriteAllText(metadataPath, jsonString);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save metadata for asset {asset.Id}: {e.Message}");
                return false;
            }

            if (asset.IsTemp)
            {
                string assetPath = Path.Combine(GetAssetsPath(), asset.Id);

                try
                {
                    DirectoryCopy(asset.AssetPath, assetPath, true);
                    Directory.Delete(asset.AssetPath, true);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to move asset {asset.Id} to assets directory: {e.Message}");
                    return false;
                }

                asset.AssetPath = assetPath;
                asset.IsTemp = false;
            }

            if (!AddAsset(asset))
            {
                Debug.LogError($"Failed to add asset {asset.Id} to database");
                return false;
            }

            return true;
        }

        public bool DeleteAsset(string id)
        {
            if (!Assets.TryGetValue(id, out var asset))
            {
                return false;
            }

            try {
                Directory.Delete(asset.AssetPath, true);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete asset {id}: {e.Message}");
                return false;
            }

            Assets.Remove(id);

            if (AssetsByType.TryGetValue(asset.GetAssetType(), out var assetSet))
            {
                assetSet.Remove(id);
            }

            return true;
        }

        public void ScanFileSystemForAssets()
        {
            string assetsPath = GetAssetsPath();
            Debug.Log($"Scanning assets directory {assetsPath}");

            if (!Directory.Exists(assetsPath))
            {
                return;
            }

            string[] assetPaths = Directory.GetDirectories(assetsPath);

            foreach (string assetPath in assetPaths)
            {
                string metadataPath = Path.Combine(assetPath, "metadata.json");
                if (!File.Exists(metadataPath))
                {
                    Debug.LogError($"Metadata file for asset {Path.GetFileName(assetPath)} does not exist");
                    continue;
                }

                string jsonString;
                try
                {
                    jsonString = File.ReadAllText(metadataPath);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to load metadata for asset {Path.GetFileName(assetPath)}: {e.Message}");
                    continue;
                }

                JObject json;
                try
                {
                    json = JObject.Parse(jsonString);
                }
                catch (JsonException e)
                {
                    Debug.LogError($"Failed to parse metadata for asset {Path.GetFileName(assetPath)}: {e.Message}");
                    continue;
                }

                if (!json.TryGetValue("Type", out var typeToken) || !(typeToken is JValue typeValue) || !(typeValue.Value is string typeString))
                {
                    Debug.LogError($"Failed to read type for asset {Path.GetFileName(assetPath)}");
                    continue;
                }

                AssetType type = NeuralAssetTypeFromString(typeString);
                if (type == AssetType.Unknown)
                {
                    Debug.LogError($"Unknown type for asset {Path.GetFileName(assetPath)}");
                    continue;
                }

                if (!json.TryGetValue("Id", out var idToken) || !(idToken is JValue idValue) || !(idValue.Value is string id))
                {
                    Debug.LogError($"Failed to read id for asset {Path.GetFileName(assetPath)}");
                    continue;
                }

                if (Assets.ContainsKey(id))
                {
                    continue;
                }

                Asset asset;

                switch (type)
                {
                    case AssetType.Mesh:
                        asset = new MeshAsset();
                        break;
                    case AssetType.Material:
                        asset = new MaterialAsset();
                        break;
                    default:
                        Debug.LogError($"Unknown asset type {typeString}");
                        continue;
                }

                if (asset == null)
                {
                    Debug.LogError($"Failed to create asset for type {typeString}");
                    continue;
                }

                if (!asset.DeserializeAsset(json))
                {
                    Debug.LogError($"Failed to deserialize asset {Path.GetFileName(assetPath)}");
                    continue;
                }

                asset.AssetPath = Path.Combine(assetsPath, id);
                if (!AddAsset(asset))
                {
                    Debug.LogError($"Failed to add asset {Path.GetFileName(assetPath)} to database");
                    continue;
                }
            }
        }

        private bool AddAsset(Asset asset)
        {
            if (asset.IsTemp)
            {
                Debug.LogError($"Cannot add temporary asset {asset.Id} to database");
                return false;
            }

            Assets[asset.Id] = asset;

            if (!AssetsByType.TryGetValue(asset.GetAssetType(), out var assetSet))
            {
                assetSet = new HashSet<string>();
                AssetsByType[asset.GetAssetType()] = assetSet;
            }

            assetSet.Add(asset.Id);
            return true;
        }

        private string GetAssetsPath()
        {
            return Path.Combine(Context.GetAppDataPath(), "Assets");
        }

        private void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException($"Source directory does not exist or could not be found: {sourceDirName}");
            }

            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        private AssetType NeuralAssetTypeFromString(string typeString)
        {
            if (Enum.TryParse(typeString, out AssetType type))
            {
                return type;
            }

            return AssetType.Unknown;
        }
    }
}