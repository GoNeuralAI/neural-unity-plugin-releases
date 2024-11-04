using System;
using System.Threading.Tasks;
using UnityEngine;
using static Neural.HttpClient;

namespace Neural
{
    public abstract class ApiTask
    {
        protected HttpClient HttpClient;
        public ApiTaskStatus Status { get; protected set; } = ApiTaskStatus.Queued;
        protected DateTime StartTime;
        public event Action<ApiTaskStatus> OnStatusChanged;
        public ApiTaskModel CompletedTask { get; protected set; }
        public string TaskId { get; set; }

        protected const int MaxRetries = 10;

        public ApiTask()
        {
            HttpClient = new HttpClient();
        }

        public async Task Execute()
        {
            try
            {
                await ExecuteWithRetry();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error executing {GetType().Name} after all retries: {ex.Message}");
                SetStatus(ApiTaskStatus.Failed);
            }
        }

        protected abstract Task<ApiTaskModel> ExecuteInternal();

        private async Task ExecuteWithRetry()
        {
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    var response = await ExecuteInternal();

                    if (response != null)
                    {
                        TaskId = response.Id;
                        await PollStatusWithRetry();
                        return;
                    }
                    else
                    {
                        throw new HttpException(460, "API request returned null response", "");
                    }
                }
                catch (HttpException ex)
                {
                    if (ex.StatusCode == 401)
                    {
                        Debug.LogError("API request failed: Unauthorized. Please check your API key.");
                        SetStatus(ApiTaskStatus.Failed);
                        return;
                    }
                    else
                    {
                        Debug.LogWarning($"Attempt {attempt} failed for {GetType().Name}: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in {GetType().Name}: {ex.Message}");
                    SetStatus(ApiTaskStatus.Failed);
                    return;
                }

                if (attempt < MaxRetries)
                {
                    await Task.Delay((int)Mathf.Pow(1.5f, attempt) * 1000);
                }
                else
                {
                    SetStatus(ApiTaskStatus.Failed);
                    throw new Exception($"All {MaxRetries} attempts failed for {GetType().Name}");
                }
            }
        }

        protected async Task PollStatusWithRetry()
        {
            if (string.IsNullOrEmpty(TaskId))
            {
                Debug.LogError("TaskId is null or empty");
                SetStatus(ApiTaskStatus.Failed);
                return;
            }

            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    var response = await HttpClient.MakeApiGetRequest<ApiTaskModel>($"{GetEndpoint()}/{TaskId}");
                    if (response != null)
                    {
                        SetStatus(ParseStatus(response.Status));
                        if (Status == ApiTaskStatus.Queued || Status == ApiTaskStatus.InProgress)
                        {
                            await Task.Delay(1000);
                            await PollStatusWithRetry(); // Continue polling
                            return;
                        }
                        else
                        {
                            SetStatus(ApiTaskStatus.Succeeded);
                            CompletedTask = response;
                            return; // Success, exit the retry loop
                        }
                    }
                    else
                    {
                        throw new HttpException(460, "API request returned null response", "");
                    }
                }
                catch (HttpException ex)
                {
                    if (ex.StatusCode == 401)
                    {
                        Debug.LogError("API request failed: Unauthorized. Please check your API key.");
                        SetStatus(ApiTaskStatus.Failed);
                        return;
                    }
                    else
                    {
                        Debug.LogWarning($"Polling attempt {attempt} failed for {GetType().Name}: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Polling error in {GetType().Name}: {ex.Message}");
                    SetStatus(ApiTaskStatus.Failed);
                    return;
                }

                if (attempt < MaxRetries)
                {
                    await Task.Delay((int)Mathf.Pow(1.5f, attempt) * 1000);
                }
                else
                {
                    SetStatus(ApiTaskStatus.Failed);
                    throw new Exception($"All {MaxRetries} polling attempts failed for {GetType().Name}");
                }
            }
        }

        protected abstract string GetEndpoint();

        protected void SetStatus(ApiTaskStatus newStatus)
        {
            if (Status != newStatus)
            {
                Status = newStatus;
                OnStatusChanged?.Invoke(Status);
            }
        }

        protected static ApiTaskStatus ParseStatus(ApiTaskStatus status)
        {
            return status switch
            {
                ApiTaskStatus.Queued => ApiTaskStatus.Queued,
                ApiTaskStatus.InProgress => ApiTaskStatus.InProgress,
                ApiTaskStatus.Succeeded => ApiTaskStatus.Succeeded,
                ApiTaskStatus.Failed => ApiTaskStatus.Failed,
                _ => ApiTaskStatus.Failed,
            };
        }

        public bool IsCompleted() => Status == ApiTaskStatus.Succeeded || Status == ApiTaskStatus.Failed;

        public bool IsSuccessful() => Status == ApiTaskStatus.Succeeded;
    }
}