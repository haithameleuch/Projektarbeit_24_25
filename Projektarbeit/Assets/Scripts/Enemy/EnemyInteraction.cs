using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Enemy
{
    public class EnemyInteraction : MonoBehaviour, IInteractable
    {
        private bool _canMove = true;
        [FormerlySerializedAs("LifeText")] [SerializeField] private TextMeshPro lifeText;
        private Color _currentColor;

        public void Interact(GameObject interactor)
        {
            if (interactor.name.Equals("Player(Clone)"))
            {
                interactor.GetComponent<Stats>().DecreaseCurStat(0, gameObject.GetComponent<Stats>().GetCurStats(1));
                //canMove = false;
            }
            /*
        //Legacy Code To Be Removed
        Health health = interactor.GetComponent<Health>();
        canMove = false;

        if(health != null)
        {
            float currentHealth = health._currentHealth;
            if(interactor.name.Equals("Player"))
            {
                health._currentHealth = HealthManager.damageAbsolute(2.0f, HealthManager.DamageType.Normal, currentHealth);
            }
        }*/
        }

        public void OnExit(GameObject interactor)
        {
            if (!interactor) return;
            _canMove = true;
            UIManager.Instance.HidePanel();
        }

        public bool ShouldRepeat()
        {
            return false;
        }

        public bool CanMove() { return _canMove; }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.name.Equals("Projectile(Clone)"))
            {
                Stats stats = GetComponent<Stats>();
                float currentHealth = stats.GetCurStats(0);
                float maxHealth = stats.GetMaxStats(0);
                float healthPercent = Mathf.Clamp01(currentHealth / maxHealth);
                // Bright glow colors: green (full) to red (low)
                Color targetColor = Color.Lerp(Color.red, Color.green, healthPercent);

                // Smooth transition
                _currentColor = Color.Lerp(_currentColor, targetColor, Time.deltaTime * 8f);
                lifeText.color = _currentColor;

                // Update text
                lifeText.text = $"{currentHealth:0}";
                
                gameObject.GetComponent<Stats>().DecreaseCurStat(0, GameObject.Find("Player(Clone)").GetComponent<Stats>().GetCurStats(1) * 0.5f);
                if (gameObject.GetComponent<Stats>().GetCurStats(0) <= 0f)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
