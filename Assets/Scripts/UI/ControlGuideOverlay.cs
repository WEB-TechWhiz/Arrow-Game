using System.Collections;
using UnityEngine;
using ArrowNexus.Core;

namespace ArrowNexus.UI
{
    /// <summary>
    /// Runtime control guide that explains the basic inputs on first launch.
    /// It fades out automatically after a short delay or once the player starts interacting.
    /// </summary>
    public class ControlGuideOverlay : MonoBehaviour
    {
        [SerializeField] private float _autoHideDelay = 12f;
        [SerializeField] private float _fadeDuration = 0.35f;

        private CanvasGroup _canvasGroup;
        private InputManager _inputManager;
        private Coroutine _fadeRoutine;
        private bool _isDismissed;

        public void Setup(InputManager inputManager, CanvasGroup canvasGroup)
        {
            _inputManager = inputManager;
            _canvasGroup = canvasGroup;
        }

        private void Start()
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();

            if (_inputManager != null)
            {
                _inputManager.OnDirectionInput += HandleDirectionInput;
                _inputManager.OnDashInput += HandlePlayerInput;
                _inputManager.OnAbilityInput += HandlePlayerInput;
                _inputManager.OnPauseInput += HandlePlayerInput;
            }

            if (_canvasGroup != null)
                _fadeRoutine = StartCoroutine(AutoHideRoutine());
        }

        private void OnDestroy()
        {
            if (_inputManager != null)
            {
                _inputManager.OnDirectionInput -= HandleDirectionInput;
                _inputManager.OnDashInput -= HandlePlayerInput;
                _inputManager.OnAbilityInput -= HandlePlayerInput;
                _inputManager.OnPauseInput -= HandlePlayerInput;
            }
        }

        private void HandleDirectionInput(InputManager.Direction direction)
        {
            HandlePlayerInput();
        }

        private void HandlePlayerInput()
        {
            Dismiss();
        }

        private IEnumerator AutoHideRoutine()
        {
            yield return new WaitForSeconds(_autoHideDelay);
            Dismiss();
        }

        private void Dismiss()
        {
            if (_isDismissed || _canvasGroup == null) return;

            _isDismissed = true;
            if (_fadeRoutine != null)
                StopCoroutine(_fadeRoutine);

            StartCoroutine(FadeOutRoutine());
        }

        private IEnumerator FadeOutRoutine()
        {
            float elapsed = 0f;
            float startAlpha = _canvasGroup.alpha;

            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / _fadeDuration);
                yield return null;
            }

            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }
    }
}
