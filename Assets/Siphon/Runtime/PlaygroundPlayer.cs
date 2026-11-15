using UnityEngine;

namespace Siphon
{
    public struct MovementCommand
    {
        public Vector2 move;
        public bool jump;
        public bool ski;
        public bool jet;
        public bool grapple;
        public Vector3 grapplePoint;
    }

    [RequireComponent(typeof(CharacterController))]
    public sealed class PlaygroundPlayer : MonoBehaviour
    {
        public MovementTuning tuning;
        public Camera view;
        public Vector3 Velocity => velocity;
        public float JetCapacityMultiplier { get; set; } = 1;
        public float FuelCapacity => tuning.jetCapacity * JetCapacityMultiplier;
        public float FuelFraction => fuel / FuelCapacity;
        public bool Grounded => grounded;
        public bool Skiing => skiing;
        public bool Grappling => grappling;
        public float PeakSpeed => peakSpeed;
        public bool ControlsActive => Cursor.lockState == CursorLockMode.Locked && (combat == null || combat.Menu == 0);
        private CharacterController body;
        private Vector3 velocity;
        private Vector3 spawn;
        private Quaternion spawnRotation;
        private float pitch;
        private float fuel;
        private bool grounded;
        private bool skiing;
        private bool grappling;
        private Vector3 anchor;
        private LineRenderer rope;
        private float nextShot;
        private float nextJetNoise;
        private int hits;
        private float peakSpeed;
        private float coyoteTime;
        private PlayerCombat combat;

        private void Awake()
        {
            body = GetComponent<CharacterController>();
            combat = GetComponent<PlayerCombat>();
            spawn = transform.position;
            spawnRotation = transform.rotation;
            fuel = FuelCapacity;
            rope = gameObject.AddComponent<LineRenderer>();
            rope.positionCount = 2;
            rope.startWidth = rope.endWidth = 0.06f;
            rope.sharedMaterial = CombatEffects.LineMaterial;
            rope.startColor = rope.endColor = Color.cyan;
            rope.enabled = false;
            LockCursor();
        }
        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            bool active = ControlsActive && (combat == null || combat.Entity.Alive)
                && (WarDirector.Instance == null || !WarDirector.Instance.Finished);
            if (!ControlsActive && (combat == null || combat.Menu == 0) && Input.GetMouseButtonDown(0)) LockCursor();
            if (active)
            {
                transform.Rotate(0, Input.GetAxisRaw("Mouse X") * tuning.mouseSensitivity, 0);
                pitch = Mathf.Clamp(pitch - Input.GetAxisRaw("Mouse Y") * tuning.mouseSensitivity, -89, 89);
            }
            view.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            if (active && Input.GetKeyDown(KeyCode.E) && Physics.Raycast(view.transform.position,
                view.transform.forward, out RaycastHit hook, tuning.grappleRange, ~0, QueryTriggerInteraction.Ignore)) anchor = hook.point;
            var command = new MovementCommand
            {
                move = active ? new Vector2((Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0),
                    (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0)) : Vector2.zero,
                jump = active && Input.GetKeyDown(KeyCode.Space),
                ski = active && Input.GetKey(KeyCode.LeftShift),
                jet = active && Input.GetMouseButton(1) && (combat == null || !combat.Building),
                grapple = active && Input.GetKey(KeyCode.E) && anchor != Vector3.zero,
                grapplePoint = anchor
            };
            float remaining = Mathf.Min(Time.deltaTime, 0.15f);
            // Small sweeps make collision and slope transitions stable at high speed.
            while (remaining > 0)
            {
                float dt = Mathf.Min(remaining, 1f / 120f);
                Simulate(command, dt);
                command.jump = false;
                remaining -= dt;
            }
            if (command.jet && fuel > 0 && Time.time >= nextJetNoise)
            { nextJetNoise = Time.time + 1; WarDirector.Instance?.Noise(transform.position, combat != null ? combat.Entity.team : Team.Blue, 65); }
            view.fieldOfView = Mathf.Lerp(view.fieldOfView, 100 + Mathf.Clamp(velocity.magnitude - 20, 0, 65) * 0.18f, Time.deltaTime * 5);
            rope.enabled = grappling;
            if (grappling)
            {
                rope.SetPosition(0, view.transform.position + view.transform.right * 0.3f - view.transform.up * 0.2f);
                rope.SetPosition(1, anchor);
            }
            if (combat == null && active && Input.GetMouseButton(0) && Time.time >= nextShot) FireLab();
            if (active && Input.GetKeyDown(KeyCode.R)) ResetTo(spawn, spawnRotation);
            if (transform.position.y < -80) ResetTo(spawn, spawnRotation);
        }

        public void Simulate(MovementCommand command, float dt)
        {
            if (!body.enabled || dt <= 0) return;
            skiing = command.ski;
            Vector2 input = Vector2.ClampMagnitude(command.move, 1);
            Vector3 wish = transform.right * input.x + transform.forward * input.y;
            Vector3 feet = transform.position + body.center - Vector3.up * body.height * 0.5f;
            grounded = Physics.SphereCast(feet + Vector3.up * 0.55f, body.radius * 0.85f, Vector3.down,
                out RaycastHit ground, 0.3f, ~(1 << 8), QueryTriggerInteraction.Ignore)
                && ground.normal.y > 0.25f && Vector3.Dot(velocity, ground.normal) <= 0.1f;
            coyoteTime = grounded ? 0.1f : Mathf.Max(0, coyoteTime - dt);
            if (grounded)
            {
                velocity = Vector3.ProjectOnPlane(velocity, ground.normal);
                wish = Vector3.ProjectOnPlane(wish, ground.normal).normalized * wish.magnitude;
                if (skiing)
                {
                    velocity += Vector3.ProjectOnPlane(Vector3.down * tuning.gravity, ground.normal) * dt;
                    Accelerate(wish, tuning.skiAcceleration, dt);
                }
                else
                {
                    velocity *= Mathf.Exp(-tuning.groundFriction * dt);
                    Accelerate(wish, tuning.groundAcceleration, dt);
                }
            }
            else Accelerate(wish, tuning.airAcceleration, dt);
            if (command.jump && coyoteTime > 0)
            {
                velocity.y = Mathf.Max(0, velocity.y) + tuning.jumpSpeed;
                grounded = false;
                coyoteTime = 0;
            }
            if (command.jet && fuel > 0)
            {
                grounded = false;
                float burn = Mathf.Min(dt, fuel);
                velocity += Vector3.up * tuning.jetAcceleration * burn;
                fuel -= burn;
            }
            else if (!command.jet) fuel = Mathf.Min(FuelCapacity, fuel + tuning.jetRecharge * dt);
            grappling = command.grapple;
            anchor = command.grapplePoint;
            if (grappling)
            {
                Vector3 toAnchor = anchor - view.transform.position;
                if (toAnchor.magnitude < 3) grappling = false;
                else velocity += toAnchor.normalized * tuning.grappleAcceleration * dt;
            }
            if (!grounded) velocity += Vector3.down * tuning.gravity * dt;
            velocity = Vector3.ClampMagnitude(velocity, tuning.maximumSpeed);
            body.Move((velocity + (grounded ? Vector3.down * 2 : Vector3.zero)) * dt);
            peakSpeed = Mathf.Max(peakSpeed, velocity.magnitude);
        }
        private void Accelerate(Vector3 wish, float acceleration, float dt)
        {
            float available = tuning.groundSpeed * wish.magnitude - Vector3.Dot(velocity, wish.normalized);
            if (available > 0) velocity += wish.normalized * Mathf.Min(available, acceleration * dt);
        }
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            float inward = Vector3.Dot(velocity, hit.normal);
            if (inward < 0) velocity -= hit.normal * inward;
        }
        public void AddImpulse(Vector3 impulse) { velocity += impulse; }
        public void ResetTo(Vector3 position, Quaternion rotation)
        {
            body.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            body.enabled = true;
            velocity = Vector3.zero;
            pitch = 0;
            fuel = FuelCapacity;
            grappling = false;
            anchor = Vector3.zero;
            peakSpeed = 0;
        }
        private void FireLab()
        {
            nextShot = Time.time + 0.16f;
            Vector3 from = view.transform.position;
            Vector3 end = from + view.transform.forward * 300;
            if (Physics.Raycast(from, view.transform.forward, out RaycastHit hit, 300, ~0, QueryTriggerInteraction.Ignore))
            {
                end = hit.point;
                var target = hit.collider.GetComponent<PlaygroundTarget>();
                if (target != null) { target.Hit(); hits++; }
            }
            CombatEffects.Trace(from + view.transform.right * 0.25f - view.transform.up * 0.2f, end, Color.yellow);
        }
        private void OnGUI()
        {
            if (WarDirector.Instance != null) return;
            GUI.Box(new Rect(12, 12, 430, 132), "SIPHON / MOVEMENT PLAYGROUND");
            GUI.Label(new Rect(24, 38, 410, 100),
                $"Speed {velocity.magnitude:0.0} m/s    Peak {peakSpeed:0.0}    Hits {hits}\n" +
                $"Jet fuel {FuelFraction:P0}    {(skiing ? "SKI" : "RUN")}    {(grappling ? "GRAPPLE" : "")}\n" +
                "WASD move · Mouse aim · Space jump · Shift ski\n" +
                "RMB jet · Hold E grapple · LMB fire · R reset · Esc cursor");
            GUI.Label(new Rect(Screen.width / 2f - 5, Screen.height / 2f - 10, 20, 20), "+");
        }
        private void OnDestroy()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
