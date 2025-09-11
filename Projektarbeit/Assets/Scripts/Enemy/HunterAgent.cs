using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using System.Collections;


namespace Enemy
{
    /// <summary>
    /// Defines a pursuit-type enemy agent (HunterAgent) trained with Unity ML-Agents.
    ///
    /// <para>Core Behavior:</para>
    /// <list type="bullet">
    /// <item>Moves toward the player using continuous forward movement and rotation actions.</item>
    /// <item>Receives rewards for reducing distance to the player and properly facing the target.</item>
    /// <item>Penalized for being stuck, hitting walls, or inefficient movement.</item>
    /// </list>
    ///
    /// <para>Observations:</para>
    /// <list type="bullet">
    /// <item>Normalized vector to the player (Vector3)</item>
    /// <item>Agent's forward vector (Vector3)</item>
    /// <item>Signed angle between forward and target direction (float, normalized)</item>
    /// </list>
    /// <para>Total = 7 observations per step.</para>
    ///
    /// <para>Rewards:</para>
    /// <list type="bullet">
    /// <item>Positive reward for decreasing distance to the player.</item>
    /// <item>Positive reward for facing the player directly (alignment).</item>
    /// <item>Negative rewards for being stuck or colliding with walls.</item>
    /// <item>Step penalty to encourage efficient navigation.</item>
    /// </list>
    ///
    /// <para>Key Features:</para>
    /// <list type="bullet">
    /// <item>Rigidbody-based physics movement with smooth rotation.</item>
    /// <item>Stuck detection using positional deltas and timers.</item>
    /// <item>Coroutine-based player detection to handle dynamic spawns.</item>
    /// <item>Modular design suitable as a base for advanced enemy AI types.</item>
    /// </list>
    ///
    /// <para>Best suited for:</para>
    /// <list type="bullet">
    /// <item>Pursuer enemies that chase players directly.</item>
    /// <item>Training scenarios focusing on obstacle-aware navigation and directional control.</item>
    /// </list>
    /// </summary>
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

        /// <summary>
        /// Last recorded position of the agent, used to detect if it is stuck.
        /// </summary>
        private Vector3 _lastPosition;
        
        /// <summary>
        /// Timer tracking how long the agent has been stuck in the same position.
        /// </summary>
        private float _stuckTimer;
        
        /// <summary>
        /// Cached Rigidbody component for physics-based movement.
        /// </summary>
        private Rigidbody _rb;
        
        /// <summary>
        /// Distance to the target in the previous frame, used to compute progress-based rewards.
        /// </summary>
        private float _prevDistance;
        
        /// <summary>
        /// Called once at agent initialization.
        /// Assigns Rigidbody and configures constraints, then starts player detection coroutine.
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

        /// <summary>
        /// Called automatically by Unity when this agent or GameObject is disabled.
        /// Stops all running coroutines to prevent unwanted behavior or errors.
        /// </summary>
        protected override void OnDisable()
        {
            StopAllCoroutines();
        }

        /// <summary>
        /// Coroutine to continuously find the player GameObject by tag.
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
            movementSpeed = gameObject.GetComponent<Stats.Stats>().GetCurStats(2);
        	// isInitialized = true; // NOT USED!
    	}
        
        /// <summary>
        /// Resets agent and player positions at the beginning of an episode.
        /// </summary>
        public override void OnEpisodeBegin()
        {
            /* 
            // It is there to show the configuration in training process
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
             // Reset tracking variables
            _lastPosition = transform.localPosition;
             */
            
            _stuckTimer = 0f;
            if (!target) return;
            _prevDistance = Vector3.Distance(transform.localPosition, target.transform.localPosition);
        }

        /// <summary>
        /// Collects observations for the agent to make decisions:
        /// - Direction to target
        /// - Agent's forward direction
        /// - Signed angle between forward direction and target
        /// Number of Observations = 3 + 3 + 1 = 7
        /// </summary>
        /// <param name="sensor">The sensor collecting environment data.</param>
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
            
            var toTarget = target.transform.localPosition - transform.localPosition;
            var forward = transform.forward;

            sensor.AddObservation(toTarget.normalized); // 3 observations
            sensor.AddObservation(forward); // 3 observations

            var angleToTarget = Vector3.SignedAngle(forward, toTarget, Vector3.up) / 180f;
            sensor.AddObservation(angleToTarget); // 1 observation
        }

        /// <summary>
        /// Processes received actions to move and rotate the agent, apply rewards/penalties.
        /// </summary>
        /// <param name="actions">Agent action buffers.</param>
        public override void OnActionReceived(ActionBuffers actions)
        {
            if (!target) return;
            
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