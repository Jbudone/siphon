using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Siphon.Editor
{
    public static class PlaygroundBuilder
    {
        [MenuItem("Siphon/Rebuild prototypes")]
        public static void Build()
        {
            Directory.CreateDirectory("Assets/Siphon/Scenes");
            Directory.CreateDirectory("Assets/Siphon/Settings");
            Directory.CreateDirectory("Assets/Siphon/Materials");
            RenderSetup.Configure();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var ground = Material("Ground", new Color(0.16f, 0.25f, 0.29f));
            var ramp = Material("Ramps", new Color(0.25f, 0.5f, 0.55f));
            var obstacle = Material("Obstacles", new Color(0.43f, 0.48f, 0.57f));
            var target = Material("Targets", new Color(1f, 0.35f, 0.12f));
            Box("Floor", new Vector3(0, -1, 0), new Vector3(500, 2, 500), ground);
            // Opposing broad slopes form a valley for repeated downhill runs.
            Box("West ski slope", new Vector3(-65, 12, 50), new Vector3(85, 3, 160), ramp, new Vector3(0, 0, -18));
            Box("East ski slope", new Vector3(65, 12, 50), new Vector3(85, 3, 160), ramp, new Vector3(0, 0, 18));
            Box("Launch ramp", new Vector3(0, 5, 105), new Vector3(30, 3, 45), ramp, new Vector3(-18, 0, 0));
            Box("Spawn platform", new Vector3(-95, 29, -35), new Vector3(22, 2, 22), obstacle);
            for (int i = 0; i < 6; i++)
            {
                float z = i * 30f - 60;
                Box("Grapple tower " + i, new Vector3(i % 2 == 0 ? -18 : 18, 15, z), new Vector3(5, 30, 5), obstacle);
                var dummy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                dummy.name = "Moving target " + i;
                dummy.transform.position = new Vector3(i % 2 == 0 ? 10 : -10, 5 + i * 3, z + 10);
                dummy.transform.localScale = Vector3.one * 2;
                dummy.GetComponent<Renderer>().sharedMaterial = target;
                dummy.AddComponent<PlaygroundTarget>();
            }
            Box("Bridge", new Vector3(0, 24, 40), new Vector3(55, 2, 8), obstacle);
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.2f;
            sun.transform.rotation = Quaternion.Euler(45, -30, 0);
            RenderSettings.ambientLight = new Color(0.5f, 0.55f, 0.65f);
            RenderSettings.skybox = null;
            var player = new GameObject("Player");
            player.transform.position = new Vector3(-95, 32, -35);
            var body = player.AddComponent<CharacterController>();
            body.height = 2f;
            body.radius = 0.45f;
            body.center = Vector3.up;
            body.stepOffset = 0.3f;
            body.skinWidth = 0.04f;
            body.minMoveDistance = 0f;
            var camera = new GameObject("View").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.SetParent(player.transform, false);
            camera.transform.localPosition = new Vector3(0, 1.7f, 0);
            camera.fieldOfView = 100;
            camera.farClipPlane = 1000;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.13f, 0.2f);
            camera.gameObject.AddComponent<AudioListener>();
            const string tuningPath = "Assets/Siphon/Settings/Movement.asset";
            var tuning = AssetDatabase.LoadAssetAtPath<MovementTuning>(tuningPath);
            if (tuning == null)
            {
                tuning = ScriptableObject.CreateInstance<MovementTuning>();
                AssetDatabase.CreateAsset(tuning, tuningPath);
            }
            var controller = player.AddComponent<PlaygroundPlayer>();
            controller.view = camera;
            controller.tuning = tuning;
            PlayerSettings.companyName = "Siphon";
            PlayerSettings.productName = "Siphon";
            PlayerSettings.runInBackground = true;
            // This prototype reads the built-in Input API; production input is undecided.
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var input = settings.FindProperty("activeInputHandler");
            if (input != null) { input.intValue = 0; settings.ApplyModifiedPropertiesWithoutUndo(); }
            EditorSceneManager.SaveScene(scene, "Assets/Siphon/Scenes/MovementPlayground.unity");
            AssaultSceneBuilder.Build();
            EditorBuildSettings.scenes = new[] {
                new EditorBuildSettingsScene(AssaultSceneBuilder.ScenePath, true),
                new EditorBuildSettingsScene("Assets/Siphon/Scenes/MovementPlayground.unity", true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Siphon playground generated successfully.");
        }

        private static Material Material(string name, Color color)
        {
            string path = "Assets/Siphon/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find(RenderSetup.LitShader));
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void Box(string name, Vector3 position, Vector3 scale, Material material, Vector3 rotation = default)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.position = position;
            box.transform.localScale = scale;
            box.transform.rotation = Quaternion.Euler(rotation);
            box.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
