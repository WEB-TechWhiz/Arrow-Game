using System;
using UnityEngine;

namespace ArrowNexus.Mechanics
{
    /// <summary>
    /// Pulse Doors — doors that open only on rhythm cycles.
    /// Driven by the global PulseTimer (synced to audio beat or fixed interval).
    /// Doors visually pulse before opening; player must time movement to pass.
    /// </summary>
    public class PulseDoors : MonoBehaviour
    {
        // ─── Events ──────────────────────────────────────────────────────────────
        public event Action OnDoorOpened;
        public event Action OnDoorClosed;

        // ─── Inspector Config ────────────────────────────────────────────────────
        [Header("Timing")]
        [SerializeField] private int   _openOnBeat    = 1;    // open on every Nth beat
        [SerializeField] private float _openDuration  = 0.8f; // seconds door stays open
        [SerializeField] private float _warningBeats  = 0.25f; // fraction of beat for warning flash

        [Header("Door Cells")]
        [SerializeField] private Vector2Int[] _doorCells;     // tiles that open/close

        [Header("Visual")]
        [SerializeField] private SpriteRenderer[] _doorSprites;
        [SerializeField] private Color            _closedColor  = new(1f, 0.3f, 0.1f);
        [SerializeField] private Color            _warningColor = new(1f, 0.8f, 0.0f);
        [SerializeField] private Color            _openColor    = new(0.2f, 1f, 0.4f);

        // ─── State ───────────────────────────────────────────────────────────────
        public  bool  IsOpen        { get; private set; } = false;
        private float _beatTimer    = 0f;
        private float _openTimer    = 0f;
        private bool  _isWarning    = false;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        private void Start()
        {
            // Subscribe to global pulse timer
            if (PulseTimer.Instance != null)
                PulseTimer.Instance.OnBeat += OnBeatReceived;

            ApplyDoorState(false);
        }

        private void OnDestroy()
        {
            if (PulseTimer.Instance != null)
                PulseTimer.Instance.OnBeat -= OnBeatReceived;
        }

        private void Update()
        {
            UpdateWarningState();

            if (IsOpen)
            {
                _openTimer -= Time.deltaTime;
                if (_openTimer <= 0)
                    CloseDoor();
            }
        }

        // ─── Beat Handler ─────────────────────────────────────────────────────────

        private void OnBeatReceived(int beatNumber)
        {
            _beatTimer = 0f;

            if (_openOnBeat <= 0) return;
            if (beatNumber % _openOnBeat != 0) return;
            OpenDoor();
        }

        // ─── Open / Close ─────────────────────────────────────────────────────────

        private void OpenDoor()
        {
            if (IsOpen) return;
            IsOpen     = true;
            _openTimer = _openDuration;
            _isWarning = false;

            ApplyDoorState(true);
            SetDoorColor(_openColor);
            OnDoorOpened?.Invoke();
        }

        private void CloseDoor()
        {
            IsOpen = false;
            _isWarning = false;
            ApplyDoorState(false);
            SetDoorColor(_closedColor);
            OnDoorClosed?.Invoke();
        }

        private void UpdateWarningState()
        {
            if (IsOpen || PulseTimer.Instance == null || _openOnBeat <= 0)
            {
                SetWarning(false);
                return;
            }

            _beatTimer += Time.deltaTime;
            float beatInterval = PulseTimer.Instance.BeatInterval;
            float warningDuration = Mathf.Clamp01(_warningBeats) * beatInterval;
            int beatsUntilOpen = _openOnBeat - (PulseTimer.Instance.BeatNumber % _openOnBeat);
            float timeUntilOpen = ((beatsUntilOpen - 1) * beatInterval) + (beatInterval - _beatTimer);

            SetWarning(timeUntilOpen <= warningDuration);
        }

        private void SetWarning(bool warning)
        {
            if (_isWarning == warning) return;

            _isWarning = warning;
            SetDoorColor(warning ? _warningColor : _closedColor);
        }

        // ─── Tile Updates ─────────────────────────────────────────────────────────

        private void ApplyDoorState(bool open)
        {
            if (ArrowNexus.Maze.TileManager.Instance == null) return;

            foreach (Vector2Int cell in _doorCells)
            {
                int tileType = open ? ArrowNexus.Maze.MazeGenerator.PATH : ArrowNexus.Maze.MazeGenerator.WALL;
                ArrowNexus.Maze.TileManager.Instance.SetCell(cell, tileType);
            }
        }

        private void SetDoorColor(Color c)
        {
            foreach (var sr in _doorSprites)
                if (sr != null) sr.color = c;
        }
    }

    // ─── Global Pulse Timer ──────────────────────────────────────────────────────

    /// <summary>
    /// Global singleton that fires OnBeat at a configurable BPM.
    /// Can be synced to FMOD audio beats or run as a fixed interval timer.
    /// </summary>
    public class PulseTimer : MonoBehaviour
    {
        public static PulseTimer Instance { get; private set; }

        public event Action<int> OnBeat;   // passes beat number (1-indexed)

        [Header("Pulse Settings")]
        [SerializeField] private float _bpm          = 120f;
        [SerializeField] private bool  _syncToAudio  = false;

        public float BeatInterval => 60f / _bpm;
        public int   BeatNumber   { get; private set; } = 0;

        private float _timer = 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (_syncToAudio) return; // FMOD drives beats externally

            _timer += Time.deltaTime;
            if (_timer >= BeatInterval)
            {
                _timer -= BeatInterval;
                BeatNumber++;
                OnBeat?.Invoke(BeatNumber);
            }
        }

        /// <summary>Called externally by FMOD audio system on a beat event.</summary>
        public void RegisterExternalBeat()
        {
            BeatNumber++;
            OnBeat?.Invoke(BeatNumber);
        }
    }
}
