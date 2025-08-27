using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using System.Collections;

namespace Enemy
{
    /*
     * BomberAgent Structure and Behavior:
     *
     * This class defines an enemy agent (BomberAgent) trained using Unity ML-Agents to pursue the player
     * and drop bombs when nearby.
     *
     * Agent Overview:
     * - The BomberAgent uses continuous actions to move forward/backward and rotate left/right.
     * - It observes the direction to the player, its own forward vector, and the angle between them.
     * - Rewards are given for approaching and facing the player, with penalties for being stuck or hitting walls.
     * - A coroutine (`BombDropCoroutine`) handles bomb-dropping logic based on proximity to the player.
     * - The bomb is instantiated slightly below the agent and given downward velocity.
     *
     * Key Features:
     * - Movement is physics-based using Rigidbody.
     * - Initialization includes waiting for the player to appear in the scene.
     * - Includes a cooldown mechanism between bomb drops.
     * - Ends the episode with a significant reward if the agent touches the player.
     *
     * Enemy Type Summary:
     * - Type: Pursuer-Bomber Enemy
     * - Goal: Approach the player and drop bombs to simulate threat.
     * - Behavior: Smart navigation + area-based bombing + stuck detection logic.
     */
    
    public class BomberAgent : Agent
    {
        // Reference to the player target
        public GameObject target;
        
        // Bomb prefab to instantiate
        public GameObject bombPrefab;
        
        // Movement speed of the bomber agent
        [SerializeField] private float movementSpeed = 4f;

        // Internal state variables
        private Vector3 _lastPosition;
        private float _stuckTimer;
        private Rigidbody _rb;
        private float _prevDistance;
        private bool _isInitialized;
        private bool _canDropBomb = true;

        private readonly float _bombDropOffset = 0.5f;

        // Called once when the agent is initialized
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

        protected override void OnDisable()
        {
            StopAllCoroutines();
        }

        // Coroutine to find the player in the scene
        // ReSharper disable Unity.PerformanceAnalysis
        private IEnumerator FindPlayerCoroutine()
        {
            while (!target)
            {
                target = GameObject.FindWithTag("Player");
                if (!target)
                    yield return new WaitForSeconds(0.5f);
            }

            _isInitialized = true;
            movementSpeed = gameObject.GetComponent<Stats>().GetCurStats(2);
            // Start bomb-dropping logic once initialized
            StartCoroutine(BombDropCoroutine());
        }

        // Reset agent at the beginning of each episode
        public override void OnEpisodeBegin()
        {
            _stuckTimer = 0f;

            if (target != null)
                _prevDistance = Vector3.Distance(transform.localPosition, target.transform.localPosition);
        }

        // Collects observations for the ML model
        public override void CollectObservations(VectorSensor sensor)
        {
            if (!target)
            {
                sensor.AddObservation(Vector3.zero);
                sensor.AddObservation(transform.forward);
                sensor.AddObservation(0f);
                return;
            }
            
            Vector3 toTarget = target.transform.localPosition - transform.localPosition;
            Vector3 forward = transform.forward;

            sensor.AddObservation(toTarget.normalized); // Direction to target
            sensor.AddObservation(forward);             // Agent's facing a direction
            sensor.AddObservation(Vector3.SignedAngle(forward, toTarget, Vector3.up) / 180f); // Angle difference
            
            // Number of Observations = 3 + 3 +3
        }

        // Receives actions from the model
        public override void OnActionReceived(ActionBuffers actions)
        {
            if (!target) { AddReward(-0.001f); return; }
            
            var moveInput = actions.ContinuousActions[0];
            var turnInput = actions.ContinuousActions[1];

            // Move and rotate the agent
            Vector3 forwardMovement = transform.forward * moveInput * movementSpeed * Time.deltaTime;
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
                    AddReward(-5f); // Penalty for being stuck
                    EndEpisode();   // Restart the episode
                }
            }
            else
            {
                _stuckTimer = 0f; // Reset if agent is moving
            }

            _lastPosition = transform.localPosition;
        }

        // Trigger events for collisions
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                AddReward(10f); // Big reward for reaching player
                EndEpisode();
            }
            else if (other.CompareTag("Wall"))
            {
                AddReward(-2f); // Penalty for hitting a wall
            }
        }
        
        // Coroutine for bomb dropping logic
        private IEnumerator BombDropCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.1f);

                if (!_isInitialized || !target)
                    continue;

                // Compare XZ distances to ignore height
                Vector3 bomberXZ = new Vector3(transform.position.x, 0f, transform.position.z);
                Vector3 playerXZ = new Vector3(target.transform.position.x, 0f, target.transform.position.z);
                float distanceToPlayer = Vector3.Distance(bomberXZ, playerXZ);

                // Drop bomb if close enough and allowed
                if (distanceToPlayer < 2.5f && _canDropBomb)
                {
                    DropBomb();
                    _canDropBomb = false;

                    yield return new WaitForSeconds(3f); // Cooldown between bombs
                    _canDropBomb = true;
                }
            }
            // ReSharper disable once IteratorNeverReturns
        }

        // Spawns a bomb beneath the agent
        // ReSharper disable Unity.PerformanceAnalysis
        private void DropBomb()
        {
            if (!bombPrefab) return;

            Vector3 dropPosition = transform.position - new Vector3(0, _bombDropOffset, 0);
            GameObject bomb = Instantiate(bombPrefab, dropPosition, Quaternion.identity);

            Rigidbody rb = bomb.GetComponent<Rigidbody>();
            if (rb)
            {
                // Assign downward velocity to simulate falling
                rb.linearVelocity = Vector3.down * 5f;
            }
        }
    }
}
