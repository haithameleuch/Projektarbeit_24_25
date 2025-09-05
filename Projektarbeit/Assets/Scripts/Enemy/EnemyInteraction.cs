using TMPro;
using UnityEngine;

namespace Enemy
{
    /// <summary>
    /// Handles interactions between the player and an enemy,
    /// including applying damage over time when in contact,
    /// reacting to projectiles, updating enemy life display,
    /// and reporting death when health reaches zero.
    /// </summary>
    public class EnemyInteraction : MonoBehaviour, IInteractable
    {
        /// <summary>
        /// Reference to the <see cref="TextMeshPro"/> component
        /// used for displaying the enemy's current health.
        /// </summary>
        [SerializeField] private TextMeshPro lifeText;
        
        /// <summary>
        /// Tracks the current interpolated color of the enemy's life text,
        /// used to smoothly transition between health-based colors.
        /// </summary>
        private Color _currentColor;
        
        /// <summary>
        /// Modifier applied to incoming damage per second when
        /// the player interacts with the enemy.
        /// </summary>
        private const float DamageModifier = 0.25f;

        /// <summary>
        /// Called when the player interacts with the enemy.
        /// Deals continuous damage to the player based on the enemy's stats.
        /// </summary>
        /// <param name="interactor">The interacting GameObject, typically the player.</param>
        public void Interact(GameObject interactor)
        {
            if (!interactor || !interactor.name.Equals("Player(Clone)")) return;
            var playerStats = interactor.GetComponent<Stats>();
            var enemyStats = GetComponent<Stats>();

            if (playerStats is null || enemyStats is null) return;
            
            // Enemy damages the player over time
            var damagePerSecond = enemyStats.GetCurStats(1);
            playerStats.DecreaseCurStat(0, damagePerSecond * Time.deltaTime * DamageModifier);
        }

        /// <summary>
        /// Called when the interaction ends. Currently unused.
        /// </summary>
        /// <param name="interactor">The GameObject that ended interaction.</param>
        public void OnExit(GameObject interactor) { }

        /// <summary>
        /// Defines whether interaction should be repeated each frame.
        /// </summary>
        /// <returns><c>true</c>, indicating repeated interaction is required.</returns>
        public bool ShouldRepeat()
        {
            return true;
        }

        /// <summary>
        /// Handles collisions with projectiles.  
        /// Reduces health, updates the life text display, interpolates color,
        /// reports death if health drops to zero, and disables the projectile.
        /// </summary>
        /// <param name="collision">The collision data from Unity's physics system.</param>
        private void OnCollisionEnter(Collision collision)
        {
            if (!collision.gameObject.name.Equals("Projectile(Clone)")) return;
            
            var enemyStats = GetComponent<Stats>();
            var currentHealth = enemyStats.GetCurStats(0);
            var maxHealth = enemyStats.GetMaxStats(0);
            var healthPercent = Mathf.Clamp01(currentHealth / maxHealth);
                
            // Target health percent TMP get the color between red and green
            var targetColor = Color.Lerp(Color.red, Color.green, healthPercent);

            // Smooth transition of health color
            _currentColor = Color.Lerp(_currentColor, targetColor, Time.deltaTime * 8f);
            lifeText.color = _currentColor;

            // Update text to show current health
            lifeText.text = $"{currentHealth:0}";
                
            gameObject.GetComponent<Stats>().DecreaseCurStat(0, GameObject.Find("Player(Clone)").GetComponent<Stats>().GetCurStats(1) * 0.5f);
            
            // Handle enemy death
            if (gameObject.GetComponent<Stats>().GetCurStats(0) <= 0f)
            {
                var reporter = GetComponent<EnemyDeathReporter>();
                if (reporter != null) reporter.ReportDeath();
                
                Destroy(gameObject);
            }
            
            // Deactivate the projectile after impact
            collision.gameObject.SetActive(false);
        }
    }
}
