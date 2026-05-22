using System;
using UnityEngine;
using ArrowNexus.Core;

namespace ArrowNexus.Meta
{
    /// <summary>
    /// Tracks speedrun metrics and submits to leaderboards.
    /// </summary>
    public class SpeedrunSystem : MonoBehaviour
    {
        public static SpeedrunSystem Instance { get; private set; }

        public event Action<float> OnRunFinished;

        public bool IsRunning { get; private set; }
        public float CurrentTime { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (CollisionSystem.Instance != null)
                CollisionSystem.Instance.OnGoalReached += StopRun;
        }

        private void OnDestroy()
        {
            if (CollisionSystem.Instance != null)
                CollisionSystem.Instance.OnGoalReached -= StopRun;
        }

        private void Update()
        {
            if (IsRunning)
            {
                CurrentTime += Time.deltaTime;
            }
        }

        public void StartRun()
        {
            CurrentTime = 0f;
            IsRunning = true;
        }

        public void StopRun(Vector2Int endCell)
        {
            if (!IsRunning) return;
            IsRunning = false;
            OnRunFinished?.Invoke(CurrentTime);
            
            SubmitScoreToLeaderboard(CurrentTime);
        }

        private void SubmitScoreToLeaderboard(float time)
        {
            Debug.Log($"Submitting run time: {time:F2} seconds to Firebase");
            // Placeholder: Firebase Realtime Database submission
            // string levelId = ...
            // FirebaseDatabase.DefaultInstance.GetReference($"Leaderboards/{levelId}/...").SetValueAsync(time);
        }
    }
}
