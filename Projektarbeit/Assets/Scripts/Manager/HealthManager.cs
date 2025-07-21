using UnityEngine;
/*
public class HealthManager : MonoBehaviour
{
    public enum DamageType { Normal, Magical, Absolute};
    [SerializeField] private GameObject player;

    public static float healAbsolute(float absAmount, float currentHealth, float maxHealth)
    {
        return Mathf.Min(currentHealth + absAmount, maxHealth);
    }

    public static float healPercentage(float percentage, float currentHealth, float maxHealth)
    {
        return Mathf.Min(currentHealth + (maxHealth * (percentage / 100.0f)), maxHealth);
    }

    public static float damageAbsolute(float absAmount, DamageType type, float currentHealth)
    {
        return Mathf.Max(currentHealth - absAmount, 0.0f);
    }

    public static float damagePercentage(float percentage, DamageType type, float currentHealth, float maxHealth)
    {
        return Mathf.Max(currentHealth + (maxHealth * (percentage / 100.0f)), 0.0f);
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 100, 20), player.GetComponent<Health>()._currentHealth.ToString() + "/" + player.GetComponent<Health>()._maxHealth.ToString());
    }

}
*/