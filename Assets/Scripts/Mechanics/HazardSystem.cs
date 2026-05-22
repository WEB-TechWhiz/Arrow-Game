using System;
using System.Collections;
using UnityEngine;

namespace ArrowNexus.Mechanics
{
    /// <summary>
    /// Hazard System — manages all hazard types in the maze:
    ///   Static   — red/orange tiles, instant death on contact (handled by CollisionSystem)
    ///   Moving   — patrol a defined path
    ///   Timed    — activate on rhythm cycle (pairs with PulseDoors)
    ///   Corrupt  — Data Corruption Zone (visual glitch, fake walls, hidden traps)
    ///
    /// This component is attached to individual Moving/Timed hazard GameObjects.
    /// Static hazards are pure tile-type data (no MonoBehaviour needed).
    /// </summary>
    public class HazardSystem : MonoBehaviour
    {
        // ─── Hazard Types ────────────────────────────────────────────────────────
        public enum HazardType { Static, Moving, Timed, DataCorruption }

        // ─── Events ──────────────────────────────────────────────────────────────
        public event Action OnHazardEnabled;
        public event Action OnHazardDisabled;

        // ─── Inspector Config ────────────────────────────────────────────────────
        [Header("Type")]
        [SerializeField] private HazardType _type = HazardType.Moving;

        [Header("Moving Hazard")]
        [SerializeField] private Vector2Int[] _patrolPath;   // grid cells to patrol
        [SerializeField] private float        _moveSpeed     = 0.2f;  // seconds per cell
        [SerializeField] private bool         _pingPong      = true;

        [Header("Timed Hazard")]
        [SerializeField] private float _activeInterval   = 1.5f;  // on duration (seconds)
        [SerializeField] private float _inactiveInterval = 1.5f;  // off duration

        [Header("Data Corruption Zone")]
        [SerializeField] private Vector2Int   _corruptionOrigin;
        [SerializeField] private Vector2Int   _corruptionSize;
        [SerializeField] private float        _glitchIntensity = 0.7f;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer _sprite;
        [SerializeField] private Color          _activeColor   = new(1f, 0.3f, 0.1f);
        [SerializeField] private Color          _inactiveColor = new(0.4f, 0.1f, 0f);

        // ─── State ───────────────────────────────────────────────────────────────
        public bool IsActive { get; private set; } = true;

        private int       _patrolIndex     = 0;
        private int       _patrolDirection = 1;   // 1 = forward, -1 = backward (ping-pong)
        private Vector2Int _currentCell;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        private void Start()
        {
            if (_sprite == null) _sprite = GetComponentInChildren<SpriteRenderer>();

            switch (_type)
            {
                case HazardType.Moving:
                    if (_patrolPath.Length > 0)
                    {
                        _currentCell = _patrolPath[0];
                        StartCoroutine(PatrolCoroutine());
                    }
                    break;

                case HazardType.Timed:
                    StartCoroutine(TimedCycle());
                    break;

                case HazardType.DataCorruption:
                    StartCoroutine(CorruptionGlitch());
                    break;
            }
        }

        // ─── Moving Hazard ───────────────────────────────────────────────────────

        private IEnumerator PatrolCoroutine()
        {
            while (true)
            {
                Vector2Int target = _patrolPath[_patrolIndex];
                yield return MoveToCell(target);

                // Check player collision at new cell
                var player = FindObjectOfType<ArrowNexus.Core.PlayerArrow>();
                if (player != null && player.GridPosition == _currentCell && player.IsAlive)
                    player.Die();

                _patrolIndex += _patrolDirection;

                if (_pingPong)
                {
                    if (_patrolIndex >= _patrolPath.Length)
                    {
                        _patrolDirection = -1;
                        _patrolIndex     = _patrolPath.Length - 2;
                    }
                    else if (_patrolIndex < 0)
                    {
                        _patrolDirection = 1;
                        _patrolIndex     = 1;
                    }
                }
                else
                {
                    _patrolIndex = _patrolIndex % _patrolPath.Length;
                }
            }
        }

        private IEnumerator MoveToCell(Vector2Int target)
        {
            Vector3 origin = transform.position;
            Vector3 dest   = new(target.x, target.y, 0f);
            float   elapsed = 0f;

            while (elapsed < _moveSpeed)
            {
                elapsed           += Time.deltaTime;
                transform.position = Vector3.Lerp(origin, dest, elapsed / _moveSpeed);
                yield return null;
            }
            transform.position = dest;
            _currentCell       = target;
        }

        // ─── Timed Hazard ────────────────────────────────────────────────────────

        private IEnumerator TimedCycle()
        {
            while (true)
            {
                Activate();
                yield return new WaitForSeconds(_activeInterval);
                Deactivate();
                yield return new WaitForSeconds(_inactiveInterval);
            }
        }

        private void Activate()
        {
            IsActive          = true;
            _sprite.color     = _activeColor;
            OnHazardEnabled?.Invoke();
        }

        private void Deactivate()
        {
            IsActive          = false;
            _sprite.color     = _inactiveColor;
            OnHazardDisabled?.Invoke();
        }

        // ─── Signal-Driven Disable (from SignalNode) ─────────────────────────────

        public IEnumerator DisableTemporarily(float duration)
        {
            Deactivate();
            yield return new WaitForSeconds(duration);
            Activate();
        }

        // ─── Data Corruption Zone ────────────────────────────────────────────────

        private IEnumerator CorruptionGlitch()
        {
            // Notify FXManager to apply glitch shader over the corruption zone
            while (true)
            {
                float waitTime = UnityEngine.Random.Range(0.5f, 2f);
                yield return new WaitForSeconds(waitTime);

                // Trigger glitch burst via FXManager
                if (ArrowNexus.FX.FXManager.Instance != null)
                    ArrowNexus.FX.FXManager.Instance.TriggerCorruptionGlitch(
                        _corruptionOrigin, _corruptionSize, _glitchIntensity
                    );
            }
        }

        // ─── Player Collision (for timed/static hazards in trigger zone) ─────────

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsActive) return;
            if (other.TryGetComponent(out ArrowNexus.Core.PlayerArrow player))
            {
                player?.Die();
            }
        }
    }
}
