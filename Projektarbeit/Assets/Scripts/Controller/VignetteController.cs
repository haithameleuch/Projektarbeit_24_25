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
        public Color fullHealthColor = new Color(0, 0, 0, 0);   // transparent oder dunkel
        
        /// <summary>
        /// vignette color for half health state
        /// </summary>
        public Color halfHealthColor = new Color(1, 0.5f, 0, 0.6f);
        
        /// <summary>
        /// vignette color for low health state
        /// </summary>
        public Color lowHealthColor = new Color(1, 0, 0, 0.75f); // rot, halbtransparent

        /// <summary>
        /// volume on which we will set the vignette
        /// </summary>
        private Volume volume;
        
        /// <summary>
        /// stats of the player that our vignette depends on
        /// </summary>
        private Stats.Stats playerStats;
        
        /// <summary>
        /// actual vignette member that will be worked with
        /// </summary>
        private Vignette vignette;

        /// <summary>
        /// start event of this monobehaviour and setting the member variables of volume & vignette
        /// </summary>
        void Start()
        {
            volume = GetComponent<Volume>();
            if (volume == null)
            {
                Debug.LogError("Volume component not found on this GameObject!");
                return;
            }

            if (!volume.profile.TryGet(out vignette))
            {
                Debug.LogError("Vignette not found in Volume Profile!");
                return;
            }
        
            vignette = volume.profile.TryGet<Vignette>(out vignette) ? vignette : null;
        }

        /// <summary>
        /// update function that checks and updates the player stats to change the vignette style depending on the health value
        /// </summary>
        void Update()
        {
            if (vignette == null)
                return;

            if (playerStats == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player == null)
                {
                    return;
                }
                playerStats= player.GetComponent<Stats.Stats>();
            }
        
            float normalizedHealth = Mathf.Clamp01(playerStats.GetCurStats(0) / playerStats.GetMaxStats(0));
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
        
            vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, targetIntensity, Time.deltaTime * lerpSpeed);
            vignette.color.value = Color.Lerp(vignette.color.value, targetColor, Time.deltaTime * lerpSpeed);
        }
    }
}