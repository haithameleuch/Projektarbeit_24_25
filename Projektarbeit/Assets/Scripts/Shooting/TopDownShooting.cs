// LEGACY CODE !!!

using UnityEngine;

/// <summary>
/// Handles directional shooting for a top-down player, spawning projectiles from an object pool in the direction of input.
/// </summary>
public class TopDownShooting : MonoBehaviour
{
    /// <summary>
    /// Reference to the object pool manager that manages the pool of projectiles.
    /// </summary>
    [SerializeField] private ObjectPoolManager objectPool;
    
    /// <summary>
    /// The point from which projectiles are fired.
    /// </summary>
    [SerializeField] private Transform shootPoint;
    
    /// <summary>
    /// The distance from the shoot point where projectiles are spawned.
    /// </summary>
    [SerializeField] private float spawnDistance = 1.0f;

    /// <summary>
    /// Monitors player input for firing projectiles in different directions.
    /// </summary>
    private void Update()
    {
        // Check for directional input and fire projectiles accordingly
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            FireProjectile(Vector3.forward);
        } 
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            FireProjectile(Vector3.back);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            FireProjectile(Vector3.left);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            FireProjectile(Vector3.right);
        }
    }

    /// <summary>
    /// Fires a projectile in a specified direction by retrieving one from the object pool
    /// and positioning it at the calculated spawn point.
    /// </summary>
    /// <param name="direction">The direction in which the projectile will be fired.</param>
    void FireProjectile(Vector3 direction)
    {
        // Calculate the spawn position based on the direction and spawn distance
        Vector3 spawnPosition = shootPoint.position + direction.normalized * spawnDistance;

        // Get an inactive projectile from the object pool
        GameObject projectile = objectPool.GetPooledObject();
        if (projectile is not null)
        {
            // Position and orient the projectile towards the firing direction
            projectile.transform.position = spawnPosition;
            projectile.transform.rotation = Quaternion.LookRotation(direction);
            
            // Activate the projectile
            projectile.SetActive(true);
        }
    }
    
    
}
