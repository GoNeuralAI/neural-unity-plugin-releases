using System;
using System.Threading.Tasks;
using UnityEngine;
using System.IO;

namespace Neural
{
    public class ImageTo3dTask : ApiTask
    {
        public string Prompt { get; set; }
        public int? Seed { get; set; }
        public string NegativePrompt { get; set; }

        public string ImageFilePath { get; set; }

        protected override Task<ApiTaskModel> ExecuteInternal()
        {
            if (string.IsNullOrEmpty(ImageFilePath) || !File.Exists(ImageFilePath))
            {
                throw new FileNotFoundException("Image file not found", ImageFilePath);
            }

            byte[] imageData = File.ReadAllBytes(ImageFilePath);
            string fileName = Path.GetFileName(ImageFilePath);

            WWWForm form = new WWWForm();
            try
            {
                form.AddField("prompt", Prompt ?? string.Empty);
                form.AddBinaryData("image", imageData, fileName, "image/png");

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

        protected override string GetEndpoint() => "image-to-3d";
    }
}