using Manager;
using UnityEngine;

namespace Shooting
{
    /// <summary>
    /// Handles player shooting by getting projectiles from a pool and spawning them at the shoot point.
    /// </summary>
    public class PlayerShooting : MonoBehaviour
    {
        /// <summary>
        /// Reference to the object pool manager that provides projectiles.
        /// </summary>
        [SerializeField] private ObjectPoolManager objectPoolManager;

        /// <summary>
        /// Point where projectiles are spawned.
        /// </summary>
        [SerializeField] private Transform shootPoint;

        /// <summary>
        /// Checks input each frame and fires when the fire button ("Spacebar") is pressed.
        /// </summary>
        private void Update()
        {
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
            var projectile = objectPoolManager.GetPooledObject();
            if (projectile is null) return;
            
            projectile.transform.position = shootPoint.position;
            projectile.transform.rotation = shootPoint.rotation;
            
            projectile.SetActive(true);
        }
    
        /// <summary>
        /// Sets the object pool manager at runtime.
        /// </summary>
        /// <param name="poolManager">Pool manager to use.</param>
        public void Init(ObjectPoolManager poolManager)
        {
            objectPoolManager = poolManager;
        }
    }
}