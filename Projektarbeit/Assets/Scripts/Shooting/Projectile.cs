using UnityEngine;

/// <summary>
/// Handles the behavior of a projectile, including movement, lifetime management, and collision detection.
/// </summary>
public class Projectile : MonoBehaviour
{
    /// <summary>
    /// Speed at which the projectile travels.
    /// </summary>
    [SerializeField] private float speed = 10f;

    /// <summary>
    /// Time in seconds before the projectile is automatically deactivated.
    /// </summary>
    [SerializeField] private float lifetime = 3f;

    /// <summary>
    /// Timer tracking the remaining active lifetime of the projectile.
    /// </summary>
    [SerializeField] private float lifeTimer;

    /// <summary>
    /// Resets the lifetime timer when the projectile is activated.
    /// </summary>
    private void OnEnable()
    {
        lifeTimer = lifetime;
    }

    /// <summary>
    /// Updates the projectile's position and checks its lifetime each frame.
    /// </summary>
    private void Update()
    {
        // Move the projectile forward based on its speed
        transform.Translate(Vector3.forward * (speed * Time.deltaTime));

        // Decrease the lifetime timer
        lifeTimer -= Time.deltaTime;

        // Deactivate the projectile if its lifetime expires
        if (lifeTimer <= 0)
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Deactivates the projectile upon collision with another object.
    /// </summary>
    /// <param name="collision">Information about the collision.</param>
    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Projectile") && !collision.gameObject.CompareTag("Enemy"))
        {
            // Deactivate the projectile on collision
            gameObject.SetActive(false);
        }
    }
}