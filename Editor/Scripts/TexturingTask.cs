using System;
using System.Threading.Tasks;
using UnityEngine;
using System.IO;

namespace Neural
{
    public class TexturingTask : ApiTask
    {
        public string Prompt { get; set; }
        public int? Seed { get; set; }
        public string NegativePrompt { get; set; }

        public string DepthFilePath { get; set; }

        public string NormalsFilePath { get; set; }

        protected override Task<ApiTaskModel> ExecuteInternal()
        {
            if (string.IsNullOrEmpty(DepthFilePath) || !File.Exists(DepthFilePath))
            {
                throw new FileNotFoundException("DepthMap file not found", DepthFilePath);
            }

            if (string.IsNullOrEmpty(NormalsFilePath) || !File.Exists(NormalsFilePath))
            {
                throw new FileNotFoundException("NormalMap file not found", NormalsFilePath);
            }

            byte[] depathMapData = File.ReadAllBytes(DepthFilePath);
            string depthMapfileName = Path.GetFileName(DepthFilePath);
            
            byte[] normalMapData = File.ReadAllBytes(NormalsFilePath);
            string normalMapfileName = Path.GetFileName(NormalsFilePath);

            WWWForm form = new WWWForm();
            try
            {
                form.AddField("prompt", Prompt ?? string.Empty);
                form.AddBinaryData("depth", depathMapData, depthMapfileName, "image/png");
                form.AddBinaryData("normal", normalMapData, normalMapfileName, "image/png");

                if (Seed.HasValue)
                {
                    form.AddField("seed", Seed.Value.ToString());
                }

                if (!string.IsNullOrEmpty(NegativePrompt))
                {
                    form.AddField("negativePrompt", NegativePrompt);
                }
            }
            catch (Exception formEx)
            {
                Debug.LogError($"Error adding data to form: {formEx.Message}");
                throw;
            }

            return HttpClient.MakeApiPostRequest<ApiTaskModel>(GetEndpoint(), form);
        }

        protected override string GetEndpoint() => "texturing";
    }
}