using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using GluonGui;

namespace Neural
{
    public class CreditCost : VisualElement
    {
        public const string UxmlPath = "Packages/com.neural.unity/Editor/UI/CreditCost/CreditCost.uxml";
        public new class UxmlFactory : UxmlFactory<CreditCost, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Value = new UxmlStringAttributeDescription { name = "value", defaultValue = "" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var creditCost = ve as CreditCost;
                creditCost.Value = m_Value.GetValueFromBag(bag, cc);
                creditCost.Init();
            }
        }

        public string Value { get; private set; }
        public VisualElement Root { get; protected set; }
        private Label ValueElement => Root.Q<Label>("value");

        public CreditCost()
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

            SetValue(Value);
            Add(Root);
        }

        public void SetValue(string value)
        {
            Value = value;
            var stringCredits = Value == "1" ? "credit" : "credits";
            ValueElement.text = $"{Value} {stringCredits}";
        }
    }
}