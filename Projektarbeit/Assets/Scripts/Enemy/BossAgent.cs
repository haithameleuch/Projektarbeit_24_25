using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace Enemy
{
    /// <summary>
    /// AI agent for boss character that uses ML-Agents for training
    /// Handles movement, dodging, attacking, and learning through reinforcement
    /// </summary>
    public class BossAgent : Agent
    {
        #region Serializable Fields
        
        [Header("Debug Settings")]
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private Color detectionRadiusColor = new Color(1, 0, 0, 0.25f);
        [SerializeField] private Color dodgeSuccessColor = Color.green;
        [SerializeField] private Color dodgeMissColor = Color.red;

        [Header("Movement Settings")]
        public GameObject target;
        [SerializeField] private float movementSpeed = 4f;
        [SerializeField] private float rotationSpeed = 180f;
        [SerializeField] private float minObstacleDistance = 1.5f;
        [SerializeField] private float dodgeSpeedMultiplier = 3f;
        [SerializeField] private float dodgeDuration = 0.3f;
        [SerializeField] private float dodgeCooldown = 0.3f;
        [SerializeField] private float safeDodgeDistance = 2f;
        
        [Header("Reward Settings")]
        [SerializeField] private float facingReward = 0.01f;
        [SerializeField] private float distanceRewardFactor = 0.1f;
        [SerializeField] private float successfulDodgeReward = 2.0f;
        [SerializeField] private float dead = -20.0f;
        [SerializeField] private float hitPenalty = -4.0f;
        [SerializeField] private float wallHitPenalty = -2.0f;
        [SerializeField] private float stepPenalty = -0.001f;
        [SerializeField] private float stuckPenalty = -2f;
        [SerializeField] private float doneReward = 5f;
        [SerializeField] private float badDodgePenalty = -3f;
        
        [Header("Training Settings")]
        [SerializeField] private float maxEpisodeLength = 30f;
        
        [Header("Combat References")]
        /// <summary>
        /// Reference to the object pool manager that manages pooled projectile objects.
        /// </summary>
        [SerializeField] private ObjectPoolManager objectPoolManager;

        /// <summary>
        /// Transform representing the position and orientation from which projectiles are fired.
        /// </summary>
        [SerializeField] private Transform shootPoint;

        #endregion

        #region Private Fields

        // Movement state
        private Rigidbody _rb;
        private Vector3 _lastPosition;
        private float _stuckTimer;
        private float _prevDistance;
        
        // Dodge state
        private bool _isDodging = false;
        private float _dodgeTimer = 0f;
        private Vector3 _dodgeDirection = Vector3.zero;
        private Vector3 _projectileThreatDirection = Vector3.zero;
        private float _timeSinceLastDodge;
        
        // Debug visualization
        private Vector3 _lastDodgePosition;
        private bool _hadSuccessfulDodge;
        private float _lastDodgeTime;
        
        // Health and timing
        private Health _health;
        private float _playerHealthNormalized;
        private float _episodeTimer;

        #endregion

        #region ML-Agents Overrides

        /// <summary>
        /// Initializes the agent, setting up required components
        /// </summary>
        public override void Initialize()
        {
            _rb = GetComponent<Rigidbody>();
            _health = GetComponent<Health>();
            
            // Constrain rigidbody to prevent unwanted rotations
            _rb.constraints = RigidbodyConstraints.FreezeRotationX |
                            RigidbodyConstraints.FreezeRotationZ |
                            RigidbodyConstraints.FreezePositionY;
            
            // Find player target if not assigned
            if (target == null)
            {
                target = GameObject.FindWithTag("Player");
            }
        }

        /// <summary>
        /// Resets the agent's state at the start of each training episode
        /// </summary>
        public override void OnEpisodeBegin()
        {
            // Randomize starting position
            transform.localPosition = new Vector3(
                Random.Range(-8, 8f),
                1,
                Random.Range(3f, 4f));
            
            // Reset health
            if (_health != null)
            {
                _health._currentHealth = _health._maxHealth;
            }
            
            // Reset timers and state
            _episodeTimer = 0f;
            transform.rotation = Quaternion.identity;
            _lastPosition = transform.localPosition;
            _stuckTimer = 0f;
            _prevDistance = Vector3.Distance(transform.localPosition, target.transform.localPosition);
            _timeSinceLastDodge = 0f;
            
            // Reset dodge state
            _isDodging = false;
            _dodgeTimer = 0f;
            _dodgeDirection = Vector3.zero;
            _projectileThreatDirection = Vector3.zero;
        }

        /// <summary>
        /// Collects observations about the environment for the neural network
        /// </summary>
        public override void CollectObservations(VectorSensor sensor)
        {
            if (target == null) return;
            
            // Get target health status
            Health targetHealth = target.GetComponent<Health>();
            _playerHealthNormalized = targetHealth != null ? targetHealth._currentHealth / targetHealth._maxHealth : 0f;
            Vector3 forward = shootPoint.forward;

            // Target relative position (3 values)
            Vector3 toTarget = target.transform.localPosition - transform.localPosition;
            sensor.AddObservation(toTarget.normalized);

            // Agent direction (3 values)
            sensor.AddObservation(transform.forward);

            // Angle to target (1 value)
            float angleToTarget = Vector3.SignedAngle(transform.forward, toTarget, Vector3.up) / 180f;
            sensor.AddObservation(angleToTarget);

            // Health status (1 value)
            if (_health != null)
            {
                sensor.AddObservation(_health._currentHealth / _health._maxHealth);
            }
            else
            {
                sensor.AddObservation(1f);
            }

            // Time since last dodge (1 value)
            sensor.AddObservation(_timeSinceLastDodge / 10f);
            
            // Current dodge state (1 value)
            sensor.AddObservation(_isDodging ? 1f : 0f);
            
            // Projectile threat direction (3 values)
            sensor.AddObservation(_projectileThreatDirection);
            
            // Additional combat observations
            sensor.AddObservation(_playerHealthNormalized);
            sensor.AddObservation(shootPoint.InverseTransformDirection(toTarget)); // Direction to target in local space
            sensor.AddObservation(Vector3.Dot(forward, toTarget)); // Alignment measure (1 = perfectly aimed)

            // Angle difference (1 value)
            float angleDiff = Vector3.Angle(forward, toTarget) / 180f;
            sensor.AddObservation(angleDiff);
            
            // Total observations: 3 + 3 + 1 + 1 + 1 + 1 + 3 + 1 + 3 + 1 = 17
        }

        /// <summary>
        /// Processes actions received from the neural network
        /// </summary>
        public override void OnActionReceived(ActionBuffers actions)
        {
            if (target == null) return;

            // Update projectile threat detection
            _projectileThreatDirection = DetectProjectileThreatDirection();

            // Process discrete actions
            int moveAction = actions.DiscreteActions[0]; // 0 = nothing, 1 = forward, 2 = backward
            int turnAction = actions.DiscreteActions[1]; // 0 = nothing, 1 = turn left, 2 = turn right
            int dodgeAction = actions.DiscreteActions[2]; // 0 = no dodge, 1 = dodge left, 2 = dodge right

            // Handle firing action
            if (actions.DiscreteActions[0] == 1)
            {
                FireProjectile();
            }
            
            // Reward for aiming closer to target
            Vector3 toTarget = (target.transform.position - shootPoint.position).normalized;
            float aimReward = Vector3.Dot(shootPoint.forward, toTarget) * 0.01f;
            AddReward(aimReward);

            // Check for player defeat
            if (_playerHealthNormalized == 0)
            {
                AddReward(doneReward);
                EndEpisode();
            }
            
            // Convert discrete actions to continuous values
            float moveInput = 0f;
            if (moveAction == 1) moveInput = 1f;
            else if (moveAction == 2) moveInput = -1f;

            float turnInput = 0f;
            if (turnAction == 1) turnInput = -1f;
            else if (turnAction == 2) turnInput = 1f;

            // Handle dodging
            HandleDodge(dodgeAction);
            
            // Apply movement
            ApplyMovement(moveInput, turnInput);

            // Calculate rewards
            CalculateMovementRewards();
            
            // Check for stuck condition
            CheckStuckCondition();
            
            // Update timers
            _timeSinceLastDodge += Time.deltaTime;
            _episodeTimer += Time.deltaTime;
            
            // End episode if time limit reached
            if (_episodeTimer > maxEpisodeLength)
            {
                EndEpisode();
            }
        }

        /// <summary>
        /// Provides manual control for testing the agent's behavior
        /// </summary>
        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var discreteActions = actionsOut.DiscreteActions;
            
            // Movement
            discreteActions[0] = Input.GetKey(KeyCode.W) ? 1 : 
                               Input.GetKey(KeyCode.S) ? 2 : 0;
            
            // Turning
            discreteActions[1] = Input.GetKey(KeyCode.A) ? 1 : 
                               Input.GetKey(KeyCode.D) ? 2 : 0;
            
            // Dodging
            discreteActions[2] = Input.GetKey(KeyCode.Q) ? 1 : 
                               Input.GetKey(KeyCode.E) ? 2 : 0;
        }

        #endregion

        #region Movement Methods

        /// <summary>
        /// Applies movement based on the given inputs
        /// </summary>
        private void ApplyMovement(float moveInput, float turnInput)
        {
            Vector3 movement;
            float actualMoveSpeed = movementSpeed;
            
            if (_isDodging)
            {
                // During dodge, move quickly in dodge direction
                movement = _dodgeDirection * dodgeSpeedMultiplier * Time.deltaTime;
            }
            else
            {
                // Normal movement
                movement = transform.forward * moveInput * movementSpeed * Time.deltaTime;
                
                // Apply rotation to face target when not dodging
                Vector3 toTarget = (target.transform.position - transform.position).normalized;
                float angleToTarget = Vector3.SignedAngle(transform.forward, toTarget, Vector3.up);
                
                // If we're not facing the target, rotate toward it
                if (Mathf.Abs(angleToTarget) > 5f)
                {
                    float rotationDirection = angleToTarget > 0 ? 1f : -1f;
                    transform.Rotate(Vector3.up, rotationDirection * rotationSpeed * Time.deltaTime);
                }
                
                // Override turn input to always face target
                turnInput = 0f;
            }
            
            _rb.MovePosition(_rb.position + movement);
        }

        /// <summary>
        /// Calculates and applies rewards based on movement behavior
        /// </summary>
        private void CalculateMovementRewards()
        {
            if (target == null) return;

            Vector3 toTarget = (target.transform.localPosition - transform.localPosition).normalized;
            float currentDistance = Vector3.Distance(transform.localPosition, target.transform.localPosition);
            
            // Reward for facing the target
            float facingDot = Vector3.Dot(transform.forward, toTarget);
            AddReward(facingReward * facingDot);
            
            // Reward for closing distance
            float distanceDelta = _prevDistance - currentDistance;
            if (distanceDelta > 0)
            {
                AddReward(distanceDelta * distanceRewardFactor);
            }
            _prevDistance = currentDistance;
            
            // Small step penalty to encourage efficiency
            AddReward(stepPenalty);
        }

        /// <summary>
        /// Checks if the agent is stuck and penalizes if stuck for too long
        /// </summary>
        private void CheckStuckCondition()
        {
            float distanceMoved = Vector3.Distance(transform.localPosition, _lastPosition);
            
            if (distanceMoved < 0.05f)
            {
                _stuckTimer += Time.deltaTime;
                if (_stuckTimer > 5f)
                {
                    AddReward(stuckPenalty);
                    EndEpisode();
                }
            }
            else
            {
                _stuckTimer = 0f;
            }
            
            _lastPosition = transform.localPosition;
        }

        #endregion

        #region Dodge Methods

        /// <summary>
        /// Detects the direction of incoming projectile threats
        /// </summary>
        private Vector3 DetectProjectileThreatDirection()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, minObstacleDistance);
            Vector3 threatDirection = Vector3.zero;
            
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Projectile"))
                {
                    Vector3 toProjectile = (hit.transform.position - transform.position).normalized;
                    threatDirection += toProjectile;
                }
            }
            
            return threatDirection.normalized;
        }

        /// <summary>
        /// Handles dodge behavior based on the action received
        /// </summary>
        private void HandleDodge(int dodgeAction)
        {
            if (!_isDodging && _timeSinceLastDodge > dodgeCooldown && dodgeAction > 0)
            {
                bool shouldDodge = _projectileThreatDirection != Vector3.zero;

                if (shouldDodge)
                {
                    _isDodging = true;
                    _dodgeTimer = 0f;
                    _timeSinceLastDodge = 0f;

                    // Get vector to target
                    Vector3 toTarget = (target.transform.position - transform.position).normalized;
                    
                    // Calculate perpendicular directions (left and right relative to target vector)
                    Vector3 rightPerpendicular = Vector3.Cross(Vector3.up, toTarget).normalized;
                    Vector3 leftPerpendicular = -rightPerpendicular;

                    // Choose direction based on action
                    _dodgeDirection = (dodgeAction == 1) ? leftPerpendicular : rightPerpendicular;

                    // Check if direction is safe
                    if (Physics.Raycast(transform.position, _dodgeDirection, safeDodgeDistance))
                    {
                        // If not safe, try the opposite direction
                        _dodgeDirection = -_dodgeDirection;
                        if (Physics.Raycast(transform.position, _dodgeDirection, safeDodgeDistance))
                        {
                            // If both directions are blocked, cancel dodge
                            _isDodging = false;
                            _dodgeDirection = Vector3.zero;
                            AddReward(badDodgePenalty);
                            return;
                        }
                    }

                    AddReward(successfulDodgeReward * 0.1f); // Small initial reward for attempting to dodge
                }
                else
                {
                    // Penalize dodging when there's no threat
                    AddReward(badDodgePenalty);
                }
            }

            // Check for agent death
            if (_health != null && _health._currentHealth == 0)
            {
                AddReward(dead);
                EndEpisode();
            }

            // Update dodge timer
            if (_isDodging)
            {
                _dodgeTimer += Time.deltaTime;
                if (_dodgeTimer >= dodgeDuration)
                {
                    _isDodging = false;
                }
            }
        }

        /// <summary>
        /// Detects and rewards successful projectile dodges
        /// </summary>
        private void DetectAndRewardProjectileDodging()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, minObstacleDistance * 1.2f);
            
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Projectile"))
                {
                    Vector3 dirToProjectile = (hit.transform.position - transform.position).normalized;
                    
                    _lastDodgePosition = transform.position;
                    _lastDodgeTime = Time.time;
                    
                    // Check if projectile would have hit us
                    if (Vector3.Dot(dirToProjectile, transform.forward) < 0.7f)
                    {
                        // Check if we dodged successfully
                        if (_isDodging && Vector3.Dot(_dodgeDirection, dirToProjectile) < 0.3f)
                        {
                            _hadSuccessfulDodge = true;
                            AddReward(successfulDodgeReward);
                            Debug.DrawLine(transform.position, hit.transform.position, Color.green, 1f);
                        }
                        else
                        {
                            _hadSuccessfulDodge = false;
                            Debug.DrawLine(transform.position, hit.transform.position, Color.red, 1f);
                        }
                    }
                }
            }
        }

        #endregion

        #region Combat Methods

        /// <summary>
        /// Fires a projectile by retrieving one from the object pool and activating it at the shooting point
        /// </summary>
        private void FireProjectile()
        {
            // Get an inactive projectile from the object pool
            GameObject projectile = objectPoolManager.GetPooledObject();
            if (projectile is not null)
            {
                // Position and orient the projectile at the shooting point
                projectile.transform.position = shootPoint.position;
                projectile.transform.rotation = shootPoint.rotation;

                // Activate the projectile
                projectile.SetActive(true);
            }
        }

        #endregion

        #region Collision Detection

        /// <summary>
        /// Handles trigger collisions with other objects
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                AddReward(doneReward);
                EndEpisode();
            }
            else if (other.CompareTag("Projectile"))
            {
                AddReward(hitPenalty);
                if (_health != null)
                {
                    _health._currentHealth = 0;
                    EndEpisode();
                }
            }
            else if (other.CompareTag("Wall"))
            {
                AddReward(wallHitPenalty);
            }
        }

        #endregion

        #region Debug Visualization

        /// <summary>
        /// Draws debug gizmos in the scene view
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;
            
            // Draw detection sphere
            Gizmos.color = detectionRadiusColor;
            Gizmos.DrawWireSphere(transform.position, minObstacleDistance * 1.5f);
            
            // Draw forward vector
            Debug.DrawRay(transform.position, transform.forward * 2f, Color.blue, 0.1f);
            
            // Draw last dodge event
            if (Time.time - _lastDodgeTime < 1f)
            {
                Gizmos.color = _hadSuccessfulDodge ? dodgeSuccessColor : dodgeMissColor;
                Gizmos.DrawWireSphere(_lastDodgePosition, 0.5f);
                Debug.DrawLine(_lastDodgePosition, transform.position, 
                            _hadSuccessfulDodge ? Color.green : Color.red, 1f);
            }
            
            // Draw current dodge direction if dodging
            if (_isDodging)
            {
                Debug.DrawRay(transform.position, _dodgeDirection * 2f, Color.magenta, 0.1f);
            }
            
            // Draw projectile threat direction
            if (_projectileThreatDirection != Vector3.zero)
            {
                Debug.DrawRay(transform.position, _projectileThreatDirection * 2f, Color.yellow, 0.1f);
            }
            
            // Draw target vector and perpendicular directions
            if (target != null)
            {
                Vector3 toTarget = (target.transform.position - transform.position).normalized;
                Vector3 rightPerpendicular = Vector3.Cross(Vector3.up, toTarget).normalized;
                Vector3 leftPerpendicular = -rightPerpendicular;
                
                Debug.DrawRay(transform.position, toTarget * 2f, Color.cyan, 0.1f);
                Debug.DrawRay(transform.position, rightPerpendicular * 2f, Color.green, 0.1f);
                Debug.DrawRay(transform.position, leftPerpendicular * 2f, Color.red, 0.1f);
            }
        }

        #endregion
    }
}