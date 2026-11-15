using UnityEngine;

namespace Siphon
{
    // The accepted assault graybox. Independent of scene lifetime and the movement lab.
    // A future large-map candidate must also isolate generated assets and saves.
    public sealed class AssaultLayout
    {
        public static AssaultLayout Default { get; } = new AssaultLayout();
        public float Size => 600;
        public float HalfExtent => Size * 0.5f;
        public float RecoveryInset => 5;
        public float SaveInset => 10;
        public int TerrainCells => 100;
        public float NavigationSpacing => 4;
        public int NavigationWidth => Mathf.RoundToInt(Size / NavigationSpacing) + 1;
        public Vector2 Minimum => new Vector2(-HalfExtent, -HalfExtent);
        public Vector2 BlueBase => new Vector2(0, -185);
        public Vector2 RedBase => new Vector2(0, 185);
        public Vector2 BlueSpawn => new Vector2(-7, -178);
        public Vector2 RedSpawn => new Vector2(7, 175);
        public Vector2 FrostCamp => new Vector2(228, 92);
        public Vector2 ForestCamp => new Vector2(-225, -20);
        private AssaultLayout() { }

        // Strict outside comparison keeps points exactly on each inset boundary valid.
        public bool Outside(Vector3 point, float inset) =>
            Mathf.Abs(point.x) > HalfExtent - inset || Mathf.Abs(point.z) > HalfExtent - inset;

        public Vector2 MapPoint(Vector3 point, Rect map) => new Vector2(
            map.x + Mathf.Clamp01((point.x + HalfExtent) / Size) * map.width,
            map.y + (1 - Mathf.Clamp01((point.z + HalfExtent) / Size)) * map.height);

        public static Vector3 Planar(Vector2 point) => new Vector3(point.x, 0, point.y);
    }
}
