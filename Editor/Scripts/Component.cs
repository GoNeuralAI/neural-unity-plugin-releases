using UnityEngine;
using UnityEngine.UIElements;

namespace Neural
{
    public abstract class Component
    {
        protected struct AssetElementData
        {
            public AssetElement Element;
            public Asset Asset;
        }

        public VisualElement Root { get; protected set; }

        protected VisualElement ConfigContent;
        protected VisualElement AssetGrid;
        protected ViewportWidget Viewport;
        protected AssetElementData SelectedAsset;

        protected CreditCost CreditCostElement => Root.Q<CreditCost>("creditsCost");
        protected virtual int CreditCostAmount {  get { return 0; } }

        protected Component(string uxmlPath, VisualElement configContent, VisualElement assetGrid, ViewportWidget viewportWidget)
        {
            VisualTreeAsset visualTree = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            if (visualTree == null)
            {
                Debug.LogError($"Failed to load UXML at path: {uxmlPath}");
                return;
            }

            Root = visualTree.Instantiate();
            ConfigContent = configContent;
            AssetGrid = assetGrid;
            Viewport = viewportWidget;
            _ = Context.Billing.UpdateBilling();
            InitializeUI();
        }

        public virtual void Cleanup()
        {
            ConfigContent.Clear();
            AssetGrid.Clear();
            AssetGrid.UnregisterCallback<GeometryChangedEvent>(OnAssetGridGeometryChanged);
            Viewport.OnButtonImportClicked -= OnButtonImportClicked;

            Root = null;
        }

        protected virtual void InitializeUI()
        {
            ConfigContent.Add(Root);
            AssetGrid.RegisterCallback<GeometryChangedEvent>(OnAssetGridGeometryChanged);
            Viewport.OnButtonImportClicked += OnButtonImportClicked;
            CreditCostElement?.SetValue(CreditCostAmount.ToString());
        }

        protected float GridElementWidth()
        {
            return AssetGrid.worldBound.width / 4f - 1f;
        }

        protected void OnAssetGridGeometryChanged(GeometryChangedEvent evt)
        {
            float width = GridElementWidth();

            foreach (VisualElement child in AssetGrid.Children())
            {
                if (child is AssetElement assetElement)
                {
                    assetElement.Resize(width);
                }
            }
        }

        protected void SetPreviewMesh(Mesh mesh, Material material, bool showStats = true)
        {
            Viewport.SetPreviewMesh(mesh, material);
            Viewport.ShowToolbar(true);
            Viewport.ControlsEnabled = true;
            Viewport.AutoRotate = false;

            if (showStats)
            {
                Viewport.ShowStats(mesh.triangles.Length / 3, mesh.vertexCount);
            }
            else
            {
                Viewport.HideStats();
            }
        }

        protected void ClearPreview()
        {
            Viewport.SetPreviewMesh(null, null);
            Viewport.ShowToolbar(true);
            Viewport.ShowStats(0, 0);
            Viewport.AutoRotate = false;
        }

        protected void SetPreviewError()
        {
            Viewport.SetError();
            Viewport.HideStats();
            Viewport.ShowToolbar(false);
        }

        protected void ConfigureJob(Job job)
        {
            var element = new AssetElement();
            element.Resize(GridElementWidth());
            AssetGrid.Add(element);

            element.SetProgress(job.Progress * 100f);

            if (job.Status == JobStatus.Running)
            {
                job.OnJobProgress += (progress) =>
                {
                    element.SetProgress(progress * 100f);
                };

                job.OnJobStatusChanged += (status) =>
                {
                    if (status == JobStatus.Completed)
                    {
                        Context.AssetDatabase.SaveAsset(job.Asset);

                        PrepareJobToElement(element, job);
                    }
                    else if (status == JobStatus.Failed)
                    {
                        element.SetFailed();
                    }
                };
            } else if (job.Status == JobStatus.Completed)
            {
                PrepareJobToElement(element, job);
            } else if (job.Status == JobStatus.Failed)
            {
                element.SetFailed();
            } else
            {
                Debug.LogWarning($"Unexpected job status with ID {job.Id}; Status {job.Status}");
            }
        }

        protected void PrepareJobToElement(AssetElement element, Job job)
        {
            element.SetIcons(true, false, false, true);
            element.SetComplete(job.Asset);
            element.Resize(GridElementWidth());
            element.SetFavoriteStatus(job.Asset.IsFavorite);
            element.OnAssetElementClicked += () => OnAssetElementClicked(job.Asset, element);
            element.OnAssetElementImportClicked += () => job.Asset.ImportInScene();
            element.OnAssetElementFavoriteClicked += () => {
                job.Asset.IsFavorite = !job.Asset.IsFavorite;
                element.SetFavoriteStatus(job.Asset.IsFavorite);
                Context.AssetDatabase.SaveAsset(job.Asset);
            };
            element.OnAssetElementRemoveClicked += () => {
                Context.AssetDatabase.DeleteAsset(job.Asset.Id);
                Context.JobController.DeleteJob(job.Id);
                AssetGrid.Remove(element);
            };
            element.OnAssetElementFolderClicked += () => {
                string path = job.Asset.AssetPath;
                System.Diagnostics.Process.Start("explorer.exe", path);
            };
        }

        protected async void OnAssetElementClicked(Asset asset, AssetElement element)
        {
            if (SelectedAsset.Element != null)
            {
                SelectedAsset.Element.SetSelected(false);
            }

            SelectedAsset.Element = element;
            SelectedAsset.Element.SetSelected(true);
            SelectedAsset.Asset = null;

            Mesh mesh = await asset.LoadMesh();
            if (mesh == null)
            {
                SetPreviewError();
                return;
            }

            Material material = asset.LoadMaterial();
            if (material == null)
            {
                SetPreviewError();
                return;
            }

            bool showStats = true;
            if (asset is MaterialAsset)
            {
                showStats = false;
            }

            SelectedAsset.Asset = asset;
            SetPreviewMesh(mesh, material, showStats);
        }

        protected void OnButtonImportClicked(ClickEvent evt)
        {
            if (SelectedAsset.Asset == null)
            {
                return;
            }

            SelectedAsset.Asset.ImportInScene();
        }
    }
}