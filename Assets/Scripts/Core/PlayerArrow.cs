using System;
using System.Collections;
using UnityEngine;
using ArrowNexus.Core;

namespace ArrowNexus.Core
{
    /// <summary>
    /// The central player entity — a directional arrow / pulse entity.
    /// Handles grid-snapped movement, direction locking, flow momentum,
    /// dash ability, and death/respawn logic.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerArrow : MonoBehaviour
    {
        // ─── Events ──────────────────────────────────────────────────────────────
        public event Action                  OnDeath;
        public event Action<float>           OnComboUpdated;   // combo multiplier
        public event Action                  OnDash;
        public event Action<float>           OnMomentumChanged; // 0–1 normalised

        // ─── Inspector Config ────────────────────────────────────────────────────
        [Header("Movement")]
        [SerializeField] private float _baseMoveInterval  = 0.18f;  // seconds per cell at base speed
        [SerializeField] private float _minMoveInterval   = 0.06f;  // fastest (max momentum)
        [SerializeField] private float _momentumAccelRate = 0.008f; // interval reduction per cell
        [SerializeField] private float _momentumDecayRate = 0.04f;  // interval increase per second idle
        [SerializeField] private float _turnDampenFactor  = 0.5f;   // slow-down penalty on turn at high speed

        [Header("Dash")]
        [SerializeField] private int   _dashCells         = 3;      // cells to burst forward
        [SerializeField] private float _dashCooldown      = 1.5f;   // seconds

        [Header("Score")]
        [SerializeField] private float _comboPerCell      = 0.1f;   // combo multiplier per continuous cell
        [SerializeField] private float _maxCombo          = 5f;

        [Header("Visuals")]
        [SerializeField] private float _squishScale       = 0.8f;   // Y squish on move
        [SerializeField] private float _squishDuration    = 0.05f;

        // ─── State ───────────────────────────────────────────────────────────────
        public Vector2Int GridPosition   { get; private set; }
        public InputManager.Direction CurrentDirection { get; private set; } = InputManager.Direction.None;
        public float MomentumNormalised  { get; private set; }          // 0 = base, 1 = max speed
        public float ComboMultiplier     { get; private set; } = 1f;
        public bool  IsAlive             { get; private set; } = true;
        public bool  IsDashing           { get; private set; }

        private float _currentMoveInterval;
        private float _dashCooldownRemaining;
        private bool  _moving;
        private InputManager.Direction _lastMoveDirection = InputManager.Direction.None;

        private SpriteRenderer _sr;
        private Coroutine      _moveCoroutine;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _currentMoveInterval = _baseMoveInterval;
        }

        private void OnEnable()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnDirectionInput += OnDirectionReceived;
                InputManager.Instance.OnDashInput      += OnDashReceived;
            }
        }

        private void OnDisable()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnDirectionInput -= OnDirectionReceived;
                InputManager.Instance.OnDashInput      -= OnDashReceived;
            }
        }

        private void Update()
        {
            if (!IsAlive) return;

            // Momentum decay when not moving
            if (!_moving)
            {
                _currentMoveInterval = Mathf.Min(
                    _baseMoveInterval,
                    _currentMoveInterval + _momentumDecayRate * Time.deltaTime
                );
                UpdateMomentumNormalised();
            }

            // Dash cooldown
            if (_dashCooldownRemaining > 0)
                _dashCooldownRemaining -= Time.deltaTime;
        }

        // ─── Initialisation ──────────────────────────────────────────────────────

        public void Initialise(Vector2Int startCell)
        {
            GridPosition         = startCell;
            transform.position   = CellToWorld(startCell);
            CurrentDirection     = InputManager.Direction.None;
            _currentMoveInterval = _baseMoveInterval;
            ComboMultiplier      = 1f;
            IsAlive              = true;
        }

        // ─── Input Handlers ──────────────────────────────────────────────────────

        private void OnDirectionReceived(InputManager.Direction dir)
        {
            if (!IsAlive) return;
            StartMoving(dir);
        }

        private void OnDashReceived()
        {
            if (!IsAlive || IsDashing || _dashCooldownRemaining > 0) return;
            StartCoroutine(DashCoroutine());
        }

        // ─── Movement ────────────────────────────────────────────────────────────

        private void StartMoving(InputManager.Direction dir)
        {
            if (CollisionSystem.Instance == null)
            {
                CurrentDirection = dir;
                InputManager.Instance?.SetCurrentDirection(dir);
                return;
            }

            Vector2Int next = GridPosition + InputManager.DirectionToVector(dir);
            if (CollisionSystem.Instance.CheckTile(next) == CollisionSystem.TileResult.Wall)
            {
                if (!_moving)
                    ClearCurrentDirection();
                else
                    InputManager.Instance?.SetCurrentDirection(CurrentDirection);

                return;
            }

            bool isTurn = dir != CurrentDirection && CurrentDirection != InputManager.Direction.None;

            // Apply turn dampening at high speed
            if (isTurn && MomentumNormalised > 0.5f)
                _currentMoveInterval = Mathf.Lerp(_currentMoveInterval, _baseMoveInterval, _turnDampenFactor);

            CurrentDirection = dir;
            _lastMoveDirection = dir;
            InputManager.Instance.SetCurrentDirection(dir);

            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
            _moveCoroutine = StartCoroutine(MoveCoroutine(dir));
        }

        private IEnumerator MoveCoroutine(InputManager.Direction dir)
        {
            _moving = true;

            if (IsAlive && CurrentDirection == dir)
            {
                if (CollisionSystem.Instance == null)
                {
                    _moving = false;
                    ClearCurrentDirection();
                    yield break;
                }

                Vector2Int next = GridPosition + InputManager.DirectionToVector(dir);

                // Collision check — CollisionSystem decides outcome
                CollisionSystem.TileResult result = CollisionSystem.Instance.CheckTile(next);

                if (result == CollisionSystem.TileResult.Wall)
                {
                    _moving = false;
                    ClearCurrentDirection();
                    yield break;
                }

                // Rotate arrow sprite to face direction
                RotateToDirection(dir);

                // Animate squish
                StartCoroutine(SquishAnimation());

                // Move
                GridPosition = next;
                ArrowNexus.Maze.TileManager.Instance?.UpdateChunksAroundPlayer(GridPosition);
                yield return MoveToWorldPosition(CellToWorld(next), _currentMoveInterval);

                // Accelerate momentum
                _currentMoveInterval = Mathf.Max(_minMoveInterval, _currentMoveInterval - _momentumAccelRate);
                UpdateMomentumNormalised();

                // Update combo
                ComboMultiplier = Mathf.Min(_maxCombo, ComboMultiplier + _comboPerCell);
                OnComboUpdated?.Invoke(ComboMultiplier);

                // Process tile outcome
                CollisionSystem.Instance.ProcessTileOutcome(result, next);
            }

            _moving = false;
            ClearCurrentDirection();

            // Decay combo on stop
            ComboMultiplier = 1f;
            OnComboUpdated?.Invoke(ComboMultiplier);
        }

        private IEnumerator MoveToWorldPosition(Vector3 target, float duration)
        {
            Vector3 origin = transform.position;
            float   elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed           += Time.deltaTime;
                transform.position = Vector3.Lerp(origin, target, elapsed / duration);
                yield return null;
            }
            transform.position = target;
        }

        // ─── Dash ────────────────────────────────────────────────────────────────

        private IEnumerator DashCoroutine()
        {
            InputManager.Direction dashDirection = CurrentDirection != InputManager.Direction.None
                ? CurrentDirection
                : _lastMoveDirection;

            if (dashDirection == InputManager.Direction.None) yield break;
            if (CollisionSystem.Instance == null) yield break;

            IsDashing              = true;
            _dashCooldownRemaining = _dashCooldown;
            OnDash?.Invoke();

            for (int i = 0; i < _dashCells; i++)
            {
                Vector2Int next = GridPosition + InputManager.DirectionToVector(dashDirection);
                CollisionSystem.TileResult result = CollisionSystem.Instance.CheckTile(next);
                if (result == CollisionSystem.TileResult.Wall) break;

                GridPosition = next;
                ArrowNexus.Maze.TileManager.Instance?.UpdateChunksAroundPlayer(GridPosition);
                yield return MoveToWorldPosition(CellToWorld(next), _currentMoveInterval * 0.3f);

                CollisionSystem.Instance.ProcessTileOutcome(result, next);
            }

            IsDashing = false;
            if (!_moving)
                ClearCurrentDirection();
        }

        // ─── Death & Respawn ─────────────────────────────────────────────────────

        public void Die()
        {
            if (!IsAlive) return;
            IsAlive = false;
            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
            ComboMultiplier = 1f;
            OnDeath?.Invoke();
        }

        public void Respawn(Vector2Int spawnCell)
        {
            Initialise(spawnCell);
        }

        // ─── Gravity Channel Support ─────────────────────────────────────────────

        /// <summary>Remaps the direction bias when inside a gravity channel.</summary>
        public void ApplyGravityRemap(InputManager.Direction gravityDir)
        {
            StartMoving(gravityDir);
        }

        // ─── Visuals ─────────────────────────────────────────────────────────────

        private void RotateToDirection(InputManager.Direction dir)
        {
            float angle = dir switch
            {
                InputManager.Direction.Up    =>  90f,
                InputManager.Direction.Down  => 270f,
                InputManager.Direction.Left  => 180f,
                InputManager.Direction.Right =>   0f,
                _                            =>   0f
            };
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private IEnumerator SquishAnimation()
        {
            Vector3 original = transform.localScale;
            Vector3 squished = new(original.x, original.y * _squishScale, original.z);

            float elapsed = 0f;
            while (elapsed < _squishDuration)
            {
                elapsed             += Time.deltaTime;
                transform.localScale = Vector3.Lerp(squished, original, elapsed / _squishDuration);
                yield return null;
            }
            transform.localScale = original;
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────

        private void UpdateMomentumNormalised()
        {
            float range = _baseMoveInterval - _minMoveInterval;
            MomentumNormalised = range > 0
                ? 1f - (_currentMoveInterval - _minMoveInterval) / range
                : 0f;
            OnMomentumChanged?.Invoke(MomentumNormalised);
        }

        private void ClearCurrentDirection()
        {
            CurrentDirection = InputManager.Direction.None;
            InputManager.Instance?.SetCurrentDirection(InputManager.Direction.None);
        }

        private static Vector3 CellToWorld(Vector2Int cell) =>
            new(cell.x, cell.y, 0f);
    }
}
