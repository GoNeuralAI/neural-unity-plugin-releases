using PlasticGui.WorkspaceWindow.PendingChanges;
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Neural
{
    public enum JobStatus
    {
        Pending,
        Running,
        Completed,
        Failed
    }

    public enum JobType
    {
        Unknown,
        TextTo3D,
        ImageTo3D,
        Material,
    }

    public abstract class Job
    {
        public delegate void JobProgressDelegate(float progress);
        public event JobProgressDelegate OnJobProgress;

        public delegate void JobStatusChangedDelegate(JobStatus status);
        public event JobStatusChangedDelegate OnJobStatusChanged;

        public string Id { get; private set; }
        public float Progress { get; private set; } = 0.0f;
        public JobStatus Status { get; private set; } = JobStatus.Pending;
        public abstract JobType Type { get; }
        public Asset Asset { get; protected set; }

        protected string TempPath;

        public Job()
        {
            Id = Guid.NewGuid().ToString();

            TempPath = Path.Combine(Context.GetAppDataPath(), "Jobs", Id);

            if (!Directory.Exists(TempPath))
            {
                Directory.CreateDirectory(TempPath);
            }
        }

        public abstract void Execute();

        protected abstract Asset CreateAsset();

        protected void SetStatusRunning()
        {
            Status = JobStatus.Running;
            OnJobStatusChanged?.Invoke(Status);
        }

        protected void SetStatusFailed()
        {
            Status = JobStatus.Failed;
            OnJobStatusChanged?.Invoke(Status);
            DeleteTempPath();
        }

        protected void SetStatusCompleted()
        {
            Asset = CreateAsset();
            Status = (Asset == null) ? JobStatus.Failed: JobStatus.Completed;
            SetProgress(1.0f);
            OnJobStatusChanged?.Invoke(Status);
            DeleteTempPath();
        }

        protected void SetProgress(float newProgress)
        {
            Progress = newProgress;
            OnJobProgress?.Invoke(Progress);
        }

        protected async Task DownloadFile(string url, string path)
        {
            var httpClient = new HttpClient();
            var fileData = await httpClient.DownloadFile(url);

            try
            {
                File.WriteAllBytes(GetFilePath(path), fileData);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error saving file: {e.Message}");
            }
        }

        protected string GetFilePath(string fileName)
        {
            return Path.Combine(TempPath, fileName);
        }

        protected bool DeleteTempPath()
        {
            try
            {
                if (Directory.Exists(TempPath))
                {
                    Directory.Delete(TempPath, true);
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete temp path {TempPath}: {e.Message}");
                return false;
            }
        }
    }
}