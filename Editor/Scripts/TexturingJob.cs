using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System;

namespace Neural
{
    public class TexturingJob : Job
    {
        public override JobType Type { get; } = JobType.Texturing;
        protected string Prompt { get; private set; }
        protected string NegativePrompt { get; private set; }
        protected int Seed { get; private set; }

        protected string DepthFilePath { get; private set; }
        protected string NormalsFilePath { get; private set; }

        protected const string DepthFileName = "depth.png";
        protected const string NormalsFileName = "normal.png";
        protected const string AlbedoFileName = "albedo.png";

        private GameObject[] selectedGameObjects;
        private GameObject cameraObject;
        private float captureViewportY;
        private float captureViewportHeight;

        public TexturingJob(string prompt, string negativePrompt = "", int seed = 0)
        {
            Prompt = prompt;
            NegativePrompt = negativePrompt;
            Seed = seed;
        }

        public override async void Execute()
        {
            try
            {
                if (!ExtractDepthAndNormals())
                {
                    SetStatusFailed();
                    return;
                }

                SetStatusRunning();

                _ = Context.Billing.UpdateBilling(5000);

                TexturingTask task = new()
                {
                    Prompt = Prompt,
                    Seed = Seed,
                    NegativePrompt = NegativePrompt,
                    DepthFilePath = DepthFilePath,
                    NormalsFilePath = NormalsFilePath
                };

                await task.Execute();

                if (!task.IsSuccessful())
                {
                    SetStatusFailed();
                    return;
                }

                SetProgress(0.5f);

                try
                {
                    await DownloadFile(task.CompletedTask.Outputs.Albedo, AlbedoFileName);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to download albedo file: {e.Message}");
                    SetStatusFailed();
                    return;
                }

                if (!ProjectAlbedoToUVs(cameraObject.GetComponent<Camera>()))
                {
                    Debug.LogError("Failed to apply albedo to meshes");
                    SetStatusFailed();
                    return;
                }

                SetProgress(1f);
                SetStatusCompleted();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        protected override Asset CreateAsset()
        {
            var asset = new TexturingAsset();
            asset.InitTemp();
            asset.Prompt = Prompt;
            asset.NegativePrompt = NegativePrompt;
            asset.Seed = Seed;
            asset.AlbedoFileName = AlbedoFileName;

            var albedoPath = GetFilePath(AlbedoFileName);
            if (!asset.AddFile(albedoPath, AlbedoFileName))
            {
                Debug.LogError("Failed to add albedo file to asset.");
                return null;
            }

            return asset;
        }

        private bool ExtractDepthAndNormals()
        {
            selectedGameObjects = Selection.gameObjects;
            if (selectedGameObjects == null || selectedGameObjects.Length == 0)
            {
                Debug.LogWarning("No objects selected in hierarchy.");
                return false;
            }

            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
            {
                Debug.LogError("No active scene view found.");
                return false;
            }

            Camera sceneCamera = sceneView.camera;
            float aspectRatio = sceneCamera.aspect;
            int baseSize = 1024;
            int rtWidth = baseSize;
            int rtHeight = Mathf.RoundToInt(baseSize / aspectRatio);

            var depthRT = new RenderTexture(rtWidth, rtHeight, 24, RenderTextureFormat.ARGBFloat);
            var normalRT = new RenderTexture(rtWidth, rtHeight, 24, RenderTextureFormat.ARGB32);

            cameraObject = new GameObject("TempRenderCamera");
            cameraObject.hideFlags = HideFlags.HideInHierarchy;
            Camera tempCamera = cameraObject.AddComponent<Camera>();
            Dictionary<GameObject, int> originalLayers = new Dictionary<GameObject, int>();

            try
            {
                SetupCamera(tempCamera, sceneCamera);
                int tempLayer = SetupTemporaryLayer(selectedGameObjects, originalLayers);
                tempCamera.cullingMask = 1 << tempLayer;

                if (!RenderDepthTexture(tempCamera, depthRT, baseSize, rtWidth, rtHeight))
                    return false;

                if (!RenderNormalsTexture(tempCamera, normalRT, baseSize, rtWidth, rtHeight))
                    return false;

                captureViewportY = (float)(baseSize - rtHeight) / (2.0f * baseSize);
                captureViewportHeight = (float)rtHeight / baseSize;

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to extract depth and normals: {e.Message}");
                return false;
            }
            finally
            {
                RestoreOriginalLayers(originalLayers);
                depthRT?.Release();
                normalRT?.Release();
            }
        }

        private bool ProjectAlbedoToUVs(Camera camera)
        {
            if (!ValidateInputs(camera, out var albedoTexture))
                return false;

            // Cache matrices
            Matrix4x4 worldToViewMatrix = camera.worldToCameraMatrix;
            Matrix4x4 projectionMatrix = camera.projectionMatrix;
            Matrix4x4 worldToScreenMatrix = projectionMatrix * worldToViewMatrix;

            var folderGuid = Guid.NewGuid().ToString().Substring(0, 6);

            foreach (GameObject obj in selectedGameObjects)
            {
                if (obj == null)
                {
                    Debug.LogWarning("Skipping deleted GameObject during albedo projection");
                    continue;
                }

                ProcessObject(folderGuid, obj, camera, albedoTexture, worldToScreenMatrix);
            }

            return true;
        }

        private void SetupCamera(Camera tempCamera, Camera sceneCamera)
        {
            tempCamera.CopyFrom(sceneCamera);
            tempCamera.rect = new Rect(0, 0, 1, 1);
            tempCamera.clearFlags = CameraClearFlags.SolidColor;
            tempCamera.backgroundColor = Color.black;
            tempCamera.depthTextureMode = DepthTextureMode.Depth;
            tempCamera.fieldOfView = sceneCamera.fieldOfView;
            tempCamera.orthographic = sceneCamera.orthographic;
            if (tempCamera.orthographic)
            {
                tempCamera.orthographicSize = sceneCamera.orthographicSize;
            }
        }

        private bool RenderDepthTexture(Camera camera, RenderTexture depthRT, int baseSize, int rtWidth, int rtHeight)
        {
            Shader depthShader = Shader.Find("Neural/DepthGrayscale");
            if (depthShader == null)
            {
                Debug.LogError("Could not find DepthGrayscale shader");
                return false;
            }

            camera.targetTexture = depthRT;
            camera.SetReplacementShader(depthShader, "");
            camera.Render();

            var squareDepthTex = CreateSquareTexture(baseSize, Color.black);
            CopyRenderTextureToSquare(depthRT, squareDepthTex, rtWidth, rtHeight, baseSize);

            DepthFilePath = GetFilePath(DepthFileName);
            SaveTextureToFile(squareDepthTex, DepthFilePath);

            return true;
        }

        private bool RenderNormalsTexture(Camera camera, RenderTexture normalRT, int baseSize, int rtWidth, int rtHeight)
        {
            Shader normalShader = Shader.Find("Neural/WorldNormals");
            if (normalShader == null)
            {
                Debug.LogError("Could not find WorldNormals shader");
                return false;
            }

            camera.targetTexture = normalRT;
            camera.depthTextureMode = DepthTextureMode.DepthNormals;
            camera.clearFlags = CameraClearFlags.SolidColor;
            Color normalBg = new Color(0.5f, 0.5f, 1f);
            camera.backgroundColor = normalBg;
            camera.SetReplacementShader(normalShader, "");
            camera.Render();

            var squareNormalTex = CreateSquareTexture(baseSize, normalBg);
            CopyRenderTextureToSquare(normalRT, squareNormalTex, rtWidth, rtHeight, baseSize);

            NormalsFilePath = GetFilePath(NormalsFileName);
            SaveTextureToFile(squareNormalTex, NormalsFilePath);

            return true;
        }

        private int SetupTemporaryLayer(GameObject[] objects, Dictionary<GameObject, int> originalLayers)
        {
            int tempLayer = FindUnusedLayer();
            foreach (var obj in objects)
            {
                var allChildren = obj.GetComponentsInChildren<Transform>();
                foreach (var child in allChildren)
                {
                    originalLayers[child.gameObject] = child.gameObject.layer;
                    child.gameObject.layer = tempLayer;
                }
            }
            return tempLayer;
        }

        private void RestoreOriginalLayers(Dictionary<GameObject, int> originalLayers)
        {
            foreach (var kvp in originalLayers)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.layer = kvp.Value;
                }
            }
        }

        private int FindUnusedLayer()
        {
            HashSet<int> usedLayers = new HashSet<int>();
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

            foreach (GameObject obj in allObjects)
            {
                usedLayers.Add(obj.layer);
            }

            for (int i = 8; i < 32; i++)
            {
                if (!usedLayers.Contains(i))
                {
                    return i;
                }
            }

            throw new System.Exception("No unused layers available");
        }

        private Texture2D CreateSquareTexture(int size, Color backgroundColor)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGB24, false);
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < size * size; i++)
            {
                pixels[i] = backgroundColor;
            }
            texture.SetPixels(pixels);
            return texture;
        }

        private void CopyRenderTextureToSquare(RenderTexture source, Texture2D target, int rtWidth, int rtHeight, int baseSize)
        {
            // Store previous active render texture
            var previousActive = RenderTexture.active;

            try
            {
                Texture2D tempTex = new Texture2D(rtWidth, rtHeight, TextureFormat.RGB24, false);
                RenderTexture.active = source;
                tempTex.ReadPixels(new Rect(0, 0, rtWidth, rtHeight), 0, 0);
                tempTex.Apply();

                int yOffset = (baseSize - rtHeight) / 2;
                for (int y = 0; y < rtHeight; y++)
                {
                    for (int x = 0; x < rtWidth; x++)
                    {
                        target.SetPixel(x, y + yOffset, tempTex.GetPixel(x, y));
                    }
                }
                target.Apply();

                UnityEngine.Object.DestroyImmediate(tempTex);
            }
            finally
            {
                // Restore previous active render texture
                RenderTexture.active = previousActive;
            }
        }

        private void SaveTextureToFile(Texture2D texture, string path)
        {
            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
        }

        private bool ValidateInputs(Camera camera, out Texture2D albedoTexture)
        {
            albedoTexture = null;

            if (camera == null)
            {
                Debug.LogError("Camera is null");
                return false;
            }

            string albedoPath = GetFilePath(AlbedoFileName);
            if (!File.Exists(albedoPath))
            {
                Debug.LogError($"Albedo file not found at path: {albedoPath}");
                return false;
            }

            byte[] fileData = File.ReadAllBytes(albedoPath);
            albedoTexture = new Texture2D(2, 2);
            if (!albedoTexture.LoadImage(fileData))
            {
                Debug.LogError("Failed to load albedo texture");
                return false;
            }

            return true;
        }

        private void ProcessObject(string folderGuid, GameObject obj, Camera camera, Texture2D albedoTexture, Matrix4x4 worldToScreenMatrix)
        {
            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();

            if (meshFilter == null || renderer == null)
                return;

            Mesh mesh = meshFilter.sharedMesh;
            if (mesh == null || !mesh.isReadable)
            {
                Debug.LogWarning($"Mesh on {obj.name} is not readable or null");
                return;
            }

            // Create new texture for this object
            int textureSize = 1024;
            Texture2D objectTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[textureSize * textureSize];

            // Initialize all pixels to transparent
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(0, 0, 0, 0);
            }

            ProcessMeshData(mesh, obj.transform, camera, albedoTexture, worldToScreenMatrix, textureSize, pixels);

            objectTexture.SetPixels(pixels);
            objectTexture.Apply();

            // Save texture for debugging
            string assetPath = $"Assets/Neural/Texturing/{folderGuid}/texture_{obj.name}_{Guid.NewGuid().ToString().Substring(0, 6)}.png";
            Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
            File.WriteAllBytes(assetPath, objectTexture.EncodeToPNG());
            UnityEditor.AssetDatabase.Refresh();

            // Apply texture to material
            Material material = renderer.material;
            ApplyTextureToMaterial(material, objectTexture);
        }

        private void ProcessMeshData(Mesh mesh, Transform transform, Camera camera, Texture2D albedoTexture,
            Matrix4x4 worldToScreenMatrix, int textureSize, Color[] pixels)
        {
            Vector2[] uvs = mesh.uv;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            int[] triangles = mesh.triangles;

            // Transform vertices and normals to world space
            Matrix4x4 localToWorld = transform.localToWorldMatrix;
            Matrix4x4 normalMatrix = localToWorld.inverse.transpose;

            Vector3[] worldVertices = new Vector3[vertices.Length];
            Vector3[] worldNormals = new Vector3[normals.Length];

            for (int i = 0; i < vertices.Length; i++)
            {
                worldVertices[i] = localToWorld.MultiplyPoint3x4(vertices[i]);
                worldNormals[i] = normalMatrix.MultiplyVector(normals[i]).normalized;
            }

            // For each triangle
            for (int i = 0; i < triangles.Length; i += 3)
            {
                ProcessTriangle(i, uvs, worldVertices, worldNormals, triangles, camera,
                    albedoTexture, worldToScreenMatrix, textureSize, pixels);
            }
        }

        private void ProcessTriangle(int i, Vector2[] uvs, Vector3[] worldVertices, Vector3[] worldNormals,
            int[] triangles, Camera camera, Texture2D albedoTexture, Matrix4x4 worldToScreenMatrix,
            int textureSize, Color[] pixels)
        {
            Vector2 uv0 = uvs[triangles[i]];
            Vector2 uv1 = uvs[triangles[i + 1]];
            Vector2 uv2 = uvs[triangles[i + 2]];

            Vector3 worldPos0 = worldVertices[triangles[i]];
            Vector3 worldPos1 = worldVertices[triangles[i + 1]];
            Vector3 worldPos2 = worldVertices[triangles[i + 2]];

            Vector3 normal0 = worldNormals[triangles[i]];
            Vector3 normal1 = worldNormals[triangles[i + 1]];
            Vector3 normal2 = worldNormals[triangles[i + 2]];

            // Get screen space positions
            Vector4 clipPos0 = worldToScreenMatrix * new Vector4(worldPos0.x, worldPos0.y, worldPos0.z, 1);
            Vector4 clipPos1 = worldToScreenMatrix * new Vector4(worldPos1.x, worldPos1.y, worldPos1.z, 1);
            Vector4 clipPos2 = worldToScreenMatrix * new Vector4(worldPos2.x, worldPos2.y, worldPos2.z, 1);

            if (clipPos0.w <= 0 || clipPos1.w <= 0 || clipPos2.w <= 0)
                return;

            ProcessTrianglePixels(uv0, uv1, uv2, worldPos0, worldPos1, worldPos2,
                normal0, normal1, normal2, clipPos0, clipPos1, clipPos2,
                camera, albedoTexture, textureSize, pixels);
        }

        private void ProcessTrianglePixels(Vector2 uv0, Vector2 uv1, Vector2 uv2,
            Vector3 worldPos0, Vector3 worldPos1, Vector3 worldPos2,
            Vector3 normal0, Vector3 normal1, Vector3 normal2,
            Vector4 clipPos0, Vector4 clipPos1, Vector4 clipPos2,
            Camera camera, Texture2D albedoTexture, int textureSize, Color[] pixels)
        {
            // Calculate screen positions accounting for the padding
            Vector2 screenPos0 = new Vector2(
                (clipPos0.x / clipPos0.w + 1) * 0.5f,
                (clipPos0.y / clipPos0.w + 1) * 0.5f
            );
            Vector2 screenPos1 = new Vector2(
                (clipPos1.x / clipPos1.w + 1) * 0.5f,
                (clipPos1.y / clipPos1.w + 1) * 0.5f
            );
            Vector2 screenPos2 = new Vector2(
                (clipPos2.x / clipPos2.w + 1) * 0.5f,
                (clipPos2.y / clipPos2.w + 1) * 0.5f
            );

            // Calculate triangle bounds in UV space
            float minU = Mathf.Min(Mathf.Min(uv0.x, uv1.x), uv2.x);
            float maxU = Mathf.Max(Mathf.Max(uv0.x, uv1.x), uv2.x);
            float minV = Mathf.Min(Mathf.Min(uv0.y, uv1.y), uv2.y);
            float maxV = Mathf.Max(Mathf.Max(uv0.y, uv1.y), uv2.y);

            // Convert to texture space
            int startX = Mathf.Max(0, Mathf.FloorToInt(minU * textureSize));
            int endX = Mathf.Min(textureSize - 1, Mathf.CeilToInt(maxU * textureSize));
            int startY = Mathf.Max(0, Mathf.FloorToInt(minV * textureSize));
            int endY = Mathf.Min(textureSize - 1, Mathf.CeilToInt(maxV * textureSize));

            // For each pixel in the triangle's bounding box
            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    Vector2 uv = new Vector2((float)x / textureSize, (float)y / textureSize);

                    Vector3 barycentric;
                    if (PointInTriangle(uv, uv0, uv1, uv2, out barycentric))
                    {
                        ProcessPixel(x, y, barycentric, worldPos0, worldPos1, worldPos2,
                            normal0, normal1, normal2, camera, albedoTexture,
                            textureSize, pixels);
                    }
                }
            }
        }

        private void ProcessPixel(int x, int y, Vector3 barycentric,
            Vector3 worldPos0, Vector3 worldPos1, Vector3 worldPos2,
            Vector3 normal0, Vector3 normal1, Vector3 normal2,
            Camera camera, Texture2D albedoTexture, int textureSize, Color[] pixels)
        {
            // Interpolate world position and normal using barycentric coordinates
            Vector3 worldPos = worldPos0 * barycentric.x + worldPos1 * barycentric.y + worldPos2 * barycentric.z;
            Vector3 worldNormal = (normal0 * barycentric.x + normal1 * barycentric.y + normal2 * barycentric.z).normalized;

            // Check if point faces camera
            Vector3 viewDir = (camera.transform.position - worldPos).normalized;
            if (Vector3.Dot(worldNormal, viewDir) <= 0)
                return;

            // Project to screen space with aspect ratio correction
            Vector4 clipPos = camera.projectionMatrix * camera.worldToCameraMatrix * new Vector4(worldPos.x, worldPos.y, worldPos.z, 1);
            Vector2 screenPos = new Vector2(
                (clipPos.x / clipPos.w + 1) * 0.5f,
                (clipPos.y / clipPos.w + 1) * 0.5f
            );

            // Map screen position to albedo texture coordinates with padding compensation
            int albedoX = Mathf.FloorToInt(screenPos.x * albedoTexture.width);
            int albedoY = Mathf.FloorToInt(Mathf.Lerp(
                captureViewportY * albedoTexture.height,
                (captureViewportY + captureViewportHeight) * albedoTexture.height,
                screenPos.y
            ));

            // Sample albedo texture if within bounds
            if (albedoX >= 0 && albedoX < albedoTexture.width &&
                albedoY >= 0 && albedoY < albedoTexture.height)
            {
                Color albedoColor = albedoTexture.GetPixel(albedoX, albedoY);
                pixels[y * textureSize + x] = albedoColor;
            }
        }

        private bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c, out Vector3 barycentric)
        {
            barycentric = Vector3.zero;

            Vector2 v0 = b - a;
            Vector2 v1 = c - a;
            Vector2 v2 = p - a;

            float d00 = Vector2.Dot(v0, v0);
            float d01 = Vector2.Dot(v0, v1);
            float d11 = Vector2.Dot(v1, v1);
            float d20 = Vector2.Dot(v2, v0);
            float d21 = Vector2.Dot(v2, v1);

            float denom = d00 * d11 - d01 * d01;
            if (Mathf.Approximately(denom, 0))
                return false;

            float v = (d11 * d20 - d01 * d21) / denom;
            float w = (d00 * d21 - d01 * d20) / denom;
            float u = 1.0f - v - w;

            barycentric = new Vector3(u, v, w);

            return v >= 0 && w >= 0 && (v + w) <= 1;
        }

        private void ApplyTextureToMaterial(Material material, Texture2D texture)
        {
            string renderPipelineShader = ModelImport.GetRenderPipelineShader();

            if (renderPipelineShader == "Standard")
            {
                material.SetTexture("_MainTex", texture);
            }
            else if (renderPipelineShader == "Universal Render Pipeline/Lit")
            {
                material.SetTexture("_BaseMap", texture);
            }
            else if (renderPipelineShader == "HDRP/Lit")
            {
                material.SetTexture("_BaseColorMap", texture);
            } else
            {
                throw new System.Exception("Unsupported render pipeline shader for one or more of the selected objects");
            }
        }
    }
}