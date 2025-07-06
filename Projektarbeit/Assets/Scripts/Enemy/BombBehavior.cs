using UnityEngine;

namespace Enemy
{
    public class BombBehavior : MonoBehaviour
    {
        // Prefab for the explosion visual effect
        [SerializeField] private GameObject explosionEffectPrefab;

        // Called when this object collides with another
        private void OnCollisionEnter(Collision collision)
        {
            // Check if the bomb hits the ground
            if (collision.collider.CompareTag("Ground"))
            {
                Explode(); // Trigger explosion
            }
        }

        // Handles the explosion logic
        private void Explode()
        {
            // Instantiate explosion effect at the bomb's position
            if (explosionEffectPrefab != null)
            {
                Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            }

            // Optional: Add damage or effects to nearby objects here

            Destroy(gameObject); // Remove the bomb from the scene
        }
    }
}