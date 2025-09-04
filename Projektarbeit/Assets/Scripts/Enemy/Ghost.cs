using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using System.Collections;

namespace Enemy
{
    /*
     * GhostAgent Structure and Behavior:
     *
     * This class defines a stealthy and agile enemy agent (Ghost) using Unity ML-Agents.
     * The Ghost's behavior combines smooth movement with disappearing and dashing to confuse and approach the player target.
     *
     * Agent Overview:
     * - The Ghost uses continuous actions to move and rotate toward the player.
     * - It observes the direction and angle to the player to inform its movement decisions.
     * - Rewards are provided for reducing distance and facing the target, with penalties for idleness or poor performance.
     *
     * Enemy Type Summary:
     * - Type: Agile-Stealth Enemy
     * - Goal: Reach the player using evasive and unpredictable movement.
     * - Behavior:
     *     • Periodically becomes invisible.
     *     • Perform smooth dash movement to the left or right (instead of teleportation).
     *     • Reappears and continues pursuing the player.
     *     • Penalized for hitting walls or getting stuck.
     *
     * Key Features:
     * - Physics-based movement using Rigidbody.
     * - Ghost-like dash using `Vector3.Lerp` over `dashDuration`.
     * - Visibility controlled via alpha channel fading on the Renderer material.
     * - Timed routine for random dash behavior that improves unpredictability.
     */

    public class Ghost : Agent
    {
        public GameObject target;

        [SerializeField] private float movementSpeed = 4f;

        // Dash & visibility settings
        [Header("Dash & Visibility Settings")]
        [SerializeField] private float minTeleportTime = 3f;    // Min seconds before dash
        [SerializeField] private float maxTeleportTime = 5f;    // Max seconds before dash
        [SerializeField] private float dashDistance = 1f;       // How far to dash left/right
        [SerializeField] private float dashDuration = 0.2f;     // Duration of dash in seconds
        [SerializeField] private Renderer ghostRenderer;        // Assign your ghost's Renderer here in Inspector

        private Vector3 _lastPosition;
        private float _stuckTimer;
        private Rigidbody _rb;
        private float _prevDistance;

        /// <summary>
        /// Called once when the agent is initialized.
        /// Sets up Rigidbody constraints and starts coroutines for player detection and dash/visibility logic.
        /// </summary>
        public override void Initialize()
        {
            _rb = GetComponent<Rigidbody>();

            // Freeze rotation on X and Z axes, and freeze Y position to keep the agent grounded
            _rb.constraints = RigidbodyConstraints.FreezeRotationX |
                              RigidbodyConstraints.FreezeRotationZ |
                              RigidbodyConstraints.FreezePositionY;

            // Start coroutine to find player target asynchronously
            StartCoroutine(FindPlayerCoroutine());

            // Start coroutine to handle invisibility toggling and dashing behavior
            StartCoroutine(VisibilityToggleRoutine());
        }

        protected override void OnDisable()
        {
            StopAllCoroutines();
        }

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// Coroutine that continuously attempts to find the player GameObject by its tag.
        /// Waits and retries until player is found.
        /// </summary>
        private IEnumerator FindPlayerCoroutine()
        {
            while (!target)
            {
                target = GameObject.FindWithTag("Player");
                if (!target)
                {
                    yield return new WaitForSeconds(0.5f); // Wait before retrying
                }
            }
            movementSpeed = gameObject.GetComponent<Stats>().GetCurStats(2);
        }

        /// <summary>
        /// Called at the start of each episode.
        /// Resets stuck timer and initializes distance to the target for reward calculation.
        /// </summary>
        public override void OnEpisodeBegin()
        {
            _stuckTimer = 0f; // Reset stuck timer
            if (!target) return;
            _prevDistance = Vector3.Distance(transform.localPosition, target.transform.localPosition); // Compute the Distance
        }

        /// <summary>
        /// Collects observations for the agent including normalized direction and angle to the target.
        /// </summary>
        /// <param name="sensor">The sensor to add observations to.</param>
        public override void CollectObservations(VectorSensor sensor)
        {
            if (!target)
            {
                sensor.AddObservation(Vector3.zero);
                sensor.AddObservation(transform.forward);
                sensor.AddObservation(0f);
                return;
            }
            
            // Direction to target and current forward direction
            var toTarget = target.transform.localPosition - transform.localPosition;
            var forward = transform.forward;

            sensor.AddObservation(toTarget.normalized); // Direction to target
            sensor.AddObservation(forward);              // Current forward direction

            // Signed angle between forward and toTarget, normalized to [-1,1]
            float angleToTarget = Vector3.SignedAngle(forward, toTarget, Vector3.up) / 180f;
            sensor.AddObservation(angleToTarget);
        }

        /// <summary>
        /// Receives actions from the neural network to control movement and rotation.
        /// Provides rewards for approaching and facing the target, and penalties for being stuck or wasting time.
        /// </summary>
        /// <param name="actions">Actions from the agent's policy.</param>
        public override void OnActionReceived(ActionBuffers actions)
        {
            if (!target) { AddReward(-0.001f); return; }
            
            var moveInput = actions.ContinuousActions[0];  // Forward/backward movement input
            var turnInput = actions.ContinuousActions[1];  // Rotation input

            // Move forward/backward based on input and speed
            var forwardMovement = transform.forward * moveInput * movementSpeed * Time.deltaTime;
            _rb.MovePosition(_rb.position + forwardMovement);

            // Rotate around Y axis based on input
            transform.Rotate(Vector3.up, turnInput * 180f * Time.deltaTime);

            // Reward agent for reducing distance to target
            var currentDistance = Vector3.Distance(transform.localPosition, target.transform.localPosition);
            var distanceDelta = _prevDistance - currentDistance;
            AddReward(distanceDelta * 0.01f);
            _prevDistance = currentDistance;

            // Small reward for facing the target
            var toTarget = (target.transform.localPosition - transform.localPosition).normalized;
            var facingDot = Vector3.Dot(transform.forward, toTarget);
            AddReward(facingDot * 0.005f);

            AddReward(-0.001f); // Small time penalty to encourage efficiency

            // Detect if stuck by checking minimal movement
            if (Vector3.Distance(transform.localPosition, _lastPosition) < 0.01f)
            {
                _stuckTimer += Time.deltaTime;
                if (_stuckTimer > 2f)
                {
                    AddReward(-5f); // Penalize for being stuck
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
        /// Handles collisions with the player and walls.
        /// Rewards the agent for reaching the player and penalizes hitting walls.
        /// </summary>
        /// <param name="other">Collider of the object the agent collided with.</param>
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                AddReward(10f); // Big reward for reaching the player
                EndEpisode();
            }
            else if (other.CompareTag("Wall"))
            {
                AddReward(-2f); // Penalty for hitting walls
            }
        }

        /// <summary>
        /// Coroutine to smoothly fade the ghost's material alpha to a target value over time.
        /// Used to toggle invisibility.
        /// </summary>
        /// <param name="targetAlpha">Target alpha value (0 = invisible, 1 = fully visible).</param>
        private IEnumerator FadeToAlpha(float targetAlpha)
        {
            if (!ghostRenderer) yield break;

            Material mat = ghostRenderer.material;
            Color color = mat.color;
            float startAlpha = color.a;
            float duration = 0.5f;  // Fade duration in seconds

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                mat.color = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }

            // Ensure the final alpha set exactly
            mat.color = new Color(color.r, color.g, color.b, targetAlpha);
        }

        /// <summary>
        /// Coroutine to smoothly move the ghost left or over dashDuration seconds.
        /// Implements a smooth dash movement instead of instant teleport.
        /// </summary>
        private IEnumerator DashMovement()
        {
            var direction = Random.value > 0.5f ? Vector3.right : Vector3.left;
            var startPos = transform.position;
            var targetPos = startPos + direction * dashDistance;

            var elapsed = 0f;

            while (elapsed < dashDuration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, targetPos, elapsed / dashDuration);
                yield return null;
            }

            // Snap to exact dash target position
            transform.position = targetPos;
        }

        /// <summary>
        /// Coroutine that continuously toggles the ghost's visibility and performs dash movement on a timer.
        /// Waits a random interval, fades out, dashes left or right smoothly, then fades back in.
        /// </summary>
        private IEnumerator VisibilityToggleRoutine()
        {
            while (true)
            {
                // Wait a random time before dashing
                yield return new WaitForSeconds(Random.Range(minTeleportTime, maxTeleportTime));

                // Fade out ghost (become invisible)
                yield return StartCoroutine(FadeToAlpha(0f));

                // Perform dash movement smoothly instead of instant teleport
                yield return StartCoroutine(DashMovement());

                // Fade ghost back in (become visible)
                yield return StartCoroutine(FadeToAlpha(1f));
            }
        }
    }
}
