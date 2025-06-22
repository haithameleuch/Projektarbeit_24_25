using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using System.Collections;

namespace Enemy
{
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
        // private bool isInitialized = false; // NOT USED!

        public override void Initialize()
        {
            _rb = GetComponent<Rigidbody>();

            // Freeze rotation and vertical movement
            _rb.constraints = RigidbodyConstraints.FreezeRotationX |
                              RigidbodyConstraints.FreezeRotationZ |
                              RigidbodyConstraints.FreezePositionY;

            // Start coroutine to find player target and toggle visibility/dash
            StartCoroutine(FindPlayerCoroutine());
            StartCoroutine(VisibilityToggleRoutine());
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

            // isInitialized = true; // NOT USED!
        }

        public override void OnEpisodeBegin()
        {
            _stuckTimer = 0f;
            _prevDistance = Vector3.Distance(transform.localPosition, target.transform.localPosition);
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            Vector3 toTarget = target.transform.localPosition - transform.localPosition;
            Vector3 forward = transform.forward;

            sensor.AddObservation(toTarget.normalized);
            sensor.AddObservation(forward);

            float angleToTarget = Vector3.SignedAngle(forward, toTarget, Vector3.up) / 180f;
            sensor.AddObservation(angleToTarget);
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            float moveInput = actions.ContinuousActions[0];
            float turnInput = actions.ContinuousActions[1];

            // Move forward/backward
            Vector3 forwardMovement = transform.forward * moveInput * movementSpeed * Time.deltaTime;
            _rb.MovePosition(_rb.position + forwardMovement);

            // Rotate left/right
            transform.Rotate(Vector3.up, turnInput * 180f * Time.deltaTime);

            // Reward based on distance progress
            float currentDistance = Vector3.Distance(transform.localPosition, target.transform.localPosition);
            float distanceDelta = _prevDistance - currentDistance;
            AddReward(distanceDelta * 0.01f);
            _prevDistance = currentDistance;

            // Reward for facing target
            Vector3 toTarget = (target.transform.localPosition - transform.localPosition).normalized;
            float facingDot = Vector3.Dot(transform.forward, toTarget);
            AddReward(facingDot * 0.005f);

            AddReward(-0.001f); // Step penalty

            // Check if stuck
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

        // Coroutine to smoothly fade ghost in/out by adjusting material alpha
        private IEnumerator FadeToAlpha(float targetAlpha)
        {
            if (ghostRenderer == null) yield break;

            Material mat = ghostRenderer.material;
            Color color = mat.color;
            float startAlpha = color.a;
            float duration = 0.5f;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                mat.color = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }

            mat.color = new Color(color.r, color.g, color.b, targetAlpha);
        }

        // Coroutine to dash left or right smoothly over dashDuration seconds
        private IEnumerator DashMovement()
        {
            Vector3 direction = Random.value > 0.5f ? Vector3.right : Vector3.left;
            Vector3 startPos = transform.position;
            Vector3 targetPos = startPos + direction * dashDistance;

            float elapsed = 0f;

            while (elapsed < dashDuration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, targetPos, elapsed / dashDuration);
                yield return null;
            }

            transform.position = targetPos; // Snap to exact position at end
        }

        // Coroutine that controls invisibility toggling and dash movement on a timer
        private IEnumerator VisibilityToggleRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(minTeleportTime, maxTeleportTime));

                // Fade out (become invisible)
                yield return StartCoroutine(FadeToAlpha(0f));

                // Dash to left or right smoothly instead of instant teleport
                yield return StartCoroutine(DashMovement());

                // Fade back in (become visible)
                yield return StartCoroutine(FadeToAlpha(1f));
            }
        }
    }
}
