using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using System.Collections;

namespace Enemy
{
    /// <summary>
    /// Defines a navigation-focused enemy agent (HunterAgentObstacle) trained with Unity ML-Agents.
    ///
    /// <para>Core Behavior:</para>
    /// <list type="bullet">
    /// <item>Chases the player while avoiding obstacles using continuous forward and rotation actions.</item>
    /// <item>Receives rewards for reducing distance, aligning movement direction, and facing the target.</item>
    /// <item>Penalized for getting stuck or colliding with obstacles, walls, or doors.</item>
    /// </list>
    ///
    /// <para>Observations:</para>
    /// <list type="bullet">
    /// <item>Agent's local position (Vector3)</item>
    /// <item>Target's local position (Vector3)</item>
    /// <item>Normalized vector from agent to target (Vector3)</item>
    /// <item>Agent's normalized velocity (Vector3)</item>
    /// <item>Agent's forward vector (Vector3)</item>
    /// </list>
    /// <para>Total = 15 observations per step.</para>
    ///
    /// <para>Rewards:</para>
    /// <list type="bullet">
    /// <item>Positive reward for reducing distance to the player (progress reward).</item>
    /// <item>Positive reward for aligning movement direction with the target (velocity alignment).</item>
    /// <item>Positive reward for facing the target directly (facing reward).</item>
    /// <item>Negative reward for being stuck or colliding with obstacles.</item>
    /// </list>
    ///
    /// <para>Key Features:</para>
    /// <list type="bullet">
    /// <item>Physics-based movement with Rigidbody and smooth rotation.</item>
    /// <item>Stuck detection using position deltas and a timer threshold.</item>
    /// <item>Coroutine-based player detection for dynamic spawns.</item>
    /// <item>Gizmos for debugging forward direction and target tracking.</item>
    /// </list>
    ///
    /// <para>Best suited for:</para>
    /// <list type="bullet">
    /// <item>Obstacle-aware navigation tasks.</item>
    /// <item>Precision pursuit and pathfinding AI training.</item>
    /// <item>Navigation reward shaping scenarios.</item>
    /// </list>
    /// </summary>
    public class HunterAgentObstacle : Agent
    {
        /// <summary>
        /// Movement speed of the agent, can be modified from inspector or runtime via Stats.
        /// </summary>
        public float movementSpeed = 8f;
        
        /// <summary>
        /// Reference to the player target GameObject.
        /// </summary>
        public GameObject target;

        /// <summary>
        /// Cached Rigidbody for physics-based movement.
        /// </summary>
        private Rigidbody _rb;
        
        /// <summary>
        /// Distance to the target in the previous step, used to calculate progress reward.
        /// </summary>
        private float _prevDistance;
        
        /// <summary>
        /// Last recorded position to detect whether the agent is stuck.
        /// </summary>
        private Vector3 _lastPosition;
        
        /// <summary>
        /// Timer for how long the agent has been stuck in the same position.
        /// </summary>
        private float _stuckTimer;
        
        /// <summary>
        /// Threshold distance to consider the agent as stuck.
        /// </summary>
        private const float StuckThreshold = 0.01f;
        
        /// <summary>
        /// Maximum time allowed for being stuck before penalizing.
        /// </summary>
        private const float StuckTimeLimit = 0.3f;
        
        /// <summary>
        /// Called once when the agent is first initialized.
        /// Sets up Rigidbody constraints and starts the coroutine to find the player.
        /// </summary>
        public override void Initialize()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | 
                            RigidbodyConstraints.FreezeRotationZ | 
                            RigidbodyConstraints.FreezePositionY;
            StartCoroutine(FindPlayerCoroutine());
        }

        /// <summary>
        /// Called automatically by Unity when this agent or GameObject is disabled.
        /// Stops all running coroutines to prevent unwanted behavior or errors.
        /// </summary>
        protected override void OnDisable()
        {
            StopAllCoroutines();
        }
        
        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// Coroutine that periodically searches for the Player object by tag
        /// until it finds one and assigns it to the target variable.
        /// </summary>
        private IEnumerator FindPlayerCoroutine()
        {
            while (!target)
            {
                target = GameObject.FindWithTag("Player");
                if (!target)
                {
                    yield return new WaitForSeconds(0.5f); 
                }
            }
            movementSpeed = gameObject.GetComponent<Stats>().GetCurStats(2);
        }

        /// <summary>
        /// Called at the beginning of each training episode.
        /// Resets tracking variables such as position, timers, and reward counters.
        /// </summary>
        public override void OnEpisodeBegin()
        {
            _lastPosition = transform.localPosition;
            _stuckTimer = 0f;
            if (!target) return;
            _prevDistance = Vector3.Distance(transform.localPosition, target.transform.localPosition);
        }
        
        /// <summary>
        /// Collects observations about the agent's environment.
        /// Provides the agent with its own position, target's position,
        /// direction to the target, current velocity, and facing a direction.
        /// Number of Observations = 3 + 3 + 3 + 3 + 3 = 15
        /// </summary>
        public override void CollectObservations(VectorSensor sensor)
        {
            if (!target)
            {
                sensor.AddObservation(transform.localPosition);
                sensor.AddObservation(transform.localPosition);
                sensor.AddObservation(Vector3.zero);
                sensor.AddObservation(_rb.linearVelocity.normalized);
                sensor.AddObservation(transform.forward);
                return;
            }
            
            sensor.AddObservation(transform.localPosition);               // 3 floats (x, y, z)
            sensor.AddObservation(target.transform.localPosition);        // 3 floats (x, y, z)
            sensor.AddObservation((target.transform.position - transform.position).normalized); // 3 floats (normalized vector)
            sensor.AddObservation(_rb.linearVelocity.normalized);         // 3 floats (velocity direction)
            sensor.AddObservation(transform.forward);                     // 3 floats (forward direction)
        }

        /// <summary>
        /// Processes actions from the neural network to move and rotate the agent.
        /// Calculates multiple reward components and detects stuck conditions.
        /// </summary>
        /// <param name="actions">Continuous action buffer from ML policy.</param>
        public override void OnActionReceived(ActionBuffers actions)
        {
            if (!target) return;
            
            var moveInput = Mathf.Clamp(actions.ContinuousActions[0], 0f, 1f); // Forward/backward input
            var turnInput = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f); // Left/right rotation input

            // Compute movement vector based on input and speed, then move the agent
            var movement = transform.forward * moveInput * movementSpeed * Time.deltaTime;
            _rb.MovePosition(_rb.position + movement);
            
            // Rotate the agent smoothly around Y-axis
            transform.Rotate(Vector3.up, turnInput * 300f * Time.deltaTime);

            // Compute distance to the target and progress since last step
            var currentDistance = Vector3.Distance(transform.localPosition, target.transform.localPosition);
            var distanceDelta = _prevDistance - currentDistance;

            // Scale progress reward based on current distance
            var distanceScale = Mathf.Max(1f, currentDistance / 2f);
            var progressReward = distanceDelta * distanceScale * 2f;
            
            // Compute alignment of agent's velocity with direction to target
            var directionToTarget = (target.transform.position - transform.position).normalized;
            var movementAlignment = Vector3.Dot(_rb.linearVelocity.normalized, directionToTarget);
            var alignmentReward = movementAlignment * 0.2f;

            // Compute reward for facing the target
            var facingDot = Vector3.Dot(transform.forward, directionToTarget);
            var facingReward = Mathf.Pow(facingDot, 2) * 0.4f;

            // Add combined reward for this step
            var totalReward = progressReward + alignmentReward + facingReward;
            AddReward(totalReward);

            _prevDistance = currentDistance;

            // Detect if the agent is stuck and penalize if stuck for too long
            if (Vector3.Distance(transform.localPosition, _lastPosition) < StuckThreshold)
            {
                _stuckTimer += Time.deltaTime;
                if (_stuckTimer > StuckTimeLimit)
                {
                    AddReward(-0.5f);
                    return;
                }
            }
            else
            {
                _stuckTimer = 0f;
            }
            
            _lastPosition = transform.localPosition;
        }

        /// <summary>
        /// Handles collision with player or environmental obstacles.
        /// Rewards the agent for reaching the player and penalizes collisions.
        /// </summary>
        /// <param name="other">Collider the agent interacts with.</param>
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                AddReward(20f);
            }
            else if (other.CompareTag("Wall") || other.CompareTag("Obstacle") || other.CompareTag("Door"))
            {
                AddReward(-1f);
            }
        }
        
        /// <summary>
        /// Draws gizmos in the editor to visualize the agent's forward direction
        /// and a line pointing to the target.
        /// </summary>
        private void OnDrawGizmos()
        {
            if (target == null) return;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, target.transform.position);

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.forward * 2f);
        }
    }
}
