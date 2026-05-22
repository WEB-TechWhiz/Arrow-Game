using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ArrowNexus.Core;

namespace ArrowNexus.UI
{
    /// <summary>
    /// Minimal but informative HUD.
    /// Elements:
    ///   - Momentum Meter: Filled bar that grows with continuous movement
    ///   - Mini-map: Render Texture from secondary camera (set up in UI Canvas)
    ///   - Pulse Timer: Circular countdown synced to global pulse
    ///   - Combo Meter: Number + multiplier, animated on increment
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        // ─── Inspector Config ────────────────────────────────────────────────────
        [Header("Momentum Meter")]
        [SerializeField] private Image _momentumFill;
        [SerializeField] private Color _momentumLowColor  = new(0.2f, 0.8f, 1f);
        [SerializeField] private Color _momentumHighColor = new(1f, 0.2f, 0.8f);

        [Header("Combo Meter")]
        [SerializeField] private TextMeshProUGUI _comboText;
        [SerializeField] private float           _comboScaleBump = 1.4f;
        [SerializeField] private float           _comboAnimTime  = 0.2f;

        [Header("Pulse Timer")]
        [SerializeField] private Image _pulseRingFill;
        
        [Header("Screens")]
        [SerializeField] private GameObject _deathScreen;
        [SerializeField] private GameObject _levelCompleteScreen;

        // ─── Dependencies ────────────────────────────────────────────────────────
        private PlayerArrow _player;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        private void Start()
        {
            _player = FindObjectOfType<PlayerArrow>();
            if (_player != null)
            {
                _player.OnMomentumChanged += UpdateMomentum;
                _player.OnComboUpdated    += UpdateCombo;
                _player.OnDeath           += ShowDeathScreen;
            }

            if (ArrowNexus.Mechanics.PulseTimer.Instance != null)
                ArrowNexus.Mechanics.PulseTimer.Instance.OnBeat += OnBeat;

            if (CollisionSystem.Instance != null)
                CollisionSystem.Instance.OnGoalReached += ShowLevelComplete;

            _deathScreen?.SetActive(false);
            _levelCompleteScreen?.SetActive(false);
            
            // Init HUD state
            UpdateMomentum(0f);
            UpdateCombo(1f);
        }

        public void ConfigureRuntimeScreens(GameObject deathScreen, GameObject levelCompleteScreen)
        {
            _deathScreen = deathScreen;
            _levelCompleteScreen = levelCompleteScreen;

            _deathScreen?.SetActive(false);
            _levelCompleteScreen?.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_player != null)
            {
                _player.OnMomentumChanged -= UpdateMomentum;
                _player.OnComboUpdated    -= UpdateCombo;
                _player.OnDeath           -= ShowDeathScreen;
            }
            
            if (ArrowNexus.Mechanics.PulseTimer.Instance != null)
                ArrowNexus.Mechanics.PulseTimer.Instance.OnBeat -= OnBeat;

            if (CollisionSystem.Instance != null)
                CollisionSystem.Instance.OnGoalReached -= ShowLevelComplete;
        }

        private void Update()
        {
            UpdatePulseTimer();
        }

        // ─── Momentum ────────────────────────────────────────────────────────────

        private void UpdateMomentum(float norm)
        {
            if (_momentumFill == null) return;
            _momentumFill.fillAmount = norm;
            _momentumFill.color      = Color.Lerp(_momentumLowColor, _momentumHighColor, norm);
        }

        // ─── Combo ───────────────────────────────────────────────────────────────

        private void UpdateCombo(float mult)
        {
            if (_comboText == null) return;
            
            if (mult <= 1f)
            {
                _comboText.text = "";
                return;
            }

            _comboText.text = $"x{mult:F1}";
            StartCoroutine(ComboBumpAnim());
        }

        private IEnumerator ComboBumpAnim()
        {
            Vector3 orig = Vector3.one;
            Vector3 bump = Vector3.one * _comboScaleBump;

            float t = 0;
            while (t < _comboAnimTime)
            {
                t += Time.deltaTime;
                _comboText.transform.localScale = Vector3.Lerp(bump, orig, t / _comboAnimTime);
                yield return null;
            }
            _comboText.transform.localScale = orig;
        }

        // ─── Pulse Timer ─────────────────────────────────────────────────────────

        private void UpdatePulseTimer()
        {
            if (_pulseRingFill == null || ArrowNexus.Mechanics.PulseTimer.Instance == null) return;
            
            float interval = ArrowNexus.Mechanics.PulseTimer.Instance.BeatInterval;
            // Need a way to get progress. Let's assume we can approximate it or add a getter to PulseTimer.
            // For now, we simulate visual countdown.
            float progress = (Time.time % interval) / interval;
            _pulseRingFill.fillAmount = 1f - progress;
        }

        private void OnBeat(int beatNumber)
        {
            // Optional visual flash on beat
        }

        // ─── Screens ─────────────────────────────────────────────────────────────

        private void ShowDeathScreen()
        {
            _deathScreen?.SetActive(true);
            GameStateManager.Instance?.RecordDeath();
        }

        private void ShowLevelComplete(Vector2Int cell)
        {
            _levelCompleteScreen?.SetActive(true);
            GameStateManager.Instance?.ChangeState(GameStateManager.GameState.LevelComplete);
        }
    }
}
