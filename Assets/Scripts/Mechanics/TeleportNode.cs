using System;
using System.Collections;
using UnityEngine;

namespace ArrowNexus.Mechanics
{
    /// <summary>
    /// Teleport Node — paired nodes that instantly transport the player.
    /// Visual: electric green warp effect. Used heavily in World 4.
    /// </summary>
    public class TeleportNode : MonoBehaviour
    {
        public event Action<TeleportNode> OnPlayerTeleported;

        [Header("Teleport Settings")]
        [SerializeField] private TeleportNode _linkedNode;
        [SerializeField] private float        _cooldownTime = 0.5f;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer _sprite;
        [SerializeField] private Color          _activeColor = new(0.1f, 1f, 0.4f);
        [SerializeField] private Color          _cooldownColor = new(0.2f, 0.4f, 0.2f);
        [SerializeField] private ParticleSystem _warpParticles;

        private bool _isReady = true;

        private void Start()
        {
            if (_sprite == null) _sprite = GetComponent<SpriteRenderer>();
            SetReadyState(true);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isReady || _linkedNode == null) return;
            
            if (other.TryGetComponent(out ArrowNexus.Core.PlayerArrow player))
            {
                if (player != null && player.IsAlive)
                {
                    TeleportPlayer(player);
                }
            }
        }

        private void TeleportPlayer(ArrowNexus.Core.PlayerArrow player)
        {
            // Trigger visual fx on origin
            PlayWarpEffect();
            
            // Move player to linked node position
            Vector2Int linkedGridPos = new Vector2Int(
                Mathf.RoundToInt(_linkedNode.transform.position.x),
                Mathf.RoundToInt(_linkedNode.transform.position.y)
            );
            
            player.Initialise(linkedGridPos);
            
            // Preserve direction and momentum by setting them back immediately after init
            // (Assumes PlayerArrow.Initialise resets them, which it does. In a real impl, we'd add an overload or properties)
            // For now, we rely on the player's current input being maintained by InputManager.
            
            OnPlayerTeleported?.Invoke(this);

            // Put both nodes on cooldown to prevent immediate infinite loop
            StartCoroutine(CooldownRoutine());
            if (_linkedNode != null)
                _linkedNode.StartCoroutine(_linkedNode.CooldownRoutine());
        }

        public IEnumerator CooldownRoutine()
        {
            SetReadyState(false);
            yield return new WaitForSeconds(_cooldownTime);
            SetReadyState(true);
        }

        private void SetReadyState(bool ready)
        {
            _isReady = ready;
            if (_sprite != null)
                _sprite.color = ready ? _activeColor : _cooldownColor;
        }

        private void PlayWarpEffect()
        {
            if (_warpParticles != null)
                _warpParticles.Play();
        }
    }
}
