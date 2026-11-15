using UnityEngine;

namespace Siphon
{
    [RequireComponent(typeof(Combatant), typeof(PlaygroundPlayer))]
    public sealed class PlayerCombat : MonoBehaviour
    {
        public Combatant Entity { get; private set; }
        public ResourceBag CargoBag { get; private set; }
        public int Cargo => CargoBag.Total;
        public bool CarryingRelic { get; private set; }
        public FieldGear Gear { get; private set; }
        public int Menu { get; private set; }
        public float HackProgress { get; private set; }
        private Combatant hacking;
        private float lastHack;
        public int Weapon { get; private set; }
        public bool Building => placement != null;
        public float HitUntil { get; private set; }
        public float KillUntil { get; private set; }
        public int Kills { get; private set; }
        public float DamageUntil { get; private set; }
        public float RespawnsAt { get; private set; }
        public string Context { get; private set; }
        public EntityKind BuildKind { get; private set; }
        public bool PlacementValid { get; private set; }
        private PlaygroundPlayer motor;
        private float nextShot;
        private float nextInteract;
        private float heading;
        private GameObject placement;
        private LineRenderer previewGuide;
        private Transform weaponModel;
        private Vector3 weaponRest;
        private Material ghostMaterial;
        private Vector3 buildPoint;
        private AudioSource audioSource;
        private AudioClip pulseSound;
        private AudioClip discSound;
        private float contextUntil;

        private void Awake()
        {
            Entity = GetComponent<Combatant>();
            motor = GetComponent<PlaygroundPlayer>();
            Gear = GetComponent<FieldGear>() ?? gameObject.AddComponent<FieldGear>();
            Entity.Damaged += amount => DamageUntil = Time.time + 0.3f;
            Entity.Died += Die;
            audioSource = gameObject.AddComponent<AudioSource>();
            pulseSound = MakeSound(220, 0.09f);
            discSound = MakeSound(80, 0.18f);
            weaponModel = motor.view.transform.Find("First person weapon");
            if (weaponModel != null) weaponRest = weaponModel.localPosition;
        }
        private void Die(Combatant actor)
        {
            CancelBuild();
            RespawnsAt = Time.time + 3;
            CloseMenu();
            if (Cargo > 0 || CarryingRelic)
            {
                var lost = CargoBag.Half();
                var retained = CargoBag; retained.Spend(lost); CargoBag = retained;
                if (lost.Total > 0 || CarryingRelic) DroppedCargo.Spawn(transform.position, lost, CarryingRelic);
                CarryingRelic = false;
                WarDirector.Instance?.Announce($"Down. Dropped {lost}. Respawning in 3 seconds.");
            }
        }
        private void Update()
        {
            var director = WarDirector.Instance;
            if (weaponModel != null) weaponModel.localPosition = Vector3.Lerp(weaponModel.localPosition, weaponRest, Time.deltaTime * 16);
            if (director == null) return;
            if (director.Finished)
            {
                CancelBuild();
                if (Input.GetKeyDown(KeyCode.Return)) director.Restart();
                return;
            }
            if (Input.GetKeyDown(KeyCode.N)) ToggleMenu(3);
            if (Menu != 0 && Input.GetKeyDown(KeyCode.Escape)) CloseMenu();
            if (!Entity.Alive)
            {
                if (Time.time >= RespawnsAt)
                {
                    Entity.Restore();
                    motor.ResetTo(director.RespawnPoint(Entity.team, true), Quaternion.identity);
                }
                return;
            }
            if (Input.GetKeyDown(KeyCode.V)) ToggleMenu(1);
            if (Input.GetKeyDown(KeyCode.T)) ToggleMenu(2);
            if (Menu != 0) { if (Input.GetKeyDown(KeyCode.Escape)) CloseMenu(); return; }
            if (!motor.ControlsActive) { CancelBuild(); return; }
            if (!Input.GetKey(KeyCode.F) || Time.time > lastHack + 0.4f) { HackProgress = 0; hacking = null; }
            if (Input.GetKeyDown(KeyCode.J)) director.CycleOrder();
            if (Input.GetMouseButtonDown(2)) MarkTarget();
            if (Input.GetKeyDown(KeyCode.Alpha1)) Weapon = 0;
            if (Input.GetKeyDown(KeyCode.Alpha2)) Weapon = 1;
            if (Input.GetKeyDown(KeyCode.G)) BeginBuild(EntityKind.Turret);
            if (Input.GetKeyDown(KeyCode.B)) BeginBuild(EntityKind.Outpost);
            if (Input.GetKeyDown(KeyCode.H)) BeginBuild(EntityKind.Generator);
            if (Building) { UpdatePlacement(); return; }
            if (Input.GetMouseButton(0) && Time.time >= nextShot) Fire();
            Inspect();
            if (Input.GetKey(KeyCode.F) && Time.time >= nextInteract) Interact();
        }
        public void ConfirmHit(bool killed)
        {
            HitUntil = Time.time + (killed ? 0.35f : 0.12f);
            if (killed) { KillUntil = Time.time + 0.7f; Kills++; CombatAudio.Play(transform.position, 3, 0.15f); }
        }
        public bool MarkTarget()
        {
            if (!Entity.Alive || WarDirector.Instance == null || WarDirector.Instance.Finished) return false;
            if (!Physics.Raycast(motor.view.transform.position, motor.view.transform.forward, out RaycastHit hit, 300,
                ~0, QueryTriggerInteraction.Ignore)) { directorMessage("Aim at terrain or an enemy installation to mark a target."); return false; }
            var seen = hit.collider.GetComponentInParent<Combatant>();
            var gear = seen != null ? seen.GetComponent<FieldGear>() : null;
            if (gear != null && !gear.VisibleFrom(motor.view.transform.position)) seen = null;
            return WarDirector.Instance.MarkTarget(seen != null ? seen.AimPoint : hit.point, seen);
        }
        public void Fire()
        {
            if (!Entity.Alive || (WarDirector.Instance != null && WarDirector.Instance.Finished)) return;
            Gear.BreakCloak();
            nextShot = Time.time + (Weapon == 0 ? 0.14f : 0.8f);
            if (weaponModel != null) weaponModel.localPosition = weaponRest + new Vector3(0, -0.01f, Weapon == 0 ? -0.06f : -0.12f);
            Vector3 from = motor.view.transform.position;
            Vector3 direction = motor.view.transform.forward;
            if (Weapon == 1)
            {
                CombatProjectile.Launch(from + direction * 0.7f, direction * 95 + motor.Velocity * 0.35f,
                    Entity.team, 90, 5, 20, transform);
                audioSource.PlayOneShot(discSound, 0.35f);
            }
            else
            {
                Vector3 end = from + direction * 300;
                if (CombatPhysics.Cast(from, direction, 300, Entity.team, transform, out RaycastHit hit))
                {
                    end = hit.point;
                    var actor = hit.collider.GetComponentInParent<Combatant>();
                    var barrier = hit.collider.GetComponentInParent<DefenseStructure>();
                    if (hit.collider.isTrigger && barrier != null && barrier.BarrierActive)
                    { barrier.Absorb(35, hit.point); HitUntil = Time.time + 0.12f; }
                    else if (actor != null)
                    {
                        if (!actor.Damage(35, Entity.team, from, Entity) && actor.Shielded && actor.team != Entity.team)
                            directorMessage("Core shield active — destroy its nearby generator.");
                    }
                    CombatEffects.Burst(end, actor != null ? Combatant.TeamColor(actor.team) : Color.yellow, 3);
                }
                CombatEffects.Trace(from + motor.view.transform.right * 0.3f - motor.view.transform.up * 0.25f, end, Color.cyan);
                audioSource.PlayOneShot(pulseSound, 0.25f);
            }
            WarDirector.Instance?.Noise(from, Entity.team, Weapon == 0 ? 95 : 140);
        }
        private void directorMessage(string value)
        {
            if (WarDirector.Instance != null && Time.time > contextUntil)
            { WarDirector.Instance.Announce(value); contextUntil = Time.time + 3; }
        }
        private void Inspect()
        {
            Context = "";
            if (!Physics.Raycast(motor.view.transform.position, motor.view.transform.forward, out RaycastHit hit, 8,
                ~0, QueryTriggerInteraction.Ignore)) return;
            var node = hit.collider.GetComponentInParent<ResourceNode>();
            if (node != null)
                Context = $"Hold F · {(node.kind == ResourceKind.Timber ? "log" : "mine")} {node.kind} · {node.remaining} remaining · harvesting emits noise";
            var actor = hit.collider.GetComponentInParent<Combatant>();
            var dropped = hit.collider.GetComponentInParent<DroppedCargo>();
            if (dropped != null) Context = $"F · Recover {dropped.contents}" + (dropped.relic ? " + WARDEN CAPACITOR (deposit to equip)" : "");
            if (actor == null) return;
            var defense = actor.GetComponent<DefenseStructure>();
            if (actor.team != Entity.team && defense != null && Hackable(actor.kind))
                Context = $"Hold F · Hack {actor.kind} ({HackProgress:P0}) · exposed console · disables for 25s";
            if (actor.team == Entity.team && actor.Alive)
            {
                if ((actor.kind == EntityKind.Core || actor.kind == EntityKind.Outpost) && (Cargo > 0 || CarryingRelic))
                    Context = CarryingRelic ? $"F · Deposit {CargoBag} and equip the Warden capacitor" : $"F · Deposit {CargoBag} into team stock";
                else if (actor.health < actor.maximumHealth && actor.kind != EntityKind.Bot && actor.kind != EntityKind.Player)
                    Context = $"Hold F · Repair {actor.kind} (1 ferrite / 12 health)";
                else Context = $"{actor.kind} · {actor.health:0}/{actor.maximumHealth:0} HP";
                if (actor.kind == EntityKind.Processor) Context += " · powered smelter: 4 Fe + 2 wood → alloy";
                var siege = actor.GetComponent<SiegeBattery>();
                if (siege != null) Context += " · " + siege.Status + " · middle mouse marks target";
                if (actor.kind == EntityKind.Core || actor.kind == EntityKind.Outpost) Context += " · T craft equipment";
            }
        }
        public void Interact()
        {
            nextInteract = Time.time + 0.25f;
            if (!Physics.Raycast(motor.view.transform.position, motor.view.transform.forward, out RaycastHit hit, 8,
                ~0, QueryTriggerInteraction.Ignore)) return;
            var node = hit.collider.GetComponentInParent<ResourceNode>();
            if (node != null)
            {
                if (Cargo >= 60) { directorMessage("Cargo full (60). Deposit at a friendly core or outpost."); return; }
                Gear.BreakCloak();
                var bag = CargoBag; bag.Add(node.kind, node.Mine(Mathf.Min(2, 60 - Cargo), Entity.team)); CargoBag = bag;
                return;
            }
            var dropped = hit.collider.GetComponentInParent<DroppedCargo>();
            if (dropped != null)
            {
                if (Cargo + dropped.contents.Total > 60) { directorMessage("Deposit cargo before recovering this cache."); return; }
                if (dropped.relic && !Gear.wardenCapacitor)
                { CarryingRelic = true; WarDirector.Instance.Announce("Capacitor recovered. Deposit at a friendly core or outpost to equip it."); }
                var bag = CargoBag; bag.Add(dropped.Take()); CargoBag = bag; CombatAudio.Play(transform.position, 3); return;
            }
            var actor = hit.collider.GetComponentInParent<Combatant>();
            if (actor == null || !actor.Alive) return;
            if (actor.team != Entity.team)
            {
                var defense = actor.GetComponent<DefenseStructure>();
                if (defense == null || !Hackable(actor.kind)) return;
                if (hacking != actor) { hacking = actor; HackProgress = 0; }
                Gear.BreakCloak();
                HackProgress += 0.25f / 3;
                lastHack = Time.time;
                if (HackProgress >= 1)
                { defense.Disable(25); WarDirector.Instance.Noise(actor.transform.position, Entity.team, 110); HackProgress = 0; WarDirector.Instance.Announce($"{actor.kind} hacked: offline for 25 seconds."); }
                return;
            }
            if ((actor.kind == EntityKind.Core || actor.kind == EntityKind.Outpost) && (Cargo > 0 || CarryingRelic))
            {
                WarDirector.Instance.Deposit(Entity.team, CargoBag);
                WarDirector.Instance.Announce($"Deposited {CargoBag} into team stock.");
                CargoBag = default;
                if (CarryingRelic) { Gear.EquipCapacitor(); CarryingRelic = false; }
                CombatAudio.Play(transform.position, 3);
            }
            else if (actor.health < actor.maximumHealth && actor.kind != EntityKind.Bot && actor.kind != EntityKind.Player
                && WarDirector.Instance.Spend(Entity.team, 1)) { Gear.BreakCloak(); actor.Repair(12); }
        }
        private static bool Hackable(EntityKind kind) => kind == EntityKind.Turret || kind == EntityKind.Generator
            || kind == EntityKind.Radar || kind == EntityKind.ForceField || kind == EntityKind.Trap || kind == EntityKind.SiegeCannon;
        public void RestoreInventory(ResourceBag bag, bool relic = false) { CargoBag = bag; CarryingRelic = relic; CancelBuild(); CloseMenu(); }
        public void ToggleMenu(int which)
        {
            CancelBuild(); Menu = Menu == which ? 0 : which;
            Cursor.lockState = Menu == 0 ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = Menu != 0;
        }
        public void CloseMenu() { Menu = 0; Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        public static int Cost(EntityKind kind) => StructureCatalog.Cost(kind).ferrite;
        public void BeginBuild(EntityKind kind)
        {
            CancelBuild();
            if (!Entity.Alive || WarDirector.Instance.Finished) return;
            if (!Gear.CanBuild(kind)) { directorMessage("Craft the required equipment at a friendly workbench (T)."); return; }
            if (!WarDirector.Instance.StockBag(Entity.team).Covers(StructureCatalog.Cost(kind)))
            { WarDirector.Instance.Announce($"Need {StructureCatalog.Cost(kind)} to build {kind}."); return; }
            CloseMenu(); BuildKind = kind;
            heading = transform.eulerAngles.y;
            placement = new GameObject("Build preview");
            ghostMaterial = new Material(Resources.Load<Material>("BuildGhost"));
            StructureCatalog.Visuals(placement.transform, kind, ghostMaterial, ghostMaterial, false);
        }
        private void UpdatePlacement()
        {
            if (Input.GetMouseButtonDown(1)) { CancelBuild(); return; }
            if (Input.GetKeyDown(KeyCode.Q)) heading += 45;
            PlacementValid = Physics.Raycast(motor.view.transform.position, motor.view.transform.forward,
                out RaycastHit hit, 24, ~(1 << 8), QueryTriggerInteraction.Ignore)
                && hit.normal.y > 0.82f && hit.collider.GetComponentInParent<Combatant>() == null;
            buildPoint = hit.point;
            Vector3 size = StructureCatalog.Size(BuildKind);
            Quaternion rotation = Quaternion.Euler(0, heading, 0);
            if (PlacementValid)
            {
                // The preview must sit on terrain, and its entire footprint must be supported.
                PlacementValid = hit.collider.gameObject.layer == 9;
                for (int corner = 0; corner < 4 && PlacementValid; corner++)
                {
                    Vector3 offset = rotation * new Vector3((corner % 2 == 0 ? -1 : 1) * size.x * 0.45f, 0,
                        (corner < 2 ? -1 : 1) * size.z * 0.45f);
                    if (!Physics.Raycast(buildPoint + offset + Vector3.up * 3, Vector3.down, out RaycastHit ground, 6,
                        1 << 9, QueryTriggerInteraction.Ignore) || Mathf.Abs(ground.point.y - buildPoint.y) > 0.65f) PlacementValid = false;
                }
                var overlaps = Physics.OverlapBox(buildPoint + Vector3.up * (size.y * 0.5f + 0.15f),
                    new Vector3(size.x * 0.5f + 0.2f, Mathf.Max(0.1f, size.y * 0.5f - 0.1f), size.z * 0.5f + 0.2f), rotation,
                    ~(1 << 9), QueryTriggerInteraction.Ignore);
                PlacementValid &= overlaps.Length == 0;
            }
            PlacementValid &= WarDirector.Instance.StockBag(Entity.team).Covers(StructureCatalog.Cost(BuildKind));
            placement.SetActive(hit.collider != null);
            placement.transform.SetPositionAndRotation(buildPoint, rotation);
            ghostMaterial.color = PlacementValid ? new Color(0.1f, 1, 0.5f, 0.35f) : new Color(1, 0.2f, 0.1f, 0.35f);
            Context = $"{BuildKind} · {StructureCatalog.Cost(BuildKind)} · LMB place · Q rotate · RMB cancel";
            if (BuildKind != EntityKind.Generator && BuildKind != EntityKind.Wall && !WarDirector.Instance.IsPowered(Entity.team, buildPoint, 35))
                Context += " · POWER REQUIRED (35m)";
            DrawPlacementGuide(size, rotation);
            if (!Input.GetMouseButtonDown(0) || !PlacementValid) return;
            if (!WarDirector.Instance.Spend(Entity.team, StructureCatalog.Cost(BuildKind))) return;
            Gear.BreakCloak();
            WarDirector.Instance.CreateStructure(BuildKind, Entity.team, buildPoint, heading);
            WarDirector.Instance.Noise(buildPoint, Entity.team, 100);
            CombatAudio.Play(buildPoint, 3);
            WarDirector.Instance.Announce($"{BuildKind} built. " + (BuildKind == EntityKind.Turret ? "Coverage follows the direction you placed it." : ""));
            CancelBuild();
        }
        private void DrawPlacementGuide(Vector3 size, Quaternion rotation)
        {
            float radius = BuildKind == EntityKind.Generator ? 35 : BuildKind == EntityKind.Turret ? 12 : 0;
            if (radius <= 0) return;
            int segments = BuildKind == EntityKind.Turret ? 15 : 36;
            if (previewGuide == null)
            {
                var root = new GameObject("Coverage preview"); root.transform.SetParent(placement.transform, false);
                previewGuide = root.AddComponent<LineRenderer>(); previewGuide.sharedMaterial = CombatEffects.LineMaterial;
                previewGuide.startWidth = previewGuide.endWidth = 0.1f;
            }
            previewGuide.positionCount = segments + 1;
            previewGuide.startColor = previewGuide.endColor = PlacementValid ? Color.cyan : Color.red;
            for (int i = 0; i <= segments; i++)
            {
                float angle = BuildKind == EntityKind.Turret ? -75 + i * 10 : i * 10;
                Vector3 point = buildPoint + rotation * (Quaternion.Euler(0, angle, 0) * Vector3.forward * radius);
                previewGuide.SetPosition(i, WarDirector.GroundPoint(point) + Vector3.up * 0.15f);
            }
        }
        public void CancelBuild()
        {
            if (placement != null) Destroy(placement);
            if (ghostMaterial != null) Destroy(ghostMaterial);
            placement = null; previewGuide = null;
            ghostMaterial = null;
        }
        private static AudioClip MakeSound(float pitch, float length)
        {
            const int rate = 22050;
            var samples = new float[(int)(rate * length)];
            var random = new System.Random(13);
            for (int i = 0; i < samples.Length; i++)
            {
                float phase = i / (float)rate;
                float decay = 1 - i / (float)samples.Length;
                samples[i] = (Mathf.Sin(phase * pitch * 6.283f * (1 - phase * 2)) * 0.65f
                    + ((float)random.NextDouble() * 2 - 1) * 0.35f) * decay * decay;
            }
            var clip = AudioClip.Create("Prototype weapon", samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
        private void OnDestroy()
        {
            CancelBuild();
            if (pulseSound != null) Destroy(pulseSound);
            if (discSound != null) Destroy(discSound);
        }
    }
}
