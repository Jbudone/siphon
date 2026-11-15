using UnityEngine;

namespace Siphon
{
    // Local perception only: sound gives a fixed search point, never an actor to track.
    public sealed class StalkerBehaviour : MonoBehaviour
    {
        public string State { get; private set; } = "Prowling";
        public int Pounces { get; private set; }
        public int Bites { get; private set; }
        public int Investigations { get; private set; }
        public Vector3 InvestigatedPoint { get; private set; }
        private BotBrain brain;
        private Renderer[] eyes;
        private MaterialPropertyBlock tint;
        private float windupUntil, pounceUntil, readyAt, searchUntil;
        private Vector3 pouncePoint;
        private bool bitten;
        private void Awake()
        {
            tint = new MaterialPropertyBlock();
            brain = GetComponent<BotBrain>();
            var found = new System.Collections.Generic.List<Renderer>();
            foreach (var visual in GetComponentsInChildren<Renderer>())
                if (visual.name == "Stalker eye") found.Add(visual);
            eyes = found.ToArray();
            brain.Entity.Died += ResetAttack;
        }
        private void OnDestroy() { if (brain != null && brain.Entity != null) brain.Entity.Died -= ResetAttack; }
        private void ResetAttack(Combatant actor)
        { windupUntil = pounceUntil = searchUntil = 0; readyAt = Time.time + 1; State = "Prowling"; }
        public Vector3 Steer(Combatant target, Vector3 patrol)
        {
            var gear = target != null ? target.GetComponent<FieldGear>() : null;
            bool visible = target != null && target.Alive && Combatant.CanSee(brain.Entity.AimPoint, target)
                && (gear == null || gear.VisibleFrom(brain.Entity.AimPoint));
            Color color = windupUntil > 0 ? new Color(1, 0.45f, 0.05f) : new Color(0.25f, 1, 0.4f);
            tint.SetColor("_BaseColor", color); tint.SetColor("_EmissionColor", color * 2);
            foreach (var eye in eyes) eye.SetPropertyBlock(tint);
            if (windupUntil > 0)
            {
                State = "Pounce warning";
                if (Time.time < windupUntil) return transform.position;
                windupUntil = 0; pounceUntil = Time.time + 0.65f; readyAt = Time.time + 1.8f; bitten = false;
                Vector3 direction = Vector3.ProjectOnPlane(pouncePoint - transform.position, Vector3.up).normalized;
                brain.Motor.AddImpulse(direction * 20 + Vector3.up * 6); Pounces++;
                WarDirector.Instance.Noise(transform.position, Team.Wild, 55);
            }
            if (Time.time < pounceUntil)
            {
                State = "Pouncing";
                if (!bitten && visible && (target.AimPoint - brain.Entity.AimPoint).sqrMagnitude < 9)
                { target.Damage(25, Team.Wild, brain.Entity.AimPoint, brain.Entity); bitten = true; Bites++; }
                return pouncePoint;
            }
            if (visible)
            {
                Vector3 delta = Vector3.ProjectOnPlane(target.transform.position - transform.position, Vector3.up);
                if (Time.time < readyAt)
                { State = "Recovering"; return transform.position - delta.normalized * 4; }
                if (delta.sqrMagnitude < 81)
                {
                    pouncePoint = target.transform.position; windupUntil = Time.time + 0.45f;
                    CombatAudio.Play(transform.position, 3, 0.55f); State = "Pounce warning";
                    return transform.position;
                }
                State = "Stalking";
                return target.transform.position + Vector3.Cross(Vector3.up, delta.normalized) * Mathf.Sin(Time.time * 1.5f + brain.home.x) * 4;
            }
            if (WarDirector.Instance.Hear(Team.Wild, transform.position, out Vector3 heard)
                && (heard - brain.home).sqrMagnitude < 5625 && (searchUntil <= Time.time || (heard - InvestigatedPoint).sqrMagnitude > 1))
            { InvestigatedPoint = heard; searchUntil = Time.time + 10; Investigations++; }
            if (Time.time < searchUntil)
            { State = "Investigating noise"; return InvestigatedPoint; }
            State = "Prowling"; return patrol;
        }
    }
}
