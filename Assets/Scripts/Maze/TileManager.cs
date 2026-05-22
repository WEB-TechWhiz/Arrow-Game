using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using ArrowNexus.Maze;

namespace ArrowNexus.Maze
{
    /// <summary>
    /// Renders the generated maze int[,] grid onto Unity's Tilemap system.
    /// Handles chunk-based loading, tile type mapping, object pooling for
    /// animated/moving tiles, and GPU instancing via Tilemap batching.
    /// </summary>
    public class TileManager : MonoBehaviour
    {
        // ─── Singleton ───────────────────────────────────────────────────────────
        public static TileManager Instance { get; private set; }

        // ─── Inspector Config ────────────────────────────────────────────────────
        [Header("Tilemaps")]
        [SerializeField] private Tilemap _groundTilemap;
        [SerializeField] private Tilemap _overlayTilemap;    // hazards, signals, goals

        [Header("Tile Assets")]
        [SerializeField] private TileBase _wallTile;
        [SerializeField] private TileBase _pathTile;
        [SerializeField] private TileBase _startTile;
        [SerializeField] private TileBase _goalTile;
        [SerializeField] private TileBase _hazardTile;
        [SerializeField] private TileBase _signalNodeTile;
        [SerializeField] private TileBase _secretPathTile;
        [SerializeField] private TileBase _speedZoneTile;

        [Header("Chunk Settings")]
        [SerializeField] private int _chunkSize         = 16;    // cells per chunk
        [SerializeField] private int _visibleChunkRadius = 2;    // chunks loaded around player

        [Header("Object Pool")]
        [SerializeField] private GameObject _animatedTilePrefab;
        [SerializeField] private int        _poolSize = 64;

        // ─── State ───────────────────────────────────────────────────────────────
        private int[,]     _grid;
        private int        _gridWidth;
        private int        _gridHeight;
        private Vector2Int _lastPlayerChunk = new(-999, -999);

        // Track which chunks are currently loaded
        private HashSet<Vector2Int>                    _loadedChunks = new();
        private Dictionary<Vector2Int, List<Vector3Int>> _chunkCells  = new();

        // Object pool for animated tiles (dynamic pathways, etc.)
        private Queue<GameObject> _tilePool = new();
        private List<GameObject>  _activeTiles = new();
        private bool _poolInitialised;

        // Public grid access for collision, AI, etc.
        public int[,] Grid       => _grid;
        public int    GridWidth  => _gridWidth;
        public int    GridHeight => _gridHeight;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Start()
        {
            TryInitialisePool();
        }

        public void ConfigureRuntime(
            Tilemap groundTilemap,
            Tilemap overlayTilemap,
            TileBase wallTile,
            TileBase pathTile,
            TileBase startTile,
            TileBase goalTile,
            TileBase hazardTile,
            TileBase signalNodeTile,
            TileBase secretPathTile,
            TileBase speedZoneTile,
            GameObject animatedTilePrefab = null)
        {
            _groundTilemap      = groundTilemap;
            _overlayTilemap     = overlayTilemap;
            _wallTile           = wallTile;
            _pathTile           = pathTile;
            _startTile          = startTile;
            _goalTile           = goalTile;
            _hazardTile         = hazardTile;
            _signalNodeTile     = signalNodeTile;
            _secretPathTile     = secretPathTile;
            _speedZoneTile      = speedZoneTile;
            _animatedTilePrefab  = animatedTilePrefab;

            TryInitialisePool();
        }

        // ─── Public API ──────────────────────────────────────────────────────────

        /// <summary>Build tilemaps from a freshly generated grid.</summary>
        public void BuildFromGrid(int[,] grid)
        {
            _grid        = grid;
            _gridWidth   = grid.GetLength(0);
            _gridHeight  = grid.GetLength(1);

            if (_groundTilemap == null || _overlayTilemap == null)
            {
                Debug.LogWarning("TileManager is missing tilemap references. BuildFromGrid will only cache the grid.");
                return;
            }

            _groundTilemap.ClearAllTiles();
            _overlayTilemap.ClearAllTiles();
            _loadedChunks.Clear();
            _chunkCells.Clear();
            _lastPlayerChunk = new Vector2Int(-999, -999);

            PrecomputeChunkCells();
            // Initial full load — will be refined by UpdateChunksAroundPlayer
            LoadAllChunks();
        }

        /// <summary>
        /// Called every frame (or on player cell change) to stream chunks in/out.
        /// Pass the current player grid position.
        /// </summary>
        public void UpdateChunksAroundPlayer(Vector2Int playerCell)
        {
            Vector2Int playerChunk = CellToChunk(playerCell);
            if (playerChunk == _lastPlayerChunk) return;
            _lastPlayerChunk = playerChunk;

            HashSet<Vector2Int> desired = new();
            for (int cx = playerChunk.x - _visibleChunkRadius; cx <= playerChunk.x + _visibleChunkRadius; cx++)
                for (int cy = playerChunk.y - _visibleChunkRadius; cy <= playerChunk.y + _visibleChunkRadius; cy++)
                    desired.Add(new Vector2Int(cx, cy));

            // Unload chunks no longer needed
            foreach (Vector2Int chunk in new List<Vector2Int>(_loadedChunks))
                if (!desired.Contains(chunk)) UnloadChunk(chunk);

            // Load new chunks
            foreach (Vector2Int chunk in desired)
                if (!_loadedChunks.Contains(chunk)) LoadChunk(chunk);
        }

        /// <summary>Update a single cell's tile (e.g. when a dynamic pathway opens/closes).</summary>
        public void SetCell(Vector2Int cell, int tileType)
        {
            if (_grid == null) return;
            _grid[cell.x, cell.y] = tileType;
            Vector3Int pos = new(cell.x, cell.y, 0);
            ApplyTile(pos, tileType);
        }

        /// <summary>Returns the tile type at the given grid cell.</summary>
        public int GetTileType(Vector2Int cell)
        {
            if (_grid == null) return MazeGenerator.WALL;
            if (cell.x < 0 || cell.x >= _gridWidth || cell.y < 0 || cell.y >= _gridHeight)
                return MazeGenerator.WALL;
            return _grid[cell.x, cell.y];
        }

        // ─── Chunk Management ────────────────────────────────────────────────────

        private void PrecomputeChunkCells()
        {
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    Vector2Int chunk = CellToChunk(new Vector2Int(x, y));
                    if (!_chunkCells.ContainsKey(chunk))
                        _chunkCells[chunk] = new List<Vector3Int>();
                    _chunkCells[chunk].Add(new Vector3Int(x, y, 0));
                }
            }
        }

        private void LoadAllChunks()
        {
            foreach (var kvp in _chunkCells) LoadChunk(kvp.Key);
        }

        private void LoadChunk(Vector2Int chunk)
        {
            if (!_chunkCells.ContainsKey(chunk)) return;
            if (_loadedChunks.Contains(chunk)) return;

            foreach (Vector3Int pos in _chunkCells[chunk])
                ApplyTile(pos, _grid[pos.x, pos.y]);

            _loadedChunks.Add(chunk);
        }

        private void UnloadChunk(Vector2Int chunk)
        {
            if (!_chunkCells.ContainsKey(chunk)) return;

            foreach (Vector3Int pos in _chunkCells[chunk])
            {
                _groundTilemap.SetTile(pos, null);
                _overlayTilemap.SetTile(pos, null);
            }

            _loadedChunks.Remove(chunk);
        }

        // ─── Tile Rendering ──────────────────────────────────────────────────────

        private void ApplyTile(Vector3Int pos, int type)
        {
            if (_groundTilemap == null || _overlayTilemap == null)
                return;

            // Ground layer
            TileBase ground = type == MazeGenerator.WALL ? _wallTile : _pathTile;
            _groundTilemap.SetTile(pos, ground);

            // Overlay layer (game objects placed on top of path)
            TileBase overlay = type switch
            {
                MazeGenerator.GOAL        => _goalTile,
                MazeGenerator.HAZARD      => _hazardTile,
                MazeGenerator.SIGNAL_NODE => _signalNodeTile,
                MazeGenerator.SECRET      => _secretPathTile,
                MazeGenerator.SPEED_ZONE  => _speedZoneTile,
                MazeGenerator.START       => _startTile,
                _                         => null
            };
            _overlayTilemap.SetTile(pos, overlay);
        }

        // ─── Object Pool ─────────────────────────────────────────────────────────

        private void InitialisePool()
        {
            if (_poolInitialised) return;
            if (_animatedTilePrefab == null) return;

            for (int i = 0; i < _poolSize; i++)
            {
                GameObject go = Instantiate(_animatedTilePrefab, transform);
                go.SetActive(false);
                _tilePool.Enqueue(go);
            }

            _poolInitialised = true;
        }

        private void TryInitialisePool()
        {
            if (_poolInitialised || _animatedTilePrefab == null)
                return;

            InitialisePool();
        }

        public GameObject GetPooledTile()
        {
            if (_tilePool.Count > 0)
            {
                GameObject go = _tilePool.Dequeue();
                go.SetActive(true);
                _activeTiles.Add(go);
                return go;
            }
            // Expand pool if empty
            GameObject extra = Instantiate(_animatedTilePrefab, transform);
            _activeTiles.Add(extra);
            return extra;
        }

        public void ReturnToPool(GameObject tile)
        {
            tile.SetActive(false);
            _activeTiles.Remove(tile);
            _tilePool.Enqueue(tile);
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────

        private Vector2Int CellToChunk(Vector2Int cell) =>
            new(Mathf.FloorToInt((float)cell.x / _chunkSize),
                Mathf.FloorToInt((float)cell.y / _chunkSize));
    }
}
