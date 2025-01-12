using UnityEngine;

public class EnemyInteraction : MonoBehaviour, IInteractable
{
    private bool canMove = true;
    public void Interact(GameObject interactor)
    {
        Health health = interactor.GetComponent<Health>();
        canMove = false;

        if(health != null)
        {
            float currentHealth = health._currentHealth;
            if(interactor.name.Equals("Player"))
            {
                health._currentHealth = HealthManager.damageAbsolute(2.0f, HealthManager.DamageType.Normal, currentHealth);
                UIManager.Instance.ShowPanel(interactor.name + ": " + health._currentHealth + "/" + health._maxHealth);
            }
        }
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
}
