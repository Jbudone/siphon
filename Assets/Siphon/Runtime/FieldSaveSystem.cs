using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Siphon
{
    public sealed class FieldSaveSystem : MonoBehaviour
    {
        public static bool FreshWar;
        private static bool pendingLoad;
        private static bool Smoke => Array.IndexOf(Environment.GetCommandLineArgs(), "-siphon-smoke") >= 0;
        public static string SavePath => Path.Combine(Application.persistentDataPath, "field-war-v1.json");
        private float nextSave;
        [Serializable] public sealed class Snapshot
        {
            public int version = 3;
            public long utcTicks;
            public ResourceBag blue, red, cargo;
            public Vector3 playerPosition;
            public float playerHealth;
            public bool thermal, optics, engineering, finished, capacitor, carriedRelic, cloak;
            public Team winner;
            public bool siegeResearch, rallyEnemyCore;
            public bool rallyFireAllowed = true;
            public CaptainOrder blueOrder;
            public Vector3 rallyPoint;
            public float rallySeconds;
            public string rallyLabel;
            public SpawnPreference spawnPreference;
            public Vector3 selectedSpawn;
            public List<StructureState> structures = new List<StructureState>();
            public List<NodeState> nodes = new List<NodeState>();
            public List<BotBrain.SavedState> bots = new List<BotBrain.SavedState>();
            public List<DroppedCargo.SavedState> drops = new List<DroppedCargo.SavedState>();
            public WardenBrain.SavedState warden;
        }
        [Serializable] public sealed class StructureState
        { public EntityKind kind; public Team team; public Vector3 position; public float heading, health, disabledSeconds, charge; public bool built; }
        [Serializable] public sealed class NodeState
        { public Vector3 position; public ResourceKind kind; public int remaining; public float rechargeSeconds; }
        private IEnumerator Start()
        {
            yield return null;
            if (!Smoke)
            {
                if (FreshWar) { FreshWar = false; if (!Application.isEditor) Save(false); }
                else if (!Application.isEditor || pendingLoad) Load();
                pendingLoad = false;
            }
            nextSave = Time.time + 30;
        }
        private void Update()
        {
            if (Smoke) return;
            if (Input.GetKeyDown(KeyCode.F5)) Save(true);
            if (Input.GetKeyDown(KeyCode.F9) && File.Exists(SavePath))
            {
                // Reload the scene before restoring agent state; transient shots are discarded.
                pendingLoad = true;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            if (!Application.isEditor && Time.time >= nextSave) { nextSave = Time.time + 30; Save(false); }
        }
        private void OnApplicationQuit() { if (!Application.isEditor && !Smoke) Save(false); }
        public static Snapshot Capture()
        {
            var war = WarDirector.Instance; var player = UnityEngine.Object.FindAnyObjectByType<PlayerCombat>();
            if (war == null || player == null) return null;
            var snapshot = new Snapshot { utcTicks = DateTime.UtcNow.Ticks, blue = war.StockBag(Team.Blue), red = war.StockBag(Team.Red),
                cargo = player.CargoBag, playerPosition = player.transform.position, playerHealth = player.Entity.health,
                thermal = player.Gear.thermalLining, optics = player.Gear.surveyOptics, engineering = player.Gear.fieldEngineering,
                finished = war.Finished, winner = war.Winner, capacitor = player.Gear.wardenCapacitor, carriedRelic = player.CarryingRelic, cloak = player.Gear.cloakWeave, siegeResearch = player.Gear.siegeCalibration, blueOrder = war.BlueOrder,
                rallyPoint = war.RallyPoint, rallySeconds = Mathf.Max(0, war.RallyUntil - Time.time), rallyLabel = war.RallyLabel, rallyEnemyCore = war.RallyIsEnemyCore, rallyFireAllowed = war.RallyFireAllowed,
                spawnPreference = war.Spawns.Preference, selectedSpawn = war.Spawns.SelectedPosition };
            foreach (var actor in Combatant.All)
            {
                var structure = actor.GetComponent<DefenseStructure>();
                if (structure == null) continue;
                snapshot.structures.Add(new StructureState { kind = actor.kind, team = actor.team, position = actor.transform.position,
                    heading = structure.heading, health = actor.health, built = structure.playerBuilt,
                    disabledSeconds = Mathf.Max(0, structure.DisabledUntil - Time.time), charge = structure.Charge });
            }
            foreach (var node in ResourceNode.All)
                snapshot.nodes.Add(new NodeState { position = node.transform.position, kind = node.kind, remaining = node.remaining,
                    rechargeSeconds = node.RechargeSeconds });
            foreach (var bot in BotBrain.All) snapshot.bots.Add(bot.CaptureState());
            foreach (var drop in DroppedCargo.All) snapshot.drops.Add(drop.CaptureState());
            var warden = UnityEngine.Object.FindAnyObjectByType<WardenBrain>();
            if (warden != null) snapshot.warden = warden.CaptureState();
            return snapshot;
        }
        public void Save(bool announce)
        {
            try
            {
                var snapshot = Capture(); if (snapshot == null) return;
                WriteSnapshot(snapshot, SavePath);
                if (announce) WarDirector.Instance.Announce("Local war saved. F9 resumes this checkpoint; bases and equipment are preserved.");
            }
            catch (Exception error) { Debug.LogWarning("Could not save local war: " + error.Message); }
        }
        public static void WriteSnapshot(Snapshot snapshot, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path + ".tmp", JsonUtility.ToJson(snapshot, true));
            if (File.Exists(path))
            {
                bool previousValid = true;
                try { ParseSnapshot(File.ReadAllText(path)); } catch { previousValid = false; }
                // A recovered checkpoint must not replace the healthy backup with
                // the damaged file on its next autosave.
                File.Replace(path + ".tmp", path, previousValid ? path + ".bak" : null);
            }
            else File.Move(path + ".tmp", path);
        }
        public void Load()
        {
            if (!File.Exists(SavePath)) return;
            try
            {
                var snapshot = ReadSnapshot(SavePath, out bool recovered);
                if (recovered) Debug.LogWarning("Recovered local war from its previous save after the latest checkpoint was damaged.");
                Apply(snapshot, Math.Max(0, (DateTime.UtcNow.Ticks - snapshot.utcTicks) / (double)TimeSpan.TicksPerSecond));
                WarDirector.Instance.Announce("Local war resumed. Offline smelters ran from available supplies; depleted nodes recovered.", 8);
            }
            catch (Exception error) { Debug.LogWarning("Could not load local war: " + error.Message); }
        }
        public static Snapshot ReadSnapshot(string path, out bool recovered)
        {
            recovered = false;
            try { return ParseSnapshot(File.ReadAllText(path)); }
            catch (Exception) when (File.Exists(path + ".bak"))
            { var snapshot = ParseSnapshot(File.ReadAllText(path + ".bak")); recovered = true; return snapshot; }
        }
        private static Snapshot ParseSnapshot(string json)
        {
            var snapshot = JsonUtility.FromJson<Snapshot>(json);
            if (snapshot == null || (snapshot.version < 1 || snapshot.version > 3) || snapshot.utcTicks <= 0
                || snapshot.utcTicks > DateTime.MaxValue.Ticks || snapshot.structures == null || snapshot.nodes == null
                || snapshot.structures.Count > 10000 || snapshot.nodes.Count == 0 || snapshot.nodes.Count > 10000
                || !snapshot.blue.Valid || !snapshot.red.Valid || !snapshot.cargo.Valid
                || !Finite(snapshot.playerPosition) || !Finite(snapshot.playerHealth) || !Finite(snapshot.rallyPoint) || !Finite(snapshot.rallySeconds) || !Finite(snapshot.selectedSpawn)) throw new InvalidDataException("Invalid local checkpoint.");
            bool blueCore = false, redCore = false;
            foreach (var state in snapshot.structures)
            {
                if (state == null || !Finite(state.position) || !Finite(state.health) || !Finite(state.heading)
                    || !Finite(state.disabledSeconds) || !Finite(state.charge) || state.health < 0
                    || !Enum.IsDefined(typeof(EntityKind), state.kind) || (state.team != Team.Blue && state.team != Team.Red))
                    throw new InvalidDataException("Invalid saved structure.");
                if (!state.built && state.kind == EntityKind.Core)
                { blueCore |= state.team == Team.Blue; redCore |= state.team == Team.Red; }
            }
            if (!blueCore || !redCore) throw new InvalidDataException("Checkpoint is missing a team core.");
            foreach (var state in snapshot.nodes)
                if (state == null || !Finite(state.position) || !Finite(state.rechargeSeconds) || state.remaining < 0
                    || !Enum.IsDefined(typeof(ResourceKind), state.kind)) throw new InvalidDataException("Invalid saved resource node.");
            if (snapshot.bots != null)
                foreach (var state in snapshot.bots)
                    if (state == null || !Finite(state.position) || !Finite(state.home) || !Finite(state.health) || !Finite(state.reviveSeconds)
                        || !Enum.IsDefined(typeof(Team), state.team) || !Enum.IsDefined(typeof(BotRole), state.role)
                        || !Enum.IsDefined(typeof(ResourceKind), state.cargoKind)) throw new InvalidDataException("Invalid saved agent.");
            if (snapshot.drops != null)
                foreach (var state in snapshot.drops)
                    if (state == null || !Finite(state.position) || !Finite(state.seconds) || !state.contents.Valid)
                        throw new InvalidDataException("Invalid saved cargo cache.");
            if (snapshot.warden != null && (!Finite(snapshot.warden.position) || !Finite(snapshot.warden.health) || !Finite(snapshot.warden.reviveSeconds)))
                throw new InvalidDataException("Invalid saved boss.");
            return snapshot;
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);
        public static void SimulateOffline(Snapshot snapshot, double seconds)
        {
            if (snapshot.finished || seconds < 12) return;

            foreach (Team team in new[] { Team.Blue, Team.Red })
            {
                int cycles = 0;
                foreach (var structure in snapshot.structures)
                {
                    if (structure.team != team || structure.kind != EntityKind.Processor || structure.health <= 0) continue;
                    float delay = float.MaxValue;
                    foreach (var generator in snapshot.structures)
                        if (generator.team == team && generator.kind == EntityKind.Generator && generator.health > 0
                            && (generator.position - structure.position).sqrMagnitude <= 1225)
                            delay = Mathf.Min(delay, Mathf.Max(generator.disabledSeconds, structure.disabledSeconds));
                    if (delay < float.MaxValue) cycles += (int)Math.Min(Math.Max(0, seconds - delay) / 12, 3600);
                }
                ResourceBag stock = team == Team.Blue ? snapshot.blue : snapshot.red;
                int made = Math.Max(0, Math.Min(cycles, Math.Min((stock.ferrite - 24) / 4, (stock.timber - 10) / 2)));
                made = Math.Min(made, 100000 - stock.alloy);
                stock.Spend(new ResourceBag(made * 4, made * 2)); stock.Add(ResourceKind.Alloy, made);
                if (team == Team.Blue) snapshot.blue = stock; else snapshot.red = stock;
            }
        }
        public static void Apply(Snapshot snapshot, double offlineSeconds = 0)
        {
            SimulateOffline(snapshot, offlineSeconds);
            var war = WarDirector.Instance;
            war.blueResources = snapshot.blue.ferrite; war.blueTimber = snapshot.blue.timber; war.blueCrystal = snapshot.blue.crystal; war.blueAlloy = snapshot.blue.alloy;
            war.redResources = snapshot.red.ferrite; war.redTimber = snapshot.red.timber; war.redCrystal = snapshot.red.crystal; war.redAlloy = snapshot.red.alloy;
            foreach (var actor in Combatant.All.ToArray())
            {
                var structure = actor.GetComponent<DefenseStructure>();
                if (structure != null && structure.playerBuilt) { actor.gameObject.SetActive(false); Destroy(actor.gameObject); }
            }
            foreach (var saved in snapshot.structures)
            {
                if (saved.built && saved.health <= 0) continue;
                DefenseStructure match = null;
                if (!saved.built)
                    foreach (var actor in Combatant.All)
                        if (actor.team == saved.team && actor.kind == saved.kind && (actor.transform.position - saved.position).sqrMagnitude < 0.1f)
                        { match = actor.GetComponent<DefenseStructure>(); break; }
                if (match == null && saved.built) match = war.CreateStructure(saved.kind, saved.team, saved.position, saved.heading);
                if (match == null) continue;
                match.Entity.health = Mathf.Clamp(saved.health, 0, match.Entity.maximumHealth);
                foreach (var collider in match.GetComponentsInChildren<Collider>()) collider.enabled = match.Entity.Alive;
                foreach (var renderer in match.GetComponentsInChildren<Renderer>()) renderer.enabled = match.Entity.Alive;
                float disabled = Mathf.Max(0, saved.disabledSeconds - (float)offlineSeconds);
                match.GetComponent<SiegeBattery>()?.ResetFireControl();
                match.RestoreRuntime(disabled, Mathf.Min(160, saved.charge + 12 * Mathf.Max(0, (float)offlineSeconds - Mathf.Max(saved.disabledSeconds, 3))));
            }
            foreach (var saved in snapshot.nodes)
                foreach (var node in ResourceNode.All)
                    if (node.kind == saved.kind && (node.transform.position - saved.position).sqrMagnitude < 0.1f)
                    { node.ApplyRemaining(saved.remaining, Mathf.Max(0, saved.rechargeSeconds - (float)offlineSeconds)); break; }
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerCombat>();
            player.RestoreInventory(snapshot.cargo, snapshot.carriedRelic);
            player.Gear.RestoreEquipment(snapshot.capacitor); player.Gear.cloakWeave = snapshot.cloak; player.Gear.BreakCloak();
            player.Gear.siegeCalibration = snapshot.siegeResearch;
            war.RestoreCommand(snapshot.blueOrder, snapshot.rallyPoint, Mathf.Max(0, snapshot.rallySeconds - (float)offlineSeconds), snapshot.rallyLabel, snapshot.rallyEnemyCore, snapshot.rallyFireAllowed);
            war.Spawns.Restore(snapshot.spawnPreference, snapshot.selectedSpawn);
            player.Gear.thermalLining = snapshot.thermal; player.Gear.surveyOptics = snapshot.optics; player.Gear.fieldEngineering = snapshot.engineering;
            player.Entity.health = snapshot.playerHealth > 0 ? Mathf.Clamp(snapshot.playerHealth, 1, player.Entity.maximumHealth) : player.Entity.maximumHealth;
            Vector3 position = snapshot.playerHealth > 0 ? snapshot.playerPosition : war.RespawnPoint(Team.Blue, true);
            if (AssaultLayout.Default.Outside(position, AssaultLayout.Default.SaveInset) || float.IsNaN(position.y)) position = war.blueSpawn;
            position.y = Mathf.Max(position.y, WarDirector.GroundPoint(position).y + 0.2f);
            player.GetComponent<PlaygroundPlayer>().ResetTo(position, Quaternion.identity);
            if (snapshot.bots != null)
                foreach (var saved in snapshot.bots)
                    foreach (var bot in BotBrain.All)
                        if (bot.Entity.team == saved.team && (bot.role == saved.role || (saved.team == Team.Wild && (saved.role == BotRole.Creature || saved.role == BotRole.Stalker))) && (bot.home - saved.home).sqrMagnitude < 0.1f)
                        { bot.ApplyState(saved, offlineSeconds); break; }
            var warden = UnityEngine.Object.FindAnyObjectByType<WardenBrain>();
            if (warden != null && snapshot.warden != null) warden.ApplyState(snapshot.warden, offlineSeconds);
            foreach (var drop in DroppedCargo.All.ToArray()) { drop.gameObject.SetActive(false); Destroy(drop.gameObject); }
            if (snapshot.drops != null)
                foreach (var saved in snapshot.drops)
                {
                    float lifetime = saved.seconds - (float)offlineSeconds;
                    if (lifetime <= 0) continue;
                    var drop = DroppedCargo.Spawn(saved.position, saved.contents, saved.relic); drop.expiresAt = Time.time + lifetime;
                }
            war.RestoreOutcome(snapshot.finished, snapshot.winner);
            NavigationWorld.Instance?.Invalidate();
        }
    }
}
