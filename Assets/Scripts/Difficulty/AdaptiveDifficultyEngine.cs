using UnityEngine;
using ArrowNexus.Core;

namespace ArrowNexus.Difficulty
{
    /// <summary>
    /// Tracks player behavior (deaths, speed, hesitation) and adjusts game variables dynamically.
    /// </summary>
    public class AdaptiveDifficultyEngine : MonoBehaviour
    {
        public static AdaptiveDifficultyEngine Instance { get; private set; }

        [Header("Metrics")]
        public int DeathsThisLevel;
        public float IdleTime;
        public int WrongTurns;

        [Header("Difficulty Multipliers")]
        public float TrapTimingMultiplier = 1f;
        public float MazeComplexityMultiplier = 1f;
        public float SpeedRequirementMultiplier = 1f;

        private PlayerArrow _player;
        private Vector2Int _lastPos;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            _player = FindObjectOfType<PlayerArrow>();
            if (_player != null)
            {
                _player.OnDeath += RecordDeath;
                _lastPos = _player.GridPosition;
            }
            
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnStateChanged += OnStateChanged;
        }

        private void OnDestroy()
        {
            if (_player != null)
                _player.OnDeath -= RecordDeath;
                
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnStateChanged -= OnStateChanged;
        }

        private void Update()
        {
            if (_player == null || !(_player.IsAlive)) return;

            if (_player.GridPosition == _lastPos)
            {
                IdleTime += Time.deltaTime;
                if (IdleTime > 1.5f) // Hesitation threshold
                {
                    // Player is confused, lower complexity next run
                    MazeComplexityMultiplier = Mathf.Max(0.5f, MazeComplexityMultiplier - 0.05f);
                }
            }
            else
            {
                IdleTime = 0f;
                _lastPos = _player.GridPosition;
            }
        }

        private void RecordDeath()
        {
            DeathsThisLevel++;
            
            if (DeathsThisLevel >= 3)
            {
                // Struggling player
                TrapTimingMultiplier = Mathf.Min(2f, TrapTimingMultiplier + 0.2f); // Slower traps
                SpeedRequirementMultiplier = Mathf.Max(0.5f, SpeedRequirementMultiplier - 0.1f);
            }
        }
        
        public void RecordWrongTurn()
        {
            WrongTurns++;
        }

        private void OnStateChanged(GameStateManager.GameState state)
        {
            if (state == GameStateManager.GameState.LevelLoad)
            {
                // Reset per-level metrics
                DeathsThisLevel = 0;
                IdleTime = 0f;
                WrongTurns = 0;
                
                // Gradually restore default difficulty if doing well
                TrapTimingMultiplier = Mathf.Lerp(TrapTimingMultiplier, 1f, 0.1f);
                MazeComplexityMultiplier = Mathf.Lerp(MazeComplexityMultiplier, 1f, 0.1f);
                SpeedRequirementMultiplier = Mathf.Lerp(SpeedRequirementMultiplier, 1f, 0.1f);
            }
        }
    }
}
