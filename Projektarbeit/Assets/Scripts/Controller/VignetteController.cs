using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Controller
{
    /// <summary>
    /// Controller for managing the health vignette behaviour depending on health state
    /// </summary>
    public class VignetteController : MonoBehaviour
    {

        /// <summary>
        /// private variable that changes the lerping speed
        /// </summary>
        [SerializeField] private float lerpSpeed = 0.6f;

        /// <summary>
        /// vignette color for full health state
        /// </summary>
        public Color fullHealthColor = new(0, 0, 0, 0);   // transparent oder dunkel
        
        /// <summary>
        /// vignette color for half-health state
        /// </summary>
        public Color halfHealthColor = new(1, 0.5f, 0, 0.6f);
        
        /// <summary>
        /// vignette color for low health state
        /// </summary>
        public Color lowHealthColor = new(1, 0, 0, 0.75f); // rot, half transparent

        /// <summary>
        /// volume on which we will set the vignette
        /// </summary>
        private Volume _volume;
        
        /// <summary>
        /// stats of the player that our vignette depends on
        /// </summary>
        private Stats.Stats _playerStats;
        
        /// <summary>
        /// actual vignette member that will be worked with
        /// </summary>
        private Vignette _vignette;

        /// <summary>
        /// start event of this monobehaviour and setting the member variables of volume & vignette
        /// </summary>
        private void Start()
        {
            _volume = GetComponent<Volume>();
            if (_volume == null)
            {
                Debug.LogError("Volume component not found on this GameObject!");
                return;
            }

            if (!_volume.profile.TryGet(out _vignette))
            {
                Debug.LogError("Vignette not found in Volume Profile!");
                return;
            }
        
            _vignette = _volume.profile.TryGet(out _vignette) ? _vignette : null;
        }

        /// <summary>
        /// update function that checks and updates the player stats to change the vignette style depending on the health value
        /// </summary>
        private void Update()
        {
            if (_vignette == null)
                return;

            if (_playerStats == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player == null)
                {
                    return;
                }
                _playerStats= player.GetComponent<Stats.Stats>();
            }
        
            var normalizedHealth = Mathf.Clamp01(_playerStats.GetCurStats(0) / _playerStats.GetMaxStats(0));
            float targetIntensity;
            Color targetColor;
            if (normalizedHealth > 0.3f && normalizedHealth <= 0.5f)
            {
                targetIntensity = 0.3f;
                targetColor = halfHealthColor;
            }
            else if (normalizedHealth <= 0.3f)
            {
                targetIntensity = 0.6f;
                targetColor = lowHealthColor;
            }
            else
            {
                targetIntensity = 0.0f;
                targetColor = fullHealthColor;
            }
        
            _vignette.intensity.value = Mathf.Lerp(_vignette.intensity.value, targetIntensity, Time.deltaTime * lerpSpeed);
            _vignette.color.value = Color.Lerp(_vignette.color.value, targetColor, Time.deltaTime * lerpSpeed);
        }
    }
}