using System;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Neural
{
    public enum ApiTaskKind
    {
        Preview,
        Optimize
    }


    [JsonConverter(typeof(TaskStatusConverter))]
    public enum ApiTaskStatus
    {
        Queued,
        InProgress,
        Succeeded,
        Failed
    }

    public class ApiTaskModel
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("kind")]
        public ApiTaskKind? Kind { get; set; }

        [JsonProperty("status")]
        public ApiTaskStatus Status { get; set; }

        [JsonProperty("seed")]
        public int Seed { get; set; }

        [JsonProperty("prompt")]
        public string Prompt { get; set; }

        [JsonProperty("negativePrompt")]
        public string NegativePrompt { get; set; }

        [JsonProperty("urls")]
        [JsonConverter(typeof(TaskUrlsConverter))]
        public TaskUrls Urls { get; set; }

        [JsonProperty("finishedAt")]
        public DateTime? FinishedAt { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("creditsUsed")]
        public int CreditsUsed { get; set; }
    }

    public class TaskUrls
    {
        #nullable enable
        [JsonProperty("glb")]
        public string? Glb { get; set; }

        [JsonProperty("thumbnail")]
        public string? Thumbnail { get; set; }

        [JsonProperty("albedo")]
        public string? Albedo { get; set; }

        [JsonProperty("normal")]
        public string? Normals { get; set; }

        [JsonProperty("displacement")]
        public string? Displacement { get; set; }

        [JsonProperty("metalness")]
        public string? Metallic { get; set; }

        [JsonProperty("roughness")]
        public string? Roughness { get; set; }

        [JsonProperty("ao")]
        public string? AmbientOcclusion { get; set; }
        #nullable disable
    }

    public class TaskUrlsConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(TaskUrls);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JToken token = JToken.Load(reader);

            if (token.Type == JTokenType.Array)
            {
                // If it's an empty array, return an empty TaskUrls object
                return new TaskUrls();
            }
            else if (token.Type == JTokenType.Object)
            {
                // If it's an object, deserialize it normally
                return token.ToObject<TaskUrls>();
            }

            // If it's neither an array nor an object, return null or throw an exception
            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException("Unnecessary because CanWrite is false. The default serialization is used.");
        }

        public override bool CanWrite
        {
            get { return false; }
        }
    }

    public class TaskStatusConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ApiTaskStatus);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var value = reader.Value as string;
            if (value == null)
                return ApiTaskStatus.Failed;

            switch (value.ToLower())
            {
                case "queued":
                    return ApiTaskStatus.Queued;
                case "in_progress":
                    return ApiTaskStatus.InProgress;
                case "succeeded":
                    return ApiTaskStatus.Succeeded;
                case "failed":
                    return ApiTaskStatus.Failed;
                default:
                    return ApiTaskStatus.Failed;
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            ApiTaskStatus status = (ApiTaskStatus)value;
            switch (status)
            {
                case ApiTaskStatus.Queued:
                    writer.WriteValue("queued");
                    break;
                case ApiTaskStatus.InProgress:
                    writer.WriteValue("in_progress");
                    break;
                case ApiTaskStatus.Succeeded:
                    writer.WriteValue("succeeded");
                    break;
                case ApiTaskStatus.Failed:
                    writer.WriteValue("failed");
                    break;
                default:
                    UnityEngine.Debug.LogWarning("Unknown task status: " + status);
                    writer.WriteValue("failed");
                    break;
            }
        }
    }
}