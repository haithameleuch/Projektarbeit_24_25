using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using System.Collections;

namespace Enemy
{
    /// <summary>
    /// Defines an enemy agent (BomberAgent) trained with Unity ML-Agents.
    ///
    /// <para>Core Behavior:</para>
    /// <list type="bullet">
    /// <item>Pursues the player using continuous movement and rotation actions.</item>
    /// <item>Drops bombs when in proximity to the player using a coroutine-based system.</item>
    /// <item>Receives rewards for approaching and facing the player accurately.</item>
    /// <item>Penalized for collisions with walls or being stuck.</item>
    /// </list>
    ///
    /// <para>Observations:</para>
    /// <list type="bullet">
    /// <item>Normalized direction to the player (Vector3)</item>
    /// <item>Agent’s forward direction (Vector3)</item>
    /// <item>Signed angle to the player (float, normalized)</item>
    /// </list>
    /// <para>Total = 7 observations per step.</para>
    ///
    /// <para>Rewards:</para>
    /// <list type="bullet">
    /// <item>Positive reward for reducing distance to the player.</item>
    /// <item>Positive reward for facing the player (alignment).</item>
    /// <item>Large positive reward for touching the player (end of episode).</item>
    /// <item>Negative rewards for being stuck or colliding with walls.</item>
    /// </list>
    ///
    /// <para>Key Features:</para>
    /// <list type="bullet">
    /// <item>Rigidbody-based physics movement and rotation.</item>
    /// <item>Coroutine-based bomb-dropping with cooldown and proximity checks.</item>
    /// <item>Initialization waits for player spawn to ensure proper targeting.</item>
    /// <item>Stuck detection and distance-based reward shaping.</item>
    /// </list>
    ///
    /// <para>Best suited for:</para>
    /// <list type="bullet">
    /// <item>Pursuer-type enemies with area-of-effect attacks.</item>
    /// <item>Training scenarios emphasizing navigation, threat generation, and reward shaping.</item>
    /// </list>
    /// </summary>
    public class BomberAgent : Agent
    {
        /// <summary>
        /// Reference to the player target in the scene.
        /// </summary>
        public GameObject target;
        
        /// <summary>
        /// Prefab of the bomb object to be instantiated by the agent.
        /// </summary>
        public GameObject bombPrefab;
        
        /// <summary>
        /// Movement speed of the bomber agent (fetched from Stats on initialization).
        /// </summary>
        [SerializeField] private float movementSpeed = 4f;

        /// <summary>
        /// Stores the agent's last recorded position to detect if it gets stuck.
        /// </summary>
        private Vector3 _lastPosition;
        
        /// <summary>
        /// Timer used to track how long the agent has been stuck in place.
        /// </summary>
        private float _stuckTimer;
        
        /// <summary>
        /// Cached Rigidbody component used for movement and physics interactions.
        /// </summary>
        private Rigidbody _rb;
        
        /// <summary>
        /// Tracks the agent’s distance to the target from the previous frame
        /// to calculate progress-based rewards.
        /// </summary>
        private float _prevDistance;
        
        /// <summary>
        /// Flag indicating whether the agent has successfully found and initialized the player target.
        /// </summary>
        private bool _isInitialized;
        
        /// <summary>
        /// Flag controlling whether the agent is currently allowed to drop a bomb
        /// (enforces bomb cooldown).
        /// </summary>
        private bool _canDropBomb = true;

        /// <summary>
        /// Vertical offset to spawn bombs slightly below the agent.
        /// </summary>
        private const float BombDropOffset = 0.5f;

        /// <summary>
        /// Called once when the agent is initialized.
        /// Sets up Rigidbody constraints and starts searching for the player.
        /// </summary>
        public override void Initialize()
        {
            _rb = GetComponent<Rigidbody>();

            // Freeze unnecessary movement/rotation
            _rb.constraints = RigidbodyConstraints.FreezeRotationX |
                              RigidbodyConstraints.FreezeRotationZ |
                              RigidbodyConstraints.FreezePositionY;

            // Start searching for the player
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
        /// Coroutine that repeatedly searches for the player by tag until found.
        /// Once found, initializes movement speed and starts bomb dropping.
        /// </summary>
        private IEnumerator FindPlayerCoroutine()
        {
            while (!target)
            {
                target = GameObject.FindWithTag("Player");
                if (!target)
                    yield return new WaitForSeconds(0.5f);
            }

            _isInitialized = true;
            movementSpeed = gameObject.GetComponent<Stats.Stats>().GetCurStats(2);
            
            // Start bomb-dropping logic once initialized
            StartCoroutine(BombDropCoroutine());
        }

        /// <summary>
        /// Resets the agent at the beginning of each training episode.
        /// </summary>
        public override void OnEpisodeBegin()
        {
            _stuckTimer = 0f;

            if (target != null)
                _prevDistance = Vector3.Distance(transform.localPosition, target.transform.localPosition);
        }

        /// <summary>
        /// Collects observations from the environment to provide input for the ML model.
        /// Observations include direction to target, agent's forward direction, and angle difference.
        /// Number of Observations = 3 + 3 + 1 = 7
        /// </summary>
        /// <param name="sensor">Vector sensor used to collect numeric observations.</param>
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

            sensor.AddObservation(toTarget.normalized); // Direction to target - 3
            sensor.AddObservation(forward);             // Agent's facing a direction - 3
            sensor.AddObservation(Vector3.SignedAngle(forward, toTarget, Vector3.up) / 180f); // Angle difference - 1
        }

        /// <summary>
        /// Executes actions received from the ML model to control movement and rotation.
        /// Applies movement, rotation, and calculates rewards/penalties for approaching the target,
        /// facing the target, being stuck, or general efficiency.
        /// </summary>
        /// <param name="actions">Action buffers containing continuous action values.</param>
        public override void OnActionReceived(ActionBuffers actions)
        {
            if (!target) { AddReward(-0.001f); return; }
            
            var moveInput = actions.ContinuousActions[0];
            var turnInput = actions.ContinuousActions[1];

            // Move and rotate the agent
            var forwardMovement = transform.forward * moveInput * movementSpeed * Time.deltaTime;
            _rb.MovePosition(_rb.position + forwardMovement);
            transform.Rotate(Vector3.up, turnInput * 180f * Time.deltaTime);

            // Reward based on approaching the target
            var currentDistance = Vector3.Distance(transform.localPosition, target.transform.localPosition);
            var distanceDelta = _prevDistance - currentDistance;
            AddReward(distanceDelta * 0.01f);
            _prevDistance = currentDistance;

            // Reward for facing the target
            var toTarget = (target.transform.localPosition - transform.localPosition).normalized;
            var facingDot = Vector3.Dot(transform.forward, toTarget);
            AddReward(facingDot * 0.005f);

            // Small penalty to encourage faster resolution
            AddReward(-0.001f);

            // Check if agent is stuck
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
                _stuckTimer = 0f; // Reset if agent is moving
            }

            _lastPosition = transform.localPosition;
        }

        /// <summary>
        /// Handles trigger collision events with other colliders.
        /// Rewards for reaching the player, penalizes for hitting walls.
        /// </summary>
        /// <param name="other">Collider that the agent entered.</param>
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
        /// Coroutine that manages bomb dropping behavior.
        /// Drops bombs when the player is within range, with cooldown between drops.
        /// </summary>
        private IEnumerator BombDropCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.1f);

                if (!_isInitialized || !target)
                    continue;

                // Compare XZ distances to ignore height
                var bomberXZ = new Vector3(transform.position.x, 0f, transform.position.z);
                var playerXZ = new Vector3(target.transform.position.x, 0f, target.transform.position.z);
                var distanceToPlayer = Vector3.Distance(bomberXZ, playerXZ);

                // Drop bomb if close enough and allowed
                if (!(distanceToPlayer < 2.5f) || !_canDropBomb) continue;
                DropBomb();
                _canDropBomb = false;

                yield return new WaitForSeconds(3f); // Cooldown between bombs
                _canDropBomb = true;
            }
        }

        /// <summary>
        /// Instantiates a bomb prefab beneath the agent and applies downward velocity
        /// to simulate falling.
        /// </summary>
        private void DropBomb()
        {
            if (!bombPrefab) return;

            var dropPosition = transform.position - new Vector3(0, BombDropOffset, 0);
            var bomb = Instantiate(bombPrefab, dropPosition, Quaternion.identity);

            var rb = bomb.GetComponent<Rigidbody>();
            if (rb)
            {
                // Assign downward velocity to simulate falling
                rb.linearVelocity = Vector3.down * 5f;
            }
        }
    }
}
