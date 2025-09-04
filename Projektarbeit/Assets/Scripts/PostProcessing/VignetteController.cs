using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VignetteController : MonoBehaviour
{

    [SerializeField] private float lerpSpeed = 0.6f;

    public Color fullHealthColor = new Color(0, 0, 0, 0);   // transparent oder dunkel
    public Color halfHealthColor = new Color(1, 0.5f, 0, 0.6f);
    public Color lowHealthColor = new Color(1, 0, 0, 0.75f); // rot, halbtransparent

    private Volume volume;
    private Stats playerStats;
    private Vignette vignette;

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
            playerStats= player.GetComponent<Stats>();
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