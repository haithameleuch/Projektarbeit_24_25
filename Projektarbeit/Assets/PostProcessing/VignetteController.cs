using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VignetteControler : MonoBehaviour
{

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
        
        float health01 = Mathf.Clamp01(playerStats.GetCurStats(0) / playerStats.GetMaxStats(0));
        if (health01 > 0.3f && health01 <= 0.5f)
        {
            vignette.color.value = Color.Lerp(halfHealthColor, fullHealthColor, health01);
            vignette.intensity.value = Mathf.Lerp(0.3f, 0.0f, health01);
        }
        else if (health01 <= 0.3f)
        {
            vignette.color.value = Color.Lerp(lowHealthColor, halfHealthColor, health01);
            vignette.intensity.value = Mathf.Lerp(0.5f, 0.3f, health01);
        }
        
    }
}