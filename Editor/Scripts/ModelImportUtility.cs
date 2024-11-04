using System;
using System.IO;
using UnityEngine;
using Assimp;
using System.Threading.Tasks;
using System.Threading;
using UnityEditor;
using System.Collections.Generic;

namespace Neural
{
    public class ModelImport
    {
        private class MeshData
        {
            public Vector3[] Vertices;
            public Vector3[] Normals;
            public Vector2[] UVs;
            public int[] Triangles;
            public bool HasNormals;
        }

        private static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(3);

        static ModelImport()
        {
            AssimpUnity.InitializePlugin();
        }

        public static Task ProcessGlbAsync(string inputPath, string outputPath)
        {
            return Task.Run(() =>
            {
                using (AssimpContext importer = new AssimpContext())
                {
                    Scene scene = importer.ImportFile(inputPath, PostProcessSteps.Triangulate | PostProcessSteps.GenerateSmoothNormals | PostProcessSteps.FlipUVs);

                    // Rescale mesh to unit box
                    RescaleMeshToUnitBox(scene);

                    importer.ExportFile(scene, outputPath, "glb2");
                }
                File.Delete(inputPath);
            });
        }

        private static void RescaleMeshToUnitBox(Scene scene)
        {
            foreach (Assimp.Mesh mesh in scene.Meshes)
            {
                if (mesh.VertexCount == 0)
                {
                    Console.WriteLine("Failed to rescale mesh to unit box: No vertices.");
                    continue;
                }

                Vector3 minPosition = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                Vector3 maxPosition = new Vector3(float.MinValue, float.MinValue, float.MinValue);

                // Find min and max positions
                foreach (Vector3D vertex in mesh.Vertices)
                {
                    minPosition = Vector3.Min(minPosition, new Vector3(vertex.X, vertex.Y, vertex.Z));
                    maxPosition = Vector3.Max(maxPosition, new Vector3(vertex.X, vertex.Y, vertex.Z));
                }

                Vector3 center = (minPosition + maxPosition) * 0.5f;
                Vector3 extent = maxPosition - minPosition;
                float maxExtent = Math.Max(extent.x, Math.Max(extent.y, extent.z));
                float scale = 1.0f / maxExtent;

                // Rescale vertices
                for (int i = 0; i < mesh.VertexCount; i++)
                {
                    Vector3D vertex = mesh.Vertices[i];
                    Vector3 rescaledVertex = (new Vector3(vertex.X, vertex.Y, vertex.Z) - center) * scale;
                    mesh.Vertices[i] = new Vector3D(rescaledVertex.x, rescaledVertex.y, rescaledVertex.z);
                }
            }

            Console.WriteLine($"Rescaled mesh to unit box");
        }

        public static async Task<UnityEngine.Mesh> LoadMeshAsync(string glbPath)
        {
            await Semaphore.WaitAsync();

            try
            {
                MeshData meshData = await Task.Run(() =>
                {
                    return LoadMeshDataTask(glbPath);
                });

                if (meshData == null)
                {
                    return null;
                }

                UnityEngine.Mesh mesh = AssignMeshData(meshData);

                return mesh;
            }
            finally
            {
                Semaphore.Release();
            }
        }

        private static MeshData LoadMeshDataTask(string glbPath)
        {
            try
            {
                var importer = new AssimpContext();
                var scene = importer.ImportFile(glbPath);

                if (scene == null || !scene.HasMeshes)
                {
                    //Debug.LogError("Failed to import GLB file or the file contains no meshes.");
                    return null;
                }

                if (scene.Meshes.Count > 1)
                {
                    //Debug.LogWarning("GLB file contains multiple meshes. Only the first mesh will be imported.");
                }

                var assimpMesh = scene.Meshes[0];
                Vector3[] Vertices = new Vector3[assimpMesh.Vertices.Count];
                Vector3[] Normals = assimpMesh.HasNormals ? new Vector3[assimpMesh.Normals.Count] : null;
                Vector2[] UVs = new Vector2[assimpMesh.TextureCoordinateChannels[0].Count];
                int[] Triangles = assimpMesh.GetIndices();
                bool HasNormals = assimpMesh.HasNormals;

                for (int i = 0; i < assimpMesh.Vertices.Count; i++)
                {
                    Vertices[i] = new Vector3(assimpMesh.Vertices[i].X, assimpMesh.Vertices[i].Y, assimpMesh.Vertices[i].Z);
                }

                if (assimpMesh.HasNormals)
                {
                    for (int i = 0; i < assimpMesh.Normals.Count; i++)
                    {
                        Normals[i] = new Vector3(assimpMesh.Normals[i].X, assimpMesh.Normals[i].Y, assimpMesh.Normals[i].Z);
                    }
                }

                for (int i = 0; i < assimpMesh.TextureCoordinateChannels[0].Count; i++)
                {
                    UVs[i] = new Vector2(assimpMesh.TextureCoordinateChannels[0][i].X, 1 - assimpMesh.TextureCoordinateChannels[0][i].Y);
                }

                return new MeshData
                {
                    Vertices = Vertices,
                    Normals = Normals,
                    UVs = UVs,
                    Triangles = Triangles,
                    HasNormals = HasNormals
                };
            }
            catch
            {
                //Debug.LogError($"Error loading GLB file: {ex.Message}");
                return null;
            }
        }

        private static UnityEngine.Mesh AssignMeshData(MeshData meshData)
        {
            if (meshData == null) return null;

            UnityEngine.Mesh mesh = new UnityEngine.Mesh();
            mesh.vertices = meshData.Vertices;

            if (meshData.HasNormals)
            {
                mesh.normals = meshData.Normals;
            }

            if (meshData.UVs != null && meshData.UVs.Length > 0)
            {
                mesh.uv = meshData.UVs;
            }

            mesh.triangles = meshData.Triangles;

            mesh.RecalculateBounds();
            if (!meshData.HasNormals)
            {
                mesh.RecalculateNormals();
            }

            return mesh;
        }

        public static UnityEngine.Material LoadMaterial(string albedoPath, string normalsPath = null, string displacementPath = null, string metallicPath = null, string roughnessPath = null, string aoPath = null)
        {
            UnityEngine.Material material = Resources.Load<UnityEngine.Material>("Materials/PreviewMaterial");
            UnityEngine.Material materialCopy = new UnityEngine.Material(material);

            Texture2D albedoTexture = LoadTexture(albedoPath);
            materialCopy.SetTexture("_Albedo", albedoTexture);

            if (!string.IsNullOrEmpty(normalsPath))
            {
                Texture2D normalsTexture = LoadTexture(normalsPath);
                materialCopy.SetTexture("_Normals", normalsTexture);
            }

            if (!string.IsNullOrEmpty(displacementPath))
            {
                Texture2D displacementTexture = LoadTexture(displacementPath);
                materialCopy.SetTexture("_Displacement", displacementTexture);
            }

            if (!string.IsNullOrEmpty(metallicPath))
            {
                Texture2D metallicTexture = LoadTexture(metallicPath);
                materialCopy.SetTexture("_Metallic", metallicTexture);
            }

            if (!string.IsNullOrEmpty(roughnessPath))
            {
                Texture2D roughnessTexture = LoadTexture(roughnessPath);
                materialCopy.SetTexture("_Roughness", roughnessTexture);
            }

            if (!string.IsNullOrEmpty(aoPath))
            {
                Texture2D aoTexture = LoadTexture(aoPath);
                materialCopy.SetTexture("_AmbientOcclusion", aoTexture);
            }

            return materialCopy;
        }

        private static Texture2D LoadTexture(string path)
        {
            byte[] fileData = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(fileData);
            texture.wrapMode = UnityEngine.TextureWrapMode.Repeat;
            return texture;
        }

        public static void EmbedTexturesToGlb(string inputGlbPath, string albedoPath, string outputGlbPath)
        {
            try
            {
                using (var context = new AssimpContext())
                {
                    Assimp.Scene scene = context.ImportFile(inputGlbPath, PostProcessSteps.Triangulate | PostProcessSteps.GenerateSmoothNormals | PostProcessSteps.FlipUVs);
                    scene.Materials.Clear();
                    scene.Textures.Clear();

                    // Load texture data
                    byte[] textureData = File.ReadAllBytes(albedoPath);

                    EmbeddedTexture embeddedTexture = new EmbeddedTexture("png", textureData, Path.GetFileName(albedoPath));
                    int textureIndex = scene.Textures.Count;
                    scene.Textures.Add(embeddedTexture);

                    // Add a new material to the scene
                    Assimp.Material material = new Assimp.Material();
                    material.TextureDiffuse = new TextureSlot(
                        "albedo.png",
                        TextureType.Diffuse,
                        0,
                        TextureMapping.FromUV,
                        0,
                        1.0f,
                        TextureOperation.Add,
                        Assimp.TextureWrapMode.Wrap,
                        Assimp.TextureWrapMode.Wrap,
                        0
                    );
                    scene.Materials.Add(material);
                    scene.Meshes[0].MaterialIndex = 0;

                    context.ExportFile(scene, outputGlbPath, "glb2");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in EmbedTexturesToGlb: {e.Message}\nStackTrace: {e.StackTrace}");
                throw;
            }
        }

        public static void ExtractTexturesFromGlb(string glbPath, string outputPath)
        {
            try
            {
                using (var context = new AssimpContext())
                {
                    Assimp.Scene scene = context.ImportFile(glbPath, PostProcessSteps.Triangulate | PostProcessSteps.GenerateSmoothNormals | PostProcessSteps.FlipUVs);

                    if (scene.Materials.Count == 0)
                    {
                        Debug.LogError("No materials found in the GLB file.");
                        return;
                    }

                    Assimp.Material material = scene.Materials[0];

                    File.WriteAllBytes(outputPath, scene.Textures[material.TextureDiffuse.TextureIndex].CompressedData);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in ExtractTexturesFromGlb: {e.Message}\nStackTrace: {e.StackTrace}");
                throw;
            }
        }

        public static UnityEngine.Material ImportMaterial(string outputPath, string albedoPath, string normalsPath, string displacementPath, string metallicPath, string roughnessPath, string aoPath)
        {
            string renderPipelineShader = GetRenderPipelineShader();
            Shader shader = Shader.Find(renderPipelineShader);
            UnityEngine.Material material = new UnityEngine.Material(shader);

            Texture2D albedoTexture = LoadTexture(albedoPath);
            Texture2D normalsTexture = LoadTexture(normalsPath);
            Texture2D displacementTexture = LoadTexture(displacementPath);
            Texture2D metallicTexture = LoadTexture(metallicPath);
            Texture2D roughnessTexture = LoadTexture(roughnessPath);
            Texture2D aoTexture = LoadTexture(aoPath);

            if (renderPipelineShader == "Standard")
            {
                SetBuiltinMaterialTextures(material, outputPath, albedoTexture, normalsTexture, displacementTexture, metallicTexture, roughnessTexture, aoTexture);
            }
            else if (renderPipelineShader == "Universal Render Pipeline/Lit")
            {
                SetURPMaterialTextures(material, outputPath, albedoTexture, normalsTexture, displacementTexture, metallicTexture, roughnessTexture, aoTexture);
            }
            else if (renderPipelineShader == "HDRP/Lit")
            {
                SetHDRPMaterialTextures(material, outputPath, albedoTexture, normalsTexture, displacementTexture, metallicTexture, roughnessTexture, aoTexture);
            }

            UnityEditor.AssetDatabase.CreateAsset(material, Path.Combine(outputPath, "Material.mat"));
            UnityEditor.AssetDatabase.Refresh();

            return material;
        }

        private static string GetRenderPipelineShader()
        {
            string[] shaderNames = {
                "Universal Render Pipeline/Lit",  // URP
                "HDRP/Lit",                       // HDRP
                "Standard",                       // Built-in RP
            };

            foreach (string shaderName in shaderNames)
            {
                Shader shader = Shader.Find(shaderName);
                if (shader != null)
                {
                    return shaderName;
                }
            }

            return null;
        }

        private static void SetBuiltinMaterialTextures(
            UnityEngine.Material material,
            string outputPath,
            Texture2D albedoTexture,
            Texture2D normalsTexture,
            Texture2D displacementTexture,
            Texture2D metallicTexture,
            Texture2D roughnessTexture,
            Texture2D aoTexture
        )
        {
            string albedoPath = Path.Combine(outputPath, "Albedo.png");
            string normalsPath = Path.Combine(outputPath, "Normals.png");
            string displacementPath = Path.Combine(outputPath, "Displacement.png");
            string metallicGlossPath = Path.Combine(outputPath, "MetallicGloss.png");
            string aoPath = Path.Combine(outputPath, "AmbientOcclusion.png");

            Texture2D metallicGloss = CombineMetallicRoughnessStandard(metallicTexture, roughnessTexture);
            File.WriteAllBytes(albedoPath, albedoTexture.EncodeToPNG());
            File.WriteAllBytes(metallicGlossPath, metallicGloss.EncodeToPNG());
            File.WriteAllBytes(normalsPath, normalsTexture.EncodeToPNG());
            File.WriteAllBytes(displacementPath, displacementTexture.EncodeToPNG());
            File.WriteAllBytes(aoPath, aoTexture.EncodeToPNG());

            UnityEditor.AssetDatabase.Refresh();

            albedoTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
            normalsTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(normalsPath);
            displacementTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(displacementPath);
            metallicGloss = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(metallicGlossPath);
            aoTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(aoPath);

            TextureImporter normalImporter = AssetImporter.GetAtPath(normalsPath) as TextureImporter;
            if (normalImporter != null)
            {
                normalImporter.textureType = TextureImporterType.NormalMap;
                normalImporter.SaveAndReimport();
            }

            material.SetTexture("_MainTex", albedoTexture);
            material.SetTexture("_BumpMap", normalsTexture);
            material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_ParallaxMap", displacementTexture);
            material.EnableKeyword("_PARALLAXMAP");
            material.SetTexture("_MetallicGlossMap", metallicGloss);
            material.EnableKeyword("_METALLICGLOSSMAP");
            material.SetTexture("_OcclusionMap", aoTexture);
            material.SetFloat("_OcclusionStrength", 0.1f);
        }

        private static void SetURPMaterialTextures(
            UnityEngine.Material material,
            string outputPath,
            Texture2D albedoTexture,
            Texture2D normalsTexture,
            Texture2D displacementTexture,
            Texture2D metallicTexture,
            Texture2D roughnessTexture,
            Texture2D aoTexture
            )
        {
            string albedoPath = Path.Combine(outputPath, "Albedo.png");
            string normalsPath = Path.Combine(outputPath, "Normals.png");
            string displacementPath = Path.Combine(outputPath, "Displacement.png");
            string metallicGlossPath = Path.Combine(outputPath, "MetallicGloss.png");
            string aoPath = Path.Combine(outputPath, "AmbientOcclusion.png");

            Texture2D metallicGloss = CombineMetallicRoughnessStandard(metallicTexture, roughnessTexture);
            File.WriteAllBytes(albedoPath, albedoTexture.EncodeToPNG());
            File.WriteAllBytes(metallicGlossPath, metallicGloss.EncodeToPNG());
            File.WriteAllBytes(normalsPath, normalsTexture.EncodeToPNG());
            File.WriteAllBytes(displacementPath, displacementTexture.EncodeToPNG());
            File.WriteAllBytes(aoPath, aoTexture.EncodeToPNG());

            UnityEditor.AssetDatabase.Refresh();

            albedoTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
            normalsTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(normalsPath);
            displacementTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(displacementPath);
            metallicGloss = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(metallicGlossPath);
            aoTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(aoPath);

            TextureImporter normalImporter = AssetImporter.GetAtPath(normalsPath) as TextureImporter;
            if (normalImporter != null)
            {
                normalImporter.textureType = TextureImporterType.NormalMap;
                normalImporter.SaveAndReimport();
            }

            material.SetTexture("_BaseMap", albedoTexture);
            material.SetTexture("_BumpMap", normalsTexture);
            material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_ParallaxMap", displacementTexture);
            material.EnableKeyword("_PARALLAXMAP");
            material.SetTexture("_MetallicGlossMap", metallicGloss);
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.SetTexture("_OcclusionMap", aoTexture);
            material.EnableKeyword("_OCCLUSIONMAP");
        }

        private static void SetHDRPMaterialTextures(
            UnityEngine.Material material,
            string outputPath,
            Texture2D albedoTexture,
            Texture2D normalsTexture,
            Texture2D displacementTexture,
            Texture2D metallicTexture,
            Texture2D roughnessTexture,
            Texture2D aoTexture
        )
        {
            string albedoPath = Path.Combine(outputPath, "Albedo.png");
            string normalsPath = Path.Combine(outputPath, "Normals.png");
            string displacementPath = Path.Combine(outputPath, "Displacement.png");
            string maskMapPath = Path.Combine(outputPath, "MaskMap.png");
            string aoPath = Path.Combine(outputPath, "AmbientOcclusion.png");

            Texture2D maskMap = CombineMaskMapHDRP(metallicTexture, aoTexture, roughnessTexture);
            File.WriteAllBytes(albedoPath, albedoTexture.EncodeToPNG());
            File.WriteAllBytes(maskMapPath, maskMap.EncodeToPNG());
            File.WriteAllBytes(normalsPath, normalsTexture.EncodeToPNG());
            File.WriteAllBytes(displacementPath, displacementTexture.EncodeToPNG());

            UnityEditor.AssetDatabase.Refresh();

            albedoTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
            normalsTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(normalsPath);
            displacementTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(displacementPath);
            maskMap = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(maskMapPath);

            TextureImporter normalImporter = AssetImporter.GetAtPath(normalsPath) as TextureImporter;
            if (normalImporter != null)
            {
                normalImporter.textureType = TextureImporterType.NormalMap;
                normalImporter.SaveAndReimport();
            }

            material.SetTexture("_BaseColorMap", albedoTexture);
            material.SetTexture("_NormalMap", normalsTexture);
            material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_HeightMap", displacementTexture);
            material.EnableKeyword("_HEIGHTMAP");
            material.SetTexture("_MaskMap", maskMap);
            material.EnableKeyword("_MASKMAP");

            // Set some default values for HDRP materials
            material.SetFloat("_Metallic", 1.0f); // Full metallic influence from mask map
            material.SetFloat("_Smoothness", 1.0f); // Full smoothness influence from mask map
            material.SetFloat("_AORemapMin", 0.0f);
            material.SetFloat("_AORemapMax", 1.0f);
            material.SetFloat("_HeightAmplitude", 0.02f); // Adjust this value based on your needs
            material.SetFloat("_HeightCenter", 0.5f);
        }

        private static Texture2D CombineMetallicRoughnessStandard(Texture2D metallicTexture, Texture2D roughnessTexture)
        {
            int width = metallicTexture.width;
            int height = metallicTexture.height;
            Texture2D combinedTexture = new Texture2D(width, height);

            Color32[] metallicPixels = metallicTexture.GetPixels32();
            Color32[] roughnessPixels = roughnessTexture.GetPixels32();
            Color32[] combinedPixels = new Color32[width * height];

            for (int i = 0; i < combinedPixels.Length; i++)
            {
                combinedPixels[i] = new Color32(metallicPixels[i].r, 0, 0, (byte)(255 - roughnessPixels[i].r));
            }

            combinedTexture.SetPixels32(combinedPixels);
            combinedTexture.Apply();

            return combinedTexture;
        }

        private static Texture2D CombineMaskMapHDRP(Texture2D metallicTexture, Texture2D aoTexture, Texture2D roughnessTexture)
        {
            int width = metallicTexture.width;
            int height = metallicTexture.height;
            Texture2D combinedTexture = new Texture2D(width, height);

            Color32[] metallicPixels = metallicTexture.GetPixels32();
            Color32[] aoPixels = aoTexture.GetPixels32();
            Color32[] roughnessPixels = roughnessTexture.GetPixels32();
            Color32[] combinedPixels = new Color32[width * height];

            for (int i = 0; i < combinedPixels.Length; i++)
            {
                // R: Metallic
                // G: Ambient Occlusion
                // B: Detail Mask (unused in this case, set to 0)
                // A: Smoothness (inverted roughness)
                combinedPixels[i] = new Color32(
                    metallicPixels[i].r,
                    aoPixels[i].r,
                    0,
                    (byte)(255 - roughnessPixels[i].r)
                );
            }

            combinedTexture.SetPixels32(combinedPixels);
            combinedTexture.Apply();

            return combinedTexture;
        }
    }
}