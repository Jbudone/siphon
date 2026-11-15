using System.Collections.Generic;
using UnityEngine;

namespace Siphon
{
    public enum BotRole { Worker, Scout, Guard, Heavy, Raider, Creature, Stalker }
    [RequireComponent(typeof(Combatant), typeof(CharacterController))]
    public sealed class BotBrain : MonoBehaviour
    {
        public static readonly List<BotBrain> All = new List<BotBrain>();
        public BotRole role;
        public Vector3 home;
        public int flank;
        private bool crossedMidfield;
        public string Task { get; private set; } = "Deploying";
        public Combatant Entity { get; private set; }
        private CharacterController body;
        private Combatant target;
        private StalkerBehaviour stalker;
        private Combatant repair;
        private Combatant unpoweredOutpost;
        private DefenseStructure rebuild;
        private ResourceNode mine;
        private Vector3 destination;
        private Vector3 investigation;
        private float investigateUntil;
        private float nextSense;
        private float nextShot;
        private float nextWork;
        private float revivesAt;
        private int cargo;
        private ResourceKind cargoKind;
        private bool establishedOutpost;
        private Renderer[] visuals;
        private Transform weapon;
        public BotLocomotion Motor { get; private set; }
        public string CombatState { get; private set; } = "Moving";
        private Vector3 lastThreat;
        private float threatUntil, acquiredAt;
        public int Deliveries { get; private set; }
        public int Carried => cargo;
        public int Shots { get; private set; }
        public Vector3 Destination => destination;
        [System.Serializable] public sealed class SavedState
        {
            public Team team; public BotRole role; public Vector3 home, position;
            public float health, reviveSeconds; public int cargo, deliveries; public ResourceKind cargoKind;
        }
        public SavedState CaptureState() => new SavedState { team = Entity.team, role = role, home = home, position = transform.position,
            health = Entity.health, reviveSeconds = Entity.Alive ? 0 : Mathf.Max(0, revivesAt - Time.time), cargo = cargo, cargoKind = cargoKind, deliveries = Deliveries };
        public void ApplyState(SavedState saved, double offlineSeconds)
        {
            body.enabled = false; Entity.health = Mathf.Clamp(saved.health, 0, Entity.maximumHealth);
            bool revived = !Entity.Alive && offlineSeconds >= saved.reviveSeconds;
            if (revived) Entity.Restore();
            Vector3 point = revived ? (Entity.team == Team.Wild ? home : WarDirector.Instance.RespawnPoint(Entity.team)) : saved.position;
            if (!Finite(point) || Mathf.Abs(point.x) > 295 || Mathf.Abs(point.z) > 295) point = home;
            point.y = Mathf.Max(point.y, WarDirector.GroundPoint(point).y + 0.1f); transform.position = point;
            cargo = Mathf.Clamp(saved.cargo, 0, 12); cargoKind = saved.cargoKind; Deliveries = Mathf.Max(0, saved.deliveries);
            revivesAt = Time.time + Mathf.Max(0, saved.reviveSeconds - (float)offlineSeconds);
            foreach (var visual in visuals) visual.enabled = Entity.Alive;
            body.enabled = Entity.Alive; target = null; threatUntil = 0; Motor.ResetMotion();
            if (Entity.Alive) AssignObjective();
        }
        private static bool Finite(Vector3 value) => !float.IsNaN(value.x) && !float.IsNaN(value.y) && !float.IsNaN(value.z)
            && !float.IsInfinity(value.x) && !float.IsInfinity(value.y) && !float.IsInfinity(value.z);
        private void OnEnable() { All.Add(this); }
        private void OnDisable() { All.Remove(this); }
        private void Awake()
        {
            Entity = GetComponent<Combatant>();
            body = GetComponent<CharacterController>();
            Motor = GetComponent<BotLocomotion>() ?? gameObject.AddComponent<BotLocomotion>();
            Entity.Died += Die;
            visuals = GetComponentsInChildren<Renderer>();
            weapon = transform.Find("Weapon");
        }
        private void Start() { stalker = GetComponent<StalkerBehaviour>(); destination = home; AssignObjective(); }
        private void Die(Combatant actor)
        {
            foreach (var visual in visuals) visual.enabled = false;
            body.enabled = false;
            revivesAt = Time.time + (Entity.team == Team.Wild ? 30 : 12);
            if (cargo > 0) { var dropped = new ResourceBag(); dropped.Add(cargoKind, cargo); DroppedCargo.Spawn(transform.position, dropped); }
            cargo = 0; target = null; crossedMidfield = false; Motor.ResetMotion();
        }
        public void AssignObjective()
        {
            if (WarDirector.Instance == null || !Entity.Alive) return;
            var director = WarDirector.Instance;
            repair = null; rebuild = null; unpoweredOutpost = null;
            mine = null;
            if (Entity.team == Team.Blue && (role == BotRole.Guard || role == BotRole.Heavy || role == BotRole.Raider)
                && director.BlueOrder != CaptainOrder.Balanced)
            {
                if (director.BlueOrder == CaptainOrder.Defend)
                {
                    destination = director.blueSpawn + new Vector3(Mathf.Sin(home.x) * 12, 0, 15);
                    Task = "Ordered: defend our core";
                    return;
                }
                if (director.BlueOrder == CaptainOrder.Escort)
                {
                    var player = Object.FindAnyObjectByType<PlayerCombat>();
                    destination = player != null ? player.transform.position + new Vector3(Mathf.Sin(home.x) * 7, 0, -5) : home;
                    Task = "Ordered: escort player";
                    return;
                }
                // Assault also commits the usually stationary guard.
                destination = director.AssaultPoint(Entity.team, false);
                Task = "Ordered: assault enemy power";
                return;
            }
            if (role == BotRole.Worker)
            {
                if (cargo >= 12 || (cargo > 0 && Time.time < threatUntil))
                { destination = director.Core(Entity.team).transform.position; Task = "Delivering " + cargoKind; return; }
                float nearest = float.MaxValue;
                foreach (var actor in Combatant.All)
                {
                    if (actor.team != Entity.team || actor.Alive || actor.kind == EntityKind.Core) continue;
                    var rubble = actor.GetComponent<DefenseStructure>();
                    if (rubble == null || rubble.playerBuilt || !director.StockBag(Entity.team).Covers(StructureCatalog.Cost(actor.kind))) continue;
                    float score = (actor.transform.position - transform.position).sqrMagnitude * (actor.kind == EntityKind.Generator ? 0.05f : 1);
                    if (score < nearest) { nearest = score; rebuild = rubble; }
                }
                if (rebuild != null) { destination = rebuild.transform.position; Task = "Rebuilding " + rebuild.Entity.kind; return; }
                nearest = float.MaxValue;
                if (director.Stock(Entity.team) > 5)
                    foreach (var structure in Combatant.All)
                    {
                        if (structure.team != Entity.team || !structure.Alive || structure.kind == EntityKind.Bot
                            || structure.kind == EntityKind.Player || structure.health >= structure.maximumHealth) continue;
                        float distance = (structure.transform.position - transform.position).sqrMagnitude;
                        if (distance < nearest) { nearest = distance; repair = structure; }
                    }
                if (repair != null) { destination = repair.transform.position; Task = "Repairing " + repair.kind; return; }
                if (cargo >= 12 || (cargo > 0 && Time.time < threatUntil))
                { destination = director.Core(Entity.team).transform.position; Task = "Delivering " + cargoKind; return; }
                nearest = float.MaxValue;
                ResourceKind preferred = cargo > 0 ? cargoKind : director.Stock(Entity.team) < 45 || director.Stock(Entity.team, ResourceKind.Timber) >= 35
                    ? ResourceKind.Ferrite : ResourceKind.Timber;
                bool supplyAvailable = ResourceNode.All.Exists(node => node.kind == preferred && node.remaining > 0);
                foreach (var node in ResourceNode.All)
                {
                    if (node.remaining <= 0 || (cargo > 0 && node.kind != cargoKind) || (supplyAvailable && node.kind != preferred)) continue;
                    float distance = (node.transform.position - transform.position).sqrMagnitude;
                    // Split labor toward scarce local supplies, without sending workers
                    // through a known hostile camp simply because its resource is valuable.
                    if (node.kind == ResourceKind.Crystal) distance *= 4;
                    if (director.Stock(Entity.team, node.kind) < 30) distance *= 0.4f;
                    if (distance < nearest) { nearest = distance; mine = node; }
                }
                destination = mine != null ? mine.transform.position : home;
                if (mine == null && cargo > 0) { destination = director.Core(Entity.team).transform.position; Task = "Delivering " + cargoKind; }
                else Task = "Gathering " + (mine != null ? mine.kind.ToString() : "supplies");
            }
            else if (role == BotRole.Guard)
            {
                Vector3 anchor = director.GuardPoint(Entity.team);
                destination = anchor + new Vector3(Mathf.Sin(Time.time * 0.2f + home.x) * 8, 0, Entity.team == Team.Blue ? 12 : -12);
                Task = (anchor - director.Core(Entity.team).transform.position).sqrMagnitude > 25 ? "Defending forward spawn" : "Guarding core";
            }
            else if (Entity.team == Team.Wild) { destination = home + new Vector3(Mathf.Sin(Time.time * 0.15f) * 15, 0, Mathf.Cos(Time.time * 0.15f) * 15); Task = "Prowling"; }
            else if (role == BotRole.Scout)
            {
                establishedOutpost = false;
                foreach (var actor in Combatant.All)
                {
                    if (actor.team != Entity.team || !actor.Alive || actor.kind != EntityKind.Outpost) continue;
                    establishedOutpost = true;
                    if (actor.health < actor.maximumHealth) repair = actor;
                    bool generatorPresent = false;
                    foreach (var power in Combatant.All)
                        if (power.team == Entity.team && power.Alive && power.kind == EntityKind.Generator
                            && (power.transform.position - actor.transform.position).sqrMagnitude < 1225)
                        { generatorPresent = true; if (power.health < power.maximumHealth) repair = power; }
                    if (!generatorPresent) unpoweredOutpost = actor;
                }
                if (unpoweredOutpost != null) { destination = unpoweredOutpost.transform.position; Task = "Restoring forward power"; return; }
                if (repair != null) { destination = repair.transform.position; Task = "Maintaining forward spawn"; return; }
                if (!establishedOutpost)
                {
                    destination = new Vector3(Entity.team == Team.Blue ? -65 : 65, 0, Entity.team == Team.Blue ? -35 : 35);
                    Task = "Establishing forward base"; investigateUntil = 0; return;
                }
                if (director.Hear(Entity.team, transform.position, out investigation)
                    || director.TryContact(Entity.team, transform.position, 140, out investigation)) investigateUntil = Time.time + 10;
                destination = Time.time < investigateUntil ? investigation
                    : new Vector3(Entity.team == Team.Blue ? -65 : 65, 0, Entity.team == Team.Blue ? -35 : 35);
                if (Time.time < investigateUntil && Vector3.ProjectOnPlane(investigation - transform.position, Vector3.up).sqrMagnitude < 64)
                    destination = investigation + new Vector3(Mathf.Sin(Time.time * 0.7f) * 9, 0, Mathf.Cos(Time.time * 0.7f) * 9);
                Task = Time.time < investigateUntil ? "Searching last contact" : establishedOutpost ? "Patrolling forward base" : "Establishing forward base";
            }
            else
            {
                if (role == BotRole.Raider && !crossedMidfield && flank != 0)
                {
                    if (Entity.team == Team.Blue ? transform.position.z >= 0 : transform.position.z <= 0) crossedMidfield = true;
                    else { destination = new Vector3(flank * 55, 0, Entity.team == Team.Blue ? 8 : -8); Task = flank < 0 ? "Taking west flank" : "Taking east flank"; return; }
                }
                destination = director.AssaultPoint(Entity.team);
                Task = role == BotRole.Heavy ? "Sieging enemy power" : "Assaulting enemy base";
            }
        }

        private void Update()
        {
            var director = WarDirector.Instance;
            if (director == null || director.Finished) return;
            if (!Entity.Alive)
            {
                if (Time.time < revivesAt) return;
                Vector3 spawn = Entity.team == Team.Wild ? home : director.RespawnPoint(Entity.team);
                transform.position = WarDirector.GroundPoint(spawn) + Vector3.up * 0.1f;
                Entity.Restore();
                foreach (var visual in visuals) visual.enabled = true;
                body.enabled = true;
                Motor.ResetMotion();
                AssignObjective();
            }
            Vector3 eye = role == BotRole.Stalker ? Entity.AimPoint : transform.position + Vector3.up * 1.6f;
            if (Time.time >= nextSense)
            {
                nextSense = Time.time + 0.35f;
                Combatant sensed = Combatant.EnemyVisible(Entity.team, eye, role == BotRole.Heavy ? 95 : Entity.team == Team.Wild ? 35 : 65,
                    role == BotRole.Worker || role == BotRole.Scout || role == BotRole.Guard);
                if ((role == BotRole.Raider || role == BotRole.Heavy) && (sensed == null || (sensed.kind != EntityKind.Player && sensed.kind != EntityKind.Bot && sensed.kind != EntityKind.Creature)
                    || (sensed.AimPoint - eye).sqrMagnitude > 900))
                {
                    var core = director.Core(Entity.team == Team.Blue ? Team.Red : Team.Blue);
                    float reach = role == BotRole.Heavy ? 95 : 65;
                    if (!core.Shielded && (core.AimPoint - eye).sqrMagnitude < reach * reach && Combatant.CanSee(eye, core)) sensed = core;
                }
                if (role == BotRole.Guard && sensed != null && (sensed.transform.position - destination).sqrMagnitude > 2500) sensed = null;
                if (Entity.team == Team.Wild && sensed != null && (sensed.transform.position - home).sqrMagnitude > 4900) sensed = null;
                if (sensed != target) acquiredAt = Time.time;
                target = sensed;
                if (target != null)
                {
                    lastThreat = target.transform.position; threatUntil = Time.time + 3;
                    if (Entity.team != Team.Wild) director.ReportContact(Entity.team, target);
                }
            }
            Vector3 goal = destination;
            bool engaged = target != null && target.Alive;
            CombatState = "Moving";
            if (engaged)
            {
                Vector3 aim = target.AimPoint - eye;
                Vector3 flatAim = Vector3.ProjectOnPlane(aim, Vector3.up);
                if (flatAim.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(flatAim), 270 * Time.deltaTime);
                if (weapon != null && aim.sqrMagnitude > 0.01f) weapon.rotation = Quaternion.LookRotation(aim);
                float distance = aim.magnitude;
                bool worker = role == BotRole.Worker;
                if (worker)
                {
                    goal = director.Core(Entity.team).transform.position;
                    CombatState = "Retreating with supplies";
                    if (Vector3.Distance(goal, transform.position) < 12) goal = transform.position - flatAim.normalized * 8;
                }
                else if (role == BotRole.Heavy)
                {
                    goal = distance < 28 ? transform.position - flatAim.normalized * 15 : distance < 82 ? transform.position : target.transform.position;
                    CombatState = distance < 28 ? "Opening siege distance" : "Sieging";
                }
                else if (Entity.team == Team.Wild)
                {
                    goal = distance < 2.5f ? transform.position : target.transform.position;
                    CombatState = "Chasing";
                }
                else
                {
                    float standOff = role == BotRole.Scout ? 28 : 22;
                    Vector3 side = Vector3.Cross(Vector3.up, flatAim.normalized);
                    float phase = Mathf.Sin(Time.time * 1.3f + home.x);
                    goal = distance < standOff * 0.65f ? transform.position - flatAim.normalized * 10 + side * phase * 6
                        : distance < 48 ? transform.position + side * phase * 8 : target.transform.position;
                    CombatState = role == BotRole.Scout ? "Skirmishing" : "Engaging";
                }
                float attackRange = Entity.team == Team.Wild ? 3 : role == BotRole.Heavy ? 85 : worker ? 28 : 48;
                if (stalker == null && distance < attackRange && Time.time > acquiredAt + (role == BotRole.Heavy ? 0.4f : 0.2f)
                    && Time.time >= nextShot && Vector3.Angle(transform.forward, flatAim) < 28 && Combatant.CanSee(eye, target))
                {
                    Shots++;
                    nextShot = Time.time + (role == BotRole.Heavy ? 2.4f : Entity.team == Team.Wild ? 0.8f : 0.75f);
                    if (Entity.team == Team.Wild) target.Damage(25, Entity.team, eye, Entity);
                    else
                    {
                        float speed = role == BotRole.Heavy ? 65 : 80;
                        var player = target.GetComponent<PlaygroundPlayer>(); var enemyMotor = target.GetComponent<BotLocomotion>();
                        Vector3 velocity = player != null ? player.Velocity : enemyMotor != null ? enemyMotor.Velocity : Vector3.zero;
                        aim += (velocity - Motor.Velocity * 0.15f) * Mathf.Min(0.4f, distance / speed);
                        aim += Random.insideUnitSphere * (role == BotRole.Heavy ? 0.6f : 1.5f);
                        CombatProjectile.Launch(eye + aim.normalized * 0.8f, aim.normalized * speed + Motor.Velocity * 0.15f,
                            Entity.team, role == BotRole.Heavy ? 55 : 18, role == BotRole.Heavy ? 5 : 0, owner: transform);
                        director.Noise(eye, Entity.team, role == BotRole.Heavy ? 150 : 90);
                    }
                }
            }
            else if (Time.time < threatUntil && role == BotRole.Worker)
            { goal = director.Core(Entity.team).transform.position; CombatState = "Returning to safety"; }
            else if (Time.time < threatUntil && (role == BotRole.Scout || role == BotRole.Raider))
            { goal = lastThreat; CombatState = "Checking last sighting"; }
            if (!engaged || role == BotRole.Heavy || role == BotRole.Guard) Work(director);
            if (role == BotRole.Guard && (goal - destination).sqrMagnitude > 2500) goal = destination;
            if (stalker != null) { goal = stalker.Steer(engaged ? target : null, goal); CombatState = stalker.State; }
            Motor.Tick(goal, role, engaged, engaged, Time.deltaTime);
        }

        private void Work(WarDirector director)
        {
            if (Time.time < nextWork) return;
            nextWork = Time.time + 0.5f;
            if (role == BotRole.Worker)
            {
                if (rebuild != null && Vector3.Distance(transform.position, rebuild.transform.position) < 8)
                { rebuild.Rebuild(); AssignObjective(); }
                else if (repair != null && repair.Alive && repair.health < repair.maximumHealth && Vector3.Distance(transform.position, repair.transform.position) < 7)
                { if (director.Spend(Entity.team, 1)) repair.Repair(12); }
                else if (cargo > 0 && (mine == null || Time.time < threatUntil || cargo >= 12) && Vector3.Distance(transform.position, director.Core(Entity.team).transform.position) < 9)
                { director.Deposit(Entity.team, cargo, cargoKind); cargo = 0; Deliveries++; AssignObjective(); }
                else if (mine != null && Vector3.Distance(transform.position, mine.transform.position) < 7 && cargo < 12)
                { cargoKind = mine.kind; cargo += mine.Mine(2, Entity.team); if (cargo >= 12 || mine.remaining <= 0) AssignObjective(); }
            }
            if (role == BotRole.Scout && unpoweredOutpost != null && unpoweredOutpost.Alive
                && Vector3.Distance(transform.position, unpoweredOutpost.transform.position) < 10
                && director.TryBuildSite(unpoweredOutpost.transform.position - Vector3.right * 7, out Vector3 replacement)
                && (replacement - unpoweredOutpost.transform.position).sqrMagnitude < 1225
                && director.Spend(Entity.team, StructureCatalog.Cost(EntityKind.Generator)))
            {
                director.CreateStructure(EntityKind.Generator, Entity.team, replacement, 0);
                director.Announce(Entity.team + " scout restored forward-base power.", 4); AssignObjective();
            }
            if (role == BotRole.Scout && repair != null && repair.Alive && repair.health < repair.maximumHealth && Vector3.Distance(transform.position, repair.transform.position) < 7)
            { if (director.Spend(Entity.team, 1)) repair.Repair(12); }
            if (role == BotRole.Scout && !establishedOutpost && Vector3.ProjectOnPlane(destination - transform.position, Vector3.up).sqrMagnitude < 25
                && Time.time >= investigateUntil && director.TryBuildSite(transform.position + Vector3.right * 7, out Vector3 point)
                && director.Spend(Entity.team, new ResourceBag(75, 23)))
            {
                director.CreateStructure(EntityKind.Generator, Entity.team, point, 0);
                director.CreateStructure(EntityKind.Outpost, Entity.team, WarDirector.GroundPoint(point + Vector3.right * 7), 0);
                establishedOutpost = true;
                director.Announce(Entity.team + " scout established a powered forward spawn.", 5);
            }
        }

    }
}
