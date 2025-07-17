using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
class SpatialGrid
    {
        float cellSize;
        Vector3 minB, maxB;
        ConcurrentDictionary<Vector3Int, ConcurrentBag<int>> grid =
            new ConcurrentDictionary<Vector3Int, ConcurrentBag<int>>();

        public SpatialGrid(float cs, Vector3 min, Vector3 max)
        {
            cellSize = Mathf.Max(cs, 0.001f);
            minB = min; maxB = max;
        }

        public Vector3Int Key(Vector3 p) => new Vector3Int(
            Mathf.FloorToInt((p.x - minB.x) / cellSize),
            Mathf.FloorToInt((p.y - minB.y) / cellSize),
            Mathf.FloorToInt((p.z - minB.z) / cellSize)
        );

        public void Add(Vector3 p, int idx)
        {
            var k = Key(p);
            grid.GetOrAdd(k, _ => new ConcurrentBag<int>()).Add(idx);
        }

        public List<int> Radius(Vector3 p, float r)
        {
            var center = Key(p);
            int cr = Mathf.CeilToInt(r / cellSize);
            var res = new List<int>();
            for (int dx = -cr; dx <= cr; dx++)
            for (int dy = -cr; dy <= cr; dy++)
            for (int dz = -cr; dz <= cr; dz++)
            {
                var key = new Vector3Int(center.x + dx, center.y + dy, center.z + dz);
                if (grid.TryGetValue(key, out var bag))
                    res.AddRange(bag);
            }
            return res;
        }

        public void Clear() => grid.Clear();
        public void Build(Vector3[] pts)
        {
            Clear();
            Parallel.For(0, pts.Length, i => Add(pts[i], i));
        }
    }