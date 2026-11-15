using System.Collections.Generic;
using UnityEngine;

namespace Siphon
{
    public sealed class NavigationWorld : MonoBehaviour
    {
        public static NavigationWorld Instance { get; private set; }
        public NavigationField field;
        public int Searches { get; private set; }
        public int Revision { get; private set; }
        public int FailedSearches { get; private set; }
        private bool[] blocked;
        private int[] visited, closed, parents;
        private float[] costs;
        private int stamp;
        private bool dirty = true;
        private readonly List<QueueNode> heap = new List<QueueNode>(512);
        private struct QueueNode { public int index; public float score; public QueueNode(int index, float score) { this.index = index; this.score = score; } }
        private void Awake()
        {
            Instance = this;
            if (field == null) return;
            blocked = new bool[field.Count]; visited = new int[field.Count]; closed = new int[field.Count]; parents = new int[field.Count]; costs = new float[field.Count];
        }
        private void OnDestroy() { if (Instance == this) Instance = null; }
        public void Invalidate() { dirty = true; Revision++; }
        private void Refresh()
        {
            System.Array.Clear(blocked, 0, blocked.Length);
            foreach (var actor in Combatant.All)
            {
                if (!actor.Alive || actor.kind == EntityKind.Player || actor.kind == EntityKind.Bot || actor.kind == EntityKind.Creature) continue;
                foreach (var collider in actor.GetComponentsInChildren<Collider>())
                {
                    if (!collider.enabled || collider.isTrigger) continue;
                    Bounds bounds = collider.bounds; bounds.Expand(1.3f);
                    int low = field.Index(bounds.min), high = field.Index(bounds.max);
                    for (int z = low / field.width; z <= high / field.width; z++)
                        for (int x = low % field.width; x <= high % field.width; x++) blocked[z * field.width + x] = true;
                }
            }
            dirty = false;
        }
        private bool Free(int index) => index >= 0 && index < field.Count && field.walkable[index] && !blocked[index];
        private int NearestFree(Vector3 point)
        {
            int start = field.Index(point);
            if (Free(start)) return start;
            int sx = start % field.width, sz = start / field.width;
            for (int radius = 1; radius <= 8; radius++)
            {
                int best = -1; float distance = float.MaxValue;
                for (int z = sz - radius; z <= sz + radius; z++)
                    for (int x = sx - radius; x <= sx + radius; x++)
                    {
                        if (x < 0 || z < 0 || x >= field.width || z >= field.width) continue;
                        int index = z * field.width + x;
                        if (!Free(index)) continue;
                        float d = (field.Point(index) - point).sqrMagnitude;
                        if (d < distance) { distance = d; best = index; }
                    }
                if (best >= 0) return best;
            }
            return -1;
        }
        public bool FindPath(Vector3 from, Vector3 to, List<Vector3> path)
        {
            path.Clear();
            if (field == null || blocked == null) return false;
            if (dirty) Refresh();
            Searches++;
            int first = NearestFree(from), goal = NearestFree(to);
            if (first < 0 || goal < 0) { FailedSearches++; return false; }
            stamp++;
            heap.Clear(); visited[first] = stamp; costs[first] = 0; parents[first] = -1;
            Push(new QueueNode(first, Heuristic(first, goal)));
            int steps = 0;
            while (heap.Count > 0 && steps++ < 12000)
            {
                int current = Pop().index;
                if (closed[current] == stamp) continue;
                closed[current] = stamp;
                if (current == goal)
                {
                    for (int index = goal; index >= 0; index = parents[index]) path.Add(field.Point(index));
                    path.Reverse();
                    return true;
                }
                int cx = current % field.width, cz = current / field.width;
                for (int dz = -1; dz <= 1; dz++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dz == 0) continue;
                        int x = cx + dx, z = cz + dz;
                        if (x < 0 || z < 0 || x >= field.width || z >= field.width) continue;
                        int next = z * field.width + x;
                        if (!Free(next) || closed[next] == stamp) continue;
                        if (dx != 0 && dz != 0 && (!Free(cz * field.width + x) || !Free(z * field.width + cx))) continue;
                        float rise = Mathf.Abs(field.heights[next] - field.heights[current]);
                        float run = field.cellSize * (dx != 0 && dz != 0 ? 1.4142f : 1);
                        if (rise > run * 0.9f) continue;
                        float cost = costs[current] + run + rise * 0.4f;
                        if (visited[next] == stamp && costs[next] <= cost) continue;
                        visited[next] = stamp; costs[next] = cost; parents[next] = current;
                        Push(new QueueNode(next, cost + Heuristic(next, goal)));
                    }
            }
            FailedSearches++;
            return false;
        }
        private float Heuristic(int a, int b) => Vector2.Distance(new Vector2(a % field.width, a / field.width), new Vector2(b % field.width, b / field.width)) * field.cellSize;
        private void Push(QueueNode value)
        {
            heap.Add(value); int index = heap.Count - 1;
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (heap[parent].score <= value.score) break;
                heap[index] = heap[parent]; index = parent;
            }
            heap[index] = value;
        }
        private QueueNode Pop()
        {
            QueueNode result = heap[0], tail = heap[heap.Count - 1]; heap.RemoveAt(heap.Count - 1);
            int index = 0;
            while (index * 2 + 1 < heap.Count)
            {
                int child = index * 2 + 1;
                if (child + 1 < heap.Count && heap[child + 1].score < heap[child].score) child++;
                if (heap[child].score >= tail.score) break;
                heap[index] = heap[child]; index = child;
            }
            if (heap.Count > 0) heap[index] = tail;
            return result;
        }
    }
}
