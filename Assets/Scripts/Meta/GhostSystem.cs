using System.Collections.Generic;
using UnityEngine;
using ArrowNexus.Core;

namespace ArrowNexus.Meta
{
    /// <summary>
    /// Records player paths during a run and replays them as a "Ghost".
    /// </summary>
    public class GhostSystem : MonoBehaviour
    {
        public static GhostSystem Instance { get; private set; }

        [System.Serializable]
        public struct GhostFrame
        {
            public float Time;
            public Vector2Int Position;
            public InputManager.Direction Direction;
        }

        [Header("Recording")]
        public bool IsRecording;
        private List<GhostFrame> _currentRecording = new();
        private float _recordStartTime;

        [Header("Playback")]
        [SerializeField] private GameObject _ghostPrefab;
        private GameObject _activeGhost;
        private List<GhostFrame> _playbackData;
        private int _playbackIndex = 0;
        private float _playbackStartTime;

        private PlayerArrow _player;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            _player = FindObjectOfType<PlayerArrow>();
            if (_player != null)
            {
                // Subscribe to events that signify start/stop of a run
                // GameStateManager.Instance.OnStateChanged += HandleStateChange
            }
        }

        private void Update()
        {
            if (IsRecording && _player != null && _player.IsAlive)
            {
                // Record frame (simplified; in practice, might only record on change)
                _currentRecording.Add(new GhostFrame
                {
                    Time = Time.time - _recordStartTime,
                    Position = _player.GridPosition,
                    Direction = _player.CurrentDirection
                });
            }

            if (_activeGhost != null && _playbackData != null && _playbackIndex < _playbackData.Count)
            {
                float t = Time.time - _playbackStartTime;
                while (_playbackIndex < _playbackData.Count && _playbackData[_playbackIndex].Time <= t)
                {
                    UpdateGhostVisuals(_playbackData[_playbackIndex]);
                    _playbackIndex++;
                }
            }
        }

        public void StartRecording()
        {
            _currentRecording.Clear();
            _recordStartTime = Time.time;
            IsRecording = true;
        }

        public void StopRecording()
        {
            IsRecording = false;
        }

        public void StartPlayback(List<GhostFrame> data)
        {
            _playbackData = data;
            _playbackIndex = 0;
            _playbackStartTime = Time.time;
            
            if (_activeGhost == null && _ghostPrefab != null)
            {
                _activeGhost = Instantiate(_ghostPrefab);
            }
            _activeGhost.SetActive(true);
        }

        public void StopPlayback()
        {
            if (_activeGhost != null)
                _activeGhost.SetActive(false);
            _playbackData = null;
        }

        private void UpdateGhostVisuals(GhostFrame frame)
        {
            if (_activeGhost != null)
            {
                _activeGhost.transform.position = new Vector3(frame.Position.x, frame.Position.y, 0);
                // Also update rotation based on frame.Direction
            }
        }
        
        public List<GhostFrame> GetLastRecording() => _currentRecording;
    }
}
