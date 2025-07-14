using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using System.Collections;


namespace Enemy
{
    public class HunterAgent : Agent
    {
        /// <summary>
        /// Reference to the player GameObject (assigned via tag).
        /// </summary>
        public GameObject target;

        /// <summary>
        /// Movement speed of the agent.
        /// </summary>
        [SerializeField] private float movementSpeed = 4f;

        // Internal state tracking
        private Vector3 _lastPosition;
        private float _stuckTimer;
        private Rigidbody _rb;
        private float _prevDistance;


        /// <summary>
        /// Called once at agent initialization. Assigns the player target and configures rigidbody constraints.
        /// </summary>
        public override void Initialize()
        {

            _rb = GetComponent<Rigidbody>();

            // Freeze rotation and vertical movement
            _rb.constraints = RigidbodyConstraints.FreezeRotationX |
                              RigidbodyConstraints.FreezeRotationZ |
                              RigidbodyConstraints.FreezePositionY;

			// Start the coroutine to find the player
        	StartCoroutine(FindPlayerCoroutine());

        }

        // ReSharper disable Unity.PerformanceAnalysis
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
        
        	// isInitialized = true; // NOT USED!
    	}


        /// <summary>
        /// Resets agent and player positions at the beginning of an episode.
        /// </summary>
        public override void OnEpisodeBegin()
        {
            /* 
            // Should be eliminated. It is there to show the configuration in training process
            // Randomize hunter position
            transform.localPosition = new Vector3(
                Random.Range(-8, 8f),
                1,
                Random.Range(-4f, 4f)
            );

            // Randomize target (player) position
             target.transform.localPosition = new Vector3(
                 Random.Range(-8f, 8),
                 1,
                 Random.Range(-4f, 4f)
             );
             */

            // Reset tracking variables
            // _lastPosition = transform.localPosition;
            _stuckTimer = 0f;
            _prevDistance = Vector3.Distance(transform.localPosition, target.transform.localPosition);
        }

        /// <summary>
        /// Collects observations for the agent to make decisions:
        /// - Direction to target
        /// - Agent's forward direction
        /// - Signed angle between forward direction and target
        /// </summary>
        /// <param name="sensor">The sensor collecting environment data.</param>
        public override void CollectObservations(VectorSensor sensor)
        {
            Vector3 toTarget = target.transform.localPosition - transform.localPosition;
            Vector3 forward = transform.forward;

            sensor.AddObservation(toTarget.normalized); // 3 observations
            sensor.AddObservation(forward); // 3 observations

            float angleToTarget = Vector3.SignedAngle(forward, toTarget, Vector3.up) / 180f;
            sensor.AddObservation(angleToTarget); // 1 observation

            // Total: 7 observations
        }

        /// <summary>
        /// Processes received actions to move and rotate the agent, apply rewards/penalties.
        /// </summary>
        /// <param name="actions">Agent action buffers.</param>
        public override void OnActionReceived(ActionBuffers actions)
        {
            var moveInput = actions.ContinuousActions[0]; // Forward/backward
            var turnInput = actions.ContinuousActions[1]; // Left/right turn

            // Move forward/backward
            var forwardMovement = transform.forward * moveInput * movementSpeed * Time.deltaTime;
            _rb.MovePosition(_rb.position + forwardMovement);

            // Rotate
            transform.Rotate(Vector3.up, turnInput * 180f * Time.deltaTime);

            // Calculate distance-based progress reward
            var currentDistance = Vector3.Distance(transform.localPosition, target.transform.localPosition);
            var distanceDelta = _prevDistance - currentDistance;
            AddReward(distanceDelta * 0.01f);
            _prevDistance = currentDistance;

            // Reward for facing the target
            var toTarget = (target.transform.localPosition - transform.localPosition).normalized;
            var facingDot = Vector3.Dot(transform.forward, toTarget); // 1 = facing directly at target
            AddReward(facingDot * 0.005f);

            // Step penalty to encourage faster completion
            AddReward(-0.001f);

            // Check if the agent is stuck (not moving)
            if (Vector3.Distance(transform.localPosition, _lastPosition) < 0.01f)
            {
                _stuckTimer += Time.deltaTime;
                if (_stuckTimer > 2f)
                {
                    AddReward(-5f);
                    EndEpisode();
                }
            }
            else
            {
                _stuckTimer = 0f;
            }

            _lastPosition = transform.localPosition;
        }

        /// <summary>
        /// Handles collision with player and wall objects.
        /// </summary>
        /// <param name="other">Collider the agent touched.</param>
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                AddReward(10f);
                EndEpisode();
            }
            else if (other.CompareTag("Wall"))
            {
                AddReward(-2f);
            }
        }
    }
}