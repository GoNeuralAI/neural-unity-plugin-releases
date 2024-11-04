using UnityEngine;
using UnityEngine.UIElements;

namespace Neural
{
    public class TextTo3dComponent : Component
    {
        public const string UxmlPath = "Packages/com.neural.unity/Editor/UI/TextTo3DComponent/TextTo3dComponent.uxml";

        private TextArea PromptField => Root.Q<TextArea>("prompt");
        private Toggle HasNegativePrompt => Root.Q<Toggle>("hasNegativePrompt");
        private TextArea NegativePromptField => Root.Q<TextArea>("negativePrompt");
        private Toggle HasSeed => Root.Q<Toggle>("hasSeed");
        private TextField SeedField => Root.Q<TextField>("seed");
        private Button GenerateBtn => Root.Q<Button>("generateBtn");

        public TextTo3dComponent(VisualElement configContent, VisualElement assetGrid, ViewportWidget viewport) : base(UxmlPath, configContent, assetGrid, viewport)
        {
        }

        protected override void InitializeUI()
        {
            base.InitializeUI();

            PromptField.RegisterCallback<ChangeEvent<string>>(evt =>
            {
                CheckGenerateBtnState();
            });

            HasNegativePrompt.RegisterCallback<ChangeEvent<bool>>(evt =>
            {
                NegativePromptField.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            });

            HasSeed.RegisterCallback<ChangeEvent<bool>>(evt =>
            {
                SeedField.isReadOnly = !evt.newValue;
            });

            GenerateBtn.RegisterCallback<ClickEvent>(OnGenerateBtnClicked);

            var jobs = Context.JobController.GetJobsByType(JobType.TextTo3D);

            foreach (var job in jobs)
            {
                ConfigureJob(job);
            }

            ClearPreview();
            CheckGenerateBtnState();
        }

        protected void OnGenerateBtnClicked(ClickEvent evt)
        {
            string negativePrompt = HasNegativePrompt.value && !string.IsNullOrEmpty(NegativePromptField.Text) ? NegativePromptField.Text : null;
            int seed = 0;
            if (HasSeed.value && !string.IsNullOrEmpty(SeedField.text))
            {
                if (!int.TryParse(SeedField.text, out seed))
                {
                    Debug.LogWarning("Invalid seed value. Using default (0).");
                }
            }
            else
            {
                seed = Mathf.FloorToInt(Random.value * int.MaxValue);
                SeedField.value = seed.ToString();
            }

            var job = new TextTo3dJob(PromptField.Text, negativePrompt, seed);
            job.Execute();
            ConfigureJob(job);
            Context.JobController.AddJob(job);
        }

        protected void CheckGenerateBtnState()
        {
            GenerateBtn.SetEnabled(!string.IsNullOrEmpty(PromptField.Text));
        }
    }
}