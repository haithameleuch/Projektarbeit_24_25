using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using System.Collections;

namespace Enemy
{
    /// <summary>
    /// ML-Agents hunter agent with obstacles in the environment.
    /// Learns to navigate toward the player while avoiding obstacles and walls.
    /// </summary>
    public class HunterAgentObstacle : Agent
    {
        /// <summary>
        /// Movement speed of the agent.
        /// </summary>
        [SerializeField] private float movementSpeed = 4f;

        /// <summary>
        /// The target GameObject (e.g., the player).
        /// </summary>
        public GameObject target;

        // Internal state tracking
        private Rigidbody _rb;
        private float _prevDistance;
        private Vector3 _lastPosition;
        private float _stuckTimer;
        private bool isInitialized = false;
        private float episodeTimeLimit = 20f; // 20 seconds time limit
        private float episodeTimer = 0f;
        private float stuckThreshold = 0.01f; // Distance threshold to consider agent stuck
        private float stuckTimeLimit = 1f;   



        /// <summary>
        /// Initializes the agent: finds the player and locks unnecessary rigidbody movement.
        /// </summary>
        public override void Initialize()
        {
            _rb = GetComponent<Rigidbody>();

            // Prevent unwanted movement/rotation
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | 
                             RigidbodyConstraints.FreezeRotationZ | 
                             RigidbodyConstraints.FreezePositionY;
            // Start the coroutine to find the player
            StartCoroutine(FindPlayerCoroutine());
        }
        
        private IEnumerator FindPlayerCoroutine()
        {
            while (target == null)
            {
                target = GameObject.FindWithTag("Player");
                if (target == null)
                {
                    yield return new WaitForSeconds(0.5f); 
                }
            }
        
            isInitialized = true;
        }

        /// <summary>
        /// Called at the beginning of each episode to randomize agent and target positions.
        /// </summary>
        public override void OnEpisodeBegin()
        {
            Vector3 a = new Vector3(7.5f, 1f, -3.5f);
            Vector3 b = new Vector3(-7.5f, 1f, 3.5f);
            Vector3 c = new Vector3(7.5f, 1f, 3.5f);
            Vector3 d = new Vector3(-7.5f, 1f, -3.5f);
            
            Vector3[] positions = { a, b, c, d };
            transform.localPosition = positions[Random.Range(0, positions.Length)];
            
            Vector3 selectedPosition;
            do {
                selectedPosition = positions[Random.Range(0, positions.Length)];
            } while (selectedPosition == transform.localPosition);

            target.transform.localPosition = selectedPosition;

            episodeTimer = 0f;

            _lastPosition = transform.localPosition;
            _stuckTimer = 0f;
            _prevDistance = Vector3.Distance(transform.localPosition, target.transform.localPosition);
        }

        /// <summary>
        /// Collects spatial and motion-related observations:
        /// - Agent and target positions
        /// - Direction to target
        /// - Current velocity direction
        /// - Forward facing direction
        /// </summary>
        /// <param name="sensor">Sensor used to collect observations.</param>
        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(transform.localPosition); // 3: Agent position
            sensor.AddObservation(target.transform.localPosition); // 3: Target position
            sensor.AddObservation((target.transform.position - transform.position).normalized); // 3: Direction to target
            sensor.AddObservation(_rb.linearVelocity.normalized); // 3: Velocity direction
            sensor.AddObservation(transform.forward); // 3: Facing direction

            // Total = 15 observations
        }

        /// <summary>
        /// Processes movement and rotation actions and calculates rewards based on progress and orientation.
        /// </summary>
        /// <param name="actions">Action buffer with 2 continuous actions: move and turn.</param>
        public override void OnActionReceived(ActionBuffers actions)
        {
            float moveInput = Mathf.Clamp(actions.ContinuousActions[0], 0f, 1f); // Forward only
            float turnInput = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f); // Full rotation allowed

            // Move forward
            Vector3 movement = transform.forward * moveInput * movementSpeed * Time.deltaTime;
            _rb.MovePosition(_rb.position + movement);

            // Rotate around Y-axis
            transform.Rotate(Vector3.up, turnInput * 180f * Time.deltaTime);

            // Modified distance progress reward with scaling
            float currentDistance = Vector3.Distance(transform.localPosition, target.transform.localPosition);
            float distanceDelta = _prevDistance - currentDistance;
            float distanceScale = Mathf.Max(1f, currentDistance / 5f); // Increase reward for progress when far away
            AddReward(distanceDelta * 0.1f * distanceScale);


            // Facing reward (dot product ranges -1 to 1)
            float facingDot = Vector3.Dot(transform.forward, 
                                          (target.transform.position - transform.position).normalized);
            AddReward((facingDot + 1) * 0.005f);

            // Time penalty to encourage faster pursuit
            AddReward(-0.01f);

            _prevDistance = currentDistance;
            
            // Add at the beginning of the method
            episodeTimer += Time.deltaTime;
            if (episodeTimer >= episodeTimeLimit)
            {
                AddReward(-1f); // Penalty for timeout
                //EndEpisode();
                return;
            }

            // Check if agent is stuck
            if (Vector3.Distance(transform.localPosition, _lastPosition) < 0.01f)
            {
                _stuckTimer += Time.deltaTime;
                if (_stuckTimer > stuckTimeLimit)
                {
                    AddReward(-0.5f);  // Reduced penalty
                    //EndEpisode();
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
        /// Handles reward and termination logic on collision with player, walls, or obstacles.
        /// </summary>
        /// <param name="other">Collider that was entered.</param>
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                AddReward(10f);
                Debug.Log("Player reached!");
                EndEpisode();
            }
            else if (other.CompareTag("Wall") || other.CompareTag("Obstacle"))
            {
                AddReward(-1f);
                // EndEpisode();
            }
        }

        /// <summary>
        /// Draws visual debugging aids in the editor: red line to target, blue ray forward.
        /// </summary>
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, target.transform.position);

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.forward * 2f);
        }
    }
}