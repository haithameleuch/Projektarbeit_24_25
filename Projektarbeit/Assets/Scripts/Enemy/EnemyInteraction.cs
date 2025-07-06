using TMPro;
using UnityEngine;

namespace Enemy
{
    /// <summary>
    /// This module handles all enemy health changes triggered by player interactions.
    /// </summary>
    public class EnemyInteraction : MonoBehaviour, IInteractable
    {
        // Flag to control whether the player/enemy can move
        private bool _canMove = true;

        // Reference to the TextMeshPro component that displays life/health
        [SerializeField] private TextMeshPro LifeText;

        // Stores the current color, possibly for visual effects or status indication
        private Color _currentColor;

        /// <summary>
        /// Interact with different components
        /// </summary>
        /// <param name="interactor"></param>
        public void Interact(GameObject interactor)
        {
            // Exit the method if there's no interactor
            if (!interactor) return;
            
            // Try to get the Health component from the interactor
            var health = interactor.GetComponent<Health>();
            
            // Disable movement while processing the interaction
            _canMove = false;
            
            // Exit if the interactor does not have a Health component
            if (!health) return;
            
            
            // Get the current health value of the interactor
            float currentHealth = health.currentHealth;
            
            // Apply damage if the interactor is the player
            if(interactor.name.Equals("Player"))
            {
                // Deal 2.0f normal damage to the player's health using the HealthManager
                health.currentHealth = HealthManager.damageAbsolute(2.0f, HealthManager.DamageType.Normal, currentHealth);
            }
        }

        // Called when the interactor exits the interaction zone or completes interaction
        public void OnExit(GameObject interactor)
        {
            if (!interactor) return;

            // Allow movement again after interaction ends
            _canMove = true;

            // Hide any related UI panel
            UIManager.Instance.HidePanel();
        }

        // Indicates whether the interaction should repeat (always false here)
        public bool ShouldRepeat()
        {
            return false;
        }

        // Returns whether movement is currently allowed
        public bool CanMove() { return _canMove; }

        private void OnCollisionEnter(Collision collision)
        {
            // Check if the object colliding is a projectile
            if (collision.gameObject.name.Equals("Projectile(Clone)"))
            {
                // Get this object's Health component
                Health enemyHealth = GetComponent<Health>();
                float currentHealth = enemyHealth.currentHealth;
                float maxHealth = enemyHealth.maxHealth;

                // Calculate the remaining health percentage
                float healthPercent = Mathf.Clamp01(currentHealth / maxHealth);

                // Calculate the target color from red (low) to green (high) based on health
                Color targetColor = Color.Lerp(Color.red, Color.green, healthPercent);

                // Smoothly transition the displayed color to the target color
                _currentColor = Color.Lerp(_currentColor, targetColor, Time.deltaTime * 8f);
                LifeText.color = _currentColor;

                // Display current health value as text
                LifeText.text = $"{currentHealth:0}";
        
                // Apply damage to the enemy's health
                enemyHealth.currentHealth = HealthManager.damageAbsolute(0.5f, HealthManager.DamageType.Normal, enemyHealth.currentHealth);

                // Destroy the object if health reaches 0
                if (enemyHealth.currentHealth <= 0f)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
