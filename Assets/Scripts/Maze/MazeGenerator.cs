using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArrowNexus.Maze
{
    /// <summary>
    /// Procedural maze generation using a Hybrid Algorithm:
    ///   1. Recursive Backtracking (perfect maze skeleton)
    ///   2. Cellular Automata smoothing pass (organic feel)
    ///   3. Weighted Path Logic (multiple routes, controlled dead ends,
    ///      speed sections, secret paths)
    ///
    /// Output: a 2D int[,] grid where each cell holds a TileType value.
    /// </summary>
    public static class MazeGenerator
    {
        // ─── Tile Type Constants ─────────────────────────────────────────────────
        public const int WALL        = 0;
        public const int PATH        = 1;
        public const int START       = 2;
        public const int GOAL        = 3;
        public const int HAZARD      = 4;
        public const int SIGNAL_NODE = 5;
        public const int SECRET      = 6;   // hidden branch paths
        public const int SPEED_ZONE  = 7;   // wide open speed sections
        public const int DEAD_END    = 8;   // intentional dead-end markers

        // ─── Generation Settings ─────────────────────────────────────────────────

        [Serializable]
        public struct MazeSettings
        {
            public int   Width;
            public int   Height;
            public int   Seed;
            public float SecretPathChance;    // 0–1  (doc: ~5%)
            public float HazardDensity;       // 0–1
            public float SignalNodeDensity;   // 0–1
            public float DeadEndFactor;       // 0–1  (controls how many branches dead-end)
            public int   CellularIterations;  // smoothing passes (2–4 recommended)
            public int   MultiRouteBias;      // extra connections added to break perfect maze

            public static MazeSettings Default => new()
            {
                Width               = 25,
                Height              = 25,
                Seed                = 0,
                SecretPathChance    = 0.05f,
                HazardDensity       = 0.08f,
                SignalNodeDensity   = 0.04f,
                DeadEndFactor       = 0.3f,
                CellularIterations  = 3,
                MultiRouteBias      = 6
            };
        }

        // ─── Public API ──────────────────────────────────────────────────────────

        /// <summary>
        /// Generates a complete maze grid from the given settings.
        /// If Seed == 0, a random seed is used each time.
        /// </summary>
        public static int[,] Generate(MazeSettings settings, out Vector2Int startCell, out Vector2Int goalCell)
        {
            int seed = settings.Seed == 0 ? UnityEngine.Random.Range(1, int.MaxValue) : settings.Seed;
            UnityEngine.Random.InitState(seed);

            int w = settings.Width  % 2 == 0 ? settings.Width  + 1 : settings.Width;
            int h = settings.Height % 2 == 0 ? settings.Height + 1 : settings.Height;

            int[,] grid = InitialiseWalls(w, h);

            // 1 ─ Recursive Backtracking (perfect maze)
            startCell = new Vector2Int(1, 1);
            RecursiveBacktrack(grid, startCell, w, h);

            // 2 ─ Multi-route bias: break additional walls to add loops
            AddMultiRoutes(grid, settings.MultiRouteBias, w, h);

            // 3 ─ Cellular Automata smoothing
            for (int i = 0; i < settings.CellularIterations; i++)
                CellularSmooth(grid, w, h);

            // 4 ─ Weighted path annotations
            AnnotateDeadEnds(grid, w, h);
            PlaceSpeedZones(grid, settings.DeadEndFactor, w, h);
            PlaceSecretPaths(grid, settings.SecretPathChance, w, h);

            // 5 ─ Place game elements
            goalCell = PlaceGoal(grid, w, h);
            PlaceHazards(grid, settings.HazardDensity, startCell, goalCell, w, h);
            PlaceSignalNodes(grid, settings.SignalNodeDensity, startCell, goalCell, w, h);

            // 6 ─ Mark start
            grid[startCell.x, startCell.y] = START;

            return grid;
        }

        // ─── Step 1: Recursive Backtracking ─────────────────────────────────────

        private static int[,] InitialiseWalls(int w, int h)
        {
            int[,] g = new int[w, h];
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    g[x, y] = WALL;
            return g;
        }

        private static void RecursiveBacktrack(int[,] g, Vector2Int start, int w, int h)
        {
            Stack<Vector2Int> stack   = new();
            bool[,]           visited = new bool[w, h];

            g[start.x, start.y]         = PATH;
            visited[start.x, start.y]   = true;
            stack.Push(start);

            // Cardinals in grid-step-of-2 (to carve through walls)
            Vector2Int[] dirs =
            {
                Vector2Int.up * 2, Vector2Int.down * 2,
                Vector2Int.left * 2, Vector2Int.right * 2
            };

            while (stack.Count > 0)
            {
                Vector2Int current   = stack.Peek();
                List<Vector2Int> nbrs = new();

                // Shuffle directions each iteration for maze variety
                Shuffle(dirs);
                foreach (Vector2Int d in dirs)
                {
                    Vector2Int next = current + d;
                    if (InBounds(next, w, h) && !visited[next.x, next.y])
                        nbrs.Add(next);
                }

                if (nbrs.Count > 0)
                {
                    Vector2Int chosen = nbrs[UnityEngine.Random.Range(0, nbrs.Count)];
                    Vector2Int wall   = current + (chosen - current) / 2;

                    g[wall.x,   wall.y]   = PATH;
                    g[chosen.x, chosen.y] = PATH;
                    visited[chosen.x, chosen.y] = true;

                    stack.Push(chosen);
                }
                else
                {
                    stack.Pop();
                }
            }
        }

        // ─── Step 2: Multi-Route Bias ────────────────────────────────────────────

        private static void AddMultiRoutes(int[,] g, int count, int w, int h)
        {
            int attempts = 0;
            int added    = 0;

            while (added < count && attempts < count * 20)
            {
                attempts++;
                int x = UnityEngine.Random.Range(1, w - 1);
                int y = UnityEngine.Random.Range(1, h - 1);

                if (g[x, y] == WALL && CountAdjacentPaths(g, x, y, w, h) >= 2)
                {
                    g[x, y] = PATH;
                    added++;
                }
            }
        }

        // ─── Step 3: Cellular Automata Smoothing ─────────────────────────────────

        private static void CellularSmooth(int[,] g, int w, int h)
        {
            int[,] copy = (int[,])g.Clone();

            for (int x = 1; x < w - 1; x++)
            {
                for (int y = 1; y < h - 1; y++)
                {
                    int pathNeighbors = CountAdjacentPaths(g, x, y, w, h);

                    // Cells surrounded by 5+ path cells become path (smooth caves)
                    if (g[x, y] == WALL && pathNeighbors >= 5)
                        copy[x, y] = PATH;

                    // Isolated path cells surrounded by 1 or fewer paths revert to wall
                    if (g[x, y] == PATH && pathNeighbors <= 1)
                        copy[x, y] = WALL;
                }
            }

            Array.Copy(copy, g, g.Length);
        }

        // ─── Step 4: Weighted Path Annotations ───────────────────────────────────

        private static void AnnotateDeadEnds(int[,] g, int w, int h)
        {
            for (int x = 1; x < w - 1; x++)
                for (int y = 1; y < h - 1; y++)
                    if (g[x, y] == PATH && CountAdjacentPaths(g, x, y, w, h) == 1)
                        g[x, y] = DEAD_END;
        }

        private static void PlaceSpeedZones(int[,] g, float deadEndFactor, int w, int h)
        {
            // Find wide corridors (4+ adjacent path cells) and mark them as speed zones
            for (int x = 2; x < w - 2; x++)
            {
                for (int y = 2; y < h - 2; y++)
                {
                    if (g[x, y] != PATH) continue;
                    if (CountAdjacentPaths(g, x, y, w, h) >= 4)
                        g[x, y] = SPEED_ZONE;
                }
            }
        }

        private static void PlaceSecretPaths(int[,] g, float chance, int w, int h)
        {
            for (int x = 1; x < w - 1; x++)
            {
                for (int y = 1; y < h - 1; y++)
                {
                    if (g[x, y] != DEAD_END) continue;
                    if (UnityEngine.Random.value < chance)
                        g[x, y] = SECRET;
                }
            }
        }

        // ─── Step 5: Game Elements ───────────────────────────────────────────────

        private static Vector2Int PlaceGoal(int[,] g, int w, int h)
        {
            // Place goal as far from (1,1) as possible on a path cell
            Vector2Int best   = new(w - 2, h - 2);
            float      bestD  = 0;

            for (int x = w / 2; x < w - 1; x++)
            {
                for (int y = h / 2; y < h - 1; y++)
                {
                    if (g[x, y] != PATH) continue;
                    float d = Vector2Int.Distance(new Vector2Int(x, y), new Vector2Int(1, 1));
                    if (d > bestD) { bestD = d; best = new Vector2Int(x, y); }
                }
            }

            g[best.x, best.y] = GOAL;
            return best;
        }

        private static void PlaceHazards(int[,] g, float density, Vector2Int start, Vector2Int goal, int w, int h)
        {
            for (int x = 1; x < w - 1; x++)
            {
                for (int y = 1; y < h - 1; y++)
                {
                    if (g[x, y] != PATH) continue;
                    Vector2Int cell = new(x, y);
                    if (cell == start || cell == goal) continue;
                    if (UnityEngine.Random.value < density)
                        g[x, y] = HAZARD;
                }
            }
        }

        private static void PlaceSignalNodes(int[,] g, float density, Vector2Int start, Vector2Int goal, int w, int h)
        {
            for (int x = 1; x < w - 1; x++)
            {
                for (int y = 1; y < h - 1; y++)
                {
                    if (g[x, y] != PATH) continue;
                    Vector2Int cell = new(x, y);
                    if (cell == start || cell == goal) continue;
                    if (UnityEngine.Random.value < density)
                        g[x, y] = SIGNAL_NODE;
                }
            }
        }

        // ─── Utility ─────────────────────────────────────────────────────────────

        private static int CountAdjacentPaths(int[,] g, int x, int y, int w, int h)
        {
            int count = 0;
            if (x > 0     && g[x - 1, y] >= PATH) count++;
            if (x < w - 1 && g[x + 1, y] >= PATH) count++;
            if (y > 0     && g[x, y - 1] >= PATH) count++;
            if (y < h - 1 && g[x, y + 1] >= PATH) count++;
            return count;
        }

        private static bool InBounds(Vector2Int v, int w, int h) =>
            v.x >= 0 && v.x < w && v.y >= 0 && v.y < h;

        private static void Shuffle(Vector2Int[] arr)
        {
            for (int i = arr.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
        }
    }
}
