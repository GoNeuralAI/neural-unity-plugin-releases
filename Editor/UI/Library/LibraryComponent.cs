using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

namespace Neural
{
    public class LibraryComponent : Component
    {
        public const string UxmlPath = "Packages/com.neural.unity/Editor/UI/Library/LibraryComponent.uxml";
        private TextField PromptField => Root.Q<TextField>("promptTextField");
        private ScrollView AssetScrollView;
        private List<Asset> allAssets;
        private int currentIndex = 0;

        private TextField Search => Root.Q<TextField>("search");
        private Toggle ShowMesh => Root.Q<Toggle>("showMesh");
        private Toggle ShowMaterial => Root.Q<Toggle>("showMaterial");
        private Toggle ShowFavorites => Root.Q<Toggle>("showFavorites");

        public LibraryComponent(VisualElement configContent, VisualElement assetGrid, ViewportWidget viewport)
            : base(UxmlPath, configContent, assetGrid, viewport)
        {
        }

        protected override void InitializeUI()
        {
            base.InitializeUI();

            AssetScrollView = AssetGrid.parent as ScrollView;

            allAssets = Context.AssetDatabase.GetAllAssets().ToList();
            ShowMesh.value = true;
            ShowMaterial.value = true;
            ShowMesh.RegisterCallback<ChangeEvent<bool>>(evt => GetFilteredAssets());
            ShowMaterial.RegisterCallback<ChangeEvent<bool>>(evt => GetFilteredAssets());
            ShowFavorites.RegisterCallback<ChangeEvent<bool>>(evt => GetFilteredAssets());
            Search.RegisterCallback<ChangeEvent<string>>(evt => GetFilteredAssets());

            GetFilteredAssets();
        }

        private void GetFilteredAssets()
        {
            List<Asset> filteredAssets = new List<Asset>();

            if (ShowMesh.value)
            {
                filteredAssets.AddRange(Context.AssetDatabase.GetAssetsOfType(AssetType.Mesh));
            }

            if (ShowMaterial.value)
            {
                filteredAssets.AddRange(Context.AssetDatabase.GetAssetsOfType(AssetType.Material));
            }

            if (ShowFavorites.value)
            {
                filteredAssets = filteredAssets.Where(asset => asset.IsFavorite == ShowFavorites.value).ToList();
            }

            if (!string.IsNullOrEmpty(Search.text))
            {
                filteredAssets = filteredAssets.Where(asset =>
                {
                    if (asset is MeshAsset meshAsset)
                    {
                        return meshAsset.Prompt.ToLower().Contains(Search.text.ToLower());
                    } else if (asset is MaterialAsset materialAsset)
                    {
                        return materialAsset.Prompt.ToLower().Contains(Search.text.ToLower());
                    }

                    return false;
                }).ToList();
            }

            filteredAssets.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));

            allAssets = filteredAssets;
            AssetGrid.Clear();
            currentIndex = 0;
            Root.schedule.Execute(Update);
        }

        private void Update()
        {
            float scrollViewHeight = AssetScrollView.worldBound.height;
            float contentHeight = AssetGrid.worldBound.height;
            float scrollPosition = AssetScrollView.verticalScroller.value;

            if (contentHeight - (scrollPosition + scrollViewHeight) < 200 && currentIndex < allAssets.Count)
            {
                CreateElement(allAssets[currentIndex]);
                currentIndex++;
            }

            Root.schedule.Execute(Update);
        }

        private void CreateElement(Asset asset)
        {
            var element = new AssetElement();
            AssetGrid.Add(element);
            element.SetIcons(true, true, true, true);
            element.SetComplete(asset);
            element.Resize(GridElementWidth());
            element.SetFavoriteStatus(asset.IsFavorite);
            element.ShowLabel(AssetLabel(asset));
            element.OnAssetElementClicked += () => OnAssetElementClicked(asset, element);
            element.OnAssetElementImportClicked += () => asset.ImportInScene() ;
            element.OnAssetElementFavoriteClicked += () => {
                asset.IsFavorite = !asset.IsFavorite;
                element.SetFavoriteStatus(asset.IsFavorite);
                Context.AssetDatabase.SaveAsset(asset);
            };
            element.OnAssetElementRemoveClicked += () => {
                Context.AssetDatabase.DeleteAsset(asset.Id);
                AssetGrid.Remove(element);

                Job job = Context.JobController.GetJobByAsset(asset.Id);

                if (job != null)
                {
                    Context.JobController.DeleteJob(job.Id);
                }
            };
            element.OnAssetElementFolderClicked += () => {
                string path = asset.AssetPath;
                System.Diagnostics.Process.Start("explorer.exe", path);
            };
        }

        private string AssetLabel(Asset asset)
        {
            if (asset is MeshAsset)
            {
                return "Mesh";
            }
            else if (asset is MaterialAsset)
            {
                return "Material";
            }

            return "";
        }
    }
}