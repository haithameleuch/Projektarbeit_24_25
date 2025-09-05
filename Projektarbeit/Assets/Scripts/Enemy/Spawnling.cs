using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using System.Collections;

namespace Enemy
{
    /// <summary>
    /// Defines a summoner-type enemy agent (Spawnling) trained with Unity ML-Agents.
    ///
    /// <para>Core Behavior:</para>
    /// <list type="bullet">
    /// <item>Pursues the player using Rigidbody-based forward/backward movement and rotation.</item>
    /// <item>Periodically spawns a designated prefab at its current position to increase threat or support allies.</item>
    /// <item>Uses coroutine-based player detection and modular spawn system.</item>
    /// </list>
    ///
    /// <para>Observations:</para>
    /// <list type="bullet">
    /// <item>Direction to the player, normalized (Vector3)</item>
    /// <item>Agent's forward direction (Vector3)</item>
    /// <item>Signed angle to the player, normalized [-1,1] (float)</item>
    /// </list>
    /// <para>Total = 7 observations per step.</para>
    ///
    /// <para>Rewards:</para>
    /// <list type="bullet">
    /// <item>Positive reward for reducing distance to the player.</item>
    /// <item>Positive reward for facing the player.</item>
    /// <item>Negative reward for being stuck or colliding with walls.</item>
    /// </list>
    ///
    /// <para>Key Features:</para>
    /// <list type="bullet">
    /// <item>Rigidbody-based movement and rotation with frozen Y-axis position and X/Z rotation.</item>
    /// <item>Spawn timer controlling instantiation of prefab objects.</item>
    /// <item>Stuck detection using position delta over time.</item>
    /// <item>Modular and adaptable for swarm or reinforcement-based enemy behaviors.</item>
    /// </list>
    ///
    /// <para>Best suited for:</para>
    /// <list type="bullet">
    /// <item>Enemies that grow in threat over time (e.g., spawners or breeders).</item>
    /// <item>Cooperative enemy behaviors where spawned prefabs act as reinforcements or distractions.</item>
    /// <item>Training scenarios combining pursuit and spawning mechanics.</item>
    /// </list>
    /// </summary>
    public class Spawnling : Agent
    {
        /// <summary>
        /// Reference to the player GameObject (assigned via tag).
        /// </summary>
        public GameObject target;

        /// <summary>
        /// Movement speed of the agent.
        /// </summary>
        [SerializeField] private float movementSpeed = 4f;

        /// <summary>Tracks the agent's last position to detect being stuck.</summary>
        private Vector3 _lastPosition;
        
        /// <summary>Timer for stuck detection.</summary>
        private float _stuckTimer;
        
        /// <summary>Reference to the Rigidbody for physics-based movement.</summary>
        private Rigidbody _rb;
        
        /// <summary>Reference to the Rigidbody for physics-based movement.</summary>
        private float _prevDistance;
        
        /// <summary>Flag to indicate if the agent has finished initialization and found the target.</summary>
        private bool _isInitialized;

        /// <summary>
        /// Time interval between spawns, default 10 seconds.
        /// </summary>
        [SerializeField, Tooltip("Time interval in seconds between each spawn. Default is 10.")]
        private float spawnInterval = 10f;

        /// <summary>
        /// Prefab to spawn.
        /// </summary>
        [SerializeField, Tooltip("Prefab GameObject to spawn.")]
        private GameObject prefabToSpawn;

        /// <summary>
        /// Timer to track spawn cooldown.
        /// </summary>
        private float _spawnTimer;
        
        /// <summary>
        /// Unity Start method called before the first frame update.
        /// Immediately spawns the assigned prefab when the agent is first created.
        /// </summary> 
        private void Start()
        {
            SpawnPrefab();
        }
        
        /// <summary>
        /// Called every frame to handle spawning logic and other updates.
        /// </summary>
        private void Update()
        {
            if (!_isInitialized || !prefabToSpawn) return;

            // Countdown spawn timer
            _spawnTimer -= Time.deltaTime;

            // When the timer reaches zero, spawn a prefab instance
            if (!(_spawnTimer <= 0f)) return;
            SpawnPrefab();
            _spawnTimer = spawnInterval; // Reset timer
        }

        /// <summary>
        /// Called once at agent initialization. Assigns the player target and sets Rigidbody constraints.
        /// Also initializes the spawn timer.
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

            // Initialize spawn timer to spawn immediately at start
            _spawnTimer = spawnInterval;

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
        /// Coroutine that repeatedly searches for the player GameObject by tag ("Player").
        /// Once found, sets the target reference, updates movement speed, and marks the agent as initialized.
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
            _isInitialized = true;
        }
        
        /// <summary>
        /// Resets agent and player positions at the beginning of an episode.
        /// Resets spawn timer as well.
        /// </summary>
        public override void OnEpisodeBegin()
        {
            _stuckTimer = 0f;
            if (!target) return;
            
            _prevDistance = Vector3.Distance(transform.localPosition, target.transform.localPosition);

            // Reset spawn timer
            _spawnTimer = spawnInterval;
        }
        /// <summary>
        /// Collects observations for the agent to make decisions:
        /// - Direction to target
        /// - Agent's forward direction
        /// - Signed angle between forward direction and target
        /// - Number of Observations: 3 + 3 + 1 = 7
        /// </summary>
        /// <param name="sensor">The sensor collecting environment data.</param>
        public override void CollectObservations(VectorSensor sensor)
        {
            if (!target)
            {
                sensor.AddObservation(Vector3.zero);
                sensor.AddObservation(transform.forward);
                sensor.AddObservation(0f);
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
        /// Receives actions from the ML model and moves/rotates the agent accordingly.
        /// Rewards are applied based on:
        /// - Reducing distance to the player
        /// - Facing the player
        /// - Penalty for being stuck
        /// - Small time step penalty
        /// </summary>
        /// <param name="actions">Continuous action inputs from the agent.</param>
        public override void OnActionReceived(ActionBuffers actions)
        {
            if (!target) { AddReward(-0.001f); return; }
            
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

        /// <summary>
        /// Instantiates the prefab at the current position and rotation.
        /// </summary>
        private void SpawnPrefab()
        {
            if (prefabToSpawn is not null)
            {
                Instantiate(prefabToSpawn, transform.position, transform.rotation, transform.parent);
            }
            else
            {
                Debug.LogWarning("Prefab to spawn is not assigned in Spawnling.");
            }
        }
    }
}