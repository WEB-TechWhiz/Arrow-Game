using System;
using UnityEngine;
using ArrowNexus.Core;

namespace ArrowNexus.Mechanics
{
    /// <summary>
    /// Gravity Channel — zones where the player arrow's movement orientation is altered.
    /// Entering a channel remaps input axes and forces the player into a new direction.
    /// Visual: color-tinted zone with flowing particles.
    /// </summary>
    public class GravityChannel : MonoBehaviour
    {
        // ─── Events ──────────────────────────────────────────────────────────────
        public event Action<InputManager.Direction> OnGravityApplied;

        // ─── Inspector Config ────────────────────────────────────────────────────
        [Header("Channel Settings")]
        [SerializeField] private InputManager.Direction _gravityDirection = InputManager.Direction.Up;
        [SerializeField] private bool                   _forceDirection   = true; // force player into direction on entry

        [Header("Visual")]
        [SerializeField] private Color  _channelColor = new(0.5f, 0.2f, 1.0f, 0.4f);
        [SerializeField] private ParticleSystem _flowParticles;

        // ─── Cached ───────────────────────────────────────────────────────────────
        private SpriteRenderer _zone;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        private void Awake()
        {
            _zone = GetComponentInChildren<SpriteRenderer>();
            if (_zone != null) _zone.color = _channelColor;

            if (_flowParticles != null)
            {
                var main = _flowParticles.main;
                // Align particle emission direction to gravity direction
                var emission = _flowParticles.velocityOverLifetime;
                Vector2Int d = InputManager.DirectionToVector(_gravityDirection);
                emission.x = d.x * 3f;
                emission.y = d.y * 3f;
                _flowParticles.Play();
            }
        }

        // ─── Collision Detection ──────────────────────────────────────────────────

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerArrow>();
            if (player == null || !player.IsAlive) return;

            // Remap player's input context and force into gravity direction
            InputManager.Instance.SetCurrentDirection(_gravityDirection);

            if (_forceDirection)
                player.ApplyGravityRemap(_gravityDirection);

            OnGravityApplied?.Invoke(_gravityDirection);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            // On exit, player regains free direction control via normal input
            // No additional action needed — InputManager picks up player's next key
        }

        // ─── Gizmo Visualisation (Editor) ────────────────────────────────────────

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.5f, 0.2f, 1f, 0.3f);
            Gizmos.DrawCube(transform.position, transform.localScale);

            Gizmos.color = Color.yellow;
            Vector2Int d = InputManager.DirectionToVector(_gravityDirection);
            Gizmos.DrawRay(transform.position, new Vector3(d.x, d.y, 0) * 1.5f);
        }
    }
}
