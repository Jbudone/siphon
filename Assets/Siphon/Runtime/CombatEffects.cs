using UnityEngine;

namespace Siphon
{
    public static class CombatEffects
    {
        private static Material lines;
        public static Material LineMaterial
        {
            get
            {
                if (lines == null) lines = Resources.Load<Material>("CombatLines");
                return lines;
            }
        }
        public static void Trace(Vector3 start, Vector3 end, Color color, float width = 0.06f, float duration = 0.08f)
        {
            var line = new GameObject("Tracer").AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.sharedMaterial = LineMaterial;
            line.startColor = line.endColor = color;
            line.startWidth = width;
            line.endWidth = width * 0.4f;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            Object.Destroy(line.gameObject, duration);
        }
        public static void Burst(Vector3 point, Color color, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 direction = Random.onUnitSphere;
                Trace(point, point + direction * Random.Range(0.4f, 2f), color, 0.08f, 0.22f);
            }
        }
    }
}
