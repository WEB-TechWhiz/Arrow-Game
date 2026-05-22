using UnityEngine;
using ArrowNexus.Core;

namespace ArrowNexus.Audio
{
    /// <summary>
    /// Audio Manager using FMOD (placeholder API calls).
    /// Adapts music intensity based on player momentum.
    /// Triggers sound effects for game events.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("FMOD Events")]
        [SerializeField] private string _musicEvent = "event:/Music/MainTheme";
        [SerializeField] private string _deathEvent = "event:/SFX/Player/Death";
        [SerializeField] private string _dashEvent  = "event:/SFX/Player/Dash";
        [SerializeField] private string _goalEvent  = "event:/SFX/Environment/GoalReached";
        [SerializeField] private bool   _logAudioEvents = false;

        // FMOD instance placeholders
        // private FMOD.Studio.EventInstance _musicInstance;
        
        private PlayerArrow _player;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // FMOD initialization
            // _musicInstance = FMODUnity.RuntimeManager.CreateInstance(_musicEvent);
            // _musicInstance.start();
            StartMusic(_musicEvent);

            _player = FindObjectOfType<PlayerArrow>();
            if (_player != null)
            {
                _player.OnMomentumChanged += UpdateMusicIntensity;
                _player.OnDeath += PlayDeathSound;
                _player.OnDash += PlayDashSound;
            }

            if (CollisionSystem.Instance != null)
                CollisionSystem.Instance.OnGoalReached += PlayGoalSound;
        }

        private void OnDestroy()
        {
            if (_player != null)
            {
                _player.OnMomentumChanged -= UpdateMusicIntensity;
                _player.OnDeath -= PlayDeathSound;
                _player.OnDash -= PlayDashSound;
            }
            if (CollisionSystem.Instance != null)
                CollisionSystem.Instance.OnGoalReached -= PlayGoalSound;

            // _musicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            // _musicInstance.release();
        }

        private void UpdateMusicIntensity(float momentum)
        {
            // _musicInstance.setParameterByName("Intensity", momentum);
            // Debug.Log($"Music Intensity: {momentum}");
        }

        private void PlayDeathSound()
        {
            // FMODUnity.RuntimeManager.PlayOneShot(_deathEvent);
            PlayOneShot(_deathEvent);
        }

        private void PlayDashSound()
        {
            // FMODUnity.RuntimeManager.PlayOneShot(_dashEvent);
            PlayOneShot(_dashEvent);
        }

        private void PlayGoalSound(Vector2Int cell)
        {
            // FMODUnity.RuntimeManager.PlayOneShot(_goalEvent);
            PlayOneShot(_goalEvent);
        }

        private void StartMusic(string eventPath)
        {
            if (_logAudioEvents)
                Debug.Log($"Audio music event requested: {eventPath}");
        }

        private void PlayOneShot(string eventPath)
        {
            if (_logAudioEvents)
                Debug.Log($"Audio one-shot event requested: {eventPath}");
        }
    }
}
