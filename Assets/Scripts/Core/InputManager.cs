using System;
using UnityEngine;

namespace ArrowNexus.Core
{
    /// <summary>
    /// Handles all player input — keyboard (WASD/Arrow Keys), Space (dash),
    /// Shift (ability), ESC (pause), and mobile swipe gestures.
    /// Enforces direction locking: the player cannot instantly reverse direction.
    /// Broadcasts InputEvent via C# events for decoupled consumption.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        // ─── Singleton ──────────────────────────────────────────────────────────
        public static InputManager Instance { get; private set; }

        // ─── Enums ───────────────────────────────────────────────────────────────
        public enum Direction { Up, Down, Left, Right, None }

        // ─── Events ──────────────────────────────────────────────────────────────
        public event Action<Direction> OnDirectionInput;
        public event Action            OnDashInput;
        public event Action            OnAbilityInput;
        public event Action            OnPauseInput;

        // ─── Config ──────────────────────────────────────────────────────────────
        [Header("Swipe Settings")]
        [SerializeField] private float _swipeMinDistance  = 50f;   // px
        [SerializeField] private float _swipeMaxTime      = 0.4f;  // seconds

        // ─── State ───────────────────────────────────────────────────────────────
        private Direction _currentDirection = Direction.None;

        // Touch tracking
        private Vector2 _touchStartPos;
        private float   _touchStartTime;
        private bool    _trackingTouch;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            HandleKeyboardInput();
            HandleActionKeys();
            HandleMobileInput();
        }

        // ─── Keyboard Input ──────────────────────────────────────────────────────

        private void HandleKeyboardInput()
        {
            Direction requested = Direction.None;

            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                requested = Direction.Up;
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                requested = Direction.Down;
            else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
                requested = Direction.Left;
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
                requested = Direction.Right;

            if (requested != Direction.None)
                TryEmitDirection(requested);
        }

        private void HandleActionKeys()
        {
            if (Input.GetKeyDown(KeyCode.Space))
                OnDashInput?.Invoke();

            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
                OnAbilityInput?.Invoke();

            if (Input.GetKeyDown(KeyCode.Escape))
                OnPauseInput?.Invoke();
        }

        // ─── Mobile Swipe Input ──────────────────────────────────────────────────

        private void HandleMobileInput()
        {
            if (Input.touchCount == 0) return;

            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    _touchStartPos  = touch.position;
                    _touchStartTime = Time.time;
                    _trackingTouch  = true;
                    break;

                case TouchPhase.Ended when _trackingTouch:
                    float elapsed = Time.time - _touchStartTime;
                    if (elapsed <= _swipeMaxTime)
                        EvaluateSwipe(touch.position);
                    _trackingTouch = false;
                    break;

                case TouchPhase.Canceled:
                    _trackingTouch = false;
                    break;
            }
        }

        private void EvaluateSwipe(Vector2 endPos)
        {
            Vector2 delta = endPos - _touchStartPos;
            if (delta.magnitude < _swipeMinDistance) return;

            Direction swipeDir = Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                ? (delta.x > 0 ? Direction.Right : Direction.Left)
                : (delta.y > 0 ? Direction.Up    : Direction.Down);

            TryEmitDirection(swipeDir);
        }

        // ─── Direction Locking ───────────────────────────────────────────────────

        /// <summary>
        /// Enforces the core Direction Locking rule:
        /// A player cannot instantly reverse their current direction.
        /// e.g. if moving Right, they cannot immediately move Left.
        /// </summary>
        private void TryEmitDirection(Direction requested)
        {
            if (IsOpposite(requested, _currentDirection)) return;

            _currentDirection = requested;
            OnDirectionInput?.Invoke(requested);
        }

        /// <summary>
        /// Called by PlayerArrow to sync the authoritative current direction
        /// (e.g. on respawn or forced direction change by gravity channel).
        /// </summary>
        public void SetCurrentDirection(Direction dir)
        {
            _currentDirection = dir;
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────

        private static bool IsOpposite(Direction a, Direction b)
        {
            return (a == Direction.Up    && b == Direction.Down)
                || (a == Direction.Down  && b == Direction.Up)
                || (a == Direction.Left  && b == Direction.Right)
                || (a == Direction.Right && b == Direction.Left);
        }

        public static Vector2Int DirectionToVector(Direction dir)
        {
            return dir switch
            {
                Direction.Up    => Vector2Int.up,
                Direction.Down  => Vector2Int.down,
                Direction.Left  => Vector2Int.left,
                Direction.Right => Vector2Int.right,
                _               => Vector2Int.zero
            };
        }

        public static Direction OppositeOf(Direction dir)
        {
            return dir switch
            {
                Direction.Up    => Direction.Down,
                Direction.Down  => Direction.Up,
                Direction.Left  => Direction.Right,
                Direction.Right => Direction.Left,
                _               => Direction.None
            };
        }
    }
}
