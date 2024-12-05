using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEngine.UI;

namespace Neural
{
    public class Button : VisualElement
    {
        public const string UxmlPath = "Packages/com.neural.unity/Editor/UI/Button/Button.uxml";
        public new class UxmlFactory : UxmlFactory<Button, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Text = new UxmlStringAttributeDescription { name = "text", defaultValue = "Button" };
            UxmlStringAttributeDescription m_Tooltip = new UxmlStringAttributeDescription { name = "tooltip", defaultValue = "" };
            UxmlStringAttributeDescription m_Icon = new UxmlStringAttributeDescription { name = "icon", defaultValue = "" };
            UxmlBoolAttributeDescription m_Disabled = new UxmlBoolAttributeDescription { name = "disabled", defaultValue = false };
            UxmlBoolAttributeDescription m_Secondary = new UxmlBoolAttributeDescription { name = "secondary", defaultValue = false };
            UxmlBoolAttributeDescription m_Accent = new UxmlBoolAttributeDescription { name = "accent", defaultValue = false };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var button = ve as Button;
                button.Text = m_Text.GetValueFromBag(bag, cc);
                button.IconSource = m_Icon.GetValueFromBag(bag, cc);
                button.Disabled = m_Disabled.GetValueFromBag(bag, cc);
                button.Secondary = m_Secondary.GetValueFromBag(bag, cc);
                button.Accent = m_Accent.GetValueFromBag(bag, cc);

                button.Init();
            }
        }

        public string Text { get; private set; }
        public string Tooltip { get; private set; }
        public string IconSource { get; private set; }
        public bool Disabled { get; private set; }
        public bool Secondary { get; private set; }
        public bool Accent { get; private set; }
        public VisualElement Root { get; protected set; }
        private UnityEngine.UIElements.Image IconImageElement => Root.Q<UnityEngine.UIElements.Image>("icon");
        private UnityEngine.UIElements.Label LabelElement => Root.Q<UnityEngine.UIElements.Label>("label");

        public Button()
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
            Root.AddToClassList("button");
            LabelElement.text = Text;
            IconImageElement.image = Resources.Load<Texture2D>($"Icons/{IconSource}");

            if (Secondary)
            {
                Root.AddToClassList("button-secondary");
            }

            if (Accent)
            {
                Root.AddToClassList("button-accent");
            }

            Root.RegisterCallback<PointerDownEvent>(OnPointerDown);
            Root.RegisterCallback<PointerUpEvent>(OnPointerUp);
            Root.tooltip = Tooltip;

            Add(Root);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            Root.AddToClassList("active");
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            Root.RemoveFromClassList("active");
        }

        public void SetTooltip(string tooltip)
        {
            Tooltip = tooltip;
            Root.tooltip = tooltip;
        }

        public void SetText(string text)
        {
            Text = text;
            LabelElement.text = text;
        }
    }
}