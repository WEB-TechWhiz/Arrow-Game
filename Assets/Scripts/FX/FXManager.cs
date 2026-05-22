using System;
using System.Collections;
using UnityEngine;

namespace ArrowNexus.FX
{
    /// <summary>
    /// Central manager for visual effects and shaders.
    /// Handles player trails, death explosions, corruption zones, etc.
    /// </summary>
    public class FXManager : MonoBehaviour
    {
        public static FXManager Instance { get; private set; }

        [Header("Prefabs")]
        [SerializeField] private GameObject _deathExplosionPrefab;
        [SerializeField] private GameObject _signalPulsePrefab;
        
        [Header("Global Materials")]
        [SerializeField] private Material _corruptionMaterial;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void SpawnDeathExplosion(Vector3 position)
        {
            if (_deathExplosionPrefab != null)
            {
                Instantiate(_deathExplosionPrefab, position, Quaternion.identity);
            }
            // Screenshake handled by CameraController listening to OnDeath
        }

        public void TriggerCorruptionGlitch(Vector2Int origin, Vector2Int size, float intensity)
        {
            if (_corruptionMaterial != null)
            {
                // In a real implementation, we'd pass these properties to a global shader
                // or a post-processing volume that masks the specific area.
                // _corruptionMaterial.SetFloat("_GlitchIntensity", intensity);
                // _corruptionMaterial.SetVector("_GlitchAreaOrigin", new Vector4(origin.x, origin.y, 0, 0));
                // _corruptionMaterial.SetVector("_GlitchAreaSize", new Vector4(size.x, size.y, 0, 0));
                StartCoroutine(ResetGlitchAfterDelay(0.2f));
            }
        }

        private IEnumerator ResetGlitchAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            // _corruptionMaterial.SetFloat("_GlitchIntensity", 0f);
        }
    }
}
