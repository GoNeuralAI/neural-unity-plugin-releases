using System.Collections.Generic;

namespace Neural
{
    public class JobController
    {
        public delegate void JobAddedDelegate(Job job);
        public event JobAddedDelegate OnJobAdded;

        private Dictionary<string, Job> jobsById = new Dictionary<string, Job>();
        private Dictionary<JobType, HashSet<Job>> jobsByType = new Dictionary<JobType, HashSet<Job>>();

        public Job GetJob(string id)
        {
            if (jobsById.TryGetValue(id, out Job job))
            {
                return job;
            }
            return null;
        }

        public Job GetJobByAsset(string assetId) {
            foreach (var job in jobsById.Values)
            {
                if (job.Asset.Id == assetId)
                {
                    return job;
                }
            }
            return null;
        }

        public HashSet<Job> GetJobsByType(JobType type)
        {
            if (jobsByType.TryGetValue(type, out HashSet<Job> jobs))
            {
                return jobs;
            }
            return new HashSet<Job>();
        }

        public void AddJob(Job job)
        {
            jobsById[job.Id] = job;

            if (!jobsByType.ContainsKey(job.Type))
            {
                jobsByType[job.Type] = new HashSet<Job>();
            }
            jobsByType[job.Type].Add(job);

            OnJobAdded?.Invoke(job);
        }

        public void DeleteJobsOfType(JobType type)
        {
            if (jobsByType.TryGetValue(type, out HashSet<Job> jobs))
            {
                foreach (var job in jobs)
                {
                    jobsById.Remove(job.Id);
                }
                jobsByType.Remove(type);
            }
        }

        public void DeleteJob(string id)
        {
            if (jobsById.TryGetValue(id, out Job job))
            {
                jobsById.Remove(id);
                jobsByType[job.Type].Remove(job);
            }
        }
    }
}