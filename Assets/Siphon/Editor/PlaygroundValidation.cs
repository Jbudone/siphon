using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Siphon.Editor
{
    // Batch smoke check against the actual engine, including a short Play Mode run.
    public static class PlaygroundValidation
    {
        private const string StartedKey = "Siphon.ValidationStarted";
        private static double began;
        private static bool failed;
        private static PlaygroundPlayer player;
        private static PlaygroundTarget target;
        private static Vector3 targetOrigin;

        public static void Run()
        {
            try
            {
                EditorSceneManager.OpenScene("Assets/Siphon/Scenes/MovementPlayground.unity");
                var instance = UnityEngine.Object.FindFirstObjectByType<PlaygroundPlayer>();
                Require(instance != null && instance.tuning != null && instance.view != null, "Player references");
                Require(UnityEngine.Object.FindObjectsByType<PlaygroundTarget>(FindObjectsSortMode.None).Length == 6, "Six targets");
                Require(EditorBuildSettings.scenes.Length == 2 && EditorBuildSettings.scenes[0].enabled, "Build scenes");
                // Let editor startup finish (including its first-use search database)
                // before requesting Play Mode in a newly created project.
                began = EditorApplication.timeSinceStartup;
                EditorApplication.update += StartWhenReady;
            }
            catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        }

        private static void StartWhenReady()
        {
            if (EditorApplication.timeSinceStartup - began < 2 || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            EditorApplication.update -= StartWhenReady;
            SessionState.SetBool(StartedKey, true);
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void ResumeAfterDomainReload()
        {
            if (!SessionState.GetBool(StartedKey, false)) return;
            began = EditorApplication.timeSinceStartup;
            Application.logMessageReceived += Log;
            EditorApplication.update += Tick;
        }

        private static void Log(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) failed = true;
        }

        private static void Tick()
        {
            try
            {
                if (EditorApplication.timeSinceStartup - began > 30) throw new Exception("Play Mode timeout");
                if (!EditorApplication.isPlaying || Time.time < 0.2f) return;
                if (player == null)
                {
                    player = UnityEngine.Object.FindFirstObjectByType<PlaygroundPlayer>();
                    target = UnityEngine.Object.FindFirstObjectByType<PlaygroundTarget>();
                    Require(player != null && target != null, "Runtime objects");
                    targetOrigin = target.transform.position;
                    target.Hit();
                    Require(!target.GetComponent<Collider>().enabled, "Hit disables target");
                }
                if (Time.time < 5f) return;
                Require(!failed, "No runtime errors");
                Require(player.transform.position.y > 29.5f && player.transform.position.y < 31f, "Player settles on starting platform");
                Require(player.GetComponent<CharacterController>().isGrounded, $"Ground collision at {player.transform.position}, dt {Time.deltaTime}");
                Require(target.GetComponent<Collider>().enabled && target.GetComponent<Renderer>().enabled, "Target returns");
                Require(Vector3.Distance(targetOrigin, target.transform.position) > 0.01f, "Target movement");
                CheckMovement();
                Debug.Log("Siphon movement validation passed: scene references, ground collision, jump, jet launch/fuel/recharge, skiing momentum, grappling, moving targets, hit/restore, no runtime errors.");
                Finish(0);
            }
            catch (Exception ex) { Debug.LogException(ex); Finish(1); }
        }

        private static void CheckMovement()
        {
            player.enabled = false;
            Vector3 platform = new Vector3(-95, 30.04f, -35);
            player.ResetTo(platform, Quaternion.identity);
            Step(default, 0.2f);
            float groundY = player.transform.position.y;
            player.Simulate(new MovementCommand { jump = true }, 1f / 120);
            Step(default, 0.3f);
            Require(player.transform.position.y > groundY + 1.5f, "Jump lifts off surface");
            player.ResetTo(platform, Quaternion.identity);
            Step(default, 0.2f);
            Step(new MovementCommand { jet = true }, 1);
            Require(player.transform.position.y > groundY + 4, "Jets launch from rest");
            Require(player.FuelFraction < 0.65f && player.FuelFraction > 0.55f, "Jet consumption");
            Step(new MovementCommand { jet = true }, 2);
            Require(player.FuelFraction < 0.001f, "Fuel depletes without regenerating while held");
            Step(default, 1);
            Require(player.FuelFraction > 0.2f, "Fuel recharges on release");
            player.ResetTo(platform, Quaternion.identity);
            Step(default, 0.2f);
            player.AddImpulse(Vector3.right * 15);
            Step(new MovementCommand { ski = true }, 0.1f);
            Require(player.Velocity.x > 14, "Ski preserves flat-ground momentum");
            Step(default, 0.2f);
            Require(player.Velocity.x < 5, "Running applies friction");
            player.ResetTo(platform, Quaternion.identity);
            Step(new MovementCommand { grapple = true, grapplePoint = platform + new Vector3(0, 20, 30) }, 0.3f);
            Require(player.transform.position.z > platform.z + 1, "Grapple pulls toward anchor");
            player.ResetTo(new Vector3(-24, 0.04f, -60), Quaternion.identity);
            player.AddImpulse(new Vector3(35, 0, 12));
            Step(new MovementCommand { ski = true }, 0.2f);
            Require(player.transform.position.x < -20 && player.Velocity.z > 8, "Wall sweep blocks penetration and preserves tangential speed");
        }

        private static void Step(MovementCommand command, float seconds)
        {
            int count = Mathf.RoundToInt(seconds * 120);
            for (int i = 0; i < count; i++) player.Simulate(command, 1f / 120);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("Validation failed: " + message);
        }

        private static void Finish(int code)
        {
            SessionState.SetBool(StartedKey, false);
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= Log;
            EditorApplication.Exit(code);
        }
    }
}
