using GLTFast.Schema;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Rendering.FilterWindow;

namespace Neural
{
    public class TexturingWindow : EditorWindow
    {
        [SerializeField]
        private VisualTreeAsset VisualTreeAsset = default;

        public VisualElement Root { get; protected set; }

        private TextArea PromptField => Root.Q<TextArea>("prompt");
        private Toggle HasNegativePrompt => Root.Q<Toggle>("hasNegativePrompt");
        private TextArea NegativePromptField => Root.Q<TextArea>("negativePrompt");
        private Toggle HasSeed => Root.Q<Toggle>("hasSeed");
        private TextField SeedField => Root.Q<TextField>("seed");
        private Button GenerateBtn => Root.Q<Button>("generateBtn");
        private Label ErrorMsg => Root.Q<Label>("errorMsg");

        protected CreditCost CreditCostElement => Root.Q<CreditCost>("creditsCost");
        protected virtual int CreditCostAmount { get { return 1; } }

        protected int lastNumSelectedGameObjects = 0;

        protected bool isGenerating = false;
        protected int isGeneratingEllipsis = 0;

        private IVisualElementScheduledItem ScheduledItem;

        public static void OpenWindow()
        {
            TexturingWindow wnd = GetWindow<TexturingWindow>();
            wnd.titleContent = new GUIContent("Neural Texturing");
        }

        public void CreateGUI()
        {
            VisualElement visualTree = rootVisualElement;

            Root = VisualTreeAsset.Instantiate();
            visualTree.Add(Root);

            
            InitializeUI();
            ScheduledItem = rootVisualElement.schedule.Execute(Scheduler).Every(250);
        }

        public void Scheduler()
        {
            if (isGenerating)
            {
                if (++isGeneratingEllipsis > 3)
                {
                    isGeneratingEllipsis = 0;
                    GenerateBtn.SetText("Generating");
                }
                else
                {
                    GenerateBtn.SetText("Generating" + new string('.', isGeneratingEllipsis));
                }
            }
        }

        public void OnGUI()
        {
            if (Selection.gameObjects != null && Selection.gameObjects.Length != lastNumSelectedGameObjects)
            {
                lastNumSelectedGameObjects = Selection.gameObjects.Length;
                CheckGenerateBtnState();
            }
        }

        public void InitializeUI()
        {
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

            CheckGenerateBtnState();
            Context.Billing.OnCreditsUpdated += CheckGenerateBtnState;
            CreditCostElement?.SetValue(CreditCostAmount.ToString());
            _ = Context.Billing.UpdateBilling();
        }

        protected void OnGenerateBtnClicked(ClickEvent evt)
        {
            isGenerating = true;
            ErrorMsg.style.display = DisplayStyle.None;
            CheckGenerateBtnState();

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

            var job = new TexturingJob(PromptField.Text, negativePrompt, seed);
            job.Execute();

            job.OnJobStatusChanged += (status) =>
            {
                if (status == JobStatus.Completed)
                {
                    Context.AssetDatabase.SaveAsset(job.Asset);
                }
                else if (status == JobStatus.Failed)
                {
                    ErrorMsg.style.display = DisplayStyle.Flex;
                }

                GenerateBtn.SetEnabled(true);
                GenerateBtn.SetText("Generate");
                isGenerating = false;
            };
        }

        protected void CheckGenerateBtnState()
        {
            bool isEnabled = false;
            string tooltipText = "";

            if (isGenerating)
            {
                isEnabled = false;
                tooltipText = "Please wait for the current generation to complete.";
            }
            else if (string.IsNullOrEmpty(PromptField.Text))
            {
                isEnabled = false;
                tooltipText = "Please enter a prompt.";
            } else if (lastNumSelectedGameObjects == 0) {
                isEnabled = false;
                tooltipText = "Please select at least one object.";
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