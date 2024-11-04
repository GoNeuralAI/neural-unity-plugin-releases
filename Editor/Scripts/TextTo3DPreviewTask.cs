using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static Neural.HttpClient;

namespace Neural
{
    public class TextTo3dPreviewTask : ApiTask
    {
        public string Prompt { get; set; }
        public int? Seed { get; set; }
        public string NegativePrompt { get; set; }

        protected override Task<ApiTaskModel> ExecuteInternal()
        {
            var request = new Dictionary<string, object>
            {
                ["prompt"] = Prompt
            };
            if (Seed > 0)
            {
                request["seed"] = Seed;
            }
            if (!string.IsNullOrEmpty(NegativePrompt))
            {
                request["negativePrompt"] = NegativePrompt;
            }

            return HttpClient.MakeApiPostRequest<ApiTaskModel>(GetEndpoint(), request);
        }

        protected override string GetEndpoint() => "text-to-3d";
    }
}