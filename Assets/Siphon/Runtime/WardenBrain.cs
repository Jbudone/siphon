using System;
using UnityEngine;

namespace Siphon
{
    [RequireComponent(typeof(Combatant))]
    public sealed class WardenBrain : MonoBehaviour
    {
        public Vector3 home;
        public float patrolRadius = 12;
        public string Phase { get; private set; } = "Patrolling";
        public Vector3 MarkedPoint { get; private set; }
        public bool Telegraphing => fireAt > 0;
        public bool Venting => entity.Alive && Time.time < ventsUntil;
        public int Barrages { get; private set; }
        private Combatant entity, target;
        private Renderer[] visuals;
        private Collider[] colliders;
        private Renderer reactor;
        private MaterialPropertyBlock tint;
        private LineRenderer warning;
        private float nextSense, nextAttack, fireAt, reviveAt, ventsUntil;
        private int volley;
        private bool airburst;
        [Serializable] public sealed class SavedState
        { public Vector3 position; public float health, reviveSeconds; public int volley; }
        public SavedState CaptureState() => new SavedState { position = transform.position, health = entity.health,
            reviveSeconds = entity.Alive ? 0 : Mathf.Max(0, reviveAt - Time.time), volley = volley };
        public void ApplyState(SavedState saved, double offlineSeconds)
        {
            entity.health = Mathf.Clamp(saved.health, 0, entity.maximumHealth);
            if (!entity.Alive && offlineSeconds >= saved.reviveSeconds) entity.Restore();
            reviveAt = Time.time + Mathf.Max(0, saved.reviveSeconds - (float)offlineSeconds);
            transform.position = saved.position; volley = saved.volley;
            foreach (var visual in visuals) visual.enabled = entity.Alive;
            foreach (var collider in colliders) collider.enabled = entity.Alive;
            warning.enabled = false; fireAt = 0; target = null; ventsUntil = 0; nextAttack = Time.time + 3;
        }
        private void Awake()
        {
            entity = GetComponent<Combatant>();
            visuals = GetComponentsInChildren<Renderer>(); colliders = GetComponentsInChildren<Collider>();
            var core = transform.Find("Warden reactor"); if (core != null) reactor = core.GetComponent<Renderer>();
            tint = new MaterialPropertyBlock();
            entity.Died += Die;
            warning = gameObject.AddComponent<LineRenderer>(); warning.sharedMaterial = CombatEffects.LineMaterial;
            warning.positionCount = 32; warning.loop = true; warning.startWidth = warning.endWidth = 0.15f;
            warning.startColor = warning.endColor = new Color(1, 0.4f, 0.1f); warning.enabled = false;
            nextAttack = Time.time + 3;
        }
        public float DamageMultiplier(Vector3 attackFrom)
        {
            if (Venting) return 1;
            Vector3 approach = (attackFrom - entity.AimPoint).normalized;
            // Cannon-side armor rewards fast flanking and attacks from above. The
            // reactor opens after each barrage, without increasing its health pool.
            return Vector3.Dot(transform.forward, approach) > 0.25f ? 0.25f : 1;
        }
        private void Die(Combatant actor)
        {
            foreach (var visual in visuals) visual.enabled = false;
            foreach (var collider in colliders) collider.enabled = false;
            warning.enabled = false; fireAt = 0; target = null;
            reviveAt = Time.time + 180;
            DroppedCargo.Spawn(transform.position, new ResourceBag(crystal: 8, alloy: 4), true);
            WarDirector.Instance?.Announce("Warden down. Recover its capacitor cache and deposit it to equip the jet upgrade.", 7);
        }
        private void Update()
        {
            if (WarDirector.Instance == null || WarDirector.Instance.Finished) return;
            if (!entity.Alive)
            {
                if (Time.time < reviveAt) return;
                entity.Restore();
                foreach (var visual in visuals) visual.enabled = true;
                foreach (var collider in colliders) collider.enabled = true;
                nextAttack = Time.time + 3;
            }
            if (reactor != null)
            {
                tint.SetColor("_BaseColor", Venting ? new Color(1, 0.55f, 0.08f) : new Color(0.7f, 0.24f, 0.95f));
                reactor.SetPropertyBlock(tint);
            }
            float phase = Time.time * 0.25f;
            Vector3 position = home + new Vector3(Mathf.Sin(phase) * patrolRadius, 1.5f + Mathf.Sin(Time.time) * 0.6f, Mathf.Cos(phase) * patrolRadius);
            Vector3 next = Vector3.MoveTowards(transform.position, position, 7 * Time.deltaTime);
            Vector3 travel = next - transform.position;
            if (travel.sqrMagnitude > 0.0001f && !Physics.SphereCast(transform.position + Vector3.up, 1, travel.normalized, out _, travel.magnitude,
                ~(1 << 8), QueryTriggerInteraction.Ignore)) transform.position = next;
            if (Time.time >= nextSense)
            {
                nextSense = Time.time + 0.4f;
                target = Combatant.EnemyVisible(Team.Wild, entity.AimPoint, 65);
                if (target != null && (target.transform.position - home).sqrMagnitude > 6400) target = null;
            }
            if (target != null && target.Alive)
            {
                Vector3 flat = Vector3.ProjectOnPlane(target.AimPoint - entity.AimPoint, Vector3.up);
                if (flat.sqrMagnitude > 0.01f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(flat), 115 * Time.deltaTime);
            }
            if (fireAt > 0)
            {
                Phase = airburst ? "Airburst marked — change direction!" : "Ground barrage — move!";
                warning.enabled = true;
                for (int i = 0; i < 32; i++)
                {
                    float angle = i * Mathf.PI * 2 / 32;
                    warning.SetPosition(i, MarkedPoint + new Vector3(Mathf.Cos(angle) * 4, 0.12f, Mathf.Sin(angle) * 4));
                }
                if (Time.time >= fireAt)
                {
                    fireAt = 0; warning.enabled = false; ventsUntil = Time.time + 1.3f;
                    Vector3 muzzle = entity.AimPoint + transform.forward * 1.6f;
                    Vector3 direction = (MarkedPoint + Vector3.up * (airburst ? 0 : 0.5f) - muzzle).normalized;
                    for (int i = -1; i <= 1; i++)
                        CombatProjectile.Launch(muzzle, Quaternion.AngleAxis(i * (airburst ? 3 : 7), Vector3.up) * direction * 65,
                            Team.Wild, 40, 4, 12, transform);
                    volley++; Barrages++; nextAttack = Time.time + 3.5f;
                }
            }
            else if (target != null && target.Alive && Time.time >= nextAttack)
            {
                var player = target.GetComponent<PlaygroundPlayer>(); var bot = target.GetComponent<BotLocomotion>();
                Vector3 velocity = player != null ? player.Velocity : bot != null ? bot.Velocity : Vector3.zero;
                airburst = volley % 2 == 1;
                Vector3 predicted = target.AimPoint + Vector3.ClampMagnitude(velocity * 0.4f, 15);
                MarkedPoint = airburst ? predicted : WarDirector.GroundPoint(predicted);
                fireAt = Time.time + 0.9f;
                Phase = airburst ? "Airburst marked — change direction!" : "Ground barrage — move!";
                CombatEffects.Trace(entity.AimPoint, MarkedPoint, new Color(1, 0.4f, 0.1f));
                CombatAudio.Play(transform.position, 3, 0.35f);
                WarDirector.Instance.Noise(transform.position, Team.Wild, 140);
            }
            else Phase = Venting ? "Reactor exposed — attack!" : target != null ? "Armored front / flank reactor" : "Patrolling";
        }
    }
}
