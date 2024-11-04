using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;
using System.IO;

namespace Neural
{
    public class FileSelector : VisualElement
    {
        public const string UxmlPath = "Packages/com.neural.unity/Editor/UI/FileSelector/FileSelector.uxml";
        public new class UxmlFactory : UxmlFactory<FileSelector, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Label = new UxmlStringAttributeDescription { name = "label", defaultValue = "Select File" };
            UxmlStringAttributeDescription m_FileType = new UxmlStringAttributeDescription { name = "file-type", defaultValue = "All Files" };
            UxmlStringAttributeDescription m_FileExtension = new UxmlStringAttributeDescription { name = "file-extension", defaultValue = "*" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var fileSelector = ve as FileSelector;
                fileSelector.Label = m_Label.GetValueFromBag(bag, cc);
                fileSelector.FileType = m_FileType.GetValueFromBag(bag, cc);
                fileSelector.FileExtension = m_FileExtension.GetValueFromBag(bag, cc);
                fileSelector.Init();
            }
        }

        public string Label { get; set; }
        public string FileType { get; set; }
        public string FileExtension { get; set; }
        public int MaxWidth { get; set; }
        public VisualElement Root { get; protected set; }
        private Label LabelElement => Root.Q<Label>("label");
        private TextField TextField => Root.Q<TextField>("textField");
        private VisualElement SelectFileButton => Root.Q<VisualElement>("selectFileButton");
        private VisualElement PreviewContainer => Root.Q<VisualElement>("previewContainer");
        private Image Preview => Root.Q<Image>("preview");

        public string FilePath
        {
            get => TextField?.value;
            set
            {
                if (TextField != null)
                    TextField.value = value;
            }
        }

        public FileSelector()
        {
            // Empty constructor, initialization will be done in Init()
        }

        public void Init()
        {
            // Check if already initialized
            if (childCount > 0) return;

            VisualTreeAsset visualTree = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            if (visualTree == null)
            {
                Debug.LogError($"Failed to load UXML at path: {UxmlPath}");
                return;
            }

            Root = visualTree.Instantiate();
            LabelElement.text = Label;
            TextField.RegisterCallback<ClickEvent>(evt => OpenFileDialog());
            SelectFileButton.RegisterCallback<ClickEvent>(evt => OpenFileDialog());
            PreviewContainer.style.display = DisplayStyle.None;

            if (Label != "")
            {
                LabelElement.style.display = DisplayStyle.Flex;
            }

            Add(Root);
        }

        private void OpenFileDialog()
        {
            string path = EditorUtility.OpenFilePanel(Label, "", FileExtension);
            if (!string.IsNullOrEmpty(path))
            {
                TextField.value = path;
                TextField.tooltip = path;

                LoadAndDisplayImage(path);
            }
        }

        public void LoadAndDisplayImage(string path)
        {
            byte[] imageData = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2);

            if (texture.LoadImage(imageData))
            {
                float aspectRatio = (float)texture.width / texture.height;

                int newWidth, newHeight;
                if (aspectRatio > 1)
                {
                    newWidth = 300;
                    newHeight = Mathf.RoundToInt(300 / aspectRatio);
                }
                else
                {
                    newHeight = 300;
                    newWidth = Mathf.RoundToInt(300 * aspectRatio);
                }

                RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight, 0, RenderTextureFormat.ARGB32);
                RenderTexture.active = rt;

                Graphics.Blit(texture, rt);

                Texture2D scaledTexture = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
                scaledTexture.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
                scaledTexture.Apply();

                // Clean up
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(rt);
                Object.DestroyImmediate(texture);

                PreviewContainer.style.display = DisplayStyle.Flex;
                Preview.image = scaledTexture;
            }
            else
            {
                PreviewContainer.style.display = DisplayStyle.None;
                Debug.LogError("Failed to load image: " + path);
            }
        }
    }
}