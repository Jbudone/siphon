using UnityEngine;

namespace Siphon
{
    public sealed class CombatProjectile : MonoBehaviour
    {
        public Team team;
        public Vector3 velocity;
        public float damage;
        public float radius;
        public float knockback;
        public float gravity;
        public float structureMultiplier = 1;
        private float expires;
        private Transform owner;
        private Combatant attacker;
        private Vector3 attackOrigin;

        public static CombatProjectile Launch(Vector3 position, Vector3 velocity, Team team, float damage,
            float radius = 0, float knockback = 0, Transform owner = null, float gravity = 0, float structureMultiplier = 1)
        {
            if (owner == null || owner.GetComponent<PlayerCombat>() == null) CombatAudio.Play(position, radius > 0 ? 1 : 0);
            var go = new GameObject("Projectile");
            go.transform.position = position;
            var projectile = go.AddComponent<CombatProjectile>();
            projectile.team = team;
            projectile.velocity = velocity;
            projectile.damage = damage;
            projectile.radius = radius;
            projectile.knockback = knockback;
            projectile.gravity = gravity; projectile.structureMultiplier = structureMultiplier;
            projectile.owner = owner; projectile.attackOrigin = position;
            projectile.attacker = owner != null ? owner.GetComponent<Combatant>() : null;
            projectile.expires = Time.time + 6;
            var trail = go.AddComponent<TrailRenderer>();
            trail.sharedMaterial = CombatEffects.LineMaterial;
            trail.startColor = Combatant.TeamColor(team);
            trail.endColor = Color.clear;
            trail.startWidth = radius > 0 ? 0.35f : 0.12f;
            trail.endWidth = 0;
            trail.time = 0.12f;
            return projectile;
        }

        private void Update()
        {
            if (Time.time > expires) { Destroy(gameObject); return; }
            float dt = Time.deltaTime;
            Vector3 movement = velocity * dt + Vector3.down * (gravity * dt * dt * 0.5f);
            velocity += Vector3.down * (gravity * dt);
            // Sweep the whole traveled segment to avoid tunneling through walls at speed.
            if (CombatPhysics.Cast(transform.position, movement.normalized, movement.magnitude + 0.08f,
                team, owner, out RaycastHit hit))
            {
                var barrier = hit.collider.GetComponentInParent<DefenseStructure>();
                if (hit.collider.isTrigger && barrier != null && barrier.BarrierActive)
                {
                    barrier.Absorb(damage * structureMultiplier, hit.point);
                    if (attacker != null) attacker.GetComponent<PlayerCombat>()?.ConfirmHit(false);
                    Destroy(gameObject);
                }
                else Impact(hit.point, hit.collider.GetComponentInParent<Combatant>());
                return;
            }
            transform.position += movement;
        }

        private void Impact(Vector3 point, Combatant direct)
        {
            if (radius <= 0 && direct != null) direct.Damage(DamageFor(direct), team, attackOrigin, attacker);
            else
            {
                foreach (var actor in Combatant.All.ToArray())
                {
                    if (actor == null || !actor.Alive) continue;
                    float distance = Vector3.Distance(actor.AimPoint, point);
                    if (distance > radius) continue;
                    // Splash cannot pass through solid cover.
                    Vector3 origin = point + (actor.AimPoint - point).normalized * 0.12f;
                    if (distance > 0.2f && CombatPhysics.Cast(origin, (actor.AimPoint - origin).normalized, distance, team, owner, out RaycastHit cover)
                        && (cover.collider.isTrigger || cover.collider.GetComponentInParent<Combatant>() != actor)) continue;
                    float falloff = Mathf.Lerp(1, 0.3f, distance / radius);
                    actor.Damage(DamageFor(actor) * falloff, team, point, attacker);
                    var player = actor.GetComponent<PlaygroundPlayer>();
                    if (knockback > 0)
                    {
                        Vector3 impulse = (actor.AimPoint - point).normalized * knockback * falloff;
                        if (player != null) player.AddImpulse(impulse);
                        actor.GetComponent<BotLocomotion>()?.AddImpulse(impulse);
                    }
                }
            }
            CombatEffects.Burst(point, Combatant.TeamColor(team), radius > 0 ? 18 : 4);
            CombatAudio.Play(point, 2, radius > 0 ? 0.4f : 0.12f);
            WarDirector.Instance?.Noise(point, team, radius > 0 ? 140 : 60);
            Destroy(gameObject);
        }
        private float DamageFor(Combatant actor) => damage * (actor.GetComponent<DefenseStructure>() != null ? structureMultiplier : 1);
    }
}
