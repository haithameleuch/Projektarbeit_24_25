using System;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using System.Collections;
using Manager;

namespace Enemy
{
    /// <summary>
       /// Defines a ranged enemy agent (AgentShooting) trained with Unity ML-Agents.
       ///
       /// <para>Core Behavior:</para>
       /// <list type="bullet">
       /// <item>Acts like a stationary turret that rotates and fires projectiles.</item>
       /// <item>Receives continuous actions for Y-axis rotation and a discrete action for firing.</item>
       /// <item>Uses projectile pooling via <see cref="ObjectPoolManager"/> for performance.</item>
       /// </list>
       ///
       /// <para>Observations:</para>
       /// <list type="bullet">
       /// <item>Target’s normalized health (float)</item>
       /// <item>Direction to target in local space (Vector3)</item>
       /// <item>Dot product of forward vs. target direction (float)</item>
       /// <item>Angular difference to target, normalized [0,1] (float)</item>
       /// </list>
       /// <para>Total = 6 observations per step.</para>
       ///
       /// <para>Rewards:</para>
       /// <list type="bullet">
       /// <item>Positive reward for aiming closer to the player (dot product alignment).</item>
       /// <item>Large positive reward for killing the player (health = 0).</item>
       /// <item>Small negative time penalty each step to promote efficiency.</item>
       /// </list>
       ///
       /// <para>Key Features:</para>
       /// <list type="bullet">
       /// <item>Freezes Rigidbody to prevent physics-based movement.</item>
       /// <item>Keeps enemy model visually aligned with the gun barrel (shootPoint).</item>
       /// <item>Supports coroutine-based initialization to wait for player spawn.</item>
       /// <item>Uses cooldown-based firing logic to prevent spamming.</item>
       /// </list>
       ///
       /// <para>Best suited for:</para>
       /// <list type="bullet">
       /// <item>Turret-style enemies.</item>
       /// <item>Ranged AI requiring line-of-sight targeting.</item>
       /// <item>Training scenarios where accuracy and timing are critical.</item>
       /// </list>
       /// </summary>

    public class AgentShooting : Agent
    {
        /// <summary>
        /// Reference to the target GameObject (e.g., the player).
        /// </summary>
        public GameObject target;
        
        /// <summary>
        /// Reference to the enemy model that visually rotates with the shoot point.
        /// </summary>
        public GameObject enemy;

        /// <summary>
        /// Reference to the object pool manager that manages pooled projectile objects.
        /// </summary>
        [SerializeField] private ObjectPoolManager objectPoolManager;

        /// <summary>
        /// Transform representing the position and orientation from which projectiles are fired.
        /// </summary>
        [SerializeField] private Transform shootPoint;

        /// <summary>
        /// Normalized health of the target (0 = dead, 1 = full health).
        /// </summary>
        private float _healthNormalized;
        
        /// <summary>
        /// Reference to the Rigidbody component of this agent.
        /// </summary>
        private Rigidbody _rb;
        
        /// <summary>
        /// Time interval between consecutive shots.
        /// </summary>
        private const float FireRate = 0.5f; 
        
        /// <summary>
        /// Timestamp of when the agent can fire the next projectile.
        /// </summary>
        private float _nextFireTime;
        
        /// <summary>
        /// Called once at the beginning. Sets up references and constraints.
        /// </summary>
        public override void Initialize()
        {
            _rb = GetComponent<Rigidbody>();

            if (_rb != null)
                _rb.constraints = RigidbodyConstraints.FreezeAll;

            if (!objectPoolManager) objectPoolManager = FindFirstObjectByType<ObjectPoolManager>();
            
            StartCoroutine(InitializeAfterTargetFound());
        }

        /// <summary>
        /// Called automatically by Unity when this agent or GameObject is disabled.
        /// Stops all running coroutines to prevent unwanted behavior or errors.
        /// </summary>
        protected override void OnDisable()
        {
            StopAllCoroutines();
        }

        /// <summary>
        /// Update enemy model rotation to match shootPoint rotation for visuals.
        /// </summary>
        public void Update()
        {
            if (enemy && shootPoint)
            {
                // Sync enemy rotation to shootPoint's rotation
                enemy.transform.rotation = Quaternion.Euler(0f, shootPoint.rotation.eulerAngles.y, 0f);
            }
        }
        
        /// <summary>
        /// Coroutine to safely initialize after target is found.
        /// Ensures that rotation is aligned with the target before starting.
        /// </summary>
        private IEnumerator InitializeAfterTargetFound()
        {
            // Try to find the player if not yet assigned
            while (!target)
            {
                target = GameObject.FindWithTag("Player");
                if (!target)
                {
                    yield return new WaitForSeconds(0.5f);
                }
            }

            // isInitialized = true; NOT USED!
            if (!shootPoint || !target) yield break;

            // Now it's safe to use target
            var directionToTarget = (target.transform.position - shootPoint.position).normalized;
            transform.forward = directionToTarget;
        }

        /// <summary>
        /// Collects agent observations for ML input.
        /// Number of Observations = 3 + 1 + 1 + 1 = 6
        /// </summary>
        /// <param name="sensor">Vector sensor to record observations.</param>
        public override void CollectObservations(VectorSensor sensor)
        {
            if (!target || !shootPoint)
            {
                sensor.AddObservation(0f);               // Normalized health
                sensor.AddObservation(Vector3.zero);     // Local direction to target
                sensor.AddObservation(0f);               // Alignment score
                sensor.AddObservation(1f);               // Angle difference
                return;
            }
            
            // Calculate normalized health of the target
            var targetStats = target.GetComponent<Stats>();
            if (targetStats != null)
            {
                var targetMaxHealth = targetStats.GetMaxStats(0);
                var targetCurHealth = targetStats.GetCurStats(0);
                _healthNormalized = targetCurHealth / targetMaxHealth;
            }
            else
            {
                _healthNormalized = 0f;
            }
            
            // Direction to target in world space
            var toTarget = (target.transform.position - shootPoint.position).normalized;
            var forward = shootPoint.forward;

            // Observations:
            sensor.AddObservation(_healthNormalized);                              // Target health [0,1] - 1 Observation
            sensor.AddObservation(shootPoint.InverseTransformDirection(toTarget)); // Direction to target in local space - 3 Observations
            sensor.AddObservation(Vector3.Dot(forward, toTarget));         // Alignment measure (dot product) - 1 Observation
            sensor.AddObservation(Vector3.Angle(forward, toTarget) / 180f);        // Angular difference normalized - 1 Observation
        }

        /// <summary>
        /// Called when the agent receives an action from the ML model.
        /// </summary>
        /// <param name="actions">Action buffer containing continuous and discrete actions.</param>
        public override void OnActionReceived(ActionBuffers actions)
        {
            if (!shootPoint || !target) { AddReward(-0.01f); return; }
            
            var rotationY = actions.ContinuousActions[0];
            const float rotationSpeed = 50f;
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
            
            if (!shootPoint) return;
            if (!objectPoolManager || !objectPoolManager.IsReady) return;
            
            // Get an inactive projectile from the object pool
            var projectile = objectPoolManager.GetPooledObject();
            if (projectile == null) return;
            
            // Position and orient the projectile at the shooting point
            projectile.transform.position = shootPoint.position;
            projectile.transform.rotation = shootPoint.rotation;
            
            projectile.SetActive(true);
            
            _nextFireTime = Time.time + FireRate;
        }
    }
}