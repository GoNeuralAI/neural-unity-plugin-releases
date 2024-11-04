using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

namespace Neural
{
    public class AssetElement : VisualElement
    {
        public const string UxmlPath = "Packages/com.neural.unity/Editor/UI/AssetElement/AssetElement.uxml";
        public new class UxmlFactory : UxmlFactory<AssetElement, UxmlTraits> { }

        private ViewportWidget Viewport;
        public VisualElement Root { get; private set; }
        private ProgressBar ProgressBar => Root.Q<ProgressBar>("progressBar");
        private VisualElement Actions => Root.Q<VisualElement>("actions");
        private Icon ImportButton => Root.Q<Icon>("btnImport");
        private Icon FavoriteButton => Root.Q<Icon>("btnFavorite");
        private Icon FolderButton => Root.Q<Icon>("btnFolder");
        private Icon RemoveButton => Root.Q<Icon>("btnRemove");
        private VisualElement Thumbnail => Root.Q<VisualElement>("thumbnail");
        private VisualElement SpinnerContainer => Root.Q<VisualElement>("spinnerContainer");
        private Image Spinner => Root.Q<Image>("spinner");
        private VisualElement ErrorContainer => Root.Q<VisualElement>("errorContainer");
        private ScrollView ScrollView;

        private VisualElement LabelContainer => Root.Q<VisualElement>("labelContainer");
        private Label LabelText => Root.Q<Label>("labelText");

        private Color BackgroundColor = new Color(30f / 255f, 30f / 255f, 30f / 255f);
        private Color HoverColor = new Color(50f / 255f, 52f / 255f, 51f / 255f);

        private IVisualElementScheduledItem _rotationScheduler;
        private float _rotationAngle = 0f;
        private const float RotationSpeed = 100f;

        public delegate void AssetElementClickDelegate ();
        public event AssetElementClickDelegate OnAssetElementClicked;

        public delegate void AssetElementImportDelegate ();
        public event AssetElementImportDelegate OnAssetElementImportClicked;

        public delegate void AssetElementFavoriteDelegate ();
        public event AssetElementFavoriteDelegate OnAssetElementFavoriteClicked;

        public delegate void AssetElementFolderDelegate ();
        public event AssetElementFolderDelegate OnAssetElementFolderClicked;

        public delegate void AssetElementRemoveDelegate ();
        public event AssetElementRemoveDelegate OnAssetElementRemoveClicked;

        private readonly float MaxThumbnailWidth = 180f;

        public AssetElement()
        {
            VisualTreeAsset visualTree = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            if (visualTree == null)
            {
                Debug.LogError($"Failed to load UXML at path: {UxmlPath}");
                return;
            }

            Root = visualTree.Instantiate();
            Root.AddToClassList("asset-element");
            Add(Root);

            Root.RegisterCallback<MouseOverEvent>(OnMouseOver);
            Root.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            Root.RegisterCallback<MouseDownEvent>(OnMouseDown);
            Root.RegisterCallback<MouseUpEvent>(OnMouseUp);
            ImportButton.RegisterCallback<ClickEvent>(OnImportClicked);
            FavoriteButton.RegisterCallback<ClickEvent>(OnFavoriteClicked);
            FolderButton.RegisterCallback<ClickEvent>(OnFolderClicked);
            RemoveButton.RegisterCallback<ClickEvent>(OnRemoveClicked);

            Root.CaptureMouse();

            _rotationScheduler = schedule.Execute(RotateSpinner).Every(16);
        }

        public void Resize(float width)
        {
            Root.style.width = new StyleLength(width);
            ProgressBar.style.width = new StyleLength(width - 24);
            Actions.style.width = new StyleLength(width - 8);

            if (Viewport != null)
            {
                Viewport.PreviewMeshScale = Mathf.Clamp01(width / MaxThumbnailWidth) * 1.8f;
            }
        }

        public void SetProgress(float value)
        {
            ProgressBar.value = value;
            ProgressBar.style.display = DisplayStyle.Flex;
        }

        public void SetIcons(bool import, bool favorite, bool folder, bool remove)
        {
            ImportButton.style.display = import ? DisplayStyle.Flex : DisplayStyle.None;
            FavoriteButton.style.display = favorite ? DisplayStyle.Flex : DisplayStyle.None;
            FolderButton.style.display = folder ? DisplayStyle.Flex : DisplayStyle.None;
            RemoveButton.style.display = remove ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public async void SetComplete(Asset asset)
        {
            ProgressBar.style.display = DisplayStyle.None;

            Viewport = new ViewportWidget();
            Viewport.Setup();
            Viewport.SetThumbnail();
            Viewport.RegisterCallback<MouseUpEvent>(evt => OnElementClicked());
            Viewport.BackgroundColor = BackgroundColor;
            Thumbnail.Add(Viewport);

            Mesh mesh = await asset.LoadMesh();
            if (mesh == null)
            {
                Debug.LogError("Failed to load mesh from asset");
                SetFailed();
                return;
            }

            Material material = asset.LoadMaterial();
            if (material == null)
            {
                Debug.LogError("Failed to load material from asset");
                SetFailed();
                return;
            }

            SpinnerContainer.style.display = DisplayStyle.None;
            _rotationScheduler.Pause();
            Thumbnail.style.display = DisplayStyle.Flex;
            Actions.style.display = DisplayStyle.Flex;

            Viewport.SetPreviewMesh(mesh, material);
        }

        public void ShowLabel(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                LabelContainer.style.display = DisplayStyle.None;
                return;
            }

            LabelText.text = text;
            LabelContainer.style.display = DisplayStyle.Flex;
        }

        public void SetFailed()
        {
            ErrorContainer.style.display = DisplayStyle.Flex;
            ProgressBar.style.display = DisplayStyle.None;
            SpinnerContainer.style.display = DisplayStyle.None;
            Actions.style.display = DisplayStyle.None;
            _rotationScheduler.Pause();
        }

        public void SetFavoriteStatus(bool isFavorite)
        {
            FavoriteButton.ChangeIcon(isFavorite ? "star-full" : "star-outline");
        }

        public void SetSelected(bool selected)
        {
            if (selected)
            {
                Root.AddToClassList("selected");
                Viewport.BackgroundColor = HoverColor;
            } else
            {
                Root.RemoveFromClassList("selected");
                Viewport.BackgroundColor = BackgroundColor;
            }
        }

        private void OnMouseOver(MouseOverEvent evt)
        {
            if (Viewport != null)
            {
                Viewport.BackgroundColor = HoverColor;
            }
        }

        private void OnMouseLeave(MouseLeaveEvent evt)
        {
            if (Viewport != null && !Root.ClassListContains("selected"))
            {
                Viewport.BackgroundColor = BackgroundColor;
            }
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            //if (Viewport != null && evt.target == Viewport.Root.Q<Image>("previewImage"))
            //{
            //    Viewport.BackgroundColor = BackgroundColor;
            //}
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            //if (Viewport != null && evt.target == Viewport.Root.Q<Image>("previewImage"))
            //{
            //    Viewport.BackgroundColor = HoverColor;
            //}
        }

        private void RotateSpinner(TimerState timerState)
        {
            if (Spinner != null)
            {
                _rotationAngle += RotationSpeed * (timerState.deltaTime / 1000f);
                _rotationAngle %= 360f;
                Spinner.style.rotate = new StyleRotate(new Rotate(new Angle(_rotationAngle, AngleUnit.Degree)));
            }
        }

        private void OnElementClicked()
        {
            OnAssetElementClicked?.Invoke();
        }

        private void OnImportClicked(ClickEvent evt)
        {
            OnAssetElementImportClicked?.Invoke();
        }

        private void OnFavoriteClicked(ClickEvent evt)
        {
            OnAssetElementFavoriteClicked?.Invoke();
        }

        private void OnFolderClicked(ClickEvent evt)
        {
            OnAssetElementFolderClicked?.Invoke();
        }

        private void OnRemoveClicked(ClickEvent evt)
        {
            OnAssetElementRemoveClicked?.Invoke();
        }
    }
}