using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

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

        /// <summary>
        /// Initializes the agent: finds the player and locks unnecessary rigidbody movement.
        /// </summary>
        public override void Initialize()
        {
            target = GameObject.FindWithTag("Player");
            _rb = GetComponent<Rigidbody>();

            // Prevent unwanted movement/rotation
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | 
                             RigidbodyConstraints.FreezeRotationZ | 
                             RigidbodyConstraints.FreezePositionY;
        }

        /// <summary>
        /// Called at the beginning of each episode to randomize agent and target positions.
        /// </summary>
        public override void OnEpisodeBegin()
        {
            // Randomize positions on both sides of the environment
            float value1 = Random.value < 0.5f ? Random.Range(-1f, 1f) : Random.Range(6f, 8f);
            float value2 = Random.value < 0.5f ? Random.Range(-1f, 1f) : Random.Range(-6f, -8f);
            
            transform.localPosition = new Vector3(value1, 1, Random.Range(-4f, 4f));
            target.transform.localPosition = new Vector3(value2, 1, Random.Range(-4f, 4f));

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

            // Distance progress reward
            float currentDistance = Vector3.Distance(transform.localPosition, target.transform.localPosition);
            float distanceDelta = _prevDistance - currentDistance;
            AddReward(distanceDelta * 0.1f);

            // Facing reward (dot product ranges -1 to 1)
            float facingDot = Vector3.Dot(transform.forward, 
                                          (target.transform.position - transform.position).normalized);
            AddReward((facingDot + 1) * 0.005f);

            // Time penalty to encourage faster pursuit
            AddReward(-0.001f);

            _prevDistance = currentDistance;

            // Check if agent is stuck
            if (Vector3.Distance(transform.localPosition, _lastPosition) < 0.01f)
            {
                _stuckTimer += Time.deltaTime;
                if (_stuckTimer > 2f)
                    EndEpisode();
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
                EndEpisode();
            }
            else if (other.CompareTag("Wall") || other.CompareTag("Obstacle"))
            {
                AddReward(-1f);
                EndEpisode();
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
