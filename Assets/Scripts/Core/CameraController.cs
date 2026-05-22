using UnityEngine;

namespace ArrowNexus.Core
{
    /// <summary>
    /// Smooth camera that follows the player arrow across the maze.
    /// Features:
    ///   - Smooth follow with configurable damping
    ///   - Dynamic FOV zoom-out at high momentum
    ///   - Screenshake on death and danger events
    ///   - Minimap secondary camera (render texture based)
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        // ─── Singleton ───────────────────────────────────────────────────────────
        public static CameraController Instance { get; private set; }

        // ─── Inspector Config ────────────────────────────────────────────────────
        [Header("Follow")]
        [SerializeField] private float _followDamping      = 6f;    // higher = snappier
        [SerializeField] private float _lookAheadDistance  = 0.6f;  // world units ahead of player

        [Header("FOV / Orthographic Size")]
        [SerializeField] private float _baseCameraSize     = 6f;
        [SerializeField] private float _maxCameraSize      = 8f;   // at full momentum
        [SerializeField] private float _fovChangeDamping   = 4f;

        [Header("Screenshake")]
        [SerializeField] private float _deathShakeMagnitude  = 0.4f;
        [SerializeField] private float _deathShakeDuration   = 0.35f;
        [SerializeField] private float _dangerShakeMagnitude = 0.15f;
        [SerializeField] private float _dangerShakeDuration  = 0.15f;

        [Header("Minimap Camera")]
        [SerializeField] private Camera _minimapCamera;
        [SerializeField] private float  _minimapSize = 50f;

        // ─── State ───────────────────────────────────────────────────────────────
        private Camera     _cam;
        private PlayerArrow _player;
        private float      _targetCamSize;
        private float      _shakeTimer;
        private float      _shakeMagnitude;
        private Vector3    _shakeOffset;

        // Cached transform to avoid repeated GetComponent calls
        private Transform _t;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _cam             = GetComponent<Camera>();
            _t               = transform;
            _targetCamSize   = _baseCameraSize;
            _cam.orthographic = true;
            _cam.orthographicSize = _baseCameraSize;
        }

        private void Start()
        {
            _player = FindObjectOfType<PlayerArrow>();
            if (_player == null) return;

            _player.OnDeath          += OnPlayerDeath;
            _player.OnMomentumChanged += OnMomentumChanged;

            // Subscribe to collision danger (optional — driven by hazard proximity)
            if (CollisionSystem.Instance != null)
                CollisionSystem.Instance.OnHazardHit += _ => TriggerShake(_dangerShakeMagnitude, _dangerShakeDuration);

            // Setup minimap camera
            if (_minimapCamera != null)
            {
                _minimapCamera.orthographic     = true;
                _minimapCamera.orthographicSize = _minimapSize;
            }
        }

        private void OnDestroy()
        {
            if (_player == null) return;
            _player.OnDeath           -= OnPlayerDeath;
            _player.OnMomentumChanged -= OnMomentumChanged;
        }

        // ─── Update ──────────────────────────────────────────────────────────────

        private void LateUpdate()
        {
            if (_player == null) return;

            FollowPlayer();
            UpdateFOV();
            UpdateShake();
            SyncMinimapCamera();
        }

        // ─── Follow ──────────────────────────────────────────────────────────────

        private void FollowPlayer()
        {
            // Look-ahead: offset camera slightly in player's movement direction
            Vector3 lookahead = GetLookAheadOffset();
            Vector3 target    = new(
                _player.transform.position.x + lookahead.x,
                _player.transform.position.y + lookahead.y,
                _t.position.z
            );

            _t.position = Vector3.Lerp(_t.position, target + _shakeOffset, Time.deltaTime * _followDamping);
        }

        private Vector3 GetLookAheadOffset()
        {
            Vector2Int dir = InputManager.DirectionToVector(_player.CurrentDirection);
            return new Vector3(dir.x, dir.y, 0f) * _lookAheadDistance;
        }

        // ─── FOV / Ortho Size ────────────────────────────────────────────────────

        private void OnMomentumChanged(float momentum)
        {
            _targetCamSize = Mathf.Lerp(_baseCameraSize, _maxCameraSize, momentum);
        }

        private void UpdateFOV()
        {
            _cam.orthographicSize = Mathf.Lerp(
                _cam.orthographicSize,
                _targetCamSize,
                Time.deltaTime * _fovChangeDamping
            );
        }

        // ─── Screenshake ─────────────────────────────────────────────────────────

        private void OnPlayerDeath() => TriggerShake(_deathShakeMagnitude, _deathShakeDuration);

        public void TriggerShake(float magnitude, float duration)
        {
            _shakeMagnitude = magnitude;
            _shakeTimer     = duration;
        }

        private void UpdateShake()
        {
            if (_shakeTimer > 0)
            {
                _shakeTimer  -= Time.deltaTime;
                float decay   = _shakeTimer > 0 ? _shakeMagnitude * (_shakeTimer / _deathShakeDuration) : 0f;
                _shakeOffset  = new Vector3(
                    Random.Range(-1f, 1f) * decay,
                    Random.Range(-1f, 1f) * decay,
                    0f
                );
            }
            else
            {
                _shakeOffset = Vector3.Lerp(_shakeOffset, Vector3.zero, Time.deltaTime * 10f);
            }
        }

        // ─── Minimap ─────────────────────────────────────────────────────────────

        private void SyncMinimapCamera()
        {
            if (_minimapCamera == null || _player == null) return;

            // Minimap follows player position on X/Y, fixed Z
            Vector3 playerPos = _player.transform.position;
            _minimapCamera.transform.position = new Vector3(playerPos.x, playerPos.y, _minimapCamera.transform.position.z);
        }
    }
}
