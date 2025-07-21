using UnityEngine;
using TMPro;

public class EnemyInteraction : MonoBehaviour, IInteractable
{
    private bool canMove = true;
    [SerializeField] private TextMeshPro LifeText;
    private Color currentColor;

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
        if (interactor == null) return;
        canMove = true;
        UIManager.Instance.HidePanel();
    }

    public bool ShouldRepeat()
    {
        return false;
    }

    public bool CanMove() { return canMove; }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name.Equals("Projectile(Clone)"))
        {
            Stats stats = GetComponent<Stats>();
            float currentHealth = stats.GetCurStats(0);
            float maxHealth = stats.GetMaxStats(0);
            float healthPercent = Mathf.Clamp01(currentHealth / maxHealth);
            //
            /*
            Health enemyHealth = GetComponent<Health>();
            float currentHealth = enemyHealth._currentHealth;
            float maxHealth = enemyHealth._maxHealth;
            float healthPercent = Mathf.Clamp01(currentHealth / maxHealth);
            */
            // Bright glow colors: green (full) to red (low)
            Color targetColor = Color.Lerp(Color.red, Color.green, healthPercent);

            // Smooth transition
            currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * 8f);
            LifeText.color = currentColor;

            // Update text
            LifeText.text = $"{currentHealth:0}";
            
            //
            
			/**
            // Optional pulse at low HP
            if (healthPercent < 0.2f)
            {
                float pulse = Mathf.PingPong(Time.time * 4f, 0.2f) + 0.9f;
                LifeText.transform.localScale = Vector3.one * pulse;
            }
            else
            {
                LifeText.transform.localScale = Vector3.one;
            }
			**/
            
            /*
            //Legacy Code To Be Removed
            enemyHealth._currentHealth = HealthManager.damageAbsolute(0.5f, HealthManager.DamageType.Normal, enemyHealth._currentHealth);

            if (enemyHealth._currentHealth <= 0f)
            {
                Destroy(gameObject);
            }
            */
            gameObject.GetComponent<Stats>().DecreaseCurStat(0, GameObject.Find("Player(Clone)").GetComponent<Stats>().GetCurStats(1) * 0.5f);
            if (gameObject.GetComponent<Stats>().GetCurStats(0) <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }
}
