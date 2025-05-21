using UnityEngine;

/// <summary>
/// Handles player shooting by retrieving projectiles from an object pool and activating them at the shooting point.
/// </summary>
public class PlayerShooting : MonoBehaviour
{
    /// <summary>
    /// Reference to the object pool manager that manages the pool of projectiles.
    /// </summary>
    [SerializeField] private ObjectPoolManager objectPoolManager;

    /// <summary>
    /// The point from which projectiles are fired.
    /// </summary>
    [SerializeField] private Transform shootPoint;

    /// <summary>
    /// Monitors player input for firing projectiles.
    /// </summary>
    private void Update()
    {
        // Check if the "Jump" button (default: Spacebar) is pressed
        if (Input.GetButtonDown("Jump"))
        {
            FireProjectile();
        }
    }

    /// <summary>
    /// Fires a projectile by retrieving one from the object pool and activating it at the shooting point.
    /// </summary>
    private void FireProjectile()
    {
        // Get an inactive projectile from the object pool
        GameObject projectile = objectPoolManager.GetPooledObject();
        if (projectile is not null)
        {
            // Position and orient the projectile at the shooting point
            projectile.transform.position = shootPoint.position;
            projectile.transform.rotation = shootPoint.rotation;

            // Activate the projectile
            projectile.SetActive(true);
        }
    }
    
    public void Init(ObjectPoolManager poolManager)
    {
        this.objectPoolManager = poolManager;
    }

}