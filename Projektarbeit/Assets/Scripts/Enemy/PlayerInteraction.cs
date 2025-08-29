using UnityEngine;
using UnityEngine.UI;

namespace Enemy
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
        private void Update()
        {
            // Cache references in Start for performance
            var playerStats = gameObject.GetComponent<Stats>();
            _healthBar = GameObject.FindGameObjectsWithTag("Health")[0].GetComponent<Image>();
            var targetFill = playerStats.GetCurStats(0) / playerStats.GetMaxStats(0);
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
            
            var stats = GetComponent<Stats>();
            stats.DecreaseCurStat(0,2f);

        }
    }
}
