using UnityEngine;

namespace Neural
{
    public class ImageTo3dJob : Job
    {
        public override JobType Type { get; } = JobType.ImageTo3D;
        protected string Prompt { get; private set; }
        protected string NegativePrompt { get; private set; }
        protected int Seed { get; private set; }
        protected string ImageFilePath { get; private set; }

        protected const string GlbOriginalFileName = "mesh_orig.glb";
        protected const string GlbFileName = "model.glb";
        protected const string AlbedoFileName = "albedo.png";

        public ImageTo3dJob(string prompt, string imageFilePath, string negativePrompt = "", int seed = 0)
        {
            Prompt = prompt;
            NegativePrompt = negativePrompt;
            Seed = seed;
            ImageFilePath = imageFilePath;
        }

        public override async void Execute()
        {
            SetStatusRunning();

            ImageTo3dTask task = new() { 
                Prompt = Prompt, 
                ImageFilePath = ImageFilePath, 
                NegativePrompt = NegativePrompt, 
                Seed = Seed,  
            };

            await task.Execute();

            if (!task.IsSuccessful())
            {
                SetStatusFailed();
                return;
            }

            SetProgress(0.5f);

            try
            {
                await DownloadFile(task.CompletedTask.Urls.Glb, GlbOriginalFileName);
            } 
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to download glb file: {e.Message}");
                SetStatusFailed();
                return;
            }

            await ModelImport.ProcessGlbAsync(GetFilePath(GlbOriginalFileName), GetFilePath(GlbFileName));
            ModelImport.ExtractTexturesFromGlb(GetFilePath(GlbFileName), GetFilePath(AlbedoFileName));

            SetProgress(1f);
            SetStatusCompleted();
        }

        protected override Asset CreateAsset()
        {
            var asset = new MeshAsset();

            asset.InitTemp();
            asset.Prompt = Prompt;
            asset.NegativePrompt = NegativePrompt;
            asset.Seed = Seed;
            asset.MeshFileName = GlbFileName;
            asset.AlbedoFileName = AlbedoFileName;

            var meshPath = GetFilePath(GlbFileName);
            if (!asset.AddFile(meshPath, GlbFileName))
            {
                Debug.LogError("Failed to add mesh file to asset.");
                return null;
            }

            var albedoPath = GetFilePath(AlbedoFileName);
            if (!asset.AddFile(albedoPath, AlbedoFileName))
            {
                Debug.LogError("Failed to add albedo file to asset.");
                return null;
            }

            return asset;
        }
    }
}