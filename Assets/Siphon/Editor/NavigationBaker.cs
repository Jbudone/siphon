using UnityEditor;
using UnityEngine;

namespace Siphon.Editor
{
    public static class NavigationBaker
    {
        public static void Bake()
        {
            const string path = "Assets/Siphon/Settings/FieldNavigation.asset";
            var field = AssetDatabase.LoadAssetAtPath<NavigationField>(path);
            bool fresh = field == null;
            if (fresh) field = ScriptableObject.CreateInstance<NavigationField>();
            field.width = 151; field.cellSize = 4; field.origin = new Vector2(-300, -300);
            field.heights = new float[field.Count]; field.walkable = new bool[field.Count];
            Physics.SyncTransforms();
            var overlaps = new Collider[24];
            for (int i = 0; i < field.Count; i++)
            {
                Vector3 point = field.Point(i);
                if (!Physics.Raycast(point + Vector3.up * 150, Vector3.down, out RaycastHit floor, 300, 1 << 9)) continue;
                field.heights[i] = floor.point.y;
                if (floor.normal.y < 0.72f) continue;
                int count = Physics.OverlapCapsuleNonAlloc(floor.point + Vector3.up * 0.9f, floor.point + Vector3.up * 1.7f,
                    0.65f, overlaps, ~((1 << 8) | (1 << 9)), QueryTriggerInteraction.Ignore);
                bool clear = true;
                for (int j = 0; j < count; j++)
                    if (overlaps[j].GetComponentInParent<Combatant>() == null) { clear = false; break; }
                field.walkable[i] = clear;
            }
            if (fresh) AssetDatabase.CreateAsset(field, path); else EditorUtility.SetDirty(field);
            var world = new GameObject("Bot navigation").AddComponent<NavigationWorld>(); world.field = field;
            Debug.Log("Siphon navigation baked: " + field.Count + " terrain cells.");
        }
    }
}
