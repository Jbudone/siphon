using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Siphon.Editor
{
    public static class SpawnValidation
    {
        private const string Key = "Siphon.SpawnValidation";
        private static double began;
        private static bool rules, failed;
        private static PlayerCombat player;
        private static Vector3 selected;
        public static void Run()
        {
            EditorSceneManager.OpenScene(AssaultSceneBuilder.ScenePath);
            began = EditorApplication.timeSinceStartup; EditorApplication.update += Begin;
        }
        private static void Begin()
        {
            if (EditorApplication.timeSinceStartup - began < 2 || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            EditorApplication.update -= Begin; SessionState.SetBool(Key, true); EditorApplication.EnterPlaymode();
        }
        [InitializeOnLoadMethod] private static void Resume()
        {
            if (!SessionState.GetBool(Key, false)) return;
            began = EditorApplication.timeSinceStartup; EditorApplication.update += Tick; Application.logMessageReceived += Log;
        }
        private static void Log(string message, string stack, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) failed = true; }
        private static void Tick()
        {
            try
            {
                if (EditorApplication.timeSinceStartup - began > 45) throw new Exception("Spawn validation timeout");
                if (!EditorApplication.isPlaying || Time.time < 0.3f) return;
                if (!rules) { Rules(); rules = true; }
                if (Time.time < 5) return;
                Require(!failed && player.Entity.Alive && (player.transform.position - selected).sqrMagnitude < 121, "Timed player respawn reaches the selected forward base");
                Require(player.Menu == 3, "Respawn network can remain open while down and after returning");
                Debug.Log("Siphon spawn validation passed: selected forward base, independent bot defaults, home preference, power fallback, obstructed exit fallback, enemy rejection, checkpoint preference and timed player respawn.");
                Finish(0);
            }
            catch (Exception error) { Debug.LogException(error); Finish(1); }
        }
        private static void Rules()
        {
            var war = WarDirector.Instance; player = UnityEngine.Object.FindAnyObjectByType<PlayerCombat>();
            foreach (var bot in BotBrain.All.ToArray()) bot.enabled = false;
            Vector3 a = WarDirector.GroundPoint(new Vector3(-45, 0, -90)), b = WarDirector.GroundPoint(new Vector3(-45, 0, 90));
            var first = war.CreateStructure(EntityKind.Outpost, Team.Blue, a, 0);
            var second = war.CreateStructure(EntityKind.Outpost, Team.Blue, b, 0);
            war.CreateStructure(EntityKind.Generator, Team.Blue, WarDirector.GroundPoint(a + Vector3.left * 12), 0);
            var power = war.CreateStructure(EntityKind.Generator, Team.Blue, WarDirector.GroundPoint(b + Vector3.left * 12), 0);
            var enemy = war.CreateStructure(EntityKind.Outpost, Team.Red, WarDirector.GroundPoint(new Vector3(45, 0, 90)), 0);
            Physics.SyncTransforms();
            Require(!war.Spawns.Choose(enemy.Entity) && war.Spawns.Choose(second.Entity), "Only a usable friendly station can be chosen");
            Require((war.RespawnPoint(Team.Blue, true) - b).sqrMagnitude < 121 && (war.RespawnPoint(Team.Blue) - a).sqrMagnitude < 121, "Player preference does not override bot spawn priorities");
            Require(war.Spawns.Choose(war.blueCore) && war.RespawnPoint(Team.Blue, true) == war.blueSpawn, "Home preference overrides powered outposts for the player");
            war.Spawns.Choose(second.Entity); power.Disable(30);
            Require((war.RespawnPoint(Team.Blue, true) - a).sqrMagnitude < 121, "Lost preferred power falls back to a ready outpost");
            power.RestoreRuntime(0, 160);
            var blockers = new List<GameObject>();
            for (int ring = 0; ring < 2; ring++)
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * Mathf.PI / 4;
                    Vector3 point = b + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * (6 + ring * 3);
                    var block = GameObject.CreatePrimitive(PrimitiveType.Cube); block.name = "Blocked spawn exit";
                    block.transform.position = WarDirector.GroundPoint(point) + Vector3.up * 1.5f; block.transform.localScale = new Vector3(2.5f, 3, 2.5f); blockers.Add(block);
                }
            Physics.SyncTransforms();
            Require(!war.Spawns.Available(second.Entity, out _) && (war.RespawnPoint(Team.Blue, true) - a).sqrMagnitude < 121, "A fully walled exit never spawns a player inside cover");
            foreach (var block in blockers) { block.SetActive(false); UnityEngine.Object.Destroy(block); }
            Physics.SyncTransforms();
            Require(war.Spawns.Available(second.Entity, out _), "Clearing exits makes the station usable again");
            var snapshot = FieldSaveSystem.Capture(); FieldSaveSystem.Apply(snapshot);
            Require(war.Spawns.Preference == SpawnPreference.Outpost && war.Spawns.SelectedPosition == b
                && (war.RespawnPoint(Team.Blue, true) - b).sqrMagnitude < 121, "Checkpoint restores the chosen station");
            selected = b; player.Entity.Damage(10000, Team.Red); player.ToggleMenu(3);
            Require(!player.Entity.Alive && player.Menu == 3, "Network can be opened while down");
        }
        private static void Require(bool condition, string reason) { if (!condition) throw new Exception("Spawn validation failed: " + reason); }
        private static void Finish(int code)
        { SessionState.SetBool(Key, false); EditorApplication.update -= Tick; Application.logMessageReceived -= Log; EditorApplication.Exit(code); }
    }
}
