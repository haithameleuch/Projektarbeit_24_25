using TMPro;
using UnityEngine;

namespace Enemy
{
    public class EnemyInteraction : MonoBehaviour, IInteractable
    {
        [SerializeField] private TextMeshPro lifeText;
        private Color _currentColor;
        private const float DamageModifier = 0.25f;

        public void Interact(GameObject interactor)
        {
            if (!interactor || !interactor.name.Equals("Player(Clone)")) return;
            var playerStats = interactor.GetComponent<Stats>();
            var enemyStats = GetComponent<Stats>();

            if (playerStats is null || enemyStats is null) return;
            var damagePerSecond = enemyStats.GetCurStats(1);
            playerStats.DecreaseCurStat(0, damagePerSecond * Time.deltaTime * DamageModifier);
        }

        public void OnExit(GameObject interactor) { }

        public bool ShouldRepeat()
        {
            return true;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!collision.gameObject.name.Equals("Projectile(Clone)")) return;
            
            var enemyStats = GetComponent<Stats>();
            var currentHealth = enemyStats.GetCurStats(0);
            var maxHealth = enemyStats.GetMaxStats(0);
            var healthPercent = Mathf.Clamp01(currentHealth / maxHealth);
                
            // Bright glow colors: green (full) to red (low)
            var targetColor = Color.Lerp(Color.red, Color.green, healthPercent);

            // Smooth transition
            _currentColor = Color.Lerp(_currentColor, targetColor, Time.deltaTime * 8f);
            lifeText.color = _currentColor;

            // Update text
            lifeText.text = $"{currentHealth:0}";
                
            gameObject.GetComponent<Stats>().DecreaseCurStat(0, GameObject.Find("Player(Clone)").GetComponent<Stats>().GetCurStats(1) * 0.5f);
            if (gameObject.GetComponent<Stats>().GetCurStats(0) <= 0f)
            {
                var reporter = GetComponent<EnemyDeathReporter>();
                if (reporter != null) reporter.ReportDeath();
                
                Destroy(gameObject);
            }
            collision.gameObject.SetActive(false);
        }
    }
}
