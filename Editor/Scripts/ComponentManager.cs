using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Neural
{
    public class ComponentManager
    {
        private Dictionary<string, Func<Component>> componentFactories;
        private List<Component> activeComponents;

        public ComponentManager(VisualElement configContent, VisualElement assetGrid, ViewportWidget viewport)
        {
            // This only creates the factory functions, not the actual instances
            componentFactories = new Dictionary<string, Func<Component>>
            {
                { "route-library", () => new LibraryComponent(configContent, assetGrid, viewport) },
                { "route-textTo3d", () => new TextTo3dComponent(configContent, assetGrid, viewport) },
                { "route-imageTo3d", () => new ImageTo3dComponent(configContent, assetGrid, viewport) },
                { "route-textToMaterial", () => new TextToMaterialComponent(configContent, assetGrid, viewport) }
            };

            activeComponents = new List<Component>();
        }

        public Component CreateComponent(string componentName)
        {
            if (componentFactories.TryGetValue(componentName, out var factory))
            {
                var component = factory();

                activeComponents.Add(component);

                return component;
            }
            throw new ArgumentException($"Unknown component: {componentName}");
        }

        public void DestroyComponent(Component component)
        {
            var activeComponent = activeComponents.Find(c => c == component);

            if (activeComponent != null) {
                activeComponents.Remove(activeComponent);
                activeComponent.Cleanup();
            }
        }

        public void DestroyAllComponents()
        {
            foreach (var component in new List<Component>(activeComponents))
            {
                DestroyComponent(component);
            }
        }
    }
}