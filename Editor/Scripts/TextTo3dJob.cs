using UnityEngine;

namespace Neural
{
    public class TextTo3dJob : Job
    {
        public override JobType Type { get; } = JobType.TextTo3D;
        protected string Prompt { get; private set; }
        protected string NegativePrompt { get; private set; }
        protected int Seed { get; private set; }

        protected const string GlbOriginalFileName = "mesh_orig.glb";
        protected const string GlbFileName = "mesh.glb";
        protected const string AlbedoFileName = "albedo.png";

        public TextTo3dJob (string prompt, string negativePrompt = "", int seed = 0)
        {
            Prompt = prompt;
            NegativePrompt = negativePrompt;
            Seed = seed;
        }

        public override async void Execute()
        {
            SetStatusRunning();

            TextTo3dPreviewTask previewTask = new() { 
                Prompt = Prompt, 
                NegativePrompt = NegativePrompt, 
                Seed = Seed
            };

            await previewTask.Execute();

            if (!previewTask.IsSuccessful())
            {
                SetStatusFailed();
                return;
            }

            SetProgress(0.33f);
            TextTo3dOptimizeTask optimizeTask = new TextTo3dOptimizeTask { TaskId = previewTask.CompletedTask.Id };
            await optimizeTask.Execute();

            if (!optimizeTask.IsSuccessful())
            {
                SetStatusFailed();
                return;
            }

            SetProgress(0.66f);

            try
            {
                await DownloadFile(optimizeTask.CompletedTask.Urls.Glb, GlbOriginalFileName);
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