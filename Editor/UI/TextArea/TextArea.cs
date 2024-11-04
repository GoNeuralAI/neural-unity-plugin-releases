using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEngine.UI;

namespace Neural
{
    public class TextArea : VisualElement
    {
        public const string UxmlPath = "Packages/com.neural.unity/Editor/UI/TextArea/TextArea.uxml";
        public new class UxmlFactory : UxmlFactory<TextArea, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Label = new UxmlStringAttributeDescription { name = "label", defaultValue = "" };
            UxmlStringAttributeDescription m_Value = new UxmlStringAttributeDescription { name = "value", defaultValue = "" };
            UxmlIntAttributeDescription m_MaxLength = new UxmlIntAttributeDescription { name = "max-length", defaultValue = 0 };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var textArea = ve as TextArea;
                textArea.Label = m_Label.GetValueFromBag(bag, cc);
                textArea.Value = m_Value.GetValueFromBag(bag, cc);
                textArea.MaxLength = m_MaxLength.GetValueFromBag(bag, cc);

                textArea.Init();
            }
        }

        public string Label { get; private set; }
        public string Value { get; private set; }
        public int MaxLength { get; private set; }

        public string Text
        {
            get
            {
                return TextFieldElement.text;
            }
        }

        public VisualElement Root { get; protected set; }
        private UnityEngine.UIElements.Label LabelElement => Root.Q<UnityEngine.UIElements.Label>("label");
        private UnityEngine.UIElements.TextField TextFieldElement => Root.Q<UnityEngine.UIElements.TextField>("textField");
        private UnityEngine.UIElements.Label CounterElement => Root.Q<UnityEngine.UIElements.Label>("counter");

        public TextArea()
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
            Root.AddToClassList("textarea");
            LabelElement.text = Label;
            TextFieldElement.value = Value;
            TextFieldElement.maxLength = MaxLength;

            if (Label != "") { 
                LabelElement.style.display = DisplayStyle.Flex;
            }

            if (MaxLength > 0)
            {
                CounterElement.style.display = DisplayStyle.Flex;
                CounterElement.text = $"{TextFieldElement.text.Length} / {MaxLength}";

                TextFieldElement.RegisterCallback<ChangeEvent<string>>(evt =>
                {
                    CounterElement.text = $"{evt.newValue.Length} / {MaxLength}";
                });
            }

            Add(Root);
        }
    }
}