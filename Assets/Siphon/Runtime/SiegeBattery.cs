using UnityEngine;

namespace Siphon
{
    public static class Ballistics
    {
        public static bool TryVelocity(Vector3 from, Vector3 to, float speed, float gravity, out Vector3 velocity)
        {
            velocity = Vector3.zero;
            Vector3 delta = to - from, flat = Vector3.ProjectOnPlane(delta, Vector3.up);
            float distance = flat.magnitude, square = speed * speed;
            if (distance < 0.01f || speed <= 0 || gravity <= 0) return false;
            float discriminant = square * square - gravity * (gravity * distance * distance + 2 * delta.y * square);
            if (discriminant < 0 || float.IsNaN(discriminant) || float.IsInfinity(discriminant)) return false;
            float angle = Mathf.Atan((square - Mathf.Sqrt(discriminant)) / (gravity * distance));
            velocity = flat.normalized * (Mathf.Cos(angle) * speed) + Vector3.up * (Mathf.Sin(angle) * speed);
            return true;
        }
    }

    [RequireComponent(typeof(DefenseStructure))]
    public sealed class SiegeBattery : MonoBehaviour
    {
        public const float ShellSpeed = 68, ShellGravity = 18;
        public static ResourceBag Ammunition => new ResourceBag(2, alloy: 1);
        public string Status { get; private set; } = "Awaiting target mark";
        public int Shots { get; private set; }
        public bool Charging => fireAt > 0;
        private DefenseStructure defense;
        private LineRenderer guide;
        private float fireAt, nextShot;
        private Vector3 marked, shellVelocity;
        private void Awake()
        {
            defense = GetComponent<DefenseStructure>();
            var root = new GameObject("Siege charge warning"); root.transform.SetParent(transform, false);
            guide = root.AddComponent<LineRenderer>(); guide.sharedMaterial = CombatEffects.LineMaterial;
            guide.positionCount = 24; guide.startWidth = guide.endWidth = 0.12f;
            guide.startColor = guide.endColor = new Color(1, 0.55f, 0.15f); guide.enabled = false;
        }
        public void ResetFireControl() { fireAt = 0; nextShot = Time.time + 2; guide.enabled = false; Status = "Awaiting target mark"; }
        private void Update()
        {
            var war = WarDirector.Instance;
            guide.enabled = false;
            if (war == null || war.Finished || !defense.Entity.Alive) { fireAt = 0; return; }
            if (!defense.Powered) { fireAt = 0; Status = "Power offline"; return; }
            if (defense.Entity.team != Team.Blue || !war.RallyActive) { fireAt = 0; Status = "Awaiting target mark"; return; }
            if (!war.RallyFireAllowed) { fireAt = 0; Status = "Holding fire at friendly rally"; return; }
            Vector3 pivot = transform.position + Vector3.up * 2.8f;
            Vector3 point = war.SiegeAimPoint;
            float range = Vector3.ProjectOnPlane(point - pivot, Vector3.up).magnitude;
            if (range < 35 || range > 230) { fireAt = 0; Status = "Target outside 35–230m range"; return; }
            if (!war.StockBag(defense.Entity.team).Covers(Ammunition)) { fireAt = 0; Status = "Need 2 Fe + 1 alloy per shell"; return; }
            if (!Ballistics.TryVelocity(pivot, point, ShellSpeed, ShellGravity, out Vector3 velocity))
            { fireAt = 0; Status = "Target beyond ballistic reach"; return; }
            if (fireAt > 0 && (point - marked).sqrMagnitude > 1) fireAt = 0;
            var barrel = defense.barrel;
            if (barrel != null) barrel.rotation = Quaternion.RotateTowards(barrel.rotation, Quaternion.LookRotation(velocity), 60 * Time.deltaTime);
            if (Time.time < nextShot) { Status = "Reloading"; return; }
            if (barrel != null && Vector3.Angle(barrel.forward, velocity) > 5) { Status = "Traversing to target"; return; }
            if (fireAt <= 0)
            {
                marked = point; shellVelocity = velocity; fireAt = Time.time + 2;
                war.Noise(transform.position, defense.Entity.team, 220); CombatAudio.Play(transform.position, 3, 0.35f);
            }
            Status = "Charging shell · take cover";
            guide.enabled = true;
            Vector3 warningStart = pivot + shellVelocity.normalized * 3.2f;
            Vector3 warningVelocity = Ballistics.TryVelocity(warningStart, marked, ShellSpeed, ShellGravity, out Vector3 solved) ? solved : shellVelocity;
            float flight = Vector3.ProjectOnPlane(marked - warningStart, Vector3.up).magnitude / Mathf.Max(1, Vector3.ProjectOnPlane(warningVelocity, Vector3.up).magnitude);
            for (int i = 0; i < guide.positionCount; i++)
            { float time = flight * i / (guide.positionCount - 1); guide.SetPosition(i, warningStart + warningVelocity * time + Vector3.down * (ShellGravity * time * time * 0.5f)); }
            if (Time.time < fireAt) return;
            if (!war.Spend(defense.Entity.team, Ammunition)) { fireAt = 0; return; }
            Vector3 muzzle = pivot + shellVelocity.normalized * 3.2f;
            // Solve again at the barrel mouth so its offset does not skew long shots.
            if (!Ballistics.TryVelocity(muzzle, marked, ShellSpeed, ShellGravity, out shellVelocity)) shellVelocity = velocity;
            CombatProjectile.Launch(muzzle, shellVelocity, defense.Entity.team, 55, 5, 15, transform, ShellGravity, 3.3f);
            Shots++; fireAt = 0; nextShot = Time.time + 6; guide.enabled = false;
            war.Noise(transform.position, defense.Entity.team, 240);
        }
    }
}
