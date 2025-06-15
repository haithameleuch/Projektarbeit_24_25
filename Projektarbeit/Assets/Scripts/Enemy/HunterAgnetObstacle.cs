using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Enemy
{
    public class HunterAgentObstacle : Agent
    {
        public float movementSpeed = 8f;
        public GameObject target;

        private Rigidbody _rb;
        private float _prevDistance;
        private Vector3 _lastPosition;
        private float _stuckTimer;
        private float stuckThreshold = 0.01f;
        private float stuckTimeLimit = 0.3f;
        private float lastReward = 0f;
        private bool isInitialized = false;

        // Statistik-Tracking
        private List<float> episodeRewards = new List<float>();
        private float episodeStartTime;
        private int episodeCount = 0;
        private const int STATS_INTERVAL = 50;
        private int collisionCount = 0;

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
            while (!target)
            {
                target = GameObject.FindWithTag("Player");
                if (!target)
                {
                    yield return new WaitForSeconds(0.5f); 
                }
            }
            isInitialized = true;
        }

        public override void OnEpisodeBegin()
        {
            /*
            Vector3[] corner1 = { 
                new Vector3(7.5f, 1f, -3.5f),
                new Vector3(-7.5f, 1f, 3.5f)
            };
            
            Vector3[] corner2 = {
                new Vector3(7.5f, 1f, 3.5f),
                new Vector3(-7.5f, 1f, -3.5f)
            };

            Vector3[] selectedPair = Random.value < 0.5f ? corner1 : corner2;
            
            if (Random.value < 0.5f)
            {
                transform.localPosition = selectedPair[0];
                target.transform.localPosition = selectedPair[1];
            }
            else
            {
                transform.localPosition = selectedPair[1];
                target.transform.localPosition = selectedPair[0];
            }
            */

            _lastPosition = transform.localPosition;
            _stuckTimer = 0f;
            _prevDistance = Vector3.Distance(transform.localPosition, target.transform.localPosition);
            
            episodeStartTime = Time.time;
            lastReward = 0f;
            collisionCount = 0;
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(transform.localPosition);
            sensor.AddObservation(target.transform.localPosition);
            sensor.AddObservation((target.transform.position - transform.position).normalized);
            sensor.AddObservation(_rb.linearVelocity.normalized);
            sensor.AddObservation(transform.forward);
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            float moveInput = Mathf.Clamp(actions.ContinuousActions[0], 0f, 1f);
            float turnInput = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);

            Vector3 movement = transform.forward * moveInput * movementSpeed * Time.deltaTime;
            _rb.MovePosition(_rb.position + movement);
            transform.Rotate(Vector3.up, turnInput * 300f * Time.deltaTime);

            float currentDistance = Vector3.Distance(transform.localPosition, target.transform.localPosition);
            float distanceDelta = _prevDistance - currentDistance;

            float distanceScale = Mathf.Max(1f, currentDistance / 2f);
            float progressReward = distanceDelta * distanceScale * 2f;
            
            Vector3 directionToTarget = (target.transform.position - transform.position).normalized;
            float movementAlignment = Vector3.Dot(_rb.linearVelocity.normalized, directionToTarget);
            float alignmentReward = movementAlignment * 0.2f;

            float facingDot = Vector3.Dot(transform.forward, directionToTarget);
            float facingReward = Mathf.Pow(facingDot, 2) * 0.4f;

            float totalReward = progressReward + alignmentReward + facingReward;
            AddReward(totalReward);
            lastReward += totalReward;

            _prevDistance = currentDistance;

            if (Vector3.Distance(transform.localPosition, _lastPosition) < stuckThreshold)
            {
                _stuckTimer += Time.deltaTime;
                if (_stuckTimer > stuckTimeLimit)
                {
                    AddReward(-0.5f);
                    lastReward += -0.5f;
                    EndEpisodeWithStats();
                    return;
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
                AddReward(20f);
                lastReward += 20f;
                EndEpisodeWithStats();
            }
            else if (other.CompareTag("Wall") || other.CompareTag("Obstacle") || other.CompareTag("Door"))
            {
                collisionCount++;
                AddReward(-1f);
                lastReward += -1f;
                EndEpisodeWithStats();
            }
        }

        private void EndEpisodeWithStats()
        {
            episodeRewards.Add(lastReward);
            episodeCount++;

            if (episodeCount % STATS_INTERVAL == 0)
            {
                float mean = 0f;
                float sumSquaredDiff = 0f;

                for (int i = episodeRewards.Count - STATS_INTERVAL; i < episodeRewards.Count; i++)
                {
                    mean += episodeRewards[i];
                }
                mean /= STATS_INTERVAL;

                for (int i = episodeRewards.Count - STATS_INTERVAL; i < episodeRewards.Count; i++)
                {
                    float diff = episodeRewards[i] - mean;
                    sumSquaredDiff += diff * diff;
                }
                float standardDeviation = Mathf.Sqrt(sumSquaredDiff / STATS_INTERVAL);

                float episodeDuration = Time.time - episodeStartTime;
                Debug.Log($"==========================================");
                Debug.Log($"Statistiken für Episode {episodeCount}:");
                Debug.Log($"Mittelwert der Belohnungen: {mean:F2}");
                Debug.Log($"Standardabweichung: {standardDeviation:F2}");
                Debug.Log($"Episodendauer: {episodeDuration:F2} Sekunden");
                Debug.Log($"Durchschnittliche Distanz zum Ziel: {_prevDistance:F2}");
                Debug.Log($"Kollisionen mit Hindernissen: {collisionCount}");
                Debug.Log($"==========================================");

                if (episodeRewards.Count > STATS_INTERVAL * 2)
                {
                    episodeRewards.RemoveRange(0, episodeRewards.Count - STATS_INTERVAL);
                }
            }

            EndEpisode();
        }

        private void OnDrawGizmos()
        {
            if (target != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, target.transform.position);

                Gizmos.color = Color.blue;
                Gizmos.DrawRay(transform.position, transform.forward * 2f);
            }
        }
    }
}