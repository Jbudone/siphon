using System.Collections.Generic;
using UnityEngine;

namespace Siphon
{
    public sealed class DroppedCargo : MonoBehaviour
    {
        public static readonly List<DroppedCargo> All = new List<DroppedCargo>();
        public ResourceBag contents;
        public bool relic;
        public float expiresAt;
        [System.Serializable] public sealed class SavedState
        { public Vector3 position; public ResourceBag contents; public bool relic; public float seconds; }
        public SavedState CaptureState() => new SavedState { position = transform.position, contents = contents, relic = relic, seconds = Mathf.Max(0, expiresAt - Time.time) };
        private void OnEnable() { All.Add(this); }
        private void OnDisable() { All.Remove(this); }
        public static DroppedCargo Spawn(Vector3 point, ResourceBag contents, bool relic = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = relic ? "Warden salvage" : "Dropped supplies";
            go.transform.position = WarDirector.GroundPoint(point) + Vector3.up * 0.6f;
            go.transform.localScale = Vector3.one * 0.8f;
            go.GetComponent<Renderer>().sharedMaterial = WarDirector.Instance != null ? WarDirector.Instance.wildMaterial : CombatEffects.LineMaterial;
            var cargo = go.AddComponent<DroppedCargo>(); cargo.contents = contents; cargo.relic = relic;
            cargo.expiresAt = Time.time + 180;
            if (relic)
            {
                WarDirector.Part(go.transform, PrimitiveType.Cylinder, "Capacitor spindle", Vector3.up * 0.7f,
                    new Vector3(0.3f, 0.4f, 0.3f), CombatEffects.LineMaterial, false);
                var ring = new GameObject("Salvage signature"); ring.transform.SetParent(go.transform, false);
                var line = ring.AddComponent<LineRenderer>(); line.sharedMaterial = CombatEffects.LineMaterial;
                line.useWorldSpace = false; line.loop = true; line.positionCount = 24; line.startWidth = line.endWidth = 0.06f;
                line.startColor = line.endColor = new Color(0.9f, 0.5f, 1);
                for (int i = 0; i < 24; i++)
                { float angle = i * Mathf.PI * 2 / 24; line.SetPosition(i, new Vector3(Mathf.Cos(angle) * 0.7f, 0.6f, Mathf.Sin(angle) * 0.7f)); }
            }
            return cargo;
        }
        private void Update()
        {
            transform.Rotate(0, 35 * Time.deltaTime, 0);
            if (Time.time >= expiresAt) Destroy(gameObject);
        }
        public ResourceBag Take()
        {
            var result = contents; contents = default;
            Destroy(gameObject); return result;
        }
    }
}
