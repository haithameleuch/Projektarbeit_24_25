using System.Collections.Generic;
using UnityEngine;

namespace Manager
{
    /// <summary>
    /// Manages object pool for reusable <see cref="GameObject"/> instances (e.g., projectiles).
    /// Preloads a set of inactive objects and reuses them to avoid frequent instantiation.
    /// Expands the pool on demand if needed.
    /// </summary>
    public class ObjectPoolManager : MonoBehaviour
    {
        /// <summary>
        /// Prefab for the pooled objects (e.g., projectiles).
        /// </summary>
        [SerializeField] private GameObject projectilePrefab;

        /// <summary>
        /// Initial size of the object pool.
        /// </summary>
        [SerializeField] private int poolSize = 10;

        /// <summary>
        /// Internal list to store the pooled objects.
        /// </summary>
        private List<GameObject> _pool;
    
        /// <summary>
        /// Indicates whether the pool can provide objects
        /// (pool is created and either has entries or a valid prefab to spawn more).
        /// </summary>
        public bool IsReady => _pool != null && (_pool.Count > 0 || projectilePrefab != null);

        /// <summary>
        /// Initializes the object pool by instantiating inactive objects at the start.
        /// </summary>
        private void Awake()
        {
            if (_pool == null) _pool = new List<GameObject>();

            if (projectilePrefab == null) return;

            for (var i = 0; i < poolSize; i++)
            {
                var obj = Instantiate(projectilePrefab, transform);
                obj.SetActive(false);
                _pool.Add(obj);
            }
        }

        /// <summary>
        /// Retrieves an inactive object from the pool. Returns null if no object is available.
        /// </summary>
        /// <returns>
        /// A <see cref="GameObject"/> from the pool if an inactive one is found; otherwise, null.
        /// </returns>
        public GameObject GetPooledObject()
        {
            if (_pool == null) _pool = new List<GameObject>();
        
            for (var i = 0; i < _pool.Count; i++)
            {
                var obj = _pool[i];
                if (obj is not null && !obj.activeInHierarchy)
                    return obj;
            }
        
            if (projectilePrefab is not null)
            {
                var extra = Instantiate(projectilePrefab, transform);
                extra.SetActive(false);
                _pool.Add(extra);
                return extra;
            }

            return null; // Return null if all objects in the pool are active
        }
    }
}