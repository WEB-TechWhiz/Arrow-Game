using System;
using UnityEngine;
using ArrowNexus.Maze;

namespace ArrowNexus.Core
{
    /// <summary>
    /// Grid-based collision and interaction system.
    /// Checks the tile type at the player's next cell and returns a TileResult,
    /// then processes the outcome (death, level complete, signal trigger, etc.).
    /// Pure positional logic — no Unity physics engine involved.
    /// </summary>
    public class CollisionSystem : MonoBehaviour
    {
        // ─── Singleton ───────────────────────────────────────────────────────────
        public static CollisionSystem Instance { get; private set; }

        // ─── Tile Outcomes ───────────────────────────────────────────────────────
        public enum TileResult
        {
            Safe,        // PATH / SPEED_ZONE / SECRET — proceed
            Wall,        // WALL — block movement
            Goal,        // GOAL — level complete
            Hazard,      // HAZARD — player dies
            SignalNode,  // SIGNAL_NODE — trigger mechanic
            SpeedZone,   // SPEED_ZONE — boost momentum
            DeadEnd      // DEAD_END — valid path, just a dead end
        }

        // ─── Events ──────────────────────────────────────────────────────────────
        public event Action<Vector2Int>  OnGoalReached;
        public event Action<Vector2Int>  OnHazardHit;
        public event Action<Vector2Int>  OnSignalNodeTriggered;
        public event Action<Vector2Int>  OnSpeedZoneEntered;

        // ─── Dependencies ────────────────────────────────────────────────────────
        private TileManager  _tileManager;
        private PlayerArrow  _player;

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
            _tileManager = TileManager.Instance;
            _player      = FindObjectOfType<PlayerArrow>();
        }

        // ─── Core Query ──────────────────────────────────────────────────────────

        /// <summary>
        /// Called by PlayerArrow BEFORE moving to a cell.
        /// Returns what type of outcome awaits the player there.
        /// </summary>
        public TileResult CheckTile(Vector2Int cell)
        {
            if (_tileManager == null)
                return TileResult.Wall;

            int tileType = _tileManager.GetTileType(cell);

            return tileType switch
            {
                MazeGenerator.WALL        => TileResult.Wall,
                MazeGenerator.GOAL        => TileResult.Goal,
                MazeGenerator.HAZARD      => TileResult.Hazard,
                MazeGenerator.SIGNAL_NODE => TileResult.SignalNode,
                MazeGenerator.SPEED_ZONE  => TileResult.SpeedZone,
                MazeGenerator.DEAD_END    => TileResult.DeadEnd,
                MazeGenerator.PATH        => TileResult.Safe,
                MazeGenerator.SECRET      => TileResult.Safe,
                MazeGenerator.START       => TileResult.Safe,
                _                         => TileResult.Wall
            };
        }

        /// <summary>
        /// Called by PlayerArrow AFTER physically moving to the cell.
        /// Fires the appropriate game event based on tile outcome.
        /// </summary>
        public void ProcessTileOutcome(TileResult result, Vector2Int cell)
        {
            switch (result)
            {
                case TileResult.Goal:
                    OnGoalReached?.Invoke(cell);
                    break;

                case TileResult.Hazard:
                    OnHazardHit?.Invoke(cell);
                    _player?.Die();
                    break;

                case TileResult.SignalNode:
                    OnSignalNodeTriggered?.Invoke(cell);
                    // Consume the signal node after first trigger
                    _tileManager?.SetCell(cell, MazeGenerator.PATH);
                    break;

                case TileResult.SpeedZone:
                    OnSpeedZoneEntered?.Invoke(cell);
                    break;

                // Safe / DeadEnd — no special behaviour
                default:
                    break;
            }
        }

        // ─── Wall Slide Helper ────────────────────────────────────────────────────

        /// <summary>
        /// Returns the best fallback direction if the player's intended move is blocked.
        /// Checks left/right perpendicular to current direction to create a "wall slide" feel.
        /// </summary>
        public InputManager.Direction GetWallSlideDirection(
            Vector2Int currentCell, InputManager.Direction blockedDir)
        {
            var perp = GetPerpendicularDirections(blockedDir);

            foreach (var dir in perp)
            {
                Vector2Int candidate = currentCell + InputManager.DirectionToVector(dir);
                if (CheckTile(candidate) != TileResult.Wall)
                    return dir;
            }

            return InputManager.Direction.None;
        }

        private static InputManager.Direction[] GetPerpendicularDirections(InputManager.Direction dir)
        {
            return dir switch
            {
                InputManager.Direction.Up or InputManager.Direction.Down =>
                    new[] { InputManager.Direction.Left, InputManager.Direction.Right },
                InputManager.Direction.Left or InputManager.Direction.Right =>
                    new[] { InputManager.Direction.Up, InputManager.Direction.Down },
                _ => Array.Empty<InputManager.Direction>()
            };
        }

        // ─── Utility ─────────────────────────────────────────────────────────────

        /// <summary>True if cell is a passable (non-wall) tile.</summary>
        public bool IsPassable(Vector2Int cell) => CheckTile(cell) != TileResult.Wall;
    }
}
