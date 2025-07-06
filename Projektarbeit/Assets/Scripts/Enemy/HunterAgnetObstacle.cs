using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Enemy
{
    public class HunterAgentObstacle : Agent
    {
        public float movementSpeed = 8f;
        public GameObject target;

        private Rigidbody _rb;
        private float _prevDistance;
        private Vector3 _lastPosition;
        private float _stuckTimer;
        private const float StuckThreshold = 0.01f;
        private const float StuckTimeLimit = 0.3f;

        private float _lastReward;
        // private bool isInitialized = false;  // NOT USED!

        // Statistik-Tracking
        private readonly List<float> _episodeRewards = new List<float>();
        private float _episodeStartTime;
        private int _episodeCount;
        private const int StatsInterval = 50;
        private int _collisionCount;

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
            /* Training
            Vector3[] corner1 = { 
                new Vector3(7.5f, 1f, -3.5f),
                new Vector3(-7.5f, 1f, 3.5f)
            };
            
            Vector3[] corner2 = {
                new Vector3(7.5f, 1f, 3.5f),
                new Vector3(-7.5f, 1f, -3.5f)
            };

            Vector3[] selectedPair = Random.value < 0.5f ? corner1 : corner2;
            
            if (Random.value < 0.5f)
            {
                transform.localPosition = selectedPair[0];
                target.transform.localPosition = selectedPair[1];
            }
            else
            {
                transform.localPosition = selectedPair[1];
                target.transform.localPosition = selectedPair[0];
            }
            */

            _lastPosition = transform.localPosition;
            _stuckTimer = 0f;
            _prevDistance = Vector3.Distance(transform.localPosition, target.transform.localPosition);
            
            _episodeStartTime = Time.time;
            _lastReward = 0f;
            _collisionCount = 0;
        }
        
        /// <summary>
        /// Collects observations about the agent's environment.
        /// Provides the agent with its own position, target's position,
        /// direction to the target, current velocity, and facing direction.
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
            float moveInput = Mathf.Clamp(actions.ContinuousActions[0], 0f, 1f);
            float turnInput = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);

            Vector3 movement = transform.forward * moveInput * movementSpeed * Time.deltaTime;
            _rb.MovePosition(_rb.position + movement);
            transform.Rotate(Vector3.up, turnInput * 300f * Time.deltaTime);

            float currentDistance = Vector3.Distance(transform.localPosition, target.transform.localPosition);
            float distanceDelta = _prevDistance - currentDistance;

            float distanceScale = Mathf.Max(1f, currentDistance / 2f);
            float progressReward = distanceDelta * distanceScale * 2f;
            
            Vector3 directionToTarget = (target.transform.position - transform.position).normalized;
            float movementAlignment = Vector3.Dot(_rb.linearVelocity.normalized, directionToTarget);
            float alignmentReward = movementAlignment * 0.2f;

            float facingDot = Vector3.Dot(transform.forward, directionToTarget);
            float facingReward = Mathf.Pow(facingDot, 2) * 0.4f;

            float totalReward = progressReward + alignmentReward + facingReward;
            AddReward(totalReward);
            _lastReward += totalReward;

            _prevDistance = currentDistance;

            // Detect if the agent is stuck and penalize if stuck for too long
            if (Vector3.Distance(transform.localPosition, _lastPosition) < StuckThreshold)
            {
                _stuckTimer += Time.deltaTime;
                if (_stuckTimer > StuckTimeLimit)
                {
                    AddReward(-0.5f);
                    _lastReward += -0.5f;
                    EndEpisodeWithStats();
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
                _lastReward += 20f;
                EndEpisodeWithStats();
            }
            else if (other.CompareTag("Wall") || other.CompareTag("Obstacle") || other.CompareTag("Door"))
            {
                _collisionCount++;
                AddReward(-1f);
                _lastReward += -1f;
                EndEpisodeWithStats();
            }
        }

        /// <summary>
        /// Ends the current episode and logs statistics every StatsInterval episodes.
        /// Tracks reward mean, standard deviation, duration, distance, and collisions.
        /// Maintains a rolling list of recent rewards for statistics.
        /// </summary>
        private void EndEpisodeWithStats()
        {
            _episodeRewards.Add(_lastReward);
            _episodeCount++;

            if (_episodeCount % StatsInterval == 0)
            {
                float mean = 0f;

                for (int i = _episodeRewards.Count - StatsInterval; i < _episodeRewards.Count; i++)
                {
                    mean += _episodeRewards[i];
                }
                mean /= StatsInterval;

                for (int i = _episodeRewards.Count - StatsInterval; i < _episodeRewards.Count; i++)
                {
                    float diff = _episodeRewards[i] - mean;
                }

                // Keep only the most recent rewards for stats to save memory
                if (_episodeRewards.Count > StatsInterval * 2)
                {
                    _episodeRewards.RemoveRange(0, _episodeRewards.Count - StatsInterval);
                }
            }

            EndEpisode();
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
