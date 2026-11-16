using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Siphon.Editor
{
    public static class PrototypeArtifacts
    {
        public static void Package()
        {
            PlaygroundBuilder.Build();
            Capture();
            BuildLinux();
        }
        public static void Capture()
        {
            EditorSceneManager.OpenScene(AssaultSceneBuilder.ScenePath);
            Directory.CreateDirectory("docs/images");
            var camera = Camera.main;
            CaptureCamera(camera, "docs/images/player-view.png");
            foreach (var renderer in camera.GetComponentsInChildren<Renderer>()) renderer.enabled = false;
            camera.transform.position = new Vector3(210, 150, -235);
            camera.transform.LookAt(new Vector3(0, 0, -15));
            camera.fieldOfView = 65;
            CaptureCamera(camera, "docs/images/battlefield-overview.png");
            // Reload the serialized scene so preview camera changes never enter a build.
            EditorSceneManager.OpenScene(AssaultSceneBuilder.ScenePath);
            Debug.Log("Siphon preview images captured.");
        }
        private static void CaptureCamera(Camera camera, string path)
        {
            var render = new RenderTexture(1280, 720, 24);
            camera.targetTexture = render;
            // Warm up a newly selected pipeline before reading back its first frame.
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = render });
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = render });
            RenderTexture.active = render;
            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(render);
        }
        [MenuItem("Siphon/Build Linux prototype")]
        public static void BuildLinux() => BuildLinuxTo("Builds/Siphon");
        // Stage a reviewable build without replacing files used by a running playtest.
        public static void BuildLinuxCandidate() => BuildLinuxTo("Builds/SiphonCandidate");
        [MenuItem("Siphon/Build isolated 600m candidate Linux")]
        public static void BuildLinuxMapCandidate() => BuildLinuxTo("Builds/SiphonMapCandidate600", AssaultSceneBuilder.CandidateScenePath);
        private static void BuildLinuxTo(string output, string scenePath = AssaultSceneBuilder.ScenePath)
        {
            Directory.CreateDirectory(output);
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { scenePath, "Assets/Siphon/Scenes/MovementPlayground.unity" },
                locationPathName = Path.Combine(output, "Siphon.x86_64"),
                target = BuildTarget.StandaloneLinux64,
                options = BuildOptions.Development
            });
            if (result.summary.result != BuildResult.Succeeded)
                throw new System.Exception("Linux prototype build failed: " + result.summary.result);
            Debug.Log("Siphon Linux prototype built successfully.");
        }
    }
}
