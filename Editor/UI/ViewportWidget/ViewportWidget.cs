using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Rendering;
using UnityEditor;
using GluonGui;

namespace Neural
{
    public class ViewportWidget : VisualElement
    {
        public const string UxmlPath = "Packages/com.neural.unity/Editor/UI/ViewportWidget/ViewportWidget.uxml";
        public new class UxmlFactory : UxmlFactory<ViewportWidget, UxmlTraits> { }

        public bool ControlsEnabled = true;
        public bool AutoRotate = false;
        public float AutoRotateSpeed = 50.0f;
        public Color BackgroundColor = new(24f / 255f, 24f / 255f, 24f / 255f);
        public float PreviewMeshScale = 1.8f;

        private int FrameRateLimit = -1;
        private int Supersampling = 2;
        private bool ShowGrid = true;
        private bool ShowWireframe = false;
        private bool ShowTextures = true;
        private bool ShowLighting = true;
        private float CameraZoomTarget = 0.7f;
        private float CameraYawTarget = 30.0f;
        private float CameraPitchTarget = 30.0f;
        private float CameraMinDistance = 2.0f;
        private float CameraMaxDistance = 10.0f;
        private RenderTexture RenderTexture;
        private Mesh GridMesh;
        private Material GridMaterial;
        private Mesh PreviewMesh;
        private Material PreviewMaterial;
        private Material WireframeMaterial;
        private Material PlainWhiteMaterial;
        private readonly float CameraFov = 45.0f;
        private readonly float CameraNear = 1.0f;
        private readonly float CameraFar = 100.0f;
        private Vector3 CameraLookAtPosition = Vector3.zero;
        private Vector3 CameraLookAtTarget = Vector3.zero;
        private float CameraZoom = 0.7f;
        private float CameraYaw = 30.0f;
        private float CameraPitch = 30.0f;
        private bool IsPanning = false;
        private bool IsRotating = false;
        private float YOffset = 0.0f;

        private double LastFrameTime = 0.0;
        private double TimeSinceLastRenderedFrame = 0.0;

        public VisualElement Root { get; set; }
        private Image PreviewImage => Root.Q<Image>("previewImage");

        private VisualElement ToolbarContainer => Root.Q<VisualElement>("toolbarContainer");
        private VisualElement Toolbar => Root.Q<VisualElement>("toolbar");
        private Icon ToggleGrid => Toolbar.Q<Icon>("toggleGrid");
        private Icon ToggleLighting => Toolbar.Q<Icon>("toggleLighting");
        private Icon ToggleWireframe => Toolbar.Q<Icon>("toggleWireframe");
        private Icon ToggleTexture => Toolbar.Q<Icon>("toggleTexture");
        private Icon ImportButton => Toolbar.Q<Icon>("btnImport");
        private VisualElement Stats => Root.Q<VisualElement>("stats");
        private Label StatTriangles => Stats.Q<Label>("statTriangles");
        private Label StatVertices => Stats.Q<Label>("statVertices");
        private VisualElement ErrorContainer => Root.Q<VisualElement>("errorContainer");

        public int MeshVertexCount
        {
            get
            {
                if (PreviewMesh == null)
                {
                    return 0;
                }

                return PreviewMesh.vertexCount;
            }
        }

        public int MeshTriangleCount
        {
            get
            {
                if (PreviewMesh == null)
                {
                    return 0;
                }

                return PreviewMesh.triangles.Length / 3;
            }
        }

        public delegate void ButtonImportDelegate(ClickEvent evt);
        public event ButtonImportDelegate OnButtonImportClicked;

        ~ViewportWidget()
        {
            if (RenderTexture != null)
            {
                RenderTexture.Release();
                RenderTexture = null;
            }

            if (GridMaterial != null)
            {
                Object.DestroyImmediate(GridMaterial);
                GridMaterial = null;
            }

            if (PreviewMaterial != null)
            {
                Object.DestroyImmediate(PreviewMaterial);
                PreviewMaterial = null;
            }

            if (WireframeMaterial != null)
            {
                Object.DestroyImmediate(WireframeMaterial);
                WireframeMaterial = null;
            }
        }

        public void Setup()
        {
            VisualTreeAsset visualTree = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            if (visualTree == null)
            {
                UnityEngine.Debug.LogError($"Failed to load UXML at path: {UxmlPath}");
                return;
            }

            style.flexGrow = 1;

            Root = visualTree.Instantiate();
            Root.style.width = Length.Percent(100);
            Root.style.height = Length.Percent(100);
            Add(Root);

            WireframeMaterial = Resources.Load<Material>("Materials/WireframeMaterial");
            PlainWhiteMaterial = Resources.Load<Material>("Materials/PlainWhiteMaterial");
            RenderTexture = new RenderTexture(1, 1, 32);
            UpdateRenderTexture();
            SetupImage();
            SetupGrid();
            SetupToolbarActions();

            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            RegisterCallback<WheelEvent>(OnMouseWheel);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            ImportButton.RegisterCallback<ClickEvent>((evt) => OnButtonImportClicked?.Invoke(evt));

            Render();
        }

        public void SetThumbnail()
        {
            PreviewImage.AddToClassList("thumbnail");
            FrameRateLimit = 30;
            AutoRotate = true;
            ControlsEnabled = false;
            ShowGrid = false;
            CameraYaw = 20.0f;
            CameraPitch = 20.0f;
            CameraZoom = 1.0f;
            CameraYawTarget = 20.0f;
            CameraPitchTarget = 20.0f;
            CameraZoomTarget = 1.0f;
            CameraMinDistance = 4.0f;
            CameraMaxDistance = 4.0f;
        }

        public void ShowLogo()
        {
            Mesh mesh = Resources.Load<Mesh>("Models/LogoMesh");
            Material material = Resources.Load<Material>("Materials/LogoMaterial");

            SetPreviewMesh(mesh, material, 2.0f);
            ShowToolbar(false);
            HideStats();
            ControlsEnabled = false;
            AutoRotate = true;
            AutoRotateSpeed = 2f;
            CameraMinDistance = 2.0f;
            CameraMaxDistance = 10.0f;
            CameraYaw = 30.0f;
            CameraPitch = 30.0f;
            CameraZoom = 0.7f;
            CameraYawTarget = 30.0f;
            CameraPitchTarget = 30.0f;
            CameraZoomTarget = 0.7f;
            CameraZoom = 0.7f;
            ShowGrid = true;
            ShowWireframe = false;
            ShowTextures = true;
            ShowLighting = true;
        }

        public void ShowToolbar(bool show)
        {
            ToolbarContainer.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void HideStats()
        {
            Stats.style.display = DisplayStyle.None;
        }

        public void ShowStats(int triangles, int vertices)
        {
            Stats.style.display = DisplayStyle.Flex;
            StatTriangles.text = $"{triangles}";
            StatVertices.text = $"{vertices}";
        }

        public void SetPreviewMesh(Mesh mesh, Material materal, float yOffset = 0.0f)
        {
            PreviewImage.style.display = DisplayStyle.Flex;
            ErrorContainer.style.display = DisplayStyle.None;
            PreviewMesh = mesh;
            PreviewMaterial = materal;
            YOffset = yOffset;

            if (PreviewMesh)
            {
                CameraLookAtTarget = new Vector3(0.0f, PreviewMesh.bounds.size.y * 0.5f * PreviewMeshScale + YOffset, 0.0f);
                CameraLookAtPosition = CameraLookAtTarget;
            }
        }

        public void SetError()
        {
            PreviewMesh = null;
            PreviewMaterial = null;
            PreviewImage.style.display = DisplayStyle.None;
            ErrorContainer.style.display = DisplayStyle.Flex;
        }

        public void SetVisibility(bool isVisible)
        {
            style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SetupImage()
        {
            PreviewImage.image = RenderTexture;
            PreviewImage.style.width = Length.Percent(100);
            PreviewImage.style.height = Length.Percent(100);
        }

        private void SetupGrid()
        {
            GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Plane);
            GridMesh = tmp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(tmp);

            GridMaterial = Resources.Load<Material>("Materials/GridMaterial");
            if (!GridMaterial)
            {
                Debug.LogError("GridMaterial not found");
            }
        }

        private void SetupToolbarActions()
        {
            SetUIState();

            ToggleGrid.RegisterCallback<MouseDownEvent>((evt) =>
            {
                ShowGrid = !ShowGrid;
                SetUIState();
            });

            ToggleLighting.RegisterCallback<MouseDownEvent>((evt) =>
            {
                ShowLighting = !ShowLighting;
                SetUIState();
            });

            ToggleWireframe.RegisterCallback<MouseDownEvent>((evt) =>
            {
                ShowWireframe = !ShowWireframe;
                SetUIState();
            });

            ToggleTexture.RegisterCallback<MouseDownEvent>((evt) =>
            {
                ShowTextures = !ShowTextures;
                SetUIState();
            });
        }

        private void SetUIState()
        {
            if (!ShowGrid)
            {
                ToggleGrid.AddToClassList("inactive");
            } else
            {
                ToggleGrid.RemoveFromClassList("inactive");
            }

            if (!ShowLighting)
            {
                ToggleLighting.AddToClassList("inactive");
            } else
            {
                ToggleLighting.RemoveFromClassList("inactive");
            }

            if (!ShowWireframe)
            {
                ToggleWireframe.AddToClassList("inactive");
            } else
            {
                ToggleWireframe.RemoveFromClassList("inactive");
            }

            if (!ShowTextures)
            {
                ToggleTexture.AddToClassList("inactive");
            } else
            {
                ToggleTexture.RemoveFromClassList("inactive");
            }
        }

        private void UpdateRenderTexture()
        {
            if (float.IsNaN(resolvedStyle.width) || float.IsNaN(resolvedStyle.height))
            {
                return;
            }

            int width = (int)resolvedStyle.width;
            int height = (int)resolvedStyle.height;

            if (width <= 0 || height <= 0)
            {
                return;
            }

            if (RenderTexture.width == width * Supersampling && RenderTexture.height == height * Supersampling)
            {
                return;
            }

            RenderTexture.Release();
            RenderTexture.width = width * Supersampling;
            RenderTexture.height = height * Supersampling;
            RenderTexture.Create();
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            UpdateRenderTexture();
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            IsPanning = false;
            IsRotating = false;

            if (!ControlsEnabled)
            {
                return;
            }

            if (evt.button == 0)
            {
                IsRotating = true;
            }
            else if (evt.button == 1 || evt.button == 2)
            {
                IsPanning = true;
            }
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            IsPanning = false;
            IsRotating = false;
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (!ControlsEnabled)
            {
                return;
            }

            if (IsRotating)
            {
                CameraYawTarget -= evt.mouseDelta.x * 0.2f;
                CameraPitchTarget += evt.mouseDelta.y * 0.2f;
                CameraPitchTarget = Mathf.Clamp(CameraPitchTarget, -89.0f, 89.0f);
            }
            else if (IsPanning)
            {
                Vector3 cameraPosition = GetCameraPosition();
                Vector3 forward = (CameraLookAtPosition - cameraPosition).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
                Vector3 up = Vector3.Cross(forward, right).normalized;
                CameraLookAtTarget -= right * evt.mouseDelta.x * 0.01f;
                CameraLookAtTarget += up * evt.mouseDelta.y * 0.01f;
            }
        }

        private void OnMouseLeave(MouseLeaveEvent evt)
        {
            IsPanning = false;
            IsRotating = false;
        }

        private void OnMouseWheel(WheelEvent evt)
        {
            if (!ControlsEnabled)
            {
                return;
            }

            CameraZoomTarget += evt.delta.y * 0.1f;
            CameraZoomTarget = Mathf.Clamp(CameraZoomTarget, 0.0f, 1.0f);
        }

        private Vector3 GetCameraPosition()
        {
            float radius = Mathf.Lerp(CameraMinDistance, CameraMaxDistance, CameraZoom);
            float yawRadians = Mathf.Deg2Rad * CameraYaw;
            float pitchRadians = Mathf.Deg2Rad * CameraPitch;

            float x = radius * Mathf.Cos(yawRadians) * Mathf.Cos(pitchRadians);
            float y = radius * Mathf.Sin(pitchRadians);
            float z = radius * Mathf.Sin(yawRadians) * Mathf.Cos(pitchRadians);

            return new Vector3(x, y, z) + CameraLookAtPosition;
        }

        public bool IsElementVisible()
        {
            if (Root == null)
                return false;

            // Get the element's rectangle in world space
            Rect elementRect = Root.worldBound;

            // Get the parent scroll view, if any
            ScrollView scrollView = Root.GetFirstAncestorOfType<ScrollView>();
            if (scrollView != null)
            {
                // If in a scroll view, check against the scroll view's visible area
                Rect scrollViewRect = scrollView.contentViewport.worldBound;
                return elementRect.Overlaps(scrollViewRect);
            }

            return true;
        }

        private void Render()
        {
            float deltaTime = (float)(EditorApplication.timeSinceStartup - LastFrameTime);
            LastFrameTime = EditorApplication.timeSinceStartup;

            if (FrameRateLimit > 0)
            {
                deltaTime = Mathf.Min(deltaTime, 1.0f / FrameRateLimit);

                TimeSinceLastRenderedFrame += deltaTime;
                if (TimeSinceLastRenderedFrame < 1.0 / FrameRateLimit)
                {
                    schedule.Execute(Render);
                    return;
                }

                TimeSinceLastRenderedFrame -= 1.0 / FrameRateLimit;
            }

            if (!IsElementVisible())
            {
                schedule.Execute(Render);
                return;
            }

            if (RenderTexture == null)
            {
                schedule.Execute(Render);
                return;
            }

            if (AutoRotate)
            {
                CameraYawTarget -= AutoRotateSpeed * deltaTime;
            }

            float speed = 8.0f * deltaTime;
            CameraZoom = Mathf.Lerp(CameraZoom, CameraZoomTarget, speed);
            CameraYaw = Mathf.LerpAngle(CameraYaw, CameraYawTarget, speed);
            CameraPitch = Mathf.Lerp(CameraPitch, CameraPitchTarget, speed);
            CameraLookAtPosition = Vector3.Lerp(CameraLookAtPosition, CameraLookAtTarget, speed);

            CommandBuffer cmdBuf = new();

            cmdBuf.SetRenderTarget(RenderTexture);
            cmdBuf.ClearRenderTarget(true, true, BackgroundColor.linear);

            // create view and projection matrices
            float aspectRatio = (float)RenderTexture.width / RenderTexture.height;

            Vector3 cameraPosition = GetCameraPosition();
            Quaternion cameraRotation = Quaternion.LookRotation((CameraLookAtPosition - cameraPosition).normalized, Vector3.up);            
            Matrix4x4 viewMatrix = Matrix4x4.TRS(cameraPosition, cameraRotation, Vector3.one).inverse;
            if (SystemInfo.usesReversedZBuffer)
            {
                viewMatrix.m20 = -viewMatrix.m20;
                viewMatrix.m21 = -viewMatrix.m21;
                viewMatrix.m22 = -viewMatrix.m22;
                viewMatrix.m23 = -viewMatrix.m23;
            }

            Matrix4x4 projectionMatrix = Matrix4x4.Perspective
            (
                CameraFov,
                aspectRatio,
                CameraNear,
                CameraFar
            );
            cmdBuf.SetViewProjectionMatrices(viewMatrix, projectionMatrix);

            if (PreviewMesh != null && PreviewMaterial != null)
            {
                PlainWhiteMaterial.SetVector("_CameraPosition", cameraPosition);
                PreviewMaterial.SetVector("_CameraPosition", cameraPosition);
                PreviewMaterial.SetInt("_IsLightingEnabled", ShowLighting ? 1 : 0);

                Material material;

                if (ShowWireframe)
                {
                    material = WireframeMaterial;
                }
                else if (!ShowTextures)
                {
                    material = PlainWhiteMaterial;
                }
                else
                {
                    material = PreviewMaterial;
                }

                // Draw the preview mesh
                Matrix4x4 modelMatrix = Matrix4x4.TRS(new Vector3(0, -PreviewMesh.bounds.min.y * PreviewMeshScale + YOffset, 0), Quaternion.identity, Vector3.one * PreviewMeshScale);
                cmdBuf.DrawMesh(PreviewMesh, modelMatrix, material, 0, 0);

                if (ShowGrid)
                {
                    // Draw grid            
                    cmdBuf.DrawMesh(GridMesh, Matrix4x4.identity, GridMaterial, 0, 0);
                }
            }

            // Execute the command buffer
            Graphics.ExecuteCommandBuffer(cmdBuf);
            PreviewImage.MarkDirtyRepaint();

            schedule.Execute(Render);
        }
    }
}