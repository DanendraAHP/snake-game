using System.Collections.Generic;
using UnityEngine;

namespace SerpentRogue
{
    // Splits the world into square cells so we only test collisions against
    // things that are nearby instead of everything on the map.
    public class SpatialHash<T>
    {
        readonly float size;
        readonly Dictionary<long, List<T>> cells = new Dictionary<long, List<T>>();
        readonly List<List<T>> used = new List<List<T>>();

        public SpatialHash(float cellSize) { size = cellSize; }

        static long Key(int cx, int cz)
        {
            return ((long)(cx + 100000) << 32) | (uint)(cz + 100000);
        }

        public void Clear()
        {
            for (int i = 0; i < used.Count; i++) used[i].Clear();
            used.Clear();
        }

        public void Insert(float x, float z, T item)
        {
            long k = Key(Mathf.FloorToInt(x / size), Mathf.FloorToInt(z / size));
            List<T> cell;
            if (!cells.TryGetValue(k, out cell))
            {
                cell = new List<T>();
                cells[k] = cell;
            }
            if (cell.Count == 0) used.Add(cell);
            cell.Add(item);
        }

        // Fills `results` with every item in the cells overlapping the circle (x, z, r).
        public void Query(float x, float z, float r, List<T> results)
        {
            results.Clear();
            int x0 = Mathf.FloorToInt((x - r) / size), x1 = Mathf.FloorToInt((x + r) / size);
            int z0 = Mathf.FloorToInt((z - r) / size), z1 = Mathf.FloorToInt((z + r) / size);
            for (int cx = x0; cx <= x1; cx++)
                for (int cz = z0; cz <= z1; cz++)
                {
                    List<T> cell;
                    if (cells.TryGetValue(Key(cx, cz), out cell)) results.AddRange(cell);
                }
        }
    }
}
