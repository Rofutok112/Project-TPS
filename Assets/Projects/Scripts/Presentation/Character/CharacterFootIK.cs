using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class CharacterFootIK : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterMotor motor;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private bool disableWhileAirborne = true;
    [SerializeField] private float raycastOriginHeight = 0.45f;
    [SerializeField] private float raycastDistance = 1.15f;
    [SerializeField] private float footOffset = 0.04f;
    [SerializeField] private float positionWeight = 1f;
    [SerializeField] private float rotationWeight = 0.85f;
    [SerializeField] private float weightBlendSpeed = 12f;
    [SerializeField] private float stationaryMaxSpeed = 0.12f;
    [SerializeField] private float maxGroundAngle = 62f;

    private float currentWeight;

    private void Reset()
    {
        animator = GetComponent<Animator>();
        motor = GetComponent<CharacterMotor>();
    }

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (motor == null)
        {
            motor = GetComponent<CharacterMotor>();
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || !animator.isHuman)
        {
            return;
        }

        bool canUseIk = (!disableWhileAirborne || motor == null || motor.IsGrounded) && IsStationary();
        float targetWeight = canUseIk ? 1f : 0f;
        currentWeight = Mathf.MoveTowards(currentWeight, targetWeight, weightBlendSpeed * Time.deltaTime);

        ApplyFootIK(AvatarIKGoal.LeftFoot, HumanBodyBones.LeftFoot, positionWeight, rotationWeight);
        ApplyFootIK(AvatarIKGoal.RightFoot, HumanBodyBones.RightFoot, positionWeight, rotationWeight);
    }

    private void ApplyFootIK(
        AvatarIKGoal goal,
        HumanBodyBones footBone,
        float targetPositionWeight,
        float targetRotationWeight)
    {
        Transform foot = animator.GetBoneTransform(footBone);
        if (foot == null)
        {
            animator.SetIKPositionWeight(goal, 0f);
            animator.SetIKRotationWeight(goal, 0f);
            return;
        }

        Vector3 rayOrigin = foot.position + Vector3.up * raycastOriginHeight;
        float rayLength = raycastOriginHeight + raycastDistance;
        if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayLength, groundMask, QueryTriggerInteraction.Ignore)
            || Vector3.Angle(hit.normal, Vector3.up) > maxGroundAngle)
        {
            animator.SetIKPositionWeight(goal, 0f);
            animator.SetIKRotationWeight(goal, 0f);
            return;
        }

        Vector3 footForward = Vector3.ProjectOnPlane(foot.forward, hit.normal);
        if (footForward.sqrMagnitude < 0.0001f)
        {
            footForward = Vector3.ProjectOnPlane(transform.forward, hit.normal);
        }

        Quaternion targetRotation = Quaternion.LookRotation(footForward.normalized, hit.normal);
        Vector3 targetPosition = hit.point + hit.normal * footOffset;

        float blendedPositionWeight = currentWeight * targetPositionWeight;
        float blendedRotationWeight = currentWeight * targetRotationWeight;
        animator.SetIKPositionWeight(goal, blendedPositionWeight);
        animator.SetIKRotationWeight(goal, blendedRotationWeight);
        animator.SetIKPosition(goal, targetPosition);
        animator.SetIKRotation(goal, targetRotation);
    }

    private bool IsStationary()
    {
        if (motor == null)
        {
            return true;
        }

        Vector3 velocity = motor.Velocity;
        velocity.y = 0f;
        return velocity.sqrMagnitude <= stationaryMaxSpeed * stationaryMaxSpeed;
    }
}
