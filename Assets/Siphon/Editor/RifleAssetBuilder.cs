using System;
using UnityEditor;
using UnityEngine;

namespace Siphon.Editor
{
    public static class RifleAssetBuilder
    {
        public const string Folder = "Assets/Siphon/Art/PulseRifle";
        public const string PrefabPath = Folder + "/PulseRifle.prefab";

        public static void BuildCandidate()
        {
            Build();
            AssaultSceneBuilder.Build();
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(AssaultSceneBuilder.ScenePath);
            var camera = Camera.main;
            var weapon = camera.transform.Find("First person weapon");
            if (weapon == null || !PrefabUtility.IsPartOfPrefabInstance(weapon.gameObject))
                throw new Exception("Rebuilt scene must reference the rifle prefab.");
            var presentation = weapon.GetComponent<WeaponPresentation>();
            if (presentation == null || presentation.pulse == null || presentation.disc == null)
                throw new Exception("Missing weapon presentation references.");
            if (weapon.GetComponentsInChildren<Collider>(true).Length != 0)
                throw new Exception("Weapon visuals must not affect collision or raycasts.");
            var renderers = presentation.pulse.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
                foreach (var material in renderer.sharedMaterials)
                    if (material == null || material.shader.name != RenderSetup.LitShader)
                        throw new Exception("Rifle must use URP materials.");
            }
            if (Mathf.Abs(bounds.size.z - 0.8f) > 0.001f)
                throw new Exception("Rifle length must be 0.8m.");
            var muzzle = presentation.pulse.transform.Find("Muzzle (visual only)");
            if (muzzle == null || Vector3.Dot(muzzle.forward, camera.transform.forward) < 0.999f)
                throw new Exception("Visual muzzle must point along camera aim.");
            Debug.Log($"Rifle integration checks passed: prefab persists after rebuild/reload, no colliders, URP, bounds {bounds.size}, forward muzzle.");
            PrototypeArtifacts.BuildLinuxCandidate();
        }

        public static void Build()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/model.fbx");
            if (model == null) throw new Exception("Import the accepted rifle FBX before building its prefab.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/PulseRifle.mat");
            if (material == null)
            {
                material = new Material(Shader.Find(RenderSetup.LitShader));
                AssetDatabase.CreateAsset(material, Folder + "/PulseRifle.mat");
            }
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/base_color.png"));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Metallic", 0.25f);
            material.SetFloat("_Smoothness", 0.3f);
            // Meshy supplies metalness and roughness separately; URP expects smoothness in alpha.
            var metal = ReadLinear("metallic.png");
            var rough = ReadLinear("roughness.png");
            var packed = new Texture2D(metal.width, metal.height, TextureFormat.RGBA32, false, true);
            var pixels = metal.GetPixels();
            var roughPixels = rough.GetPixels();
            if (pixels.Length != roughPixels.Length) throw new Exception("PBR texture dimensions differ.");
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(pixels[i].r, 0, 0, 1 - roughPixels[i].r);
            packed.SetPixels(pixels); packed.Apply();
            System.IO.File.WriteAllBytes(Folder + "/metallic_smoothness.png", packed.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(packed);
            AssetDatabase.ImportAsset(Folder + "/metallic_smoothness.png");
            var packedImporter = (TextureImporter)AssetImporter.GetAtPath(Folder + "/metallic_smoothness.png");
            packedImporter.sRGBTexture = false; packedImporter.SaveAndReimport();
            material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/metallic_smoothness.png"));
            material.SetFloat("_Metallic", 1); material.SetFloat("_Smoothness", 1);
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/normal.png");
            if (normal != null)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(Folder + "/normal.png");
                if (importer.textureType != TextureImporterType.NormalMap)
                { importer.textureType = TextureImporterType.NormalMap; importer.SaveAndReimport(); }
                material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/normal.png"));
                material.EnableKeyword("_NORMALMAP");
            }
            EditorUtility.SetDirty(material);
            var root = new GameObject("First person weapon");
            try
            {
                var pulse = new GameObject("Pulse rifle");
                pulse.transform.SetParent(root.transform, false);
                var mesh = (GameObject)PrefabUtility.InstantiatePrefab(model, pulse.transform);
                // Unity's FBX conversion puts the barrel along +X. Fit it to +Z at 0.8m.
                mesh.transform.localRotation = Quaternion.Euler(0, -90, 0);
                var renderers = mesh.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) throw new Exception("Rifle has no renderers.");
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers)
                {
                    bounds.Encapsulate(renderer.bounds);
                    var materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++) materials[i] = material;
                    renderer.sharedMaterials = materials;
                }
                float scale = 0.8f / bounds.size.z;
                mesh.transform.localScale *= scale;
                mesh.transform.localPosition = -bounds.center * scale;
                foreach (var collider in mesh.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
                var muzzle = new GameObject("Muzzle (visual only)").transform;
                muzzle.SetParent(pulse.transform, false);
                muzzle.localPosition = new Vector3(0, 0.065f, 0.4f);
                var disc = new GameObject("Disc placeholder");
                disc.transform.SetParent(root.transform, false);
                var dark = AssetDatabase.LoadAssetAtPath<Material>("Assets/Siphon/Materials/Industrial.mat");
                var blue = AssetDatabase.LoadAssetAtPath<Material>("Assets/Siphon/Materials/Blue team.mat");
                WarDirector.Part(disc.transform, PrimitiveType.Cube, "Receiver", Vector3.zero,
                    new Vector3(0.13f, 0.14f, 0.5f), dark, false);
                WarDirector.Part(disc.transform, PrimitiveType.Cube, "Accent", new Vector3(0, 0.09f, 0.02f),
                    new Vector3(0.08f, 0.025f, 0.35f), blue, false);
                disc.SetActive(false);
                var presentation = root.AddComponent<WeaponPresentation>();
                presentation.pulse = pulse; presentation.disc = disc;
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"Rifle prefab saved: length 0.8m; source bounds {bounds.size}.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static Texture2D ReadLinear(string name)
        {
            string path = Folder + "/" + name;
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.sRGBTexture = false; importer.isReadable = true; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
