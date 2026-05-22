using System;
using UnityEngine;

namespace ArrowNexus.Meta
{
    /// <summary>
    /// Fetches a daily seed (e.g. from Firebase) to generate a deterministic daily maze.
    /// Manages special daily modifiers.
    /// </summary>
    public class DailyChallengeSystem : MonoBehaviour
    {
        public static DailyChallengeSystem Instance { get; private set; }

        public event Action<int> OnDailySeedFetched;

        public int CurrentDailySeed { get; private set; }
        public bool IsDailyChallengeActive { get; private set; }

        [Header("Modifiers")]
        public float SpeedMultiplier = 1f;
        public bool IsInvisibleMaze = false;
        public bool GravityFlipped = false;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void FetchDailyChallenge()
        {
            // Placeholder: Fetch from Firebase
            // FirebaseDatabase.DefaultInstance.GetReference("DailySeed").GetValueAsync().ContinueWithOnMainThread(...)
            
            // Simulating a deterministic daily seed based on the date
            string dateString = DateTime.UtcNow.ToString("yyyyMMdd");
            CurrentDailySeed = dateString.GetHashCode();
            
            // Generate modifiers deterministically from seed
            System.Random rand = new System.Random(CurrentDailySeed);
            SpeedMultiplier = rand.NextDouble() > 0.5 ? 1.5f : 1f;
            IsInvisibleMaze = rand.NextDouble() > 0.8;
            GravityFlipped = rand.NextDouble() > 0.7;

            IsDailyChallengeActive = true;
            OnDailySeedFetched?.Invoke(CurrentDailySeed);
        }

        public void EndDailyChallenge()
        {
            IsDailyChallengeActive = false;
            SpeedMultiplier = 1f;
            IsInvisibleMaze = false;
            GravityFlipped = false;
        }
    }
}
