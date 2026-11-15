using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Siphon.Editor
{
    public static class FieldValidation
    {
        private const string Key = "Siphon.FieldValidation";
        private static double start;
        private static bool failed;
        private static bool checkedRules, respawnChecked;
        private static PlayerCombat player;
        private static readonly Dictionary<BotBrain, Vector3> botStarts = new Dictionary<BotBrain, Vector3>();
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
            Application.logMessageReceived += Log;
        }
        private static void Log(string message, string stack, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) failed = true; }
        private static void Tick()
        {
            try
            {
                if (EditorApplication.timeSinceStartup - start > 100) throw new Exception("Field validation timeout");
                if (!EditorApplication.isPlaying || Time.time < 0.3f) return;
                if (!checkedRules)
                {
                    CheckRules();
                    checkedRules = true;
                }
                if (!respawnChecked && Time.time >= 6)
                { Require(player.Entity.Alive && player.Entity.health == player.Entity.maximumHealth, "Player death and timed respawn"); respawnChecked = true; }
                if (Time.time < 45) return;
                Require(!failed, "No runtime errors");
                int moved = 0;
                foreach (var pair in botStarts)
                {
                    Vector3 position = pair.Key.transform.position;
                    Require(!float.IsNaN(position.x) && position.y > -30, "Bot remains in world");
                    if (Vector3.Distance(pair.Value, position) > 5) moved++;
                }
                Require(moved >= 10, "Squads travel toward orders");
                Require(respawnChecked, "Timed respawn was verified");
                var war = WarDirector.Instance;
                int deliveries = 0, outposts = 0;
                foreach (var bot in BotBrain.All)
                {
                    deliveries += bot.Deliveries;
                    if (bot.role == BotRole.Worker || bot.role == BotRole.Scout) Debug.Log($"Support bot evidence: {bot.Entity.team}, {bot.Task}, {bot.Carried} cargo, {bot.Deliveries} deliveries, at {bot.transform.position}, goal {bot.Destination}");
                }
                foreach (var actor in Combatant.All) if (actor.Alive && actor.kind == EntityKind.Outpost) outposts++;
                Require(deliveries >= 2, "Workers mine and return actual cargo");
                Require(outposts >= 1, "Scout establishes a forward outpost");
                Require(war.blueAlloy + war.redAlloy > 0, "Powered factories produce alloy during play");
                Combatant generator = null;
                foreach (var actor in Combatant.All)
                    if (actor.team == Team.Red && actor.kind == EntityKind.Generator && actor.Alive) { generator = actor; break; }
                if (generator != null) generator.Damage(10000, Team.Blue);
                foreach (var actor in Combatant.All.ToArray())
                    if (actor.team == Team.Red && actor.kind == EntityKind.Generator && actor.Alive) actor.Damage(10000, Team.Blue);
                Require(war.redCore.Damage(10000, Team.Blue), "Core can be destroyed after power loss");
                Require(war.Finished && war.Winner == Team.Blue, "Win condition");
                Debug.Log("Siphon field validation passed: typed resource transactions, atomic recipes, equipment, navigation, force-field interception/overload, hacking, sensor snapshots, captain orders, friendly fire, power/shields, mining/depletion, repairs, player respawn, bot movement, core victory.");
                Finish(0);
            }
            catch (Exception ex) { Debug.LogException(ex); Finish(1); }
        }
        private static void CheckRules()
        {
            var war = WarDirector.Instance;
            player = UnityEngine.Object.FindAnyObjectByType<PlayerCombat>();
            Require(war != null && player != null && war.blueCore != null && war.redCore != null, "Scene references");
            Require(BotBrain.All.Count == 19, "Fourteen team bots and five creatures");
            foreach (var bot in BotBrain.All) botStarts[bot] = bot.transform.position;
            war.CycleOrder();
            Require(war.BlueOrder == CaptainOrder.Assault, "Player can request assault");
            war.CycleOrder();
            foreach (var bot in BotBrain.All)
                if (bot.Entity.team == Team.Blue && bot.role == BotRole.Raider)
                    Require(bot.Task.Contains("defend"), "Captain assigns defense order");
            war.CycleOrder(); war.CycleOrder();
            Require(war.BlueOrder == CaptainOrder.Balanced, "Orders cycle back to autonomous behavior");
            int stock = war.blueResources;
            Require(!war.Spend(Team.Blue, stock + 1) && war.blueResources == stock, "Cannot overspend");
            Require(!war.Spend(Team.Blue, -1), "Reject negative costs");
            Require(war.Spend(Team.Blue, 10), "Spend stock"); war.Deposit(Team.Blue, 10);
            Require(war.blueResources == stock, "Deposit conservation");
            var before = war.StockBag(Team.Blue);
            Require(!war.Spend(Team.Blue, new ResourceBag(1, crystal: 100)), "Reject missing advanced resource");
            Require(war.StockBag(Team.Blue).ferrite == before.ferrite, "Multi-resource spend is atomic");
            war.Deposit(Team.Blue, 2, ResourceKind.Crystal);
            Require(war.Stock(Team.Blue, ResourceKind.Crystal) == 2, "Typed crystal deposit");
            player.GetComponent<PlaygroundPlayer>().ResetTo(war.blueCore.transform.position + new Vector3(-7, 1, 4), Quaternion.identity);
            Require(player.Gear.AtWorkbench() && player.Gear.Craft(Upgrade.ThermalLining), "Craft insulation at friendly core");
            Require(player.Gear.thermalLining && war.Stock(Team.Blue, ResourceKind.Crystal) == 0, "Recipe consumes crystals");
            Require(!player.Gear.Craft(Upgrade.ThermalLining), "Cannot craft owned equipment twice");
            var path = new List<Vector3>();
            Require(NavigationWorld.Instance != null && NavigationWorld.Instance.FindPath(war.blueSpawn, war.redSpawn, path)
                && path.Count > 20, "Navigation connects main bases around obstacles");
            Require(NavigationWorld.Instance.FindPath(war.blueSpawn, new Vector3(-180, 0, -40), path), "Navigation reaches forest supplies");
            Require(NavigationWorld.Instance.FindPath(war.blueSpawn, new Vector3(190, 0, 65), path), "Navigation crosses icefield ridge");
            var testPoint = WarDirector.GroundPoint(new Vector3(50, 0, -115));
            var fieldPower = war.CreateStructure(EntityKind.Generator, Team.Blue, testPoint + Vector3.right * 12, 0);
            var barrier = war.CreateStructure(EntityKind.ForceField, Team.Blue, testPoint, 0);
            Physics.SyncTransforms();
            Require(barrier.BarrierActive, "Powered force field activates");
            Vector3 rayOrigin = testPoint + Vector3.up * 2 - Vector3.forward * 8;
            Require(CombatPhysics.Cast(rayOrigin, Vector3.forward, 16, Team.Red, null, out RaycastHit fieldHit)
                && fieldHit.collider.isTrigger, "Hostile fire is intercepted by field");
            Require(!CombatPhysics.Cast(rayOrigin, Vector3.forward, 16, Team.Blue, null, out _), "Friendly fire passes through field");
            barrier.Absorb(160, testPoint + Vector3.up * 2);
            Require(!barrier.BarrierActive && barrier.Charge == 0, "Field overload creates an attack window");
            fieldPower.Disable(25);
            Require(!war.IsPowered(Team.Blue, testPoint, 35), "Hacked generator supplies no power");
            barrier.gameObject.SetActive(false); fieldPower.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(barrier.gameObject); UnityEngine.Object.Destroy(fieldPower.gameObject);
            var sensorTarget = BotBrain.All.Find(bot => bot.Entity.team == Team.Red).Entity;
            war.ReportContact(Team.Blue, sensorTarget, 5);
            Vector3 sensed = sensorTarget.transform.position;
            sensorTarget.transform.position += Vector3.right * 2;
            Require(war.SensorContacts.Find(contact => contact.id == sensorTarget && contact.observer == Team.Blue).position == sensed,
                "Sensor records a snapshot rather than live tracking");
            var save = FieldSaveSystem.Capture();
            Require(save != null && save.structures.Count >= 10 && save.nodes.Count >= 60, "Snapshot captures world economy and bases");
            string probe = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "siphon-save-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                FieldSaveSystem.WriteSnapshot(save, probe);
                FieldSaveSystem.WriteSnapshot(save, probe);
                var restored = FieldSaveSystem.ReadSnapshot(probe, out bool recovered);
                Require(!recovered, "Healthy checkpoint loads directly");
                Require(restored.structures.Count == save.structures.Count && restored.blue.ferrite == save.blue.ferrite,
                    "Atomic save creation and replacement round trip");
                System.IO.File.WriteAllText(probe, "{}");
                restored = FieldSaveSystem.ReadSnapshot(probe, out recovered);
                Require(recovered && restored.structures.Count == save.structures.Count, "Incomplete checkpoint recovers its previous atomic backup");
                restored.version = 1; FieldSaveSystem.WriteSnapshot(restored, probe);
                Require(FieldSaveSystem.ReadSnapshot(probe, out recovered).version == 1 && !recovered, "Previous save version remains readable");
                System.IO.File.WriteAllText(probe, "{}");
                Require(FieldSaveSystem.ReadSnapshot(probe, out recovered).version == save.version && recovered, "Saving after recovery preserves the healthy backup");
            }
            finally { foreach (var checkpointPath in new[] { probe, probe + ".bak", probe + ".tmp" }) if (System.IO.File.Exists(checkpointPath)) System.IO.File.Delete(checkpointPath); }
            var offline = JsonUtility.FromJson<FieldSaveSystem.Snapshot>(JsonUtility.ToJson(save));
            FieldSaveSystem.SimulateOffline(offline, 120);
            Require(offline.blue.alloy > save.blue.alloy && offline.blue.ferrite >= 24 && offline.blue.timber >= 10,
                "Offline production conserves inputs and supply reserve");
            Require(offline.structures.Find(state => state.kind == EntityKind.Core && state.team == Team.Red).health == war.redCore.health,
                "Offline production does not attack cores");
            FieldSaveSystem.Apply(save);
            Require(war.blueResources == save.blue.ferrite && player.Gear.thermalLining, "Save round trip restores stock and gear");
            Require(war.redCore.Shielded && !war.redCore.Damage(35, Team.Blue), "Powered core rejects damage");
            Require(!player.Entity.Damage(35, Team.Blue) && player.Entity.health == 100, "Friendly fire disabled");
            Combatant generator = null;
            foreach (var actor in Combatant.All)
                if (actor.team == Team.Red && actor.kind == EntityKind.Generator) { generator = actor; break; }
            Require(generator != null, "Enemy generator");
            generator.Damage(35, Team.Blue);
            Require(generator.Repair(12) == 12 && generator.health == 157, "Repair restores bounded health");
            generator.Damage(10000, Team.Blue);
            Require(!war.redCore.Shielded && war.redCore.Damage(35, Team.Blue), "Power loss exposes core");
            war.redCore.Repair(35);
            // Restore the generator so the soak run starts with normal defenses.
            generator.Restore();
            foreach (var collider in generator.GetComponentsInChildren<Collider>()) collider.enabled = true;
            foreach (var visual in generator.GetComponentsInChildren<Renderer>()) visual.enabled = true;
            var node = ResourceNode.All.Find(candidate => candidate.kind == ResourceKind.Ferrite && candidate.remaining > 2);
            Require(node != null, "Ordinary ferrite deposit available");
            int remaining = node.remaining;
            Require(node.Mine(2, Team.Blue) == 2 && node.remaining == remaining - 2, "Mining conservation");
            node.Mine(10000, Team.Blue);
            Require(node.remaining == 0 && node.Mine(2, Team.Blue) == 0, "Depleted nodes cannot be mined");
            var structure = war.CreateStructure(EntityKind.Turret, Team.Blue, new Vector3(100, 30, -100), 90);
            Require(!structure.Powered, "Remote turret needs local power");
            var power = war.CreateStructure(EntityKind.Generator, Team.Blue, new Vector3(110, 30, -100), 0);
            Require(structure.Powered, "Nearby generator powers turret");
            power.GetComponent<Combatant>().Damage(10000, Team.Red);
            Require(!structure.Powered, "Generator destruction disables turret");
            UnityEngine.Object.Destroy(structure.gameObject);
            UnityEngine.Object.Destroy(power.gameObject);
            player.Entity.Damage(1000, Team.Red);
            Require(!player.Entity.Alive, "Player dies");
        }
        private static void Require(bool pass, string reason)
        { if (!pass) throw new Exception("Field validation failed: " + reason); }
        private static void Finish(int code)
        {
            SessionState.SetBool(Key, false);
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= Log;
            EditorApplication.Exit(code);
        }
    }
}
