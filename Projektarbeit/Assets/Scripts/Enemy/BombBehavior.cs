using UnityEngine;

public class BombBehavior : MonoBehaviour
{
    [SerializeField] private GameObject explosionEffectPrefab;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Ground"))
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }

        // Optional: Add damage or effects to nearby objects here

        Destroy(gameObject); // Remove the bomb
    }
}