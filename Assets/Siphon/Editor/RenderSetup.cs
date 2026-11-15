using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Siphon.Editor
{
    public static class RenderSetup
    {
        public const string LitShader = "Universal Render Pipeline/Lit";
        public const string PipelinePath = "Assets/Siphon/Settings/SiphonURP.asset";
        [MenuItem("Siphon/Configure URP rendering")]
        public static void Configure()
        {
            Directory.CreateDirectory("Assets/Siphon/Settings");
            Directory.CreateDirectory("Assets/Siphon/Resources");
            const string rendererPath = "Assets/Siphon/Settings/SiphonRenderer.asset";
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                renderer.renderingMode = RenderingMode.Forward;
                AssetDatabase.CreateAsset(renderer, rendererPath);
            }
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                pipeline.msaaSampleCount = 2;
                pipeline.shadowDistance = 160;
                pipeline.shadowCascadeCount = 2;
                pipeline.mainLightShadowmapResolution = 2048;
                pipeline.supportsCameraDepthTexture = false;
                pipeline.supportsCameraOpaqueTexture = false;
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }
            GraphicsSettings.defaultRenderPipeline = pipeline;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(current, false);

            MaterialAsset("Assets/Siphon/Resources/CombatLines.mat", "Siphon/Combat Lines");
            var ghost = MaterialAsset("Assets/Siphon/Resources/BuildGhost.mat", "Universal Render Pipeline/Unlit");
            ghost.SetFloat("_Surface", 1);
            ghost.SetFloat("_Blend", 0);
            ghost.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            ghost.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            ghost.SetFloat("_ZWrite", 0);
            ghost.SetFloat("_Cull", (float)CullMode.Off);
            ghost.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            ghost.SetOverrideTag("RenderType", "Transparent");
            ghost.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(ghost);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Siphon/Migrate materials and settings to URP")]
        public static void Migrate()
        {
            // Preserve original material files outside Assets for a reviewable backup.
            string backup = "Backups/BeforeURP/Materials";
            Directory.CreateDirectory(backup);
            foreach (string path in Directory.GetFiles("Assets/Siphon/Materials", "*.mat"))
            {
                string destination = Path.Combine(backup, Path.GetFileName(path));
                if (!File.Exists(destination)) File.Copy(path, destination);
            }
            Configure();
            foreach (string id in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Siphon" }))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(id));
                if (material.shader == null || material.shader.name != "Standard") continue;
                Color color = material.color;
                float smoothness = material.GetFloat("_Glossiness");
                float metallic = material.GetFloat("_Metallic");
                material.shader = RequiredShader(LitShader);
                material.color = color;
                material.SetFloat("_Smoothness", smoothness);
                material.SetFloat("_Metallic", metallic);
                EditorUtility.SetDirty(material);
            }
            AssetDatabase.SaveAssets();
            Validate();
            Debug.Log("Siphon URP migration complete: pipeline, all quality levels, scene materials, combat lines, and build ghosts.");
        }

        public static void MigrateAndPackage()
        {
            Migrate();
            PrototypeArtifacts.Capture();
            PrototypeArtifacts.BuildLinux();
        }

        public static void Validate()
        {
            if (!(GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset))
                throw new Exception("Default graphics pipeline must be URP.");
            int current = QualitySettings.GetQualityLevel();
            try
            {
                for (int i = 0; i < QualitySettings.names.Length; i++)
                {
                    QualitySettings.SetQualityLevel(i, false);
                    if (!(QualitySettings.renderPipeline is UniversalRenderPipelineAsset))
                        throw new Exception("Missing URP in quality level " + QualitySettings.names[i]);
                }
            }
            finally { QualitySettings.SetQualityLevel(current, false); }
            foreach (string id in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Siphon" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(id);
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material.shader == null || ShaderUtil.ShaderHasError(material.shader))
                    throw new Exception("Shader error in " + path);
                if (material.shader.name == "Standard" || material.shader.name == "Sprites/Default")
                    throw new Exception("Legacy shader remains in " + path);
            }
            Debug.Log("Siphon URP validation passed.");
        }

        private static Material MaterialAsset(string path, string shaderName)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(RequiredShader(shaderName));
                AssetDatabase.CreateAsset(material, path);
            }
            return material;
        }
        private static Shader RequiredShader(string name)
        {
            var shader = Shader.Find(name);
            if (shader == null) throw new Exception("Missing shader: " + name);
            return shader;
        }
    }
}
