using Newtonsoft.Json.Linq;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Neural
{
    public class MaterialAsset : Asset
    {
        public string Prompt;
        public string NegativePrompt;
        public int Seed;
        public string AlbedoFileName;
        public string NormalsFileName;
        public string DisplacementFileName;
        public string MetallicFileName;
        public string RoughnessFileName;
        public string AmbientOcclusionFileName;

        public override AssetType GetAssetType()
        {
            return AssetType.Material;
        }

        public override void SerializeAsset(JObject json)
        {
            base.SerializeAsset(json);

            json["Prompt"] = Prompt;
            json["NegativePrompt"] = NegativePrompt;
            json["Seed"] = Seed;
            json["AlbedoFileName"] = AlbedoFileName;
            json["NormalsFileName"] = NormalsFileName;
            json["DisplacementFileName"] = DisplacementFileName;
            json["MetallicFileName"] = MetallicFileName;
            json["RoughnessFileName"] = RoughnessFileName;
            json["AmbientOcclusionFileName"] = AmbientOcclusionFileName;
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

            if (!json.TryGetValue("AlbedoFileName", out var albedoFileNameString))
            {
                Debug.LogError("Failed to deserialize MeshAsset. AlbedoFileName not found.");
                return false;
            }

            AlbedoFileName = albedoFileNameString.Value<string>();

            if (!json.TryGetValue("NormalsFileName", out var normalsFileNameString))
            {
                Debug.LogError("Failed to deserialize MeshAsset. NormalsFileName not found.");
                return false;
            }

            NormalsFileName = normalsFileNameString.Value<string>();

            if (!json.TryGetValue("DisplacementFileName", out var displacementFileNameString))
            {
                Debug.LogError("Failed to deserialize MeshAsset. DisplacementFileName not found.");
                return false;
            }

            DisplacementFileName = displacementFileNameString.Value<string>();

            if (!json.TryGetValue("MetallicFileName", out var metallicFileNameString))
            {
                Debug.LogError("Failed to deserialize MeshAsset. MetallicFileName not found.");
                return false;
            }

            MetallicFileName = metallicFileNameString.Value<string>();

            if (!json.TryGetValue("RoughnessFileName", out var roughnessFileNameString))
            {
                Debug.LogError("Failed to deserialize MeshAsset. RoughnessFileName not found.");
                return false;
            }

            RoughnessFileName = roughnessFileNameString.Value<string>();

            if (!json.TryGetValue("AmbientOcclusionFileName", out var ambientOcclusionFileNameString))
            {
                Debug.LogError("Failed to deserialize MeshAsset. AmbientOcclusionFileName not found.");
                return false;
            }

            AmbientOcclusionFileName = ambientOcclusionFileNameString.Value<string>();

            return true;
        }

        public override Task<Mesh> LoadMesh()
        {
            GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Mesh originalMesh = tmp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(tmp);

            return Task.FromResult(originalMesh);
        }

        public override Material LoadMaterial()
        {
            return ModelImport.LoadMaterial(GetFilePath(AlbedoFileName), GetFilePath(NormalsFileName), GetFilePath(DisplacementFileName), GetFilePath(MetallicFileName), GetFilePath(RoughnessFileName), GetFilePath(AmbientOcclusionFileName));
        }

        public override void ImportInScene()
        {
            Debug.Log("Import material in scene: " + Id);
            string assetSavePath = $"Assets/Neural/Materials/{Id.Substring(0, 6)}";
            Directory.CreateDirectory(assetSavePath);
            Material material = ModelImport.ImportMaterial(
                assetSavePath,
                GetFilePath(AlbedoFileName), 
                GetFilePath(NormalsFileName), 
                GetFilePath(DisplacementFileName), 
                GetFilePath(MetallicFileName),
                GetFilePath(RoughnessFileName), 
                GetFilePath(AmbientOcclusionFileName)
            );

            Selection.activeObject = material;
            EditorUtility.FocusProjectWindow();
            EditorGUIUtility.PingObject(material);
        }
    }
}