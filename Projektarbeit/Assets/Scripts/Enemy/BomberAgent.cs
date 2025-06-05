using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using System.Collections;

namespace Enemy
{
    public class BomberAgent : Agent
    {
        public GameObject target;

        [SerializeField] private float movementSpeed = 4f;
        public GameObject bombPrefab;
        public Transform bombSpawnPoint;

        private Vector3 _lastPosition;
        private float _stuckTimer;
        private Rigidbody _rb;
        private float _prevDistance;
        private bool isInitialized = false;
        private bool canDropBomb = true;

        private float bombDropOffset = 0.5f;

        public override void Initialize()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.constraints = RigidbodyConstraints.FreezeRotationX |
                              RigidbodyConstraints.FreezeRotationZ |
                              RigidbodyConstraints.FreezePositionY;

            StartCoroutine(FindPlayerCoroutine());
        }

        private IEnumerator FindPlayerCoroutine()
        {
            while (target == null)
            {
                target = GameObject.FindWithTag("Player");
                if (target == null)
                    yield return new WaitForSeconds(0.5f);
            }

            isInitialized = true;
            StartCoroutine(BombDropCoroutine()); // Start dropping bombs
        }

        public override void OnEpisodeBegin()
        {
            _stuckTimer = 0f;
            if (target != null)
                _prevDistance = Vector3.Distance(transform.localPosition, target.transform.localPosition);
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            Vector3 toTarget = target.transform.localPosition - transform.localPosition;
            Vector3 forward = transform.forward;

            sensor.AddObservation(toTarget.normalized);
            sensor.AddObservation(forward);
            sensor.AddObservation(Vector3.SignedAngle(forward, toTarget, Vector3.up) / 180f);
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            float moveInput = actions.ContinuousActions[0];
            float turnInput = actions.ContinuousActions[1];

            Vector3 forwardMovement = transform.forward * moveInput * movementSpeed * Time.deltaTime;
            _rb.MovePosition(_rb.position + forwardMovement);
            transform.Rotate(Vector3.up, turnInput * 180f * Time.deltaTime);

            float currentDistance = Vector3.Distance(transform.localPosition, target.transform.localPosition);
            float distanceDelta = _prevDistance - currentDistance;
            AddReward(distanceDelta * 0.01f);
            _prevDistance = currentDistance;

            Vector3 toTarget = (target.transform.localPosition - transform.localPosition).normalized;
            float facingDot = Vector3.Dot(transform.forward, toTarget);
            AddReward(facingDot * 0.005f);
            AddReward(-0.001f);

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

        private IEnumerator BombDropCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.1f);

                if (!isInitialized || target == null)
                    continue;

                Vector3 bomberXZ = new Vector3(transform.position.x, 0f, transform.position.z);
                Vector3 playerXZ = new Vector3(target.transform.position.x, 0f, target.transform.position.z);
                float distanceToPlayer = Vector3.Distance(bomberXZ, playerXZ);

                if (distanceToPlayer < 2.5f && canDropBomb)
                {
                    DropBomb();
                    canDropBomb = false;
                    yield return new WaitForSeconds(3f); // Cooldown
                    canDropBomb = true;
                }
            }
        }

        private void DropBomb()
        {
            if (bombPrefab == null) return;

            Vector3 dropPosition = transform.position - new Vector3(0, bombDropOffset, 0);
            GameObject bomb = Instantiate(bombPrefab, dropPosition, Quaternion.identity);

            Rigidbody rb = bomb.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.down * 5f; // Adjust speed as needed
            }
        }
    }
}
