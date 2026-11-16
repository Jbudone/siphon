using UnityEngine;

namespace Siphon
{
    // The accepted assault graybox. Independent of scene lifetime and the movement lab.
    // Candidate is deliberately still a 600m clone; changed geometry must get a new ID.
    public sealed class AssaultLayout
    {
        public const string DefaultId = "assault600-v1";
        public const string CandidateId = "assault-candidate600-v1";
        public static AssaultLayout Default { get; } = new AssaultLayout(DefaultId, "field-war-v1.json");
        public static AssaultLayout Candidate { get; } = new AssaultLayout(CandidateId, "field-war-candidate600-v1.json");
        public string Id { get; }
        public string SaveFileName { get; }
        public static AssaultLayout Resolve(string id)
        {
            if (string.IsNullOrEmpty(id) || id == DefaultId) return Default;
            if (id == CandidateId) return Candidate;
            throw new System.InvalidOperationException("Unknown assault layout: " + id);
        }
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
        private AssaultLayout(string id, string saveFileName) { Id = id; SaveFileName = saveFileName; }

        // Strict outside comparison keeps points exactly on each inset boundary valid.
        public bool Outside(Vector3 point, float inset) =>
            Mathf.Abs(point.x) > HalfExtent - inset || Mathf.Abs(point.z) > HalfExtent - inset;

        public Vector2 MapPoint(Vector3 point, Rect map) => new Vector2(
            map.x + Mathf.Clamp01((point.x + HalfExtent) / Size) * map.width,
            map.y + (1 - Mathf.Clamp01((point.z + HalfExtent) / Size)) * map.height);

        public static Vector3 Planar(Vector2 point) => new Vector3(point.x, 0, point.y);
    }
}
