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
        public int FaceLimit;
        public bool Pbr;
        public string MeshFileName;
        public string AlbedoFileName;
        public string MetallicRoughnessFileName;
        public string NormalsFileName;

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
            json["FaceLimit"] = FaceLimit;
            json["Pbr"] = Pbr;
            json["MeshFileName"] = MeshFileName;
            json["AlbedoFileName"] = AlbedoFileName;

            if (Pbr)
            {
                json["MetallicRoughnessFileName"] = MetallicRoughnessFileName;
                json["NormalsFileName"] = NormalsFileName;
            }
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

            if (!json.TryGetValue("FaceLimit", out var faceLimitString))
            {
                Debug.LogError("Failed to deserialize MeshAsset. FaceLimit not found.");
                return false;
            }

            FaceLimit = faceLimitString.Value<int>();

            if (!json.TryGetValue("Pbr", out var pbrString))
            {
                Debug.LogError("Failed to deserialize MeshAsset. Pbr not found.");
                return false;
            }

            Pbr = pbrString.Value<bool>();

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

            if (Pbr)
            {
                if (!json.TryGetValue("MetallicRoughnessFileName", out var metallicRoughnessFileNameString))
                {
                    Debug.LogError("Failed to deserialize MeshAsset. MetallicRoughnessFileName not found.");
                    return false;
                }

                MetallicRoughnessFileName = metallicRoughnessFileNameString.Value<string>();

                if (!json.TryGetValue("NormalsFileName", out var normalsFileNameString))
                {
                    Debug.LogError("Failed to deserialize MeshAsset. NormalsFileName not found.");
                    return false;
                }

                NormalsFileName = normalsFileNameString.Value<string>();
            }

            return true;
        }

        public override Task<Mesh> LoadMesh()
        {
            return ModelImport.LoadMeshAsync(GetFilePath(MeshFileName));
        }

        public override Material LoadMaterial()
        {
            if (!Pbr)
            {
                return ModelImport.LoadMaterial(GetFilePath(AlbedoFileName));
            }
            else
            {
                return ModelImport.LoadMaterial(GetFilePath(AlbedoFileName), GetFilePath(NormalsFileName), null, GetFilePath(MetallicRoughnessFileName), null, null, true);
            }
        }

        public override void ImportInScene()
        {
            string assetSavePath = "Assets/Neural/Meshes";
            string outputPath = Path.Combine(assetSavePath, $"Model_{Id.Substring(0, 6)}.glb");
            Directory.CreateDirectory(assetSavePath);
            File.Copy(GetFilePath(MeshFileName), outputPath, true);

            UnityEditor.AssetDatabase.Refresh();

            Object obj = UnityEditor.AssetDatabase.LoadAssetAtPath<Object>(outputPath);

            Selection.activeObject = obj;
            EditorUtility.FocusProjectWindow();
            EditorGUIUtility.PingObject(obj);
        }
    }
}