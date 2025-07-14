using System;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using System.Collections;

namespace Enemy
{
    /// <summary>
    /// Agent responsible for shooting projectiles at a player target using ML-Agents.
    /// The agent learns to rotate towards the player and fire projectiles from a fixed shoot point.
    /// </summary>
    public class AgentShooting : Agent
    {
        /// <summary>
        /// Reference to the target GameObject (e.g., the player).
        /// </summary>
        public GameObject target;
        
        public GameObject enemy;

        /// <summary>
        /// Reference to the object pool manager that manages pooled projectile objects.
        /// </summary>
        [SerializeField] private ObjectPoolManager objectPoolManager;

        /// <summary>
        /// Transform representing the position and orientation from which projectiles are fired.
        /// </summary>
        [SerializeField] private Transform shootPoint;

        private float _healthNormalized;
        private Rigidbody _rb;
        private const float FireRate = 0.5f; // Time to shoot
        private float _nextFireTime;

        
        /// <summary>
        /// Called once at the beginning. Sets up references and constraints.
        /// </summary>
        public override void Initialize()
        {
            _rb = GetComponent<Rigidbody>();

            if (_rb != null)
                _rb.constraints = RigidbodyConstraints.FreezeAll;

            StartCoroutine(InitializeAfterTargetFound());
        }

        public void Update()
        {
            if (enemy && shootPoint)
            {
                // Sync enemy rotation to shootPoint's rotation
                enemy.transform.rotation = Quaternion.Euler(0f, shootPoint.rotation.eulerAngles.y, 0f);
            }
        }



        // ReSharper disable Unity.PerformanceAnalysis
        private IEnumerator InitializeAfterTargetFound()
        {
            // Try to find the player if not yet assigned
            while (!target)
            {
                target = GameObject.FindWithTag("Player");
                if (target == null)
                {
                    yield return new WaitForSeconds(0.5f);
                }
            }

            // isInitialized = true; NOT USED!

            // Now it's safe to use target
            var directionToTarget = (target.transform.position - shootPoint.position).normalized;
            Debug.Log(directionToTarget);
            transform.forward = directionToTarget;
        }


        /// <summary>
        /// Called at the beginning of each new episode.
        /// Resets target's position and health.
        /// </summary>
        public override void OnEpisodeBegin()
        {

        }

        /// <summary>
        /// Collects observations to feed into the agent’s neural network.
        /// </summary>
        /// <param name="sensor">Sensor used to collect observations.</param>
        public override void CollectObservations(VectorSensor sensor)
        {
            Health targetHealth = target.GetComponent<Health>();
            _healthNormalized = targetHealth != null ? targetHealth._currentHealth / targetHealth._maxHealth : 0f;
            var toTarget = (target.transform.position - shootPoint.position).normalized;
            var forward = shootPoint.forward;

            sensor.AddObservation(_healthNormalized);
            sensor.AddObservation(shootPoint.InverseTransformDirection(toTarget)); // Direction to target in local space
            sensor.AddObservation(Vector3.Dot(forward, toTarget)); // Alignment measure (1 = perfectly aimed)

            var angleDiff = Vector3.Angle(forward, toTarget) / 180f;
            sensor.AddObservation(angleDiff);
        }

        /// <summary>
        /// Called when the agent receives an action from the ML model.
        /// </summary>
        /// <param name="actions">Action buffer containing continuous and discrete actions.</param>
        public override void OnActionReceived(ActionBuffers actions)
        {
            var rotationY = actions.ContinuousActions[0];
            var rotationSpeed = 50f;
            shootPoint.Rotate(0, rotationY * rotationSpeed * Time.deltaTime, 0);
            if (actions.DiscreteActions[0] == 1)
            {
                FireProjectile();
            }
            
            // Reward for aiming closer to target
            var toTarget = (target.transform.position - shootPoint.position).normalized;
            var aimReward = Vector3.Dot(shootPoint.forward, toTarget) * 0.01f;
            AddReward(aimReward);

            if (_healthNormalized == 0)
            {


                AddReward(10f);
                EndEpisode();
            }

            AddReward(-0.01f);
        }


        /// <summary>
        /// Fires a projectile by retrieving one from the object pool and activating it at the shooting point.
        /// </summary>
        private void FireProjectile()
        {
            // Test that the fire cool down is done
            if (Time.time < _nextFireTime)
                return;
    
            // Set the next fire time
            _nextFireTime = Time.time + FireRate;
    
            // Get an inactive projectile from the object pool
            var projectile = objectPoolManager.GetPooledObject();
            if (projectile == null) return;
            // Position and orient the projectile at the shooting point
            projectile.transform.position = shootPoint.position;
            projectile.transform.rotation = shootPoint.rotation;

            // Activate the projectile
            projectile.SetActive(true);
        }
    }
}