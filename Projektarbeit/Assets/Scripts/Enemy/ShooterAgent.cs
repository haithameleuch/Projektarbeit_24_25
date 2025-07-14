using System;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using System.Collections;

namespace Enemy
{
    /*
     * AgentShooting Structure and Behavior:
     *
     * This class defines a ranged enemy agent (AgentShooting) trained using Unity ML-Agents.
     * The agent learns to rotate toward the player and fire projectiles using a local shoot point.
     * It uses health feedback and target alignment to learn efficient aiming and timing.
     *
     * Agent Overview:
     * - Type: Ranged-Shooter Enemy
     * - Goal: Eliminate the player by accurately aiming and firing projectiles.
     * - Behavior:
     *     • Uses continuous rotation around the Y-axis to align with the target.
     *     • Fires a projectile using a discrete action when properly aimed.
     *     • Observes:
     *         - Player's normalized health
     *         - Local-space direction to target
     *         - Alignment score between aim direction and target
     *         - Angular difference for precision
     *     • Rewards:
     *         - Positive reward for aiming accuracy (using dot product)
     *         - Large reward for reducing target’s health to 0
     *         - Small time penalty per step to encourage efficient shooting
     *
     * Key Features:
     * - Uses an `ObjectPoolManager` to efficiently reuse projectiles
     * - Keeps enemy model synced with the gun's rotation
     * - Freezes Rigidbody to act as a stationary turret-type enemy
     * - Includes optional debug logs and flexible reward shaping
     *
     * Suitable for:
     * - Training precision shooter agents
     * - Defensive turret-style AI
     * - Scenarios where reactive, line-of-sight attacks are key
     */

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