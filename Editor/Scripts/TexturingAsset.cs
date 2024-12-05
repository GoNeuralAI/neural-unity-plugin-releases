using Newtonsoft.Json.Linq;
using System.IO;
using System.Threading.Tasks;
using UnityEditor.VersionControl;
using UnityEditor;
using UnityEngine;

namespace Neural
{
    public class TexturingAsset : Asset
    {
        public string Prompt;
        public string NegativePrompt;
        public int Seed;
        public string AlbedoFileName;

        public override AssetType GetAssetType()
        {
            return AssetType.Texture;
        }

        public override void SerializeAsset(JObject json)
        {
            base.SerializeAsset(json);

            json["Prompt"] = Prompt;
            json["NegativePrompt"] = NegativePrompt;
            json["Seed"] = Seed;
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
            return null;
        }

        public override Material LoadMaterial()
        {
            return null;
        }

        public override void ImportInScene()
        {
            // do nothing
        }
    }
}