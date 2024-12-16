using UnityEngine;

public class TopDownShooting : MonoBehaviour
{
    [SerializeField] private ObjectPoolManager objectPool;
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float spawnDistance = 1.0f;

    private void Update()
    {
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

    void FireProjectile(Vector3 direction)
    {
        Vector3 spawnPosition = shootPoint.position + direction.normalized * spawnDistance;

        GameObject projectile = objectPool.GetPooledObject();
        if (projectile is not null)
        {
            projectile.transform.position = spawnPosition;
            projectile.transform.rotation = Quaternion.LookRotation(direction);
            
            projectile.SetActive(true);
        }
    }
    
    
}
