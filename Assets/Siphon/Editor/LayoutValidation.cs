using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Siphon.Editor
{
    public static class LayoutValidation
    {
        private const string Key = "Siphon.LayoutValidation";
        private static double start;

        public static void RebuildAndCheck()
        {
            AssaultSceneBuilder.Build();
            EditorSceneManager.OpenScene(AssaultSceneBuilder.ScenePath);
            var terrain = GameObject.Find("Rolling battlefield").GetComponent<MeshFilter>().sharedMesh;
            Require(terrain.vertexCount == 10201 && terrain.triangles.Length == 60000, "Generated terrain topology");
            Require(terrain.bounds.min.x == -300 && terrain.bounds.max.x == 300
                && terrain.bounds.min.z == -300 && terrain.bounds.max.z == 300, "Generated terrain extent");
            var nav = UnityEngine.Object.FindAnyObjectByType<NavigationWorld>().field;
            Require(nav.Count == 22801 && nav.Point(0).x == -300 && nav.Point(0).z == -300
                && nav.Point(nav.Count - 1).x == 300 && nav.Point(nav.Count - 1).z == 300, "Baked navigation coverage");
            var war = UnityEngine.Object.FindAnyObjectByType<WarDirector>();
            Require(Flat(war.blueCore.transform.position, new Vector3(0, 0, -185))
                && Flat(war.redCore.transform.position, new Vector3(0, 0, 185)), "Generated core anchors");
            Require(Flat(war.blueSpawn, new Vector3(-7, 0, -178)) && Flat(war.redSpawn, new Vector3(7, 0, 175)), "Generated spawn anchors");
            Require(Flat(GameObject.Find("Frost vault").transform.position, new Vector3(228, 0, 92))
                && Flat(GameObject.Find("Forest relay ruins").transform.position, new Vector3(-225, 0, -20)), "Generated camp anchors");
            var weapon = Camera.main.transform.Find("First person weapon");
            Require(weapon != null && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(weapon.gameObject) == RifleAssetBuilder.PrefabPath
                && weapon.GetComponent<WeaponPresentation>() != null, "Accepted rifle survives rebuild and reload");
            var rect = new Rect(17, 23, 210, 140);
            Require(AssaultLayout.Default.MapPoint(new Vector3(-300, 0, 300), rect) == rect.min
                && AssaultLayout.Default.MapPoint(new Vector3(300, 0, -300), rect) == rect.max
                && AssaultLayout.Default.MapPoint(Vector3.zero, rect) == rect.center
                && AssaultLayout.Default.MapPoint(new Vector3(-400, 0, 400), rect) == rect.min,
                "HUD projection corners, center and clamping");
            Debug.Log("Siphon layout generation validation passed: terrain, navigation coverage, anchors, rifle prefab and HUD projection.");
        }

        public static void Run()
        {
            EditorSceneManager.OpenScene(AssaultSceneBuilder.ScenePath);
            start = EditorApplication.timeSinceStartup;
            EditorApplication.update += Begin;
        }
        private static void Begin()
        {
            if (EditorApplication.timeSinceStartup - start < 2 || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            EditorApplication.update -= Begin;
            SessionState.SetBool(Key, true);
            EditorApplication.EnterPlaymode();
        }
        [InitializeOnLoadMethod]
        private static void Resume()
        {
            if (!SessionState.GetBool(Key, false)) return;
            start = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
        }
        private static void Tick()
        {
            try
            {
                if (EditorApplication.timeSinceStartup - start > 60) throw new Exception("Layout validation timeout");
                if (!EditorApplication.isPlaying || Time.time < 0.3f) return;
                CheckRuntime();
                Debug.Log("Siphon layout runtime validation passed: inclusive target/bot boundaries on all four edges and saved player restore at/beyond all four edges.");
                Finish(0);
            }
            catch (Exception error) { Debug.LogException(error); Finish(1); }
        }
        private static void CheckRuntime()
        {
            var war = WarDirector.Instance;
            var saved = FieldSaveSystem.Capture();
            var bot = BotBrain.All.Find(candidate => candidate.Entity.team == Team.Blue);
            var body = bot.GetComponent<CharacterController>();
            var motor = bot.GetComponent<BotLocomotion>();
            Vector3 originalBot = bot.transform.position;
            var player = UnityEngine.Object.FindAnyObjectByType<PlaygroundPlayer>();
            foreach (Vector3 axis in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
            {
                Vector3 edge = axis * 295 + Vector3.up * 40;
                Require(war.MarkTarget(edge), "Target accepts inclusive 295m edge");
                Require(!war.MarkTarget(edge + axis * 0.01f), "Target rejects beyond 295m edge");
                body.enabled = false; bot.transform.position = edge; body.enabled = true; motor.ResetMotion();
                Physics.SyncTransforms();
                motor.Tick(edge, bot.role, false, false, 0.00001f);
                Require(Flat(bot.transform.position, edge), "Bot remains at inclusive 295m edge");
                body.enabled = false; bot.transform.position = edge + axis * 0.01f; body.enabled = true; motor.ResetMotion();
                Physics.SyncTransforms();
                motor.Tick(bot.transform.position, bot.role, false, false, 0.00001f);
                Require(Flat(bot.transform.position, bot.home), "Bot recovers home beyond 295m edge");
                saved.playerPosition = axis * 290 + Vector3.up * 40;
                FieldSaveSystem.Apply(saved);
                Require(Flat(player.transform.position, saved.playerPosition), "Save restores inclusive 290m edge");
                saved.playerPosition += axis * 0.01f;
                FieldSaveSystem.Apply(saved);
                Require(Flat(player.transform.position, war.blueSpawn), "Save restores spawn beyond 290m edge");
            }
            body.enabled = false; bot.transform.position = originalBot; body.enabled = true; motor.ResetMotion();
        }
        private static bool Flat(Vector3 a, Vector3 b) => Vector3.ProjectOnPlane(a - b, Vector3.up).sqrMagnitude < 0.000001f;
        private static void Require(bool pass, string reason)
        { if (!pass) throw new Exception("Layout validation failed: " + reason); }
        private static void Finish(int code)
        {
            SessionState.SetBool(Key, false);
            EditorApplication.update -= Tick;
            EditorApplication.Exit(code);
        }
    }
}
