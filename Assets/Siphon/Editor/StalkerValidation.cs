using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Siphon.Editor
{
    public static class StalkerValidation
    {
        private const string Key = "Siphon.StalkerValidation";
        private static double began;
        private static bool setup, failed, opened, warning;
        private static StalkerBehaviour stalker;
        private static PlayerCombat player;
        private static GameObject wall;
        private static Vector3 noise;
        private static float peak;
        public static void Run()
        { EditorSceneManager.OpenScene(AssaultSceneBuilder.ScenePath); began = EditorApplication.timeSinceStartup; EditorApplication.update += Begin; }
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
                if (EditorApplication.timeSinceStartup - began > 50) throw new Exception("Stalker validation timeout");
                if (!EditorApplication.isPlaying || Time.time < 0.3f) return;
                if (!setup) { Setup(); setup = true; }
                if (!opened && Time.time > 2)
                {
                    Require(stalker.Investigations > 0 && stalker.InvestigatedPoint == noise, "Occluded enemy is heard at a copied sound position");
                    Require(stalker.Pounces == 0, "No pounce through a sight blocker");
                    wall.SetActive(false); UnityEngine.Object.Destroy(wall); Physics.SyncTransforms(); opened = true;
                }
                warning |= stalker.State == "Pounce warning";
                peak = Mathf.Max(peak, stalker.transform.position.y - WarDirector.GroundPoint(stalker.transform.position).y);
                if (Time.time < 14) return;
                Require(warning && stalker.Pounces > 0 && stalker.Bites > 0 && player.Entity.health < 10000, "Visible encounter warns, lunges and delivers melee damage");
                Require(peak > 0.45f, "Grounded external impulse actually lifts the creature");
                Require(!stalker.GetComponent<BotLocomotion>().Boosting && !failed, "Stalkers never jet and encounter has no engine errors");
                var snapshot = FieldSaveSystem.Capture();
                foreach (var state in snapshot.bots) if (state.team == Team.Wild && state.role == BotRole.Stalker) state.role = BotRole.Creature;
                FieldSaveSystem.Apply(snapshot);
                Require(stalker.GetComponent<BotBrain>().Entity.Alive, "Legacy forest creature saves restore into the new role");
                Debug.Log($"Siphon stalker validation passed: fixed sound investigation, occluded restraint, warning, {stalker.Pounces} pounces, {stalker.Bites} bites, impulse height {peak:0.00}m, legacy wild save."); Finish(0);
            }
            catch (Exception error) { Debug.LogException(error); Finish(1); }
        }
        private static void Setup()
        {
            foreach (var bot in BotBrain.All.ToArray()) bot.enabled = false;
            player = UnityEngine.Object.FindAnyObjectByType<PlayerCombat>();
            player.GetComponent<PlaygroundPlayer>().enabled = false; player.GetComponent<CharacterController>().enabled = false;
            noise = WarDirector.GroundPoint(new Vector3(-240, 0, -230)) + Vector3.up * 0.1f;
            player.transform.position = noise + Vector3.right * 2; player.Entity.maximumHealth = player.Entity.health = 10000;
            Vector3 origin = WarDirector.GroundPoint(noise + Vector3.forward * 15);
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.name = "Forest sight blocker";
            wall.transform.position = WarDirector.GroundPoint(noise + Vector3.forward * 7) + Vector3.up * 2;
            wall.transform.localScale = new Vector3(40, 4, 1);
            stalker = WarDirector.Instance.SpawnBot(Team.Wild, BotRole.Stalker, origin).GetComponent<StalkerBehaviour>();
            WarDirector.Instance.Noise(noise, Team.Blue, 80); Physics.SyncTransforms();
        }
        private static void Require(bool condition, string reason) { if (!condition) throw new Exception("Stalker validation failed: " + reason); }
        private static void Finish(int code)
        { SessionState.SetBool(Key, false); EditorApplication.update -= Tick; Application.logMessageReceived -= Log; EditorApplication.Exit(code); }
    }
}
