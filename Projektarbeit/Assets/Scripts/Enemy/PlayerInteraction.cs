using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles interaction logic for the player, including reactions to collisions with projectiles
/// and implementing the IInteractable interface.
/// </summary>
public class PlayerInteraction : MonoBehaviour, IInteractable
{
    
    /// <summary>
    /// Health-bar to show the life of the player during the game 
    /// </summary>
    public Image healthBar;
    private float lerpSpeed = 1f;

    private void Update()
    {
        // Cache references in Start for performance
        Stats playerStats;
        playerStats = GameObject.Find("Player(Clone)").GetComponent<Stats>();
        Debug.Log(playerStats);
        healthBar = GameObject.FindGameObjectsWithTag("Health")[0].GetComponent<Image>();
        float targetFill = playerStats.GetCurStats(0) / playerStats.GetMaxStats(0);
        healthBar.fillAmount = Mathf.Lerp(healthBar.fillAmount, targetFill, Time.deltaTime * lerpSpeed);
    }
    
    
    /// <summary>
    /// Called when another GameObject interacts with this one.
    /// Currently left empty but can be customized for specific interactions.
    /// </summary>
    /// <param name="interactor">The GameObject initiating the interaction.</param>
    public void Interact(GameObject interactor)
    {
        // Interaction logic can be implemented here
    }

    /// <summary>
    /// Called when the interactor exits the interaction range or stops interacting.
    /// Currently left empty but can be extended as needed.
    /// </summary>
    /// <param name="interactor">The GameObject exiting interaction.</param>
    public void OnExit(GameObject interactor)
    {
        // Cleanup or feedback logic after interaction ends
    }

    /// <summary>
    /// Indicates whether the interaction should repeat over time.
    /// Always returns true in this implementation.
    /// </summary>
    /// <returns>True if the interaction is repeatable; otherwise, false.</returns>
    public bool ShouldRepeat()
    {
        return true;
    }

    /// <summary>
    /// Unity event method called when this GameObject collides with another.
    /// Checks if the object colliding is a projectile and applies damage to the player's health.
    /// </summary>
    /// <param name="collision">Collision information provided by Unity.</param>
    private void OnCollisionEnter(Collision collision)
    {
        // Check if the colliding object is a projectile
        if (collision.gameObject.name.Equals("ProjectileDrone(Clone)"))
        {
            Stats stats = GetComponent<Stats>();
            stats.DecreaseCurStat(0,2f);
            /*
            // Access the Health component of the player
            Health playerHealth = GetComponent<Health>();

            // Apply absolute damage using HealthManager
            playerHealth._currentHealth = HealthManager.damageAbsolute(
                2.0f,
                HealthManager.DamageType.Normal,
                playerHealth._currentHealth
            );
            */
        }
        
    }
}
