using UnityEngine;

namespace Siphon
{
    public static class CombatPhysics
    {
        private static readonly RaycastHit[] buffer = new RaycastHit[64];
        public static bool Cast(Vector3 origin, Vector3 direction, float distance, Team team, Transform owner, out RaycastHit nearest)
        {
            int count = Physics.RaycastNonAlloc(origin, direction, buffer, distance, ~0, QueryTriggerInteraction.Collide);
            RaycastHit[] hits = buffer;
            if (count == buffer.Length) { hits = Physics.RaycastAll(origin, direction, distance, ~0, QueryTriggerInteraction.Collide); count = hits.Length; }
            nearest = default; float best = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (owner != null && hit.collider.transform.IsChildOf(owner)) continue;
                if (hit.collider.isTrigger)
                {
                    var barrier = hit.collider.GetComponentInParent<DefenseStructure>();
                    if (barrier == null || !barrier.BarrierActive || barrier.Entity.team == team) continue;
                }
                if (hit.distance >= best) continue;
                best = hit.distance; nearest = hit;
            }
            return best < float.MaxValue;
        }
    }
}
