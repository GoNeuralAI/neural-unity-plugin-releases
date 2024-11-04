using Newtonsoft.Json.Linq;
using System.IO;
using System.Threading.Tasks;
using UnityEditor.VersionControl;
using UnityEditor;
using UnityEngine;

namespace Neural
{
    public class MeshAsset : Asset
    {
        public string Prompt;
        public string NegativePrompt;
        public int Seed;
        public string MeshFileName;
        public string AlbedoFileName;

        public override AssetType GetAssetType()
        {
            return AssetType.Mesh;
        }

        public override void SerializeAsset(JObject json)
        {
            base.SerializeAsset(json);

            json["Prompt"] = Prompt;
            json["NegativePrompt"] = NegativePrompt;
            json["Seed"] = Seed;
            json["MeshFileName"] = MeshFileName;
            json["AlbedoFileName"] = AlbedoFileName;
        }

        public override bool DeserializeAsset(JObject json)
        {
            base.DeserializeAsset(json);

            if (!json.TryGetValue("Prompt", out var promptString))
            {
                Debug.LogError("Failed to deserialize MeshAsset. Prompt not found.");
                return false;
            }

            Prompt = promptString.Value<string>();

            if (!json.TryGetValue("NegativePrompt", out var negativePromptString))
            {
                Debug.LogError("Failed to deserialize MeshAsset. NegativePrompt not found.");
                return false;
            }

            NegativePrompt = negativePromptString.Value<string>();

            if (!json.TryGetValue("Seed", out var seedString))
            {
                Debug.LogError("Failed to deserialize MeshAsset. Seed not found.");
                return false;
            }

            Seed = seedString.Value<int>();

            if (!json.TryGetValue("MeshFileName", out var meshFileNameString))
            {
                Debug.LogError("Failed to deserialize MeshAsset. MeshFileName not found.");
                return false;
            }

            MeshFileName = meshFileNameString.Value<string>();

            if (!json.TryGetValue("AlbedoFileName", out var albedoFileNameString))
            {
                Debug.LogError("Failed to deserialize MeshAsset. AlbedoFileName not found.");
                return false;
            }

            AlbedoFileName = albedoFileNameString.Value<string>();

            return true;
        }

        public override Task<Mesh> LoadMesh()
        {
            return ModelImport.LoadMeshAsync(GetFilePath(MeshFileName));
        }

        public override Material LoadMaterial()
        {
            return ModelImport.LoadMaterial(GetFilePath(AlbedoFileName));
        }

        public override void ImportInScene()
        {
            string assetSavePath = "Assets/Neural/Meshes";
            string outputPath = Path.Combine(assetSavePath, $"Model_{Id.Substring(0, 6)}.glb");
            Directory.CreateDirectory(assetSavePath);
            ModelImport.EmbedTexturesToGlb(GetFilePath(MeshFileName), GetFilePath(AlbedoFileName), outputPath);

            UnityEditor.AssetDatabase.Refresh();

            Object obj = UnityEditor.AssetDatabase.LoadAssetAtPath<Object>(outputPath);

            Selection.activeObject = obj;
            EditorUtility.FocusProjectWindow();
            EditorGUIUtility.PingObject(obj);
        }
    }
}