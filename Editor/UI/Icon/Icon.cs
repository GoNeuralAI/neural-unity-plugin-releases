using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using GluonGui;

namespace Neural
{
    public class Icon : VisualElement
    {
        public const string UxmlPath = "Packages/com.neural.unity/Editor/UI/Icon/Icon.uxml";
        public new class UxmlFactory : UxmlFactory<Icon, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Icon = new UxmlStringAttributeDescription { name = "icon", defaultValue = "" };
            //UxmlStringAttributeDescription m_Tooltip = new UxmlStringAttributeDescription { name = "tooltip", defaultValue = "" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var icon = ve as Icon;
                icon.IconSource = m_Icon.GetValueFromBag(bag, cc);
                icon.Init();
            }
        }

        public string IconSource { get; private set; }
        public VisualElement Root { get; protected set; }
        private UnityEngine.UIElements.Image IconImageElement => Root.Q<UnityEngine.UIElements.Image>("icon");

        public Icon()
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
            Root.RegisterCallback<MouseDownEvent>(OnMouseDown);
            Root.RegisterCallback<MouseUpEvent>(OnMouseUp);
            Root.AddToClassList("icon");
            IconImageElement.image = Resources.Load<Texture2D>($"Icons/{IconSource}");

            Add(Root);
        }

        public void ChangeIcon(string icon)
        {
            IconImageElement.image = Resources.Load<Texture2D>($"Icons/{icon}");
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            Root.AddToClassList("active");
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            Root.RemoveFromClassList("active");
        }
    }
}