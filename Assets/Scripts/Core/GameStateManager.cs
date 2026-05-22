using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArrowNexus.Core
{
    /// <summary>
    /// Central finite state machine for game progression.
    /// Manages scene transitions, tracks current mode, and holds session-level stats.
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        public enum GameState
        {
            MainMenu,
            ModeSelect,
            LevelLoad,
            Playing,
            Paused,
            LevelComplete,
            Death,
            GameOver
        }

        public enum GameMode
        {
            Classic,
            TimedPulse,
            SurvivalGrid,
            LogicCore
        }

        public event Action<GameState> OnStateChanged;
        public event Action<GameMode>  OnModeChanged;

        public GameState CurrentState { get; private set; } = GameState.MainMenu;
        public GameMode  CurrentMode  { get; private set; } = GameMode.Classic;

        // Session Stats
        public int   SessionDeaths { get; private set; }
        public float SessionTime   { get; private set; }
        public float MaxCombo      { get; private set; }

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
            if (CurrentState == GameState.Playing)
            {
                SessionTime += Time.deltaTime;
            }
        }

        public void ChangeState(GameState newState)
        {
            if (CurrentState == newState) return;
            
            CurrentState = newState;
            
            switch (newState)
            {
                case GameState.LevelLoad:
                    Time.timeScale = 1f;
                    ResetSessionStats();
                    break;
                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;
                case GameState.Playing:
                    Time.timeScale = 1f;
                    break;
            }

            OnStateChanged?.Invoke(newState);
        }

        public void SetMode(GameMode mode)
        {
            CurrentMode = mode;
            OnModeChanged?.Invoke(mode);
        }

        public void RecordDeath()
        {
            SessionDeaths++;
            ChangeState(GameState.Death);
        }

        public void UpdateMaxCombo(float combo)
        {
            if (combo > MaxCombo) MaxCombo = combo;
        }

        private void ResetSessionStats()
        {
            SessionDeaths = 0;
            SessionTime   = 0f;
            MaxCombo      = 0f;
        }
        
        public void LoadScene(string sceneName)
        {
            ChangeState(GameState.LevelLoad);
            SceneManager.LoadScene(sceneName);
            // Scene load complete event should change state to Playing
        }
    }
}
