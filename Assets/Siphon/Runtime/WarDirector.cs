using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Siphon
{
    public enum CaptainOrder { Balanced, Assault, Defend, Escort }
    public sealed class WarDirector : MonoBehaviour
    {
        public static WarDirector Instance { get; private set; }
        // Empty in old scenes: resolve to the accepted default without scene-global caching.
        public string layoutId;
        public AssaultLayout Layout => AssaultLayout.Resolve(layoutId);
        public Combatant blueCore;
        public Combatant redCore;
        public Vector3 blueSpawn;
        public Vector3 redSpawn;
        public Material blueMaterial;
        public Material redMaterial;
        public Material darkMaterial;
        public Material wildMaterial;
        public int blueResources = 100;
        public int redResources = 100;
        public int blueTimber = 50, redTimber = 50;
        public int blueCrystal, redCrystal, blueAlloy, redAlloy;
        public bool spawnBots = true;
        public bool Finished { get; private set; }
        public Team Winner { get; private set; }
        public CaptainOrder BlueOrder { get; private set; }
        public RespawnNetwork Spawns { get; private set; }
        public Vector3 RallyPoint { get; private set; }
        public string RallyLabel { get; private set; } = "";
        public float RallyUntil { get; private set; }
        public bool RallyActive => !Finished && Time.time < RallyUntil;
        public bool RallyFireAllowed { get; private set; } = true;
        private bool rallyEnemyCore;
        public string Message { get; private set; } = "Destroy Red power and its core. Harvest supplies; V builds defenses and T crafts equipment.";
        public float MessageUntil { get; private set; } = 12;
        private float nextOrders;
        public struct BattleEvent { public string text; public Team team; public float expires; }
        public readonly List<BattleEvent> BattleEvents = new List<BattleEvent>();
        public struct SensorContact { public Combatant id; public Team observer; public Vector3 position; public EntityKind kind; public float expires; }
        public readonly List<SensorContact> SensorContacts = new List<SensorContact>();
        private readonly List<NoiseContact> contacts = new List<NoiseContact>();
        public struct NoiseContact { public Vector3 position; public Team team; public float range; public float expires; }
        private void Awake() { Instance = this; Spawns = GetComponent<RespawnNetwork>() ?? gameObject.AddComponent<RespawnNetwork>(); }
        private void OnDestroy() { if (Instance == this) Instance = null; }
        private void Start()
        {
            if (!spawnBots) return;
            SpawnSquad(Team.Blue, blueSpawn);
            SpawnSquad(Team.Red, redSpawn);
            for (int i = 0; i < 3; i++)
                SpawnBot(Team.Wild, BotRole.Creature, GroundPoint(new Vector3(160 + i * 10, 0, 30 + i * 15)));
            for (int i = 0; i < 2; i++)
                SpawnBot(Team.Wild, BotRole.Stalker, GroundPoint(new Vector3(-220 + i * 15, 0, -35 + i * 20)));
        }
        private void Update()
        {
            contacts.RemoveAll(contact => Time.time > contact.expires);
            SensorContacts.RemoveAll(contact => Time.time > contact.expires);
            BattleEvents.RemoveAll(item => Time.time > item.expires);
            if (Finished) return;
            if (Time.time < nextOrders) return;
            nextOrders = Time.time + 2;
            foreach (var bot in BotBrain.All)
                if (bot != null && bot.Entity.Alive) bot.AssignObjective();
        }
        private void SpawnSquad(Team team, Vector3 origin)
        {
            BotRole[] roles = { BotRole.Worker, BotRole.Worker, BotRole.Guard, BotRole.Scout, BotRole.Heavy, BotRole.Raider, BotRole.Raider };
            for (int i = 0; i < roles.Length; i++)
            {
                var bot = SpawnBot(team, roles[i], GroundPoint(origin + new Vector3((i - 3) * 4, 0, team == Team.Blue ? 24 : -24)));
                if (roles[i] == BotRole.Raider) bot.flank = i == 5 ? -1 : 1;
            }
        }
        public BotBrain SpawnBot(Team team, BotRole role, Vector3 position)
        {
            var root = new GameObject(team + " " + role);
            root.transform.position = position + Vector3.up * 0.1f;
            root.layer = 8;
            var controller = root.AddComponent<CharacterController>();
            controller.height = role == BotRole.Stalker ? 1.3f : role == BotRole.Heavy ? 2.5f : 2;
            controller.radius = 0.5f;
            controller.center = Vector3.up * controller.height * 0.5f;
            controller.minMoveDistance = 0;
            controller.stepOffset = 0.4f;
            var actor = root.AddComponent<Combatant>();
            actor.team = team;
            actor.kind = team == Team.Wild ? EntityKind.Creature : EntityKind.Bot;
            actor.maximumHealth = actor.health = role == BotRole.Heavy ? 130 : role == BotRole.Creature ? 90 : 80;
            actor.aimOffset = Vector3.up * (role == BotRole.Stalker ? 0.9f : 1.2f);
            Material material = team == Team.Blue ? blueMaterial : team == Team.Red ? redMaterial : wildMaterial;
            if (role == BotRole.Stalker)
            {
                Part(root.transform, PrimitiveType.Sphere, "Stalker body", new Vector3(0, 0.65f, 0), new Vector3(1.1f, 0.75f, 1.7f), material, false);
                Part(root.transform, PrimitiveType.Sphere, "Stalker head", new Vector3(0, 0.9f, 0.8f), new Vector3(0.7f, 0.55f, 0.65f), darkMaterial, false);
                for (int side = -1; side <= 1; side += 2)
                {
                    for (int end = -1; end <= 1; end += 2)
                        Part(root.transform, PrimitiveType.Cube, "Stalker leg", new Vector3(side * 0.55f, 0.3f, end * 0.5f), new Vector3(0.2f, 0.6f, 0.35f), darkMaterial, false);
                    Part(root.transform, PrimitiveType.Sphere, "Stalker eye", new Vector3(side * 0.22f, 1, 1.1f), Vector3.one * 0.14f, material, false);
                }
            }
            else
            {
            var visual = Part(root.transform, PrimitiveType.Capsule, "Armor", Vector3.up, new Vector3(0.85f, 0.85f, 0.85f), material, false);
            if (role == BotRole.Heavy) visual.localScale = new Vector3(1.3f, 1.1f, 1.3f);
            Part(root.transform, PrimitiveType.Cube, "Visor", new Vector3(0, 1.6f, 0.4f), new Vector3(0.55f, 0.18f, 0.15f), darkMaterial, false);
            Part(root.transform, PrimitiveType.Cube, "Weapon", new Vector3(0.5f, 1.2f, 0.5f),
                new Vector3(0.2f, 0.2f, role == BotRole.Heavy ? 1.5f : 0.7f), darkMaterial, false);
            if (role == BotRole.Worker)
                Part(root.transform, PrimitiveType.Cube, "Supply pack", new Vector3(0, 1.1f, -0.5f), new Vector3(0.8f, 1, 0.4f), darkMaterial, false);
            else if (role == BotRole.Scout)
            {
                Part(root.transform, PrimitiveType.Cylinder, "Sensor antenna", new Vector3(-0.3f, 2.1f, -0.2f), new Vector3(0.08f, 0.4f, 0.08f), material, false);
                Part(root.transform, PrimitiveType.Cube, "Scout fins", new Vector3(0, 1.2f, -0.4f), new Vector3(1.3f, 0.25f, 0.2f), darkMaterial, false);
            }
            else if (role == BotRole.Heavy)
            {
                Part(root.transform, PrimitiveType.Cube, "Left shoulder armor", new Vector3(-0.8f, 1.7f, 0), new Vector3(0.6f, 0.7f, 0.7f), material, false);
                Part(root.transform, PrimitiveType.Cube, "Right shoulder armor", new Vector3(0.8f, 1.7f, 0), new Vector3(0.6f, 0.7f, 0.7f), material, false);
            }
            else if (role == BotRole.Guard)
                Part(root.transform, PrimitiveType.Cube, "Guard breastplate", new Vector3(0, 1.1f, 0.4f), new Vector3(0.75f, 0.6f, 0.18f), darkMaterial, false);
            }
            var brain = root.AddComponent<BotBrain>();
            brain.role = role;
            brain.home = position;
            if (role == BotRole.Stalker) root.AddComponent<StalkerBehaviour>();
            return brain;
        }

        public int Stock(Team team, ResourceKind kind = ResourceKind.Ferrite)
        {
            if (team == Team.Wild) return 0;
            bool blue = team == Team.Blue;
            return kind == ResourceKind.Ferrite ? (blue ? blueResources : redResources)
                : kind == ResourceKind.Timber ? (blue ? blueTimber : redTimber)
                : kind == ResourceKind.Crystal ? (blue ? blueCrystal : redCrystal) : (blue ? blueAlloy : redAlloy);
        }
        public ResourceBag StockBag(Team team) => new ResourceBag(Stock(team), Stock(team, ResourceKind.Timber), Stock(team, ResourceKind.Crystal), Stock(team, ResourceKind.Alloy));
        public bool Spend(Team team, ResourceBag cost)
        {
            if (team == Team.Wild || !StockBag(team).Covers(cost)) return false;
            SetStock(team, ResourceKind.Ferrite, Stock(team) - cost.ferrite);
            SetStock(team, ResourceKind.Timber, Stock(team, ResourceKind.Timber) - cost.timber);
            SetStock(team, ResourceKind.Crystal, Stock(team, ResourceKind.Crystal) - cost.crystal);
            SetStock(team, ResourceKind.Alloy, Stock(team, ResourceKind.Alloy) - cost.alloy);
            return true;
        }
        public bool Spend(Team team, int amount)
        {
            return Spend(team, new ResourceBag(amount));
        }
        public void Deposit(Team team, int amount, ResourceKind kind = ResourceKind.Ferrite)
        {
            if (amount <= 0) return;
            SetStock(team, kind, Mathf.Min(100000, Stock(team, kind) + amount));
        }
        public void Deposit(Team team, ResourceBag amount)
        { foreach (ResourceKind kind in System.Enum.GetValues(typeof(ResourceKind))) Deposit(team, amount.Count(kind), kind); }
        private void SetStock(Team team, ResourceKind kind, int amount)
        {
            if (team == Team.Blue)
            { switch (kind) { case ResourceKind.Ferrite: blueResources = amount; break; case ResourceKind.Timber: blueTimber = amount; break; case ResourceKind.Crystal: blueCrystal = amount; break; case ResourceKind.Alloy: blueAlloy = amount; break; } }
            else if (team == Team.Red)
            { switch (kind) { case ResourceKind.Ferrite: redResources = amount; break; case ResourceKind.Timber: redTimber = amount; break; case ResourceKind.Crystal: redCrystal = amount; break; case ResourceKind.Alloy: redAlloy = amount; break; } }
        }
        public bool IsPowered(Team team, Vector3 position, float range)
        {
            foreach (var entity in Combatant.All)
                if (entity.Alive && entity.team == team && entity.kind == EntityKind.Generator
                    && (entity.transform.position - position).sqrMagnitude <= range * range
                    && (entity.GetComponent<DefenseStructure>() == null || entity.GetComponent<DefenseStructure>().Operational)) return true;
            return false;
        }
        public Combatant Core(Team team) => team == Team.Blue ? blueCore : redCore;
        public Vector3 RespawnPoint(Team team, bool player = false) => Spawns.Resolve(team, player);
        public void EntityKilled(Combatant entity)
        {
            string victim = ActorName(entity);
            string attacker = entity.LastAttacker != null ? ActorName(entity.LastAttacker) : entity.LastDamageTeam == Team.Wild ? "WILDERNESS" : entity.LastDamageTeam.ToString().ToUpperInvariant();
            BattleEvents.Add(new BattleEvent { text = attacker + "  >  " + victim, team = entity.LastDamageTeam, expires = Time.time + 5 });
            if (BattleEvents.Count > 4) BattleEvents.RemoveAt(0);
            if (entity.kind == EntityKind.Core)
            {
                Finished = true;
                Winner = entity.team == Team.Blue ? Team.Red : Team.Blue;
                Announce(Winner.ToString().ToUpperInvariant() + " WINS — enemy core destroyed. Press Enter to restart.", 9999);
            }
            else if (entity.kind == EntityKind.Generator)
                Announce(entity.team.ToString().ToUpperInvariant() + " generator destroyed. Nearby defenses and core shielding are offline.", 8);
            else if (entity.kind == EntityKind.Outpost)
                Announce(entity.team.ToString().ToUpperInvariant() + " forward spawn destroyed.", 6);
        }
        private static string ActorName(Combatant entity)
        {
            if (entity.kind == EntityKind.Player) return "YOU";
            if (entity.GetComponent<WardenBrain>() != null) return "WARDEN";
            var bot = entity.GetComponent<BotBrain>();
            return entity.team.ToString().ToUpperInvariant() + " " + (bot != null ? bot.role.ToString().ToUpperInvariant() : StructureCatalog.Name(entity.kind).ToUpperInvariant());
        }
        public void Announce(string message, float duration = 4)
        {
            Message = message;
            MessageUntil = Time.time + duration;
        }
        public void Noise(Vector3 position, Team source, float range)
        {
            // Merge repeated emissions so automatic fire does not grow an unbounded contact list.
            for (int i = 0; i < contacts.Count; i++)
            {
                if (contacts[i].team != source || (contacts[i].position - position).sqrMagnitude > 100) continue;
                contacts[i] = new NoiseContact { position = position, team = source, range = range, expires = Time.time + 7 };
                return;
            }
            contacts.Add(new NoiseContact { position = position, team = source, range = range, expires = Time.time + 7 });
        }
        public bool Hear(Team team, Vector3 position, out Vector3 contact)
        {
            for (int i = contacts.Count - 1; i >= 0; i--)
                if (contacts[i].team != team && (contacts[i].position - position).sqrMagnitude < contacts[i].range * contacts[i].range)
                { contact = contacts[i].position; return true; }
            contact = default;
            return false;
        }
        public void ReportContact(Team observer, Combatant actor, float memory = 5)
        {
            var contact = new SensorContact { id = actor, observer = observer, position = actor.transform.position,
                kind = actor.kind, expires = Time.time + memory };
            for (int i = 0; i < SensorContacts.Count; i++)
                if (SensorContacts[i].id == contact.id && SensorContacts[i].observer == observer)
                { SensorContacts[i] = contact; return; }
            SensorContacts.Add(contact);
        }
        public Vector3 AssaultPoint(Team team, bool seekOutposts = true)
        {
            var core = Core(team == Team.Blue ? Team.Red : Team.Blue);
            if (team == Team.Blue && RallyActive && !(rallyEnemyCore && core.Shielded)) return RallyPoint;
            if (!core.Shielded) return core.transform.position;
            if (seekOutposts)
            {
                Vector3 from = team == Team.Blue ? blueSpawn : redSpawn;
                float nearest = float.MaxValue; Vector3 point = default;
                foreach (var contact in SensorContacts)
                    if (contact.observer == team && contact.kind == EntityKind.Outpost && contact.expires > Time.time)
                    {
                        float distance = (contact.position - from).sqrMagnitude;
                        if (distance < nearest) { nearest = distance; point = contact.position; }
                    }
                if (nearest < float.MaxValue) return point;
            }
            foreach (var actor in Combatant.All)
            {
                if (actor.team == team || actor.team == Team.Wild || actor.kind != EntityKind.Generator || !actor.Alive
                    || (actor.transform.position - core.transform.position).sqrMagnitude > 1600) continue;
                var defense = actor.GetComponent<DefenseStructure>();
                // Fixed main-base power is a known strategic installation. Built power
                // must have been sighted before it can become an assault destination.
                if (defense == null || !defense.Operational) continue;
                if (!defense.playerBuilt) return actor.transform.position;
                foreach (var contact in SensorContacts)
                    if (contact.observer == team && contact.id == actor && contact.expires > Time.time) return contact.position;
            }
            return core.transform.position;
        }
        public Vector3 GuardPoint(Team team)
        {
            Vector3 core = Core(team).transform.position;
            foreach (var contact in SensorContacts)
                if (contact.observer == team && contact.expires > Time.time && (contact.position - core).sqrMagnitude < 6400) return core;
            foreach (var actor in Combatant.All)
            {
                if (!actor.Alive || actor.team != team || actor.kind != EntityKind.Outpost) continue;
                if (Time.time - actor.LastDamagedAt < 8) return actor.transform.position;
                foreach (var contact in SensorContacts)
                    if (contact.observer == team && contact.expires > Time.time && (contact.position - actor.transform.position).sqrMagnitude < 2500)
                        return actor.transform.position;
            }
            return core;
        }
        public bool TryContact(Team observer, Vector3 from, float range, out Vector3 point)
        {
            point = default; float score = float.MaxValue;
            foreach (var contact in SensorContacts)
            {
                if (contact.observer != observer || contact.expires <= Time.time || (contact.kind != EntityKind.Player && contact.kind != EntityKind.Bot
                    && contact.kind != EntityKind.Outpost)) continue;
                float distance = (contact.position - from).sqrMagnitude;
                if (distance > range * range || distance >= score) continue;
                score = distance; point = contact.position;
            }
            return score < float.MaxValue;
        }
        public void RestoreOutcome(bool finished, Team winner) { Finished = finished; Winner = winner; }
        public bool MarkTarget(Vector3 point, Combatant seen = null)
        {
            if (Finished || !Finite(point) || Layout.Outside(point, Layout.RecoveryInset)) return false;
            RallyPoint = point; RallyUntil = Time.time + 45;
            rallyEnemyCore = seen != null && seen.team == Team.Red && seen.kind == EntityKind.Core;
            RallyLabel = seen != null ? ActorName(seen) : "FIELD POSITION";
            RallyFireAllowed = seen == null || seen.team != Team.Blue;
            if (seen != null && seen.team != Team.Blue) ReportContact(Team.Blue, seen);
            BlueOrder = CaptainOrder.Assault;
            foreach (var bot in BotBrain.All) if (bot.Entity.team == Team.Blue && bot.Entity.Alive) bot.AssignObjective();
            Announce("Marked " + RallyLabel + (RallyFireAllowed ? ". Assault squad committed; siege batteries will aim at this point." : ". Squad rally set; siege batteries hold fire."), 5);
            return true;
        }
        public void RestoreCommand(CaptainOrder order, Vector3 point, float seconds, string label, bool enemyCore, bool fireAllowed = true)
        {
            BlueOrder = System.Enum.IsDefined(typeof(CaptainOrder), order) ? order : CaptainOrder.Balanced;
            RallyPoint = Finite(point) ? point : blueSpawn; RallyUntil = Time.time + Mathf.Clamp(seconds, 0, 45);
            RallyLabel = label ?? "FIELD POSITION"; rallyEnemyCore = enemyCore;
            RallyFireAllowed = fireAllowed;
        }
        public bool RallyIsEnemyCore => rallyEnemyCore;
        public Vector3 SiegeAimPoint => rallyEnemyCore && redCore != null && redCore.Shielded ? AssaultPoint(Team.Blue, false) + Vector3.up * 1.3f : RallyPoint;
        private static bool Finite(Vector3 value) => !float.IsNaN(value.x) && !float.IsNaN(value.y) && !float.IsNaN(value.z)
            && !float.IsInfinity(value.x) && !float.IsInfinity(value.y) && !float.IsInfinity(value.z);
        public void Restart() { FieldSaveSystem.FreshWar = true; ReloadActiveScene(); }
        public static void ReloadActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
#if UNITY_EDITOR
            // Opt-in scenes need not be listed in the default player build settings.
            if (Application.isEditor)
            {
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scene.path,
                    new LoadSceneParameters(LoadSceneMode.Single));
                return;
            }
#endif
            SceneManager.LoadScene(scene.buildIndex);
        }
        public void CycleOrder()
        {
            BlueOrder = (CaptainOrder)(((int)BlueOrder + 1) % 4);
            foreach (var bot in BotBrain.All) if (bot.Entity.team == Team.Blue && bot.Entity.Alive) bot.AssignObjective();
            Announce("Blue captain: " + BlueOrder + ". Workers and scouts continue their support jobs.", 5);
        }

        public static Vector3 GroundPoint(Vector3 point)
        {
            if (Physics.Raycast(new Vector3(point.x, 150, point.z), Vector3.down, out RaycastHit hit, 300,
                ~(1 << 8), QueryTriggerInteraction.Ignore)) return hit.point;
            return point;
        }
        public bool TryBuildSite(Vector3 desired, out Vector3 point)
        {
            for (int radius = 0; radius <= 32; radius += 4)
                for (int side = 0; side < (radius == 0 ? 1 : 12); side++)
                {
                    float angle = side * Mathf.PI / 6;
                    Vector3 candidate = desired + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                    if (!Physics.Raycast(candidate + Vector3.up * 80, Vector3.down, out RaycastHit ground, 160, 1 << 9)) continue;
                    candidate = ground.point;
                    bool valid = true;
                    foreach (var offset in new[] { new Vector3(-2, 0, -3), new Vector3(-2, 0, 3), new Vector3(10, 0, -3), new Vector3(10, 0, 3) })
                        if (!Physics.Raycast(candidate + offset + Vector3.up * 3, Vector3.down, out RaycastHit support, 6, 1 << 9)
                            || Mathf.Abs(support.point.y - candidate.y) > 0.7f) { valid = false; break; }
                    if (!valid) continue;
                    if (Physics.CheckBox(candidate + new Vector3(3.5f, 2, 0), new Vector3(6.5f, 1.8f, 3), Quaternion.identity,
                        ~((1 << 8) | (1 << 9)), QueryTriggerInteraction.Ignore)) continue;
                    point = candidate; return true;
                }
            point = default; return false;
        }
        public DefenseStructure CreateStructure(EntityKind kind, Team team, Vector3 point, float heading, bool built = true)
        {
            var root = new GameObject(team + " " + kind);
            root.transform.SetPositionAndRotation(point, Quaternion.Euler(0, heading, 0));
            var actor = root.AddComponent<Combatant>();
            actor.kind = kind; actor.team = team;
            actor.maximumHealth = actor.health = StructureCatalog.Health(kind);
            actor.aimOffset = Vector3.up * (kind == EntityKind.Core ? 3 : kind == EntityKind.Turret ? 2.5f : 1.3f);
            var barrel = StructureCatalog.Visuals(root.transform, kind, team == Team.Blue ? blueMaterial : redMaterial, darkMaterial, true);
            var structure = root.AddComponent<DefenseStructure>();
            structure.heading = heading; structure.barrel = barrel; structure.playerBuilt = built;
            if (kind == EntityKind.SiegeCannon) root.AddComponent<SiegeBattery>();
            NavigationWorld.Instance?.Invalidate();
            return structure;
        }

        public static Transform Part(Transform parent, PrimitiveType type, string name, Vector3 position,
            Vector3 scale, Material material, bool collision = true)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            part.layer = parent.gameObject.layer;
            if (!collision)
            {
                var collider = part.GetComponent<Collider>();
                collider.enabled = false;
                if (Application.isPlaying) Destroy(collider); else DestroyImmediate(collider);
            }
            return part.transform;
        }
    }
}
