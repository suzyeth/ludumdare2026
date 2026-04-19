using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace PrismZone.Enemy
{
    /// <summary>
    /// Small BFS grid pathfinder backed by a collision Tilemap. Zero-allocation
    /// per call past the initial buffers — safe for WebGL, no external dependency.
    /// Grid space is the tilemap cell space; agents convert with WorldToCell/CellToWorld.
    /// </summary>
    public class TilemapPathfinder
    {
        private readonly Tilemap _walls;
        private readonly int _maxNodes;
        private readonly Dictionary<Vector3Int, Vector3Int> _cameFrom;
        private readonly Queue<Vector3Int> _frontier;
        private readonly HashSet<Vector3Int> _visited;
        private readonly List<Vector3Int> _reuseResult = new List<Vector3Int>(64);

        private static readonly Vector3Int[] Dirs =
        {
            new Vector3Int( 1, 0, 0), new Vector3Int(-1, 0, 0),
            new Vector3Int( 0, 1, 0), new Vector3Int( 0,-1, 0)
        };

        public TilemapPathfinder(Tilemap walls, int maxNodes = 4096)
        {
            _walls = walls;
            _maxNodes = maxNodes;
            _cameFrom = new Dictionary<Vector3Int, Vector3Int>(maxNodes);
            _frontier = new Queue<Vector3Int>(maxNodes);
            _visited = new HashSet<Vector3Int>();
        }

        public bool IsBlocked(Vector3Int cell)
        {
            if (_walls == null) return false;
            return _walls.HasTile(cell);
        }

        /// <summary>Returns path as cells from start (exclusive) to goal (inclusive). Empty if unreachable.</summary>
        public List<Vector3Int> FindPath(Vector3Int start, Vector3Int goal)
        {
            _reuseResult.Clear();
            if (start == goal) return _reuseResult;
            if (IsBlocked(goal)) return _reuseResult;

            _cameFrom.Clear();
            _frontier.Clear();
            _visited.Clear();

            _frontier.Enqueue(start);
            _visited.Add(start);
            int expanded = 0;
            bool found = false;

            while (_frontier.Count > 0 && expanded < _maxNodes)
            {
                var cur = _frontier.Dequeue();
                expanded++;
                if (cur == goal) { found = true; break; }

                for (int i = 0; i < Dirs.Length; i++)
                {
                    var n = cur + Dirs[i];
                    if (_visited.Contains(n)) continue;
                    if (IsBlocked(n)) continue;
                    _visited.Add(n);
                    _cameFrom[n] = cur;
                    _frontier.Enqueue(n);
                }
            }

            if (!found) return _reuseResult;

            var step = goal;
            while (step != start)
            {
                _reuseResult.Add(step);
                if (!_cameFrom.TryGetValue(step, out var prev)) { _reuseResult.Clear(); return _reuseResult; }
                step = prev;
            }
            _reuseResult.Reverse();
            return _reuseResult;
        }
    }
}
