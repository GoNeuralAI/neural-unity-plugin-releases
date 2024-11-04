using System.Threading.Tasks;
using UnityEngine;
using static Neural.HttpClient;

namespace Neural
{
    public class TextTo3dOptimizeTask : ApiTask
    {
        protected override  Task<ApiTaskModel> ExecuteInternal()
        {
            return HttpClient.MakeApiPostRequest<ApiTaskModel>($"{GetEndpoint()}/{TaskId}/optimize");
        }

        protected override string GetEndpoint() => "text-to-3d";
    }
}
