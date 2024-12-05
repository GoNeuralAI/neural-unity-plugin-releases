using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Text;
using System;

namespace Neural
{
    public class HttpClient
    {
        private const int Timeout = 60;

        public class HttpException : Exception
        {
            public long StatusCode { get; }
            public string ResponseBody { get; }

            public HttpException(long statusCode, string message, string responseBody)
                : base($"HTTP Error {statusCode}: {message}")
            {
                StatusCode = statusCode;
                ResponseBody = responseBody;
            }
        }

        private void ThrowHttpException(UnityWebRequest request)
        {
            long statusCode = request.responseCode;
            string message = request.error;
            string responseBody = request.downloadHandler?.text ?? string.Empty;

            Debug.LogError($"HTTP Error {statusCode}: {message}");
            Debug.LogError($"Response Body: {responseBody}");

            throw new HttpException(statusCode, message, responseBody);
        }

        public async Task<T> MakeApiGetRequest<T>(string endpoint)
        {
            string url = $"{Context.BaseUrl}/{endpoint}";
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = Timeout;
                request.SetRequestHeader("Authorization", $"Bearer {Context.ApiKey}");

                var operation = request.SendWebRequest();
                while (!operation.isDone)
                    await Task.Yield();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    ThrowHttpException(request);
                }

                string jsonResult = request.downloadHandler.text;
                //Debug.Log("Response: " + jsonResult);
                return JsonConvert.DeserializeObject<T>(jsonResult);
            }
        }

        public async Task<TResponse> MakeApiPostRequest<TResponse>(string endpoint, object data = null)
        {
            string url = $"{Context.BaseUrl}/{endpoint}";
            UnityWebRequest request = null;

            try
            {
                if (data is WWWForm form)
                {
                    request = UnityWebRequest.Post(url, form);
                }
                else
                {
                    string jsonBody = JsonConvert.SerializeObject(data);
                    request = new UnityWebRequest(url, "POST");
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");
                }

                request.timeout = Timeout;
                request.SetRequestHeader("Authorization", $"Bearer {Context.ApiKey}");

                var operation = request.SendWebRequest();

                while (!operation.isDone)
                    await Task.Yield();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    ThrowHttpException(request);
                }

                string jsonResult = request.downloadHandler.text;
                //Debug.Log("Response: " + jsonResult);
                return JsonConvert.DeserializeObject<TResponse>(jsonResult);
            } finally
            {
                request?.Dispose();
            }
        }

        public async Task<byte[]> DownloadFile(string url)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = Timeout;
                request.SetRequestHeader("Authorization", $"Bearer {Context.ApiKey}");
                var operation = request.SendWebRequest();
                while (!operation.isDone)
                    await Task.Yield();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    ThrowHttpException(request);
                }

                return request.downloadHandler.data;
            }
        }
    }
}