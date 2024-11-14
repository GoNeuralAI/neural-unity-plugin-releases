using UnityEngine;

namespace Neural
{
    public class TextToMaterialJob : Job
    {
        public override JobType Type { get; } = JobType.Material;
        protected string Prompt { get; private set; }
        protected string NegativePrompt { get; private set; }
        protected int Seed { get; private set; }

        protected const string ObjFileName = "model.obj";
        protected const string GlbFileName = "model.glb";
        protected const string AlbedoFileName = "albedo.png";
        protected const string NormalsFileName = "normals.png";
        protected const string DisplacementFileName = "displacement.png";
        protected const string MetallicFileName = "metallic.png";
        protected const string RoughnessFileName = "roughness.png";
        protected const string AmbientOcclusionFileName = "ao.png";

        public TextToMaterialJob(string prompt, string negativePrompt = "", int seed = 0)
        {
            Prompt = prompt;
            NegativePrompt = negativePrompt;
            Seed = seed;
        }

        public override async void Execute()
        {
            SetStatusRunning();

            _ = Context.Billing.UpdateBilling(5000);

            TextToMaterialPreviewTask previewTask = new() { 
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
            TextToMaterialOptimizeTask optimizeTask = new TextToMaterialOptimizeTask { TaskId = previewTask.CompletedTask.Id };
            await optimizeTask.Execute();

            if (!optimizeTask.IsSuccessful())
            {
                SetStatusFailed();
                return;
            }

            SetProgress(0.66f);

            try
            {
                await DownloadFile(optimizeTask.CompletedTask.Urls.Albedo, AlbedoFileName);
            } 
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to download albedo file: {e.Message}");
                SetStatusFailed();
                return;
            }

            SetProgress(0.715f);

            try
            {
                await DownloadFile(optimizeTask.CompletedTask.Urls.Normals, NormalsFileName);
            } 
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to download normals file: {e.Message}");
                SetStatusFailed();
                return;
            }

            SetProgress(0.77f);

            try
            {
                await DownloadFile(optimizeTask.CompletedTask.Urls.Displacement, DisplacementFileName);
            } 
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to download displacement file: {e.Message}");
                SetStatusFailed();
                return;
            }

            SetProgress(0.825f);

            try { 
                await DownloadFile(optimizeTask.CompletedTask.Urls.Metallic, MetallicFileName);
            } 
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to download metallic file: {e.Message}");
                SetStatusFailed();
                return;
            }

            SetProgress(0.88f);

            try { 
                await DownloadFile(optimizeTask.CompletedTask.Urls.Roughness, RoughnessFileName);
            } 
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to download roughness file: {e.Message}");
                SetStatusFailed();
                return;
            }

            SetProgress(0.935f);

            try
            {
                await DownloadFile(optimizeTask.CompletedTask.Urls.AmbientOcclusion, AmbientOcclusionFileName);
            } 
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to download ao file: {e.Message}");
                SetStatusFailed();
                return;
            }

            SetStatusCompleted();
        }

        protected override Asset CreateAsset()
        {
            var asset = new MaterialAsset();

            asset.InitTemp();
            asset.Prompt = Prompt;
            asset.NegativePrompt = NegativePrompt;
            asset.Seed = Seed;
            asset.AlbedoFileName = AlbedoFileName;
            asset.NormalsFileName = NormalsFileName;
            asset.DisplacementFileName = DisplacementFileName;
            asset.MetallicFileName = MetallicFileName;
            asset.RoughnessFileName = RoughnessFileName;
            asset.AmbientOcclusionFileName = AmbientOcclusionFileName;

            var albedoPath = GetFilePath(AlbedoFileName);
            if (!asset.AddFile(albedoPath, AlbedoFileName))
            {
                Debug.LogError("Failed to add albedo file to asset.");
                return null;
            }

            var normalsPath = GetFilePath(NormalsFileName);
            if (!asset.AddFile(normalsPath, NormalsFileName))
            {
                Debug.LogError("Failed to add normals file to asset.");
                return null;
            }

            var displacementPath = GetFilePath(DisplacementFileName);
            if (!asset.AddFile(displacementPath, DisplacementFileName))
            {
                Debug.LogError("Failed to add displacement file to asset.");
                return null;
            }

            var metallicPath = GetFilePath(MetallicFileName);
            if (!asset.AddFile(metallicPath, MetallicFileName))
            {
                Debug.LogError("Failed to add metallic file to asset.");
                return null;
            }

            var roughnessPath = GetFilePath(RoughnessFileName);
            if (!asset.AddFile(roughnessPath, RoughnessFileName))
            {
                Debug.LogError("Failed to add roughness file to asset.");
                return null;
            }

            var aoPath = GetFilePath(AmbientOcclusionFileName);
            if (!asset.AddFile(aoPath, AmbientOcclusionFileName))
            {
                Debug.LogError("Failed to add ao file to asset.");
                return null;
            }

            return asset;
        }
    }
}