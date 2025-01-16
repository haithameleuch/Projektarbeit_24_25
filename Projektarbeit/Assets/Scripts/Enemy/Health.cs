using UnityEngine;
using static HealthManager;

public class Health : MonoBehaviour
{
    public float _maxHealth;
    public float _currentHealth;
    private bool getDamage;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            _currentHealth = HealthManager.damageAbsolute(1.0f/3.0f, DamageType.Normal, _currentHealth);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            _currentHealth = HealthManager.healPercentage(13, _currentHealth, _maxHealth);
        }

        if (GetComponent<EnemyInteraction>() != null && GetComponent<Health>()._currentHealth <= 0.0f)
        {
            Destroy(gameObject);
        }
    }
}
