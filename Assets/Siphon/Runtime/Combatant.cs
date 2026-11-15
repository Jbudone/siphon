using System;
using System.Collections.Generic;
using UnityEngine;

namespace Siphon
{
    public enum Team { Blue, Red, Wild }
    public enum EntityKind { Player, Bot, Core, Generator, Turret, Outpost, Creature, Wall, Radar, ForceField, Trap, Processor, SiegeCannon }

    public sealed class Combatant : MonoBehaviour
    {
        public static readonly List<Combatant> All = new List<Combatant>();
        public Team team;
        public EntityKind kind;
        public float maximumHealth = 100;
        public float health = 100;
        public Vector3 aimOffset = Vector3.up;
        public float LastDamagedAt { get; private set; } = -100;
        public Vector3 LastDamageOrigin { get; private set; }
        public Team LastDamageTeam { get; private set; }
        public Combatant LastAttacker { get; private set; }
        public event Action<Combatant> Died;
        public event Action<float> Damaged;
        public bool Alive => health > 0;
        public Vector3 AimPoint => transform.position + aimOffset;
        public bool Shielded => kind == EntityKind.Core && WarDirector.Instance != null
            && WarDirector.Instance.IsPowered(team, transform.position, 40);

        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        private void OnDisable() { All.Remove(this); }
        public bool Damage(float amount, Team source, Vector3? origin = null, Combatant attacker = null)
        {
            if (!Alive || team == source || Shielded || amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount)) return false;
            var warden = GetComponent<WardenBrain>();
            if (warden != null && origin.HasValue) amount *= warden.DamageMultiplier(origin.Value);
            health = Mathf.Max(0, health - amount);
            LastDamagedAt = Time.time;
            LastDamageOrigin = origin ?? AimPoint; LastDamageTeam = source; LastAttacker = attacker;
            Damaged?.Invoke(amount);
            if (attacker != null) attacker.GetComponent<PlayerCombat>()?.ConfirmHit(!Alive);
            if (!Alive)
            {
                CombatEffects.Burst(AimPoint, TeamColor(team), 12);
                Died?.Invoke(this);
                WarDirector.Instance?.EntityKilled(this);
            }
            return true;
        }
        public float Repair(float amount)
        {
            if (!Alive || amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount)) return 0;
            float before = health;
            health = Mathf.Min(maximumHealth, health + amount);
            return health - before;
        }
        public void Restore() { health = maximumHealth; }
        public static Color TeamColor(Team value)
        {
            return value == Team.Blue ? new Color(0.15f, 0.8f, 1f)
                : value == Team.Red ? new Color(1f, 0.28f, 0.18f) : new Color(0.85f, 0.4f, 1f);
        }
        public static bool CanSee(Vector3 from, Combatant target)
        {
            Vector3 delta = target.AimPoint - from;
            return Physics.Raycast(from, delta.normalized, out RaycastHit hit, delta.magnitude + 0.5f,
                ~0, QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<Combatant>() == target;
        }
        public static Combatant EnemyVisible(Team team, Vector3 from, float range, bool actorsOnly = false)
        {
            Combatant best = null;
            float distance = range * range;
            foreach (var candidate in All)
            {
                if (!candidate.Alive || candidate.team == team || candidate.Shielded) continue;
                if (actorsOnly && candidate.kind != EntityKind.Player && candidate.kind != EntityKind.Bot
                    && candidate.kind != EntityKind.Creature) continue;
                var gear = candidate.GetComponent<FieldGear>();
                if (gear != null && !gear.VisibleFrom(from)) continue;
                float squared = (candidate.AimPoint - from).sqrMagnitude;
                if (squared >= distance || !CanSee(from, candidate)) continue;
                distance = squared;
                best = candidate;
            }
            return best;
        }
    }
}
