using UnityEngine;

namespace Siphon
{
    [RequireComponent(typeof(Combatant))]
    public sealed class DefenseStructure : MonoBehaviour
    {
        public Transform barrel;
        public float heading;
        public float coverage = 150;
        public float range = 70;
        public bool playerBuilt;
        public Combatant Entity { get; private set; }
        public float DisabledUntil { get; private set; }
        public float Charge { get; private set; } = 160;
        public int Produced { get; private set; }
        public bool Operational => Entity != null && Entity.Alive && Time.time >= DisabledUntil;
        public bool Powered => Operational && WarDirector.Instance != null
            && WarDirector.Instance.IsPowered(Entity.team, transform.position, 35);
        public bool BarrierActive => Entity != null && Entity.kind == EntityKind.ForceField && Powered && Charge > 0;
        private float nextThink, nextShot, lastAbsorbed = -100;
        private Combatant target;
        private LineRenderer field;
        private LineRenderer[] barrierLines;
        private void Awake()
        {
            Entity = GetComponent<Combatant>();
            Entity.Died += OnDeath;
            nextThink = Time.time + (Entity.kind == EntityKind.Processor ? 12 : Entity.kind == EntityKind.Radar ? 3 : 0);
            if (Entity.kind == EntityKind.Core)
            {
                field = NewLine(gameObject, 64, 0.14f);
                field.loop = true;
                for (int i = 0; i < 64; i++)
                {
                    float angle = i * Mathf.PI * 2 / 64;
                    field.SetPosition(i, transform.position + new Vector3(Mathf.Cos(angle) * 4, 3, Mathf.Sin(angle) * 4));
                }
            }
            if (Entity.kind == EntityKind.ForceField)
            {
                barrierLines = new LineRenderer[9];
                for (int i = 0; i < barrierLines.Length; i++)
                {
                    var lineRoot = new GameObject("Field lattice"); lineRoot.transform.SetParent(transform, false);
                    var line = barrierLines[i] = NewLine(lineRoot, 2, 0.045f);
                    line.useWorldSpace = false;
                    line.SetPosition(0, new Vector3(-4, i * 0.5f, 0));
                    line.SetPosition(1, new Vector3(4, i * 0.5f, 0));
                }
            }
        }
        private LineRenderer NewLine(GameObject root, int points, float width)
        {
            var line = root.AddComponent<LineRenderer>();
            line.sharedMaterial = CombatEffects.LineMaterial; line.positionCount = points;
            line.startWidth = line.endWidth = width;
            line.startColor = line.endColor = Combatant.TeamColor(Entity.team);
            return line;
        }
        public bool Rebuild()
        {
            if (Entity.Alive || playerBuilt || Entity.kind == EntityKind.Core) return false;
            if (!WarDirector.Instance.Spend(Entity.team, StructureCatalog.Cost(Entity.kind))) return false;
            Entity.Restore(); RestoreRuntime(0, 160);
            foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = true;
            foreach (var visual in GetComponentsInChildren<Renderer>()) visual.enabled = true;
            NavigationWorld.Instance?.Invalidate();
            return true;
        }
        public void RestoreRuntime(float disabledSeconds, float charge)
        { DisabledUntil = Time.time + Mathf.Max(0, disabledSeconds); Charge = Mathf.Clamp(charge, 0, 160); }
        public void Disable(float seconds)
        {
            DisabledUntil = Mathf.Max(DisabledUntil, Time.time + seconds);
            WarDirector.Instance?.Noise(transform.position, Entity.team, 110);
        }
        public void Absorb(float amount, Vector3 point)
        {
            if (!BarrierActive || amount <= 0) return;
            Charge = Mathf.Max(0, Charge - amount);
            lastAbsorbed = Time.time;
            CombatEffects.Burst(point, Combatant.TeamColor(Entity.team), 5);
            if (Charge <= 0) Disable(8);
        }
        private void OnDeath(Combatant actor)
        {
            foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
            foreach (var renderer in GetComponentsInChildren<Renderer>()) renderer.enabled = false;
            NavigationWorld.Instance?.Invalidate();
            if (playerBuilt) Destroy(gameObject, 2);
        }
        private void Update()
        {
            if (field != null) field.enabled = Entity.Alive && Entity.Shielded;
            if (barrierLines != null)
            {
                if (Powered && Time.time > lastAbsorbed + 3) Charge = Mathf.Min(160, Charge + 12 * Time.deltaTime);
                foreach (var line in barrierLines)
                {
                    line.enabled = BarrierActive;
                    line.startWidth = line.endWidth = Time.time < lastAbsorbed + 0.15f ? 0.12f : 0.045f;
                }
            }
            var war = WarDirector.Instance;
            if (!Operational || war == null || war.Finished) return;
            if (Entity.kind == EntityKind.Radar)
            {
                var dish = transform.Find("Radar dish");
                if (Powered && dish != null) dish.Rotate(Vector3.up, 90 * Time.deltaTime, Space.World);
            }
            if (!Powered) return;
            if (Entity.kind == EntityKind.Processor && Time.time >= nextThink)
            {
                nextThink = Time.time + 12;
                // Reserve enough basic supplies for repairs and a replacement generator.
                if (war.Stock(Entity.team) >= 28 && war.Stock(Entity.team, ResourceKind.Timber) >= 12
                    && war.Spend(Entity.team, new ResourceBag(4, 2)))
                { war.Deposit(Entity.team, 1, ResourceKind.Alloy); Produced++; war.Noise(transform.position, Entity.team, 75); CombatEffects.Burst(transform.position + Vector3.up * 3, Color.yellow, 3); }
            }
            else if (Entity.kind == EntityKind.Radar && Time.time >= nextThink)
            {
                nextThink = Time.time + 3;
                foreach (var actor in Combatant.All)
                {
                    if (!actor.Alive || actor.team == Entity.team || actor.team == Team.Wild
                        || (actor.transform.position - transform.position).sqrMagnitude > 12100) continue;
                    war.ReportContact(Entity.team, actor, 7);
                }
                CombatEffects.Burst(transform.position + Vector3.up * 4, Combatant.TeamColor(Entity.team), 3);
            }
            else if (Entity.kind == EntityKind.Trap && Time.time >= nextThink)
            {
                nextThink = Time.time + 0.15f;
                foreach (var actor in Combatant.All)
                {
                    if (!actor.Alive || actor.team == Entity.team || (actor.kind != EntityKind.Player && actor.kind != EntityKind.Bot && actor.kind != EntityKind.Creature)
                        || Vector3.Distance(actor.AimPoint, transform.position + Vector3.up) > 3) continue;
                    actor.Damage(50, Entity.team, transform.position);
                    Vector3 impulse = Vector3.up * 12 + (actor.transform.position - transform.position).normalized * 10;
                    actor.GetComponent<PlaygroundPlayer>()?.AddImpulse(impulse);
                    actor.GetComponent<BotLocomotion>()?.AddImpulse(impulse);
                    CombatEffects.Burst(transform.position + Vector3.up * 0.5f, Combatant.TeamColor(Entity.team), 15);
                    war.Noise(transform.position, Entity.team, 100);
                    nextThink = Time.time + 8;
                    break;
                }
            }
            else if (Entity.kind == EntityKind.Turret) Turret(war);
        }
        private void Turret(WarDirector war)
        {
            Vector3 muzzle = transform.position + Vector3.up * 2.7f;
            if (Time.time >= nextThink)
            {
                target = Combatant.EnemyVisible(Entity.team, muzzle, range, true);
                nextThink = Time.time + 0.25f;
            }
            if (target == null || !target.Alive) return;
            Vector3 direction = target.AimPoint - muzzle;
            Vector3 forward = Quaternion.Euler(0, heading, 0) * Vector3.forward;
            if (Vector3.Angle(Vector3.ProjectOnPlane(direction, Vector3.up), forward) > coverage * 0.5f) return;
            if (barrel != null)
            {
                barrel.rotation = Quaternion.RotateTowards(barrel.rotation, Quaternion.LookRotation(direction), 100 * Time.deltaTime);
                if (Vector3.Angle(barrel.forward, direction) > 8) return;
            }
            if (Time.time < nextShot || !Combatant.CanSee(muzzle, target)) return;
            nextShot = Time.time + 0.65f;
            CombatProjectile.Launch(muzzle + direction.normalized * 0.9f, direction.normalized * 85,
                Entity.team, 22, owner: transform);
            war.Noise(muzzle, Entity.team, 100);
        }
    }
}
