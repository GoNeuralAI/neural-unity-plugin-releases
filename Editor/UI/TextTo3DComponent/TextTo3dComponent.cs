using UnityEngine;
using UnityEngine.UIElements;

namespace Neural
{
    public class TextTo3dComponent : Component
    {
        public const string UxmlPath = "Packages/com.neural.unity/Editor/UI/TextTo3DComponent/TextTo3dComponent.uxml";

        protected TextArea PromptField => Root.Q<TextArea>("prompt");
        protected Toggle HasNegativePrompt => Root.Q<Toggle>("hasNegativePrompt");
        protected TextArea NegativePromptField => Root.Q<TextArea>("negativePrompt");
        protected Toggle HasSeed => Root.Q<Toggle>("hasSeed");
        protected TextField SeedField => Root.Q<TextField>("seed");
        protected Button GenerateBtn => Root.Q<Button>("generateBtn");

        protected override int CreditCostAmount { get { return 1; } }

        public TextTo3dComponent(VisualElement configContent, VisualElement assetGrid, ViewportWidget viewport) : base(UxmlPath, configContent, assetGrid, viewport)
        {
        }

        public override void Cleanup()
        {
            base.Cleanup();
            Context.Billing.OnCreditsUpdated -= CheckGenerateBtnState;
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
            Context.Billing.OnCreditsUpdated += CheckGenerateBtnState;
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
            bool isEnabled = false;
            string tooltipText = "";

            if (string.IsNullOrEmpty(PromptField.Text))
            {
                isEnabled = false;
                tooltipText = "Please enter a prompt.";
            }
            else if (Context.Billing.Model.Credits < CreditCostAmount)
            {
                isEnabled = false;
                tooltipText = "Insufficient credits.";
            }
            else
            {
                isEnabled = true;
                tooltipText = "";
            }

            GenerateBtn.SetEnabled(isEnabled);
            GenerateBtn.SetTooltip(tooltipText);
        }
    }
}