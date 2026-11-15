using UnityEngine;

namespace Siphon
{
    public sealed class NavigationField : ScriptableObject
    {
        public Vector2 origin = AssaultLayout.Default.Minimum;
        public float cellSize = AssaultLayout.Default.NavigationSpacing;
        public int width = AssaultLayout.Default.NavigationWidth;
        public float[] heights;
        public bool[] walkable;
        public int Count => width * width;
        public Vector3 Point(int index) => new Vector3(origin.x + (index % width) * cellSize, heights[index], origin.y + (index / width) * cellSize);
        public int Index(Vector3 point) => Mathf.Clamp(Mathf.RoundToInt((point.z - origin.y) / cellSize), 0, width - 1) * width
            + Mathf.Clamp(Mathf.RoundToInt((point.x - origin.x) / cellSize), 0, width - 1);
    }
}
