using System.Collections.Generic;
using System.Timers;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Neural
{
    public class MainWindow : EditorWindow
    {
        [SerializeField]
        private VisualTreeAsset VisualTreeAsset = default;

        private ComponentManager ComponentManager;
        private Component CurrentComponent;
        private ViewportWidget Viewport;

        private VisualElement AssetContent => rootVisualElement.Q<VisualElement>("assetContent");
        private VisualElement PreviewContent => rootVisualElement.Q<VisualElement>("previewContent");
        private VisualElement MainPanel => rootVisualElement.Q<VisualElement>("mainPanel");
        private VisualElement SecondaryPanel => rootVisualElement.Q<VisualElement>("secondaryPanel");
        private VisualElement AssetGrid => rootVisualElement.Q<VisualElement>("assetGrid");
        private VisualElement Navigation => rootVisualElement.Q<VisualElement>("navigation");
        private VisualElement ComponentConfig => rootVisualElement.Q<VisualElement>("componentConfig");
        private VisualElement ConfigContent => rootVisualElement.Q<VisualElement>("configContent");
        private Icon BtnBack => rootVisualElement.Q<Icon>("btnBack");
        private VisualElement BtnDocumentation => rootVisualElement.Q<VisualElement>("documentation");
        private VisualElement BtnSettings => rootVisualElement.Q<VisualElement>("settings");
        private Label ComponentTitle => rootVisualElement.Q<Label>("componentTitle");
        private Label ManageSubscriptionLink => rootVisualElement.Q<Label>("manageSubscriptionLink");
        private Label AvailableCredits => rootVisualElement.Q<Label>("availableCredits");

        Timer Timer;

        private Dictionary<string, string> Components = new Dictionary<string, string>
        {
            { "route-library", "My Library" },
            { "route-textTo3d", "Text to 3D" },
            { "route-imageTo3d", "Image to 3D" },
            { "route-textToMaterial", "Text to Material" }
        };

        private const float MinWidthForHorizontalLayout = 1000f;

        [MenuItem("Window/Neural")]
        public static void OpenWindow()
        {
            MainWindow wnd = GetWindow<MainWindow>();
            wnd.titleContent = new GUIContent("Neural");
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            VisualElement labelFromUXML = VisualTreeAsset.Instantiate();
            root.Add(labelFromUXML);

            BtnBack.RegisterCallback<ClickEvent>(OnBackButtonClicked);
            BtnSettings.RegisterCallback<ClickEvent>(OnSettingsButtonClicked);
            BtnDocumentation.RegisterCallback<ClickEvent>(OnDocumentationButtonClicked);
            ManageSubscriptionLink.RegisterCallback<ClickEvent>(OnManageSubscriptionLinkClicked);

            SetupViewport();
            ComponentManager = new ComponentManager(ConfigContent, AssetGrid, Viewport);
            SetupNavButtons();
            rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            UpdateCredits();
            Context.Billing.OnCreditsUpdated += UpdateCredits;
        }

        private void SetupViewport()
        {
            Viewport = new ViewportWidget();
            Viewport.Setup();
            Viewport.ShowLogo();
            PreviewContent.Add(Viewport);
        }

        private void SetupNavButtons()
        {
            var navButtons = Navigation.Query<Button>(null).ToList();

            foreach (var button in navButtons)
            {
                if (Components.ContainsKey(button.name))
                {
                    button.RegisterCallback<ClickEvent>(evt => LoadComponent(button.name));
                }
            }

            var texturingButton = Navigation.Q<Button>("texturing");

            if (texturingButton != null)
            {
                texturingButton.RegisterCallback<ClickEvent>(evt => TexturingWindow.OpenWindow());
            }
        }

        private void LoadComponent(string name)
        {
            if (!Components.ContainsKey(name))
            {
                Debug.LogWarning($"Component not found for {name}");
                return;
            }

            Navigation.style.display = DisplayStyle.None;
            ComponentConfig.style.display = DisplayStyle.Flex;
            AssetContent.style.display = DisplayStyle.Flex;
            ComponentTitle.text = Components[name];

            UnloadComponent();
            CurrentComponent = ComponentManager.CreateComponent(name);
            UpdateLayout();
        }

        private void UnloadComponent()
        {
            if (CurrentComponent != null) {
                ComponentManager.DestroyComponent(CurrentComponent);
                CurrentComponent = null;
            }
        }

        private void OnBackButtonClicked(ClickEvent evt)
        {
            Navigation.style.display = DisplayStyle.Flex;
            ComponentConfig.style.display = DisplayStyle.None;
            AssetContent.style.display = DisplayStyle.None;
            UnloadComponent();
            UpdateLayout();
            Viewport.ShowLogo();
        }

        private void OnSettingsButtonClicked(ClickEvent evt)
        {
            SettingsService.OpenProjectSettings("Project/Neural");
        }

        private void OnDocumentationButtonClicked(ClickEvent evt)
        {
            Application.OpenURL("https://docs.goneural.ai");
        }

        private void OnManageSubscriptionLinkClicked(ClickEvent evt)
        {
            Application.OpenURL("https://app.goneural.ai/account");
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            UpdateLayout();
        }

        private void UpdateLayout()
        {
            var headerHeight = 40f;
            var mainPanelWidth = MainPanel.worldBound.width;
            var windowWidth = rootVisualElement.worldBound.width - mainPanelWidth;
            var windowHeight = rootVisualElement.worldBound.height - headerHeight;

            if (windowWidth < MinWidthForHorizontalLayout)
            {
                SecondaryPanel.style.flexDirection = FlexDirection.Column;
                AssetContent.style.width = new StyleLength(windowWidth); ;
                AssetContent.style.height = new StyleLength(windowHeight * 0.5f);
                PreviewContent.style.width = new StyleLength(windowWidth);
                PreviewContent.style.height = new StyleLength(CurrentComponent == null ? windowHeight : windowHeight * 0.5f);
            }
            else
            {
                float assetContentWidth = Mathf.Max(520f, windowWidth * 0.4f);

                SecondaryPanel.style.flexDirection = FlexDirection.Row;
                AssetContent.style.width = new StyleLength(assetContentWidth);
                AssetContent.style.height = new StyleLength(windowHeight);
                PreviewContent.style.width = new StyleLength(CurrentComponent == null ? windowWidth : windowWidth - assetContentWidth);
                PreviewContent.style.height = new StyleLength(windowHeight);
            }
        }

        private void UpdateCredits()
        {
            var availableCredits = Context.Billing.Model.Credits;

            var str = availableCredits == 1 ? "credit" : "credits";

            AvailableCredits.text = $"{availableCredits} {str}";
        }

        private void OnDestroy()
        {
            if (ComponentManager != null)
            {
                ComponentManager.DestroyAllComponents();
            }
            
            Context.Billing.OnCreditsUpdated -= UpdateCredits;
        }

        private void OnDisable()
        {
            if (ComponentManager != null)
            {
                ComponentManager.DestroyAllComponents();
            }

            Context.Billing.OnCreditsUpdated -= UpdateCredits;
        }
    }
}