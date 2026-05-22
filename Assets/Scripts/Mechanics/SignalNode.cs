using System;
using System.Collections;
using UnityEngine;

namespace ArrowNexus.Mechanics
{
    /// <summary>
    /// Signal Node — interactive puzzle nodes placed in the maze.
    /// Types:
    ///   BridgeActivator  — opens a DynamicPathway
    ///   TrapDisabler     — disables a nearby Hazard
    ///   GravityReverser  — flips player movement orientation
    ///   LayoutChanger    — reshuffles a maze section
    ///
    /// Triggered when player enters the node's cell. Has a cooldown.
    /// Visual feedback: neon pulse animation (purple/yellow palette).
    /// </summary>
    public class SignalNode : MonoBehaviour
    {
        // ─── Types ───────────────────────────────────────────────────────────────
        public enum NodeType
        {
            BridgeActivator,
            TrapDisabler,
            GravityReverser,
            LayoutChanger
        }

        // ─── Events ──────────────────────────────────────────────────────────────
        public event Action<SignalNode> OnActivated;

        // ─── Inspector Config ────────────────────────────────────────────────────
        [Header("Node Settings")]
        [SerializeField] private NodeType      _type         = NodeType.BridgeActivator;
        [SerializeField] private float         _cooldown     = 5f;
        [SerializeField] private bool          _singleUse    = false;

        [Header("Bridge Activator")]
        [SerializeField] private DynamicPathway _targetPathway;

        [Header("Trap Disabler")]
        [SerializeField] private HazardSystem   _targetHazard;
        [SerializeField] private float          _disableDuration = 4f;

        [Header("Gravity Reverser")]
        [SerializeField] private ArrowNexus.Core.InputManager.Direction _gravityDir =
            ArrowNexus.Core.InputManager.Direction.Up;

        [Header("Layout Changer")]
        [SerializeField] private MazeSection    _targetSection; // sub-maze region to reshuffle

        [Header("Visual")]
        [SerializeField] private SpriteRenderer _sprite;
        [SerializeField] private Color          _idleColor   = new(0.6f, 0.2f, 0.9f); // purple
        [SerializeField] private Color          _activeColor = new(1.0f, 0.9f, 0.0f); // yellow
        [SerializeField] private float          _pulseSpeed  = 2f;

        // ─── State ───────────────────────────────────────────────────────────────
        public  bool  IsReady    { get; private set; } = true;
        private bool  _used      = false;
        private float _cooldownRemaining;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        private void Awake()
        {
            if (_sprite == null) _sprite = GetComponent<SpriteRenderer>();
            _sprite.color = _idleColor;
        }

        private void Start()
        {
            // Subscribe to collision system's signal node event
            if (ArrowNexus.Core.CollisionSystem.Instance != null)
                ArrowNexus.Core.CollisionSystem.Instance.OnSignalNodeTriggered += OnGridTrigger;
        }

        private void OnDestroy()
        {
            if (ArrowNexus.Core.CollisionSystem.Instance != null)
                ArrowNexus.Core.CollisionSystem.Instance.OnSignalNodeTriggered -= OnGridTrigger;
        }

        private void Update()
        {
            if (!IsReady && _cooldownRemaining > 0)
            {
                _cooldownRemaining -= Time.deltaTime;
                if (_cooldownRemaining <= 0)
                {
                    IsReady = true;
                    _sprite.color = _idleColor;
                }
            }

            // Idle pulse animation
            if (IsReady && !_used)
                AnimatePulse();
        }

        // ─── Activation ──────────────────────────────────────────────────────────

        private void OnGridTrigger(Vector2Int cell)
        {
            // Only respond to triggers on our own world cell
            Vector2Int myCell = new(
                Mathf.RoundToInt(transform.position.x),
                Mathf.RoundToInt(transform.position.y)
            );
            if (cell != myCell) return;

            Activate();
        }

        private void Activate()
        {
            if (!IsReady || _used) return;

            IsReady = false;
            _cooldownRemaining = _cooldown;
            _sprite.color      = _activeColor;
            OnActivated?.Invoke(this);

            if (_singleUse) _used = true;

            ExecuteNodeEffect();
            StartCoroutine(PulseBurst());
        }

        private void ExecuteNodeEffect()
        {
            switch (_type)
            {
                case NodeType.BridgeActivator:
                    _targetPathway?.TriggerBySignal();
                    break;

                case NodeType.TrapDisabler:
                    if (_targetHazard != null)
                        StartCoroutine(_targetHazard.DisableTemporarily(_disableDuration));
                    break;

                case NodeType.GravityReverser:
                    var player = FindObjectOfType<ArrowNexus.Core.PlayerArrow>();
                    player?.ApplyGravityRemap(_gravityDir);
                    break;

                case NodeType.LayoutChanger:
                    _targetSection?.Reshuffle();
                    break;
            }
        }

        // ─── Visual Pulse ─────────────────────────────────────────────────────────

        private void AnimatePulse()
        {
            float t = (Mathf.Sin(Time.time * _pulseSpeed) + 1f) * 0.5f;
            _sprite.color = Color.Lerp(_idleColor, _activeColor, t * 0.4f);
        }

        private IEnumerator PulseBurst()
        {
            float elapsed = 0f;
            float duration = 0.4f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(Vector3.one * 1.4f, Vector3.one, t);
                yield return null;
            }
            transform.localScale = Vector3.one;
        }
    }
}
