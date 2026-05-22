using UnityEngine;
using ArrowNexus.Core;

namespace ArrowNexus.Mechanics
{
    /// <summary>
    /// Clone mechanic: Creates a duplicate player arrow.
    /// Both arrows share input. Used for synchronization puzzles.
    /// </summary>
    public class SplitPath : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private PlayerArrow _playerPrefab;
        [SerializeField] private Vector2Int  _spawnOffset = new(2, 0);
        [SerializeField] private bool        _sharedLife = true; // if one dies, both die

        private PlayerArrow _clone;
        private PlayerArrow _mainPlayer;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out PlayerArrow player) && _clone == null)
            {
                _mainPlayer = player;
                if (_mainPlayer != null && _mainPlayer.IsAlive)
                {
                    CreateClone();
                }
            }
        }

        private void CreateClone()
        {
            Vector2Int spawnPos = _mainPlayer.GridPosition + _spawnOffset;
            
            // Instantiate clone
            _clone = Instantiate(_playerPrefab, new Vector3(spawnPos.x, spawnPos.y, 0), Quaternion.identity);
            _clone.Initialise(spawnPos);
            
            // Sync direction
            InputManager.Instance.SetCurrentDirection(_mainPlayer.CurrentDirection);
            
            if (_sharedLife)
            {
                _mainPlayer.OnDeath += OnMainPlayerDeath;
                _clone.OnDeath += OnCloneDeath;
            }
        }

        private void OnMainPlayerDeath()
        {
            if (_clone != null && _clone.IsAlive)
                _clone.Die();
        }

        private void OnCloneDeath()
        {
            if (_mainPlayer != null && _mainPlayer.IsAlive)
                _mainPlayer.Die();
        }

        private void OnDestroy()
        {
            if (_sharedLife)
            {
                if (_mainPlayer != null) _mainPlayer.OnDeath -= OnMainPlayerDeath;
                if (_clone != null) _clone.OnDeath -= OnCloneDeath;
            }
        }
    }
}
