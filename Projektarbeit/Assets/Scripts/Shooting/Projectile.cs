using UnityEngine;

namespace Shooting
{
    /// <summary>
    /// Handles the behavior of a projectile, including movement, lifetime management, and collision detection.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        /// <summary>
        /// Speed of the projectile.
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
        /// Deactivate on collision with non-projectile and non-enemy objects.
        /// </summary>
        /// <param name="collision">Collision data.</param>
        private void OnCollisionEnter(Collision collision)
        {
            if (!collision.gameObject.CompareTag("Projectile") && !collision.gameObject.CompareTag("Enemy"))
            {
                gameObject.SetActive(false);
            }
        }
    }
}