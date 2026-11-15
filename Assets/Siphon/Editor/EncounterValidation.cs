using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Siphon.Editor
{
    public static class EncounterValidation
    {
        private const string Key = "Siphon.EncounterValidation";
        private static double began;
        private static bool checkedRules, failed, workerRetreated;
        private static BotBrain raider, worker, scout;
        private static DefenseStructure forwardSpawn;
        private static WardenBrain liveWarden;
        private static Combatant bossTarget;
        private static bool telegraphed, ventChecked, orphanChecked;
        private static PlayerCombat player;
        private static float peakHeight;
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
                if (EditorApplication.timeSinceStartup - began > 60) throw new Exception("Encounter validation timeout");
                if (!EditorApplication.isPlaying || Time.time < 0.3f) return;
                if (!checkedRules) { Rules(); checkedRules = true; }
                workerRetreated |= worker.CombatState.Contains("Retreat") || worker.CombatState.Contains("safety");
                if (!orphanChecked && Time.time > 1)
                { Require(bossTarget.health == 9999, "Projectile still deals damage after its firing structure is destroyed"); orphanChecked = true; }
                if (liveWarden.Telegraphing) Require(liveWarden.Phase.Contains("barrage") || liveWarden.Phase.Contains("Airburst"), "Attack warning describes the current telegraph immediately");
                telegraphed |= liveWarden.Telegraphing && liveWarden.Barrages == 0;
                if (!ventChecked && liveWarden.Venting)
                {
                    var entity = liveWarden.GetComponent<Combatant>(); float before = entity.health;
                    entity.Damage(40, Team.Blue, entity.AimPoint + liveWarden.transform.forward * 10, player.Entity);
                    Require(entity.health == before - 40, "Venting removes frontal armor during a live barrage"); ventChecked = true;
                }
                peakHeight = Mathf.Max(peakHeight, raider.transform.position.y - WarDirector.GroundPoint(raider.transform.position).y);
                if (Time.time < 16) return;
                Require(!failed, "No runtime errors");
                Debug.Log($"Encounter evidence: raider boosts {raider.Motor.Boosts}, shots {raider.Shots}, peak height {peakHeight:0.0}m, player health {player.Entity.health:0}, worker retreated {workerRetreated}.");
                Require(raider.Motor.Boosts >= 1 && peakHeight > 3, "Raider uses jets during combat");
                Require(raider.Shots > 0 && player.Entity.health < 100000, "Mobile raider aims and lands a projectile hit");
                Require(workerRetreated, "Worker retreats from a visible hostile");
                Require(forwardSpawn.Powered, "Scout restores the generator of an unpowered forward spawn");
                Debug.Log($"Boss evidence: {liveWarden.Barrages} barrages, warning {telegraphed}, exposed reactor {ventChecked}, target health {bossTarget.health:0}.");
                Require(telegraphed && ventChecked && liveWarden.Barrages >= 2 && bossTarget.health < 9999, "Live Warden warns, alternates volleys, vents and lands an attack");
                Require(player.GetComponent<PlaygroundPlayer>().JetCapacityMultiplier == 1, "Default movement retains its original fuel capacity");
                Debug.Log("Siphon encounter validation passed: cloak visibility/radar/collision, signature break, boss armor, agent/loot/death-timer persistence, capacitor pickup/deposit, airborne AI fire, worker retreat.");
                Finish(0);
            }
            catch (Exception error) { Debug.LogException(error); Finish(1); }
        }
        private static void Rules()
        {
            var war = WarDirector.Instance; player = UnityEngine.Object.FindAnyObjectByType<PlayerCombat>();
            Require(war != null && player != null, "Scene references");
            var original = FieldSaveSystem.Capture();
            var motor = player.GetComponent<PlaygroundPlayer>();
            Vector3 arena = WarDirector.GroundPoint(new Vector3(-240, 0, -230)) + Vector3.up * 0.15f;
            motor.ResetTo(arena, Quaternion.identity);
            player.Gear.cloakWeave = true;
            Require(player.Gear.ToggleCloak(), "Crafted cloak can activate");
            Require(!player.Gear.VisibleFrom(arena + Vector3.forward * 25) && player.Gear.VisibleFrom(arena + Vector3.forward * 5), "Cloak conceals at range but permits close detection");
            Vector3 rayFrom = player.Entity.AimPoint + Vector3.forward * 8;
            Require(CombatPhysics.Cast(rayFrom, Vector3.back, 10, Team.Red, null, out RaycastHit hit)
                && hit.collider.GetComponentInParent<Combatant>() == player.Entity, "Cloak does not disable damage collision");
            war.ReportContact(Team.Red, player.Entity);
            Require(war.TryContact(Team.Red, arena, 40, out Vector3 contact) && contact == player.transform.position, "Radar can report a cloaked target");
            player.Fire(); Require(!player.Gear.Cloaked && !player.Gear.ToggleCloak(), "Firing breaks cloak and imposes a reveal period");
            var warden = UnityEngine.Object.FindAnyObjectByType<WardenBrain>(); var boss = warden.GetComponent<Combatant>();
            Vector3 front = boss.AimPoint + warden.transform.forward * 10, back = boss.AimPoint - warden.transform.forward * 10;
            Require(warden.DamageMultiplier(front) == 0.25f && warden.DamageMultiplier(back) == 1, "Warden frontal armor rewards flanking");
            float health = boss.health; boss.Damage(40, Team.Blue, front, player.Entity);
            Require(boss.health == health - 10, "Frontal armor reduces actual incoming damage");
            boss.Damage(40, Team.Blue, back, player.Entity); Require(boss.health == health - 50, "Rear damage reaches the reactor");
            boss.Damage(10000, Team.Blue, back, player.Entity);
            Require(!boss.Alive && DroppedCargo.All.Exists(drop => drop.relic), "Warden drops its unique cache");
            Require(player.Kills > 0 && player.KillUntil > Time.time, "Projectile and pulse damage credit the attacker");
            var deadWorld = FieldSaveSystem.Capture();
            Require(deadWorld.version >= 2 && deadWorld.bots.Count == 19 && deadWorld.warden.health == 0 && deadWorld.drops.Count > 0, "Snapshot includes agents and boss loot");
            var savedBot = deadWorld.bots[0]; savedBot.cargo = 7; savedBot.cargoKind = ResourceKind.Timber;
            FieldSaveSystem.Apply(Clone(deadWorld));
            Require(!boss.Alive && DroppedCargo.All.Exists(drop => drop.relic), "Reload preserves a dead boss and unclaimed loot");
            Require(BotBrain.All[0].Carried == 7, "Reload restores worker cargo");
            FieldSaveSystem.Apply(Clone(deadWorld), 181);
            Require(boss.Alive && DroppedCargo.All.Count == 0, "Offline timers respawn the boss and expire old loot");
            Require(war.redCore.health == original.structures.Find(state => state.kind == EntityKind.Core && state.team == Team.Red).health, "Offline catch-up does not attack cores");
            FieldSaveSystem.Apply(Clone(original));
            var cache = DroppedCargo.Spawn(war.blueCore.transform.position + new Vector3(7, 0, 6), new ResourceBag(crystal: 8, alloy: 4), true);
            motor.ResetTo(war.blueCore.transform.position + new Vector3(7, 1, 9), Quaternion.identity);
            motor.view.transform.LookAt(cache.transform.position); Physics.SyncTransforms(); player.Interact();
            Require(player.CarryingRelic && !player.Gear.wardenCapacitor && player.Cargo == 12, "Unique module must be carried home before equipping");
            var carried = FieldSaveSystem.Capture(); FieldSaveSystem.Apply(Clone(carried));
            Require(player.CarryingRelic && !player.Gear.wardenCapacitor, "Save preserves a carried, unequipped module");
            motor.ResetTo(war.blueCore.transform.position + new Vector3(6, 1, 2), Quaternion.identity);
            motor.view.transform.LookAt(war.blueCore.AimPoint); Physics.SyncTransforms(); player.Interact();
            Require(player.Gear.wardenCapacitor && !player.CarryingRelic && player.Cargo == 0 && motor.JetCapacityMultiplier == 1.25f, "Deposit equips the capacitor and banks materials");
            var equipped = FieldSaveSystem.Capture(); FieldSaveSystem.Apply(Clone(equipped));
            Require(player.Gear.wardenCapacitor && motor.JetCapacityMultiplier == 1.25f, "Save preserves equipped boss gear");
            FieldSaveSystem.Apply(Clone(original));
            foreach (var bot in BotBrain.All.ToArray()) bot.enabled = false;
            motor.ResetTo(arena, Quaternion.identity); player.Entity.maximumHealth = player.Entity.health = 100000;
            raider = war.SpawnBot(Team.Red, BotRole.Raider, WarDirector.GroundPoint(arena + Vector3.forward * 22));
            worker = war.SpawnBot(Team.Blue, BotRole.Worker, WarDirector.GroundPoint(arena + Vector3.right * 24));
            Vector3 outpostPoint = WarDirector.GroundPoint(new Vector3(-45, 0, -85));
            forwardSpawn = war.CreateStructure(EntityKind.Outpost, Team.Blue, outpostPoint, 0);
            scout = war.SpawnBot(Team.Blue, BotRole.Scout, outpostPoint + Vector3.right * 5);
            scout.AssignObjective(); Require(scout.Task.Contains("Restoring"), "Scout prioritizes lost outpost power");
            liveWarden = warden;
            var targetBot = war.SpawnBot(Team.Blue, BotRole.Guard, WarDirector.GroundPoint(warden.home + Vector3.right * 25));
            targetBot.enabled = false; bossTarget = targetBot.Entity; bossTarget.maximumHealth = bossTarget.health = 10000;
            var source = new GameObject("Destroyed projectile owner").AddComponent<Combatant>(); source.team = Team.Red;
            CombatProjectile.Launch(bossTarget.AimPoint + Vector3.back * 4, Vector3.forward * 20, Team.Red, 1, owner: source.transform);
            UnityEngine.Object.Destroy(source.gameObject);
            Physics.SyncTransforms();
        }
        private static FieldSaveSystem.Snapshot Clone(FieldSaveSystem.Snapshot value) => JsonUtility.FromJson<FieldSaveSystem.Snapshot>(JsonUtility.ToJson(value));
        private static void Require(bool value, string reason) { if (!value) throw new Exception("Encounter validation failed: " + reason); }
        private static void Finish(int code)
        {
            SessionState.SetBool(Key, false); EditorApplication.update -= Tick; Application.logMessageReceived -= Log; EditorApplication.Exit(code);
        }
    }
}
