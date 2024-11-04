using GLTFast.Schema;
using GluonGui;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Neural
{
    public class SettingsWindow : SettingsProvider
    {
        public const string UxmlPath = "Packages/com.neural.unity/Editor/UI/Settings/SettingsWindow.uxml";

        public VisualElement Root { get; protected set; }
        private TextField ApiKeyField => Root.Q<TextField>("apiKey");
        //private TextField BaseUrlFIeld => Root.Q<TextField>("baseUrl");

        public SettingsWindow(string path, SettingsScope scope = SettingsScope.Project)
            : base(path, scope) { }

        public override void OnActivate(string searchContext, VisualElement rootVisualElement)
        {
            VisualTreeAsset visualTree = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            if (visualTree == null)
            {
                Debug.LogError($"Failed to load UXML at path: {UxmlPath}");
                return;
            }

            Root = visualTree.Instantiate();
            rootVisualElement.Add(Root);

            ApiKeyField.isPasswordField = true;
            ApiKeyField.value = Context.ApiKey;
            ApiKeyField.maxLength = 128;
            ApiKeyField.RegisterCallback<ChangeEvent<string>>(evt =>
            {
                Context.ApiKey = evt.newValue;
            });

            //BaseUrlFIeld.value = Context.BaseUrl;
            //BaseUrlFIeld.RegisterCallback<ChangeEvent<string>>(evt =>
            //{
            //    Context.BaseUrl = evt.newValue;
            //});
        }

        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            var provider = new SettingsWindow("Project/Neural", SettingsScope.Project);
            provider.keywords = new HashSet<string>(new[] { "API", "Key", "Neural" });
            return provider;
        }
    }
}