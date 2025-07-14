using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using System.Collections;

namespace Enemy
{
    /*
     * HunterAgentObstacle Structure and Behavior:
     *
     * This class defines a navigation-focused enemy agent (HunterAgentObstacle) trained using Unity ML-Agents.
     * The agent specializes in efficiently reaching a player while navigating through an environment with static obstacles.
     *
     * Agent Overview:
     * - Type: Pursuer-Obstacle Navigator Enemy
     * - Goal: Reach the player by learning obstacle-aware pathfinding and target alignment.
     * - Behavior:
     *     • Moves toward the player using continuous actions (forward + rotation).
     *     • Observes its position, velocity, forward direction, and vector to the player.
     *     • Uses multiple reward components:
     *         - Distance reduction toward the player (progress reward)
     *         - Alignment of movement direction with target (velocity alignment reward)
     *         - Facing the target directly (facing reward)
     *     • Penalized for:
     *         - Getting stuck
     *         - Colliding with obstacles or walls
     *
     * Key Features:
     * - Rigidbody-based physics movement
     * - Smooth rotation and acceleration control
     * - Coroutine-based player detection
     * - Stuck detection using position delta and time threshold
     * - Gizmo support for debugging forward direction and target tracking
     *
     * Training Utility:
     * - This agent is well-suited for tasks involving obstacle avoidance, precision pursuit, and navigation-based reward shaping.
     */

    public class HunterAgentObstacle : Agent
    {
        // Movement speed of the bomber agent
        public float movementSpeed = 8f;
        
        // Reference to the player target
        public GameObject target;

        // Internal state variables for training
        private Rigidbody _rb;
        private float _prevDistance;
        private Vector3 _lastPosition;
        private float _stuckTimer;
        private const float StuckThreshold = 0.01f;
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
        }

        /// <summary>
        /// Called at the beginning of each training episode.
        /// Resets tracking variables such as position, timers, and reward counters.
        /// </summary>
        public override void OnEpisodeBegin()
        {
            _lastPosition = transform.localPosition;
            _stuckTimer = 0f;
            _prevDistance = Vector3.Distance(transform.localPosition, target.transform.localPosition);
        }
        
        /// <summary>
        /// Collects observations about the agent's environment.
        /// Provides the agent with its own position, target's position,
        /// direction to the target, current velocity, and facing a direction.
        /// </summary>
        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(transform.localPosition);
            sensor.AddObservation(target.transform.localPosition);
            sensor.AddObservation((target.transform.position - transform.position).normalized);
            sensor.AddObservation(_rb.linearVelocity.normalized);
            sensor.AddObservation(transform.forward);
        }

        /// <summary>
        /// Called when the agent receives an action from the policy.
        /// Moves and rotates the agent according to the action inputs,
        /// calculates rewards based on progress towards the target,
        /// alignment with the target direction, and handles stuck detection.
        /// </summary>
        public override void OnActionReceived(ActionBuffers actions)
        {
            var moveInput = Mathf.Clamp(actions.ContinuousActions[0], 0f, 1f);
            var turnInput = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);

            var movement = transform.forward * moveInput * movementSpeed * Time.deltaTime;
            _rb.MovePosition(_rb.position + movement);
            transform.Rotate(Vector3.up, turnInput * 300f * Time.deltaTime);

            var currentDistance = Vector3.Distance(transform.localPosition, target.transform.localPosition);
            var distanceDelta = _prevDistance - currentDistance;

            var distanceScale = Mathf.Max(1f, currentDistance / 2f);
            var progressReward = distanceDelta * distanceScale * 2f;
            
            var directionToTarget = (target.transform.position - transform.position).normalized;
            var movementAlignment = Vector3.Dot(_rb.linearVelocity.normalized, directionToTarget);
            var alignmentReward = movementAlignment * 0.2f;

            var facingDot = Vector3.Dot(transform.forward, directionToTarget);
            var facingReward = Mathf.Pow(facingDot, 2) * 0.4f;

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
        /// Handles collision events with other objects.
        /// Rewards the agent for colliding with the player (goal)
        /// and penalizes collisions with walls, obstacles, or doors.
        /// Ends the episode after collision.
        /// </summary>
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
