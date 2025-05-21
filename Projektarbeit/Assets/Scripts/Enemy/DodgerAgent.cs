using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace Enemy
        {
        public class DodgerAgent : Agent
{
    // Add these new fields
    [Header("Training Settings")]
    [SerializeField] private float maxEpisodeLength = 30f;
    [SerializeField] private float maxDistanceFromTarget = 15f;
    private float episodeTimer;

        [Header("References")]
        public GameObject target;  // The shooter/player to avoid
        private Rigidbody _rb;

        [Header("Movement Settings")]
        public float moveSpeed = 5f;
        public float dodgeSpeed = 10f;
        public float dodgeDuration = 0.3f;
        public float dodgeCooldown = 1f;
        
        
        private Health _health;
        
        private bool isDodging = false;
        private float dodgeTimer = 0f;
        private float cooldownTimer = 0f;
        private Vector3 startPosition;

         public override void Initialize()
        {
            _rb = GetComponent<Rigidbody>();
            _health = GetComponent<Health>();

            _rb.constraints = RigidbodyConstraints.FreezeRotationX |
                            RigidbodyConstraints.FreezeRotationZ |
                            RigidbodyConstraints.FreezePositionY;

            if (target == null)
            {
                target = GameObject.FindWithTag("Player");
            }
        }

    public override void OnEpisodeBegin()
    {
        // Keep your existing reset logic...
        
        // Add these lines
        episodeTimer = 0f;
        
            // Position target in random corner
            int corner = Random.Range(0, 4);
            Vector3 shooterPosition = corner switch
            {
                0 => new Vector3(7.5f, 1f, 3.5f),  // Top-right
                1 => new Vector3(-7.5f, 1f, 3.5f), // Top-left
                2 => new Vector3(7.5f, 1f, -3.5f),  // Bottom-right
                _ => new Vector3(-7.5f, 1f, -3.5f)  // Bottom-left
            };
            
            target.transform.localPosition = shooterPosition;

            // Position agent diagonally opposite
            Vector3 agentPosition = new Vector3(
                    Random.Range(-8f, 8),
                   1,
                    Random.Range(-4f, 4f)
                );
            
            // Reset position
            transform.localPosition = agentPosition;
            transform.rotation = Quaternion.identity;
            target.transform.forward = transform.localPosition - target.transform.localPosition;
            
            // Reset health and timers
            if (_health != null) _health._currentHealth = _health._maxHealth;
            
            // Reset state
            isDodging = false;
            dodgeTimer = 0f;
            cooldownTimer = 0f;
            
            if (_rb != null)
                _rb.linearVelocity = Vector3.zero;
        }
        

        public override void CollectObservations(VectorSensor sensor)
        {
            if (target == null) return;

            // Direction to target (normalized)
            Vector3 dirToTarget = (target.transform.position - transform.position).normalized;
            sensor.AddObservation(dirToTarget);

            // Distance to target (scaled)
            float maxDistance = 15f; // Adjust based on your arena size
            sensor.AddObservation(Vector3.Distance(transform.position, target.transform.position) / maxDistance);

            // Agent's forward direction (normalized)
            sensor.AddObservation(transform.forward);

            // Dodge state
            sensor.AddObservation(isDodging ? 1f : 0f);
            sensor.AddObservation(Mathf.Clamp01(cooldownTimer / dodgeCooldown)); // Normalized cooldown

            // Projectile observations
            GameObject[] projectiles = GameObject.FindGameObjectsWithTag("Projectile");
            float closestDist = float.MaxValue;
            Vector3 closestDir = Vector3.zero;

            foreach (GameObject proj in projectiles)
            {
                float dist = Vector3.Distance(transform.position, proj.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestDir = (proj.transform.position - transform.position).normalized;
                }
            }

            sensor.AddObservation(closestDir);
            sensor.AddObservation(Mathf.Clamp01(closestDist / maxDistance)); // Normalized distance
        }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Keep your existing action logic...

        // Add episode termination conditions
        episodeTimer += Time.deltaTime;
        
        // End episode conditions
        if (episodeTimer >= maxEpisodeLength)
        {
            AddReward(0.5f); // Partial reward for surviving full episode
            EndEpisode();
            return;
        }
        
        

        // Check if agent is too far from target
        float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);
        if (distanceToTarget > maxDistanceFromTarget)
        {
            AddReward(-0.5f);
            EndEpisode();
            return;
        }

        // Add survival reward
        AddReward(0.001f);
            // Get actions
            float moveX = actions.ContinuousActions[0];
            float moveZ = actions.ContinuousActions[1];
            bool shouldDodge = actions.DiscreteActions[0] == 1;

            // Handle dodge cooldown
            if (cooldownTimer > 0)
                cooldownTimer -= Time.deltaTime;

            // Handle dodging
            if (isDodging)
            {
                dodgeTimer += Time.deltaTime;
                if (dodgeTimer >= dodgeDuration)
                {
                    isDodging = false;
                    dodgeTimer = 0f;
                    cooldownTimer = dodgeCooldown;
                }
            }
            else if (shouldDodge && cooldownTimer <= 0)
            {
                isDodging = true;
                dodgeTimer = 0f;
                AddReward(0.1f); // Small reward for dodging
            }

            // Apply movement
            Vector3 moveDirection = new Vector3(moveX, 0, moveZ).normalized;
            float currentSpeed = isDodging ? dodgeSpeed : moveSpeed;
            _rb.linearVelocity = moveDirection * currentSpeed;

            // Small negative reward per step to encourage efficient movement
            AddReward(-0.001f);
        }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Projectile"))
        {
            AddReward(-1f);
            EndEpisode();
        }
        else if (collision.gameObject.CompareTag("Player"))
        {
            AddReward(+5f);
            EndEpisode();
        }
        else if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.1f);
            // End episode if hitting wall too hard
            if (collision.relativeVelocity.magnitude > moveSpeed * 1.5f)
            {
                AddReward(-0.5f);
                EndEpisode();
            }
        }
    }

    // Add this method to track stuck condition
    private Vector3 lastPosition;
    private float stuckTime;
    
    private void CheckIfStuck()
    {
        float movedDistance = Vector3.Distance(transform.position, lastPosition);
        if (movedDistance < 0.1f)
        {
            stuckTime += Time.deltaTime;
            if (stuckTime > 5f)
            {
                AddReward(-0.5f);
                EndEpisode();
            }
        }
        else
        {
            stuckTime = 0f;
        }
        lastPosition = transform.position;
    }

    private void FixedUpdate()
    {
        CheckIfStuck();
    }

        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("Projectile"))
            {
                if (isDodging)
                    AddReward(0.2f); // Reward for successfully dodging
                else
                    AddReward(-0.1f); // Small penalty for being near projectile without dodging
            }
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var continuousActions = actionsOut.ContinuousActions;
            var discreteActions = actionsOut.DiscreteActions;

            continuousActions[0] = Input.GetAxisRaw("Horizontal");
            continuousActions[1] = Input.GetAxisRaw("Vertical");
            discreteActions[0] = Input.GetKey(KeyCode.Space) ? 1 : 0;
        } 
        }
}