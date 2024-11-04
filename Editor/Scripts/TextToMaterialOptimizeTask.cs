using System.Threading.Tasks;
using UnityEngine;

namespace Neural
{
    public class TextToMaterialOptimizeTask : ApiTask
    {
        protected override Task<ApiTaskModel> ExecuteInternal()
        {
            return HttpClient.MakeApiPostRequest<ApiTaskModel>($"{GetEndpoint()}/{TaskId}/optimize");
        }

        protected override string GetEndpoint() => "material";
    }
}
