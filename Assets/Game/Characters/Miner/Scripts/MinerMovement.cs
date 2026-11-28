using System.Collections.Generic;
using Game.Systems.Mining;
using UnityEngine;

namespace Game.Characters.Miner
{
    /// <summary>
    /// Handles smooth 2.5D locomotion on the terrain surface with statue avoidance
    /// and soft separation between miners.
    /// </summary>
    [DisallowMultipleComponent]
    public class MinerMovement : MonoBehaviour
    {
        private static readonly List<MinerMovement> ActiveMiners = new List<MinerMovement>();

        [Header("Locomotion")]
        [SerializeField, Min(0.5f)] private float walkSpeed = 1.9375f;
        [SerializeField, Min(1f)] private float rotationSpeed = 8f;
        [SerializeField, Min(0.05f)] private float arrivalThreshold = 0.2f;

        [Header("2.5D Corridor & Avoidance")]
        [SerializeField] private float minWalkableZ = -2.45f;
        [SerializeField] private float maxWalkableZ = 2.25f;
        [SerializeField] private float statueAvoidRadius = 0f;
        [SerializeField] private float statueHardRadius = 0f;
        [SerializeField] private float minerSeparationRadius = 0f;
        [SerializeField] private float minerSeparationStrength = 0f;

        private TerrainHeightSampler terrainSampler;
        private Transform statueTransform;
        private Vector3 destination;
        private bool hasDestination;
        private float currentSpeed;

        public bool IsMoving => hasDestination;
        public float CurrentSpeed => currentSpeed;
        public float WalkSpeed => walkSpeed;

        private void OnEnable()
        {
            if (!ActiveMiners.Contains(this))
            {
                ActiveMiners.Add(this);
            }
        }

        private void OnDisable()
        {
            ActiveMiners.Remove(this);
        }

        public void Initialize(TerrainHeightSampler sampler, Transform playerStatue)
        {
            terrainSampler = sampler;
            statueTransform = playerStatue;
            SnapToGround();
        }

        public void SetDestination(Vector3 targetWorldPos)
        {
            targetWorldPos.z = Mathf.Clamp(targetWorldPos.z, minWalkableZ, maxWalkableZ);
            destination = targetWorldPos;
            hasDestination = true;
        }

        public void Stop()
        {
            hasDestination = false;
            currentSpeed = 0f;
        }

        public void FacePosition(Vector3 worldPos, float deltaTime)
        {
            FaceDirection(worldPos - transform.position, deltaTime);
        }

        public void FaceDirection(Vector3 worldDir, float deltaTime)
        {
            worldDir.y = 0f;
            if (worldDir.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion targetRot = Quaternion.LookRotation(worldDir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Mathf.Clamp01(rotationSpeed * deltaTime));
        }

        public void SnapToGround()
        {
            if (terrainSampler == null)
            {
                return;
            }

            Vector3 pos = transform.position;
            pos.y = terrainSampler.SampleHeight(pos);
            transform.position = pos;
        }

        public bool TickMovement(float deltaTime)
        {
            if (!hasDestination)
            {
                currentSpeed = 0f;
                return false;
            }

            Vector3 currentPos = transform.position;
            Vector3 toTarget = destination - currentPos;
            toTarget.y = 0f;

            float remainingDist = toTarget.magnitude;
            if (remainingDist <= arrivalThreshold)
            {
                hasDestination = false;
                currentSpeed = 0f;
                return true;
            }

            // Optional statue routing (disabled when statueAvoidRadius <= 0 so units pass freely through statues).
            Vector3 steeringTarget = destination;
            if (statueTransform != null && statueAvoidRadius > 0.01f)
            {
                Vector3 statuePos = statueTransform.position;
                bool crossingLeftToDeposit = currentPos.x > statuePos.x - 1.35f && destination.x < statuePos.x - 1.0f;
                bool crossingRightFromDeposit = currentPos.x < statuePos.x + 1.35f && destination.x > statuePos.x + 1.0f;

                if (crossingLeftToDeposit || crossingRightFromDeposit)
                {
                    float frontCorridorZ = Mathf.Clamp(statuePos.z - statueAvoidRadius - 0.1f, minWalkableZ, maxWalkableZ);
                    // Only detour when we haven't yet cleared the statue on the target side.
                    bool needsFrontDetour = currentPos.z > frontCorridorZ + 0.35f &&
                                            Mathf.Abs(currentPos.x - statuePos.x) < statueAvoidRadius + 1.8f;
                    if (needsFrontDetour)
                    {
                        float bypassX = crossingLeftToDeposit
                            ? Mathf.Min(currentPos.x, statuePos.x - 0.4f)
                            : Mathf.Max(currentPos.x, statuePos.x + 0.4f);
                        steeringTarget = new Vector3(bypassX, currentPos.y, frontCorridorZ);
                    }
                }
            }

            Vector3 toSteer = steeringTarget - currentPos;
            toSteer.y = 0f;
            Vector3 moveDir = toSteer.sqrMagnitude > 0.0001f ? toSteer.normalized : (toTarget / remainingDist);

            // Optional smooth routing around Player_Statue pedestal.
            if (statueTransform != null && statueAvoidRadius > 0.01f)
            {
                Vector3 statuePos = statueTransform.position;
                Vector3 fromStatue = currentPos - statuePos;
                fromStatue.y = 0f;
                float distToStatue = fromStatue.magnitude;
                if (distToStatue < statueAvoidRadius && distToStatue > 0.001f)
                {
                    float pushWeight = (statueAvoidRadius - distToStatue) / statueAvoidRadius;
                    Vector3 radialOut = fromStatue / distToStatue;

                    // Always prefer routing around the clear -Z front side of the statue.
                    Vector3 tangentA = new Vector3(-radialOut.z, 0f, radialOut.x);
                    Vector3 tangentB = -tangentA;
                    Vector3 tangent = tangentA.z <= tangentB.z ? tangentA : tangentB;

                    Vector3 avoidDir = (radialOut * 1.1f + tangent * 1.35f).normalized;
                    moveDir = (moveDir + avoidDir * (pushWeight * 2.4f)).normalized;
                }
            }

            // Optional soft separation from other active miners (disabled when strength/radius <= 0 so units pass freely through each other).
            if (minerSeparationStrength > 0.001f && minerSeparationRadius > 0.001f)
            {
                Vector3 separation = Vector3.zero;
                for (int i = 0; i < ActiveMiners.Count; i++)
                {
                    MinerMovement other = ActiveMiners[i];
                    if (other == null || other == this)
                    {
                        continue;
                    }

                    Vector3 diff = currentPos - other.transform.position;
                    diff.y = 0f;
                    float sqrDist = diff.sqrMagnitude;
                    float r = minerSeparationRadius;
                    if (sqrDist > 0.0001f && sqrDist < r * r)
                    {
                        float dist = Mathf.Sqrt(sqrDist);
                        separation += (diff / dist) * ((r - dist) / r);
                    }
                }

                if (separation.sqrMagnitude > 0.0001f)
                {
                    moveDir = (moveDir + separation * minerSeparationStrength).normalized;
                }
            }

            float step = Mathf.Min(walkSpeed * deltaTime, remainingDist);
            Vector3 nextPos = currentPos + moveDir * step;

            // Hard non-penetration clamp against Player_Statue pedestal.
            if (statueTransform != null && statueHardRadius > 0.1f)
            {
                Vector3 nextFromStatue = nextPos - statueTransform.position;
                nextFromStatue.y = 0f;
                float nextDist = nextFromStatue.magnitude;
                if (nextDist < statueHardRadius && nextDist > 0.0001f)
                {
                    Vector3 pushed = statueTransform.position + (nextFromStatue / nextDist) * statueHardRadius;
                    nextPos.x = pushed.x;
                    nextPos.z = pushed.z;
                }
            }

            nextPos.z = Mathf.Clamp(nextPos.z, minWalkableZ, maxWalkableZ);

            if (terrainSampler != null)
            {
                float sampledY = terrainSampler.SampleHeight(nextPos);
                nextPos.y = Mathf.Lerp(currentPos.y, sampledY, Mathf.Clamp01(deltaTime * 16f));
            }

            transform.position = nextPos;
            currentSpeed = step / Mathf.Max(0.0001f, deltaTime);

            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Mathf.Clamp01(rotationSpeed * deltaTime));
            return false;
        }
    }
}
