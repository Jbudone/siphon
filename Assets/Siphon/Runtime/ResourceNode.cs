using System.Collections.Generic;
using UnityEngine;

namespace Siphon
{
    public sealed class ResourceNode : MonoBehaviour
    {
        public static readonly List<ResourceNode> All = new List<ResourceNode>();
        public int capacity = 40;
        public int remaining = 40;
        public bool rare;
        public ResourceKind kind;
        private float restoresAt;
        public float RechargeSeconds => remaining == 0 ? Mathf.Max(0, restoresAt - Time.time) : 0;
        private float nextNoise;
        private Renderer[] visuals;
        private Collider[] shapes;
        private void OnEnable() { All.Add(this); }
        private void OnDisable() { All.Remove(this); }
        private void Awake() { visuals = GetComponentsInChildren<Renderer>(); shapes = GetComponentsInChildren<Collider>(); }
        public int Mine(int amount, Team team)
        {
            if (remaining <= 0 || amount <= 0) return 0;
            int mined = Mathf.Min(amount, remaining);
            remaining -= mined;
            CombatEffects.Burst(transform.position + Vector3.up, rare ? Color.magenta : Color.yellow, 3);
            if (Time.time >= nextNoise)
            {
                WarDirector.Instance?.Noise(transform.position, team, rare ? 110 : 65);
                nextNoise = Time.time + 1;
            }
            if (remaining == 0)
            {
                restoresAt = Time.time + 60;
                foreach (var visual in visuals) visual.enabled = false;
                foreach (var shape in shapes) shape.enabled = false;
            }
            return mined;
        }
        public void ApplyRemaining(int amount, float recharge)
        {
            remaining = Mathf.Clamp(amount, 0, capacity);
            if (remaining == 0 && recharge <= 0) remaining = capacity;
            restoresAt = Time.time + recharge;
            foreach (var visual in visuals) visual.enabled = remaining > 0;
            foreach (var shape in shapes) shape.enabled = remaining > 0;
        }
        private void Update()
        {
            if (remaining > 0 || Time.time < restoresAt) return;
            remaining = capacity;
            foreach (var visual in visuals) visual.enabled = true;
            foreach (var shape in shapes) shape.enabled = true;
        }
    }
}
