using UnityEngine;

namespace Enemy
{
    /// <summary>
    /// Controls the behavior of a bomb object in the scene. 
    /// The bomb explodes when it collides with the ground,
    /// triggering an explosion effect and destroying itself.
    /// </summary>
    public class BombBehavior : MonoBehaviour
    {
        /// <summary>
        /// Prefab for the explosion visual effect that will be instantiated when the bomb explodes.
        /// </summary>
        [SerializeField] private GameObject explosionEffectPrefab;

        /// <summary>
        /// Unity callback method triggered when this object collides with another.
        /// If the collision is with the ground, the bomb will explode.
        /// </summary>
        /// <param name="collision">Information about the collision, including the collider.</param>
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.collider.CompareTag("Ground"))
            {
                Explode();
            }
        }

        /// <summary>
        /// Handles the explosion logic:
        /// Instantiates the explosion effect (if assigned) and destroys the bomb object from the scene.
        /// </summary>
        private void Explode()
        {
            if (explosionEffectPrefab != null)
            {
                // Instantiate explosion effect at the bomb's position
                Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            }
        
            Destroy(gameObject);
        }
    }
}