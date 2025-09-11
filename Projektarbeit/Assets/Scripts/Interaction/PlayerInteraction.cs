using UnityEngine;
using UnityEngine.UI;

namespace Interaction
{
    /// <summary>
    /// Handles interaction logic for the player, including reactions to collisions with projectiles
    /// and implementing the IInteractable interface.
    /// </summary>
    public class PlayerInteraction : MonoBehaviour
    {
    
        /// <summary>
        /// Health-bar to show the life of the player during the game 
        /// </summary>
        private Image _healthBar;
        
        /// <summary>
        /// Unity Update method.
        /// Continuously updates the health bar fill based on the player's current health.
        /// </summary>
        private void Update()
        {
            // Cache reference to Stats component for current/max health
            var playerStats = gameObject.GetComponent<Stats.Stats>();

            // Cache reference to the health bar UI (first object tagged "Health")
            _healthBar = GameObject.FindGameObjectsWithTag("Health")[0].GetComponent<Image>();

            // Calculate fill ratio (0 = empty, 1 = full)
            var targetFill = playerStats.GetCurStats(0) / playerStats.GetMaxStats(0);

            // Update health bar UI
            _healthBar.fillAmount = targetFill;
        }

        /// <summary>
        /// Unity event method called when this GameObject collides with another.
        /// Checks if the object colliding is projectile and applies damage to the player's health.
        /// </summary>
        /// <param name="collision">Collision information provided by Unity.</param>
        private void OnCollisionEnter(Collision collision)
        {
            // Check if the colliding object is a projectile
            if (!collision.gameObject.name.Equals("ProjectileDrone(Clone)")) return;
            
            var stats = GetComponent<Stats.Stats>();
            stats.DecreaseCurStat(0,2f);

        }
    }
}
