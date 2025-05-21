using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

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

        
        /// <summary>
        /// Called once at the beginning. Sets up references and constraints.
        /// </summary>
        public override void Initialize()
        {
            target = GameObject.FindWithTag("Player");
            _rb = GetComponent<Rigidbody>();

            // Freeze the agent so it doesn't move at all
            if (_rb != null)
                _rb.constraints = RigidbodyConstraints.FreezeAll;
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
            Vector3 toTarget = (target.transform.position - shootPoint.position).normalized;
            Vector3 forward = shootPoint.forward;

            sensor.AddObservation(_healthNormalized);
            sensor.AddObservation(shootPoint.InverseTransformDirection(toTarget)); // Direction to target in local space
            sensor.AddObservation(Vector3.Dot(forward, toTarget)); // Alignment measure (1 = perfectly aimed)

            float angleDiff = Vector3.Angle(forward, toTarget) / 180f;
            sensor.AddObservation(angleDiff);
        }

        /// <summary>
        /// Called when the agent receives an action from the ML model.
        /// </summary>
        /// <param name="actions">Action buffer containing continuous and discrete actions.</param>
        public override void OnActionReceived(ActionBuffers actions)
        {
            float rotationY = actions.ContinuousActions[0];
            float rotationSpeed = 50f;
            shootPoint.Rotate(0, rotationY * rotationSpeed * Time.deltaTime, 0);
            if (actions.DiscreteActions[0] == 1)
            {
                FireProjectile();
            }
            
            // Reward for aiming closer to target
            Vector3 toTarget = (target.transform.position - shootPoint.position).normalized;
            float aimReward = Vector3.Dot(shootPoint.forward, toTarget) * 0.01f;
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
            // Get an inactive projectile from the object pool
            GameObject projectile = objectPoolManager.GetPooledObject();
            if (projectile is not null)
            {
                // Position and orient the projectile at the shooting point
                projectile.transform.position = shootPoint.position;
                projectile.transform.rotation = shootPoint.rotation;

                // Activate the projectile
                projectile.SetActive(true);
            }
        }
    }
}


