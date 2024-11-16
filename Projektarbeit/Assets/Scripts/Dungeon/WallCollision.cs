using System;
using UnityEngine;

/// <summary>
/// Detects nearby wall colliders at the start of the game and destroys this object if a wall is detected.
/// Ensures that objects overlapping with walls are removed to prevent unintended interactions.
/// </summary>
public class WallCollision : MonoBehaviour
{
    /// <summary>
    /// Runs once at the start to check for wall overlap and manage the collider's state accordingly.
    /// </summary>
    private void Start()
    {
        // Check for colliders within a very small radius around this object's position
        Collider[] colliders = Physics.OverlapSphere(transform.position, 0.01f);

        // Iterate through all detected colliders
        foreach (Collider collider in colliders)
        {
            // If the collider is tagged as "Wall", destroy this object and stop further checks
            if (collider.CompareTag("Wall"))
            {
                Destroy(gameObject);
                return;
            }
        }

        // If no wall collider is found, enable this object's collider
        GetComponent<Collider>().enabled = true;
    }
}