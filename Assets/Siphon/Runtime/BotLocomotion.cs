using System.Collections.Generic;
using UnityEngine;

namespace Siphon
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class BotLocomotion : MonoBehaviour
    {
        public Vector3 Velocity { get; private set; }
        public bool Boosting { get; private set; }
        public bool Airborne { get; private set; }
        public int Boosts { get; private set; }
        public float Fuel => fuel / 1.4f;
        private CharacterController body;
        private Vector3 lateral, lastProgress;
        private float vertical, fuel = 1.4f, boostUntil, nextBoost, nextProgress, nextRoute;
        private int routeIndex, revision = -1;
        private Vector3 routeGoal;
        private readonly List<Vector3> route = new List<Vector3>();
        private LineRenderer exhaust;
        private void Awake()
        {
            body = GetComponent<CharacterController>(); lastProgress = transform.position;
            nextBoost = Time.time + 1 + Mathf.Abs(transform.position.x % 3);
            var root = new GameObject("Jet exhaust"); root.transform.SetParent(transform, false);
            exhaust = root.AddComponent<LineRenderer>(); exhaust.sharedMaterial = CombatEffects.LineMaterial;
            exhaust.positionCount = 2; exhaust.startWidth = 0.3f; exhaust.endWidth = 0.02f;
            exhaust.startColor = Color.cyan; exhaust.endColor = Color.clear; exhaust.enabled = false;
        }
        public void AddImpulse(Vector3 impulse)
        { lateral = Vector3.ClampMagnitude(lateral + Vector3.ProjectOnPlane(impulse, Vector3.up), 40); vertical += impulse.y; }
        public void ResetMotion()
        { lateral = Velocity = Vector3.zero; vertical = 0; fuel = 1.4f; route.Clear(); nextRoute = 0; boostUntil = 0; Boosting = Airborne = false; exhaust.enabled = false; }
        public void Tick(Vector3 goal, BotRole role, bool combat, bool facingTarget, float delta)
        {
            if (!body.enabled || delta <= 0) return;
            float dt = Mathf.Min(delta, 0.15f);
            Vector3 steering = Steer(goal);
            Vector3 direction = Vector3.ProjectOnPlane(steering - transform.position, Vector3.up);
            if (direction.magnitude < 1.5f) direction = Vector3.zero; else direction.Normalize();
            bool canJet = role == BotRole.Raider || role == BotRole.Scout;
            bool mobile = canJet || role == BotRole.Stalker;
            bool grounded = body.isGrounded && vertical <= 0;
            bool floor = Physics.SphereCast(transform.position + Vector3.up * 0.55f, body.radius * 0.8f, Vector3.down,
                out RaycastHit ground, 0.45f, ~(1 << 8), QueryTriggerInteraction.Ignore) && ground.normal.y > 0.65f;
            grounded |= floor && vertical <= 0;
            float speed = role == BotRole.Heavy ? 7 : role == BotRole.Scout ? 17 : role == BotRole.Stalker ? 18 : role == BotRole.Creature ? 13 : role == BotRole.Worker ? 11 : 14;
            bool obstacleAhead = false;
            if (direction != Vector3.zero && Physics.SphereCast(transform.position + Vector3.up * 1.3f, 0.55f, direction,
                out RaycastHit obstacle, 2.4f, ~(1 << 8), QueryTriggerInteraction.Ignore) && obstacle.normal.y < 0.5f)
            {
                obstacleAhead = true;
                Vector3 side = Vector3.Cross(Vector3.up, obstacle.normal).normalized;
                if (Vector3.Dot(side, direction) < 0) side = -side;
                direction = (side + obstacle.normal * 0.3f).normalized;
            }
            if (grounded)
            {
                vertical = -2;
                if (canJet && fuel > 0.7f && Time.time >= nextBoost && (combat || obstacleAhead)
                    && Vector3.ProjectOnPlane(goal - transform.position, Vector3.up).sqrMagnitude > 16)
                {
                    vertical = 10; boostUntil = Time.time + 0.55f; nextBoost = Time.time + 3.8f + Mathf.Abs(transform.position.x % 2);
                    Boosts++; grounded = false;
                    WarDirector.Instance?.Noise(transform.position, GetComponent<Combatant>().team, 65);
                }
            }
            Boosting = canJet && Time.time < boostUntil && fuel > 0;
            if (Boosting) { vertical += 32 * dt; fuel = Mathf.Max(0, fuel - dt); }
            else fuel = Mathf.Min(1.4f, fuel + 0.55f * dt);
            if (!grounded) vertical -= 26 * dt;
            Airborne = !grounded;
            Vector3 desired = direction * speed * (mobile && combat ? 1.25f : 1);
            lateral = Vector3.MoveTowards(lateral, desired, (grounded ? 80 : 24) * dt);
            if (mobile && floor && ground.normal.y < 0.985f && !combat && direction != Vector3.zero)
                lateral += Vector3.ProjectOnPlane(Vector3.down * 26, ground.normal) * dt;
            lateral = Vector3.ClampMagnitude(lateral, mobile ? 30 : speed);
            if (Time.time >= nextProgress)
            {
                if (direction != Vector3.zero && (transform.position - lastProgress).sqrMagnitude < 1 && grounded)
                { vertical = mobile ? 11 : 8; nextRoute = 0; }
                lastProgress = transform.position; nextProgress = Time.time + 2;
            }
            Vector3 separation = Vector3.zero;
            foreach (var other in BotBrain.All)
            {
                if (other.gameObject == gameObject || other.Entity == null || !other.Entity.Alive) continue;
                Vector3 gap = Vector3.ProjectOnPlane(transform.position - other.transform.position, Vector3.up);
                if (gap.sqrMagnitude > 0.01f && gap.sqrMagnitude < 2.25f) separation += gap.normalized * (1.5f - gap.magnitude);
            }
            if (!facingTarget && lateral.sqrMagnitude > 1)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(lateral), 220 * dt);
            Vector3 previous = transform.position;
            body.Move((lateral + Vector3.ClampMagnitude(separation * 5, 4) + Vector3.up * vertical) * dt);
            Velocity = (transform.position - previous) / dt;
            exhaust.enabled = Boosting;
            if (Boosting)
            {
                Vector3 pack = transform.position + Vector3.up - transform.forward * 0.45f;
                exhaust.SetPosition(0, pack); exhaust.SetPosition(1, pack - Vector3.up * 1.2f);
            }
            if (transform.position.y < -40 || Mathf.Abs(transform.position.x) > 295 || Mathf.Abs(transform.position.z) > 295)
            {
                var brain = GetComponent<BotBrain>(); body.enabled = false;
                transform.position = WarDirector.GroundPoint(brain.home) + Vector3.up * 0.1f;
                body.enabled = true; ResetMotion();
            }
        }
        private Vector3 Steer(Vector3 goal)
        {
            var navigation = NavigationWorld.Instance;
            if (navigation == null || Vector3.ProjectOnPlane(goal - transform.position, Vector3.up).sqrMagnitude < 64) return goal;
            if (Time.time >= nextRoute || (goal - routeGoal).sqrMagnitude > 64 || revision != navigation.Revision)
            {
                navigation.FindPath(transform.position, goal, route); routeGoal = goal; routeIndex = 0; revision = navigation.Revision;
                nextRoute = Time.time + 4 + Mathf.Abs(transform.position.x % 3) * 0.1f;
            }
            while (routeIndex < route.Count - 1 && Vector3.ProjectOnPlane(route[routeIndex] - transform.position, Vector3.up).sqrMagnitude < 4) routeIndex++;
            if (routeIndex >= route.Count) return goal;
            for (int i = Mathf.Min(routeIndex + 3, route.Count - 1); i > routeIndex; i--)
            {
                Vector3 delta = route[i] - transform.position;
                Vector3 flat = Vector3.ProjectOnPlane(delta, Vector3.up);
                if (Mathf.Abs(delta.y) > flat.magnitude * 0.8f || flat.sqrMagnitude < 1) continue;
                if (Physics.CapsuleCast(transform.position + Vector3.up * 0.6f, transform.position + Vector3.up * 1.6f, 0.65f,
                    flat.normalized, flat.magnitude, ~((1 << 8) | (1 << 9)), QueryTriggerInteraction.Ignore)) continue;
                routeIndex = i; break;
            }
            return route[routeIndex];
        }
    }
}
