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
        protected int FaceLimit { get; private set; }
        protected bool Pbr { get; private set; }

        protected const string GlbOriginalFileName = "mesh_orig.glb";
        protected const string GlbFileName = "model.glb";
        protected const string AlbedoFileName = "albedo.png";
        protected const string MetallicRoughnessFileName = "metallicRoughness.png";
        protected const string NormalsFileName = "normals.png";

        public ImageTo3dJob(string prompt, string imageFilePath, string negativePrompt = "", int seed = 0, int faceLimit = 0, bool pbr = false)
        {
            Prompt = prompt;
            NegativePrompt = negativePrompt;
            Seed = seed;
            ImageFilePath = imageFilePath;
            FaceLimit = faceLimit;
            Pbr = pbr;
        }

        public override async void Execute()
        {
            SetStatusRunning();

            _ = Context.Billing.UpdateBilling(5000);

            ImageTo3dTask task = new() { 
                Prompt = Prompt, 
                ImageFilePath = ImageFilePath, 
                NegativePrompt = NegativePrompt, 
                Seed = Seed,
                FaceLimit = FaceLimit,
                Pbr = Pbr
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

            ModelImport.ExtractTexturesFromGlb(GetFilePath(GlbFileName), GetFilePath(AlbedoFileName), 0);

            if (Pbr)
            {
                ModelImport.ExtractTexturesFromGlb(GetFilePath(GlbFileName), GetFilePath(MetallicRoughnessFileName), 1);
                ModelImport.ExtractTexturesFromGlb(GetFilePath(GlbFileName), GetFilePath(NormalsFileName), 2);
            }

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
            asset.FaceLimit = FaceLimit;
            asset.Pbr = Pbr;
            asset.MeshFileName = GlbFileName;
            asset.AlbedoFileName = AlbedoFileName;
            asset.MetallicRoughnessFileName = MetallicRoughnessFileName;
            asset.NormalsFileName = NormalsFileName;


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

            if (Pbr)
            {
                var metallicRoughnessPath = GetFilePath(MetallicRoughnessFileName);

                if (!asset.AddFile(metallicRoughnessPath, MetallicRoughnessFileName))
                {
                    Debug.LogError("Failed to add metallicRoughness file to asset.");
                    return null;
                }

                var normalsPath = GetFilePath(NormalsFileName);

                if (!asset.AddFile(normalsPath, NormalsFileName))
                {
                    Debug.LogError("Failed to add normals file to asset.");
                    return null;
                }
            }

            return asset;
        }
    }
}