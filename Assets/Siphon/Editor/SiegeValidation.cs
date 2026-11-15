using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Siphon.Editor
{
    public static class SiegeValidation
    {
        private const string Key = "Siphon.SiegeValidation";
        private static double began;
        private static bool rules, failed, interrupted, interruptionObserved, firstShot;
        private static SiegeBattery gun;
        private static DefenseStructure generator, target;
        private static ResourceBag stock;
        private static float powerCutAt;
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
                if (EditorApplication.timeSinceStartup - began > 70) throw new Exception("Siege validation timeout");
                if (!EditorApplication.isPlaying || Time.time < 0.3f) return;
                if (!rules) { Rules(); rules = true; }
                var war = WarDirector.Instance;
                if (!interrupted && gun.Charging)
                {
                    Require(gun.Shots == 0 && war.StockBag(Team.Blue).alloy == stock.alloy, "Charging does not pre-spend ammunition");
                    generator.Disable(1.5f); powerCutAt = Time.time; interrupted = true;
                }
                else if (interrupted && !interruptionObserved && !generator.Operational && Time.time > powerCutAt + 0.1f)
                {
                    Require(!gun.Charging && gun.Shots == 0 && war.StockBag(Team.Blue).alloy == stock.alloy, "Power loss cancels charging without spending ammunition");
                    interruptionObserved = true;
                }
                if (!firstShot && gun.Shots > 0)
                {
                    var after = war.StockBag(Team.Blue);
                    Require(gun.Shots == 1 && after.ferrite == stock.ferrite - 2 && after.alloy == stock.alloy - 1, "One shell consumes exactly its ammunition");
                    firstShot = true;
                }
                if (Time.time < 15) return;
                Require(!failed && firstShot && interrupted && interruptionObserved, "Live battery recovered after interrupted power");
                Require(target.Entity.health < 900, "Ballistic shell lands and deals its structure bonus");
                Debug.Log($"Siege evidence: {gun.Shots} shells, wall health {target.Entity.health:0}, power interruption recovered, finite ammunition consumed.");
                var player = UnityEngine.Object.FindAnyObjectByType<PlayerCombat>(); player.Gear.siegeCalibration = true;
                var snapshot = FieldSaveSystem.Capture();
                string path = Path.Combine(Path.GetTempPath(), "siphon-siege-" + Guid.NewGuid().ToString("N") + ".json");
                try
                {
                    FieldSaveSystem.WriteSnapshot(snapshot, path);
                    var loaded = FieldSaveSystem.ReadSnapshot(path, out bool recovered);
                    Require(!recovered && loaded.version == 3 && loaded.siegeResearch && loaded.structures.Exists(s => s.kind == EntityKind.SiegeCannon), "New research and battery serialize to disk");
                    FieldSaveSystem.Apply(loaded);
                    Require(player.Gear.siegeCalibration && war.RallyActive && war.BlueOrder == CaptainOrder.Assault, "Checkpoint restores research and captain designation");
                }
                finally { foreach (var file in new[] { path, path + ".tmp", path + ".bak" }) if (File.Exists(file)) File.Delete(file); }
                Debug.Log("Siphon siege validation passed: target snapshots, assault orders, shielded-core power priority, research prerequisites, ballistic reach, power interruption, paid live shells, structure damage and checkpoint restoration.");
                Finish(0);
            }
            catch (Exception error) { Debug.LogException(error); Finish(1); }
        }
        private static void Rules()
        {
            var war = WarDirector.Instance; var player = UnityEngine.Object.FindAnyObjectByType<PlayerCombat>();
            var original = FieldSaveSystem.Capture();
            Require(war.MarkTarget(war.blueCore.AimPoint, war.blueCore) && !war.RallyFireAllowed, "Friendly rally marks do not authorize artillery fire");
            Require(!war.MarkTarget(new Vector3(float.NaN, 0, 0)) && !war.MarkTarget(new Vector3(400, 0, 0)), "Reject invalid target coordinates");
            Vector3 rally = new Vector3(25, 0, -45);
            Require(war.MarkTarget(rally) && war.BlueOrder == CaptainOrder.Assault, "Designation commits assault squad");
            foreach (var bot in BotBrain.All)
                if (bot.Entity.team == Team.Blue && (bot.role == BotRole.Raider || bot.role == BotRole.Heavy || bot.role == BotRole.Guard))
                    Require(bot.Destination == rally, "Combat roles receive the designated point");
            var raider = BotBrain.All.Find(bot => bot.Entity.team == Team.Red && bot.role == BotRole.Raider);
            Vector3 position = raider.transform.position;
            war.MarkTarget(raider.Entity.AimPoint, raider.Entity); Vector3 copied = war.RallyPoint;
            raider.transform.position += Vector3.right * 10;
            Require(war.RallyPoint == copied, "Designation does not continuously track a hidden moving actor"); raider.transform.position = position;
            war.MarkTarget(war.redCore.AimPoint, war.redCore);
            Require(war.redCore.Shielded && (war.AssaultPoint(Team.Blue, false) - war.redCore.transform.position).sqrMagnitude > 1
                && (war.SiegeAimPoint - war.redCore.transform.position).sqrMagnitude < 1600, "Marked shielded core redirects assault and siege to power");
            var command = FieldSaveSystem.Capture(); FieldSaveSystem.Apply(command, 46);
            Require(!war.RallyActive, "Offline time expires target designation without combat");
            FieldSaveSystem.Apply(original);
            player.GetComponent<PlaygroundPlayer>().ResetTo(war.blueSpawn, Quaternion.identity);
            war.blueResources = 100; war.blueTimber = 50; war.blueCrystal = 50; war.blueAlloy = 50;
            player.Gear.surveyOptics = player.Gear.fieldEngineering = player.Gear.siegeCalibration = false;
            int alloy = war.blueAlloy;
            Require(!player.Gear.Craft(Upgrade.SiegeCalibration) && war.blueAlloy == alloy, "Missing prerequisites do not consume resources");
            Require(player.Gear.Craft(Upgrade.SurveyOptics) && player.Gear.Craft(Upgrade.FieldEngineering)
                && player.Gear.Craft(Upgrade.SiegeCalibration) && player.Gear.CanBuild(EntityKind.SiegeCannon), "Research branches unlock the battery");
            Require(!player.Gear.Craft((Upgrade)99), "Invalid research cannot be crafted");
            Vector3 from = new Vector3(-15, 2.8f, -90), to = new Vector3(15, 1.3f, 30);
            Require(Ballistics.TryVelocity(from, to, 68, 18, out Vector3 velocity), "Ballistic solver reaches siege distance");
            float flight = Vector3.ProjectOnPlane(to - from, Vector3.up).magnitude / Vector3.ProjectOnPlane(velocity, Vector3.up).magnitude;
            Require((from + velocity * flight + Vector3.down * (9 * flight * flight) - to).magnitude < 0.05f, "Solved arc intersects the designated point");
            Require(!Ballistics.TryVelocity(Vector3.zero, Vector3.forward * 1000, 68, 18, out _), "Unreachable trajectory is rejected");
            foreach (var bot in BotBrain.All.ToArray()) bot.enabled = false;
            foreach (var actor in Combatant.All.ToArray()) actor.GetComponent<DefenseStructure>()?.Disable(1000);
            gun = war.CreateStructure(EntityKind.SiegeCannon, Team.Blue, WarDirector.GroundPoint(new Vector3(-15, 0, -90)), 0).GetComponent<SiegeBattery>();
            generator = war.CreateStructure(EntityKind.Generator, Team.Blue, WarDirector.GroundPoint(new Vector3(-28, 0, -90)), 0);
            target = war.CreateStructure(EntityKind.Wall, Team.Red, WarDirector.GroundPoint(new Vector3(15, 0, 30)), 0);
            target.Entity.maximumHealth = target.Entity.health = 1000;
            war.MarkTarget(target.Entity.AimPoint, target.Entity);
            stock = war.StockBag(Team.Blue); Physics.SyncTransforms();
        }
        private static void Require(bool condition, string reason) { if (!condition) throw new Exception("Siege validation failed: " + reason); }
        private static void Finish(int code)
        { SessionState.SetBool(Key, false); EditorApplication.update -= Tick; Application.logMessageReceived -= Log; EditorApplication.Exit(code); }
    }
}
