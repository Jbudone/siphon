using UnityEngine;

namespace Siphon
{
    public sealed class PlaygroundTarget : MonoBehaviour
    {
        private float restoreAt;
        private Renderer visual;
        private Collider shape;
        private Vector3 origin;
        public float travel = 5f;
        private void Awake()
        {
            visual = GetComponent<Renderer>();
            shape = GetComponent<Collider>();
            origin = transform.position;
        }
        public void Hit()
        {
            visual.enabled = shape.enabled = false;
            restoreAt = Time.time + 1.5f;
        }
        private void Update()
        {
            transform.position = origin + Vector3.right * Mathf.Sin(Time.time * 1.2f + origin.z) * travel;
            if (!visual.enabled && Time.time >= restoreAt) visual.enabled = shape.enabled = true;
        }
    }
}
