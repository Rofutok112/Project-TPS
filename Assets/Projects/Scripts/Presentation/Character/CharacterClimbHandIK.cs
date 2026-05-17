using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class CharacterClimbHandIK : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterMotor motor;
    [SerializeField] private float handSpacing = 0.42f;
    [SerializeField] private float ledgeBackOffset = 0.08f;
    [SerializeField] private float ledgeHeightOffset = 0.04f;
    [SerializeField] private float highClimbWeight = 1f;
    [SerializeField] private float lowClimbWeight = 0.85f;
    [SerializeField] private float stepClimbWeight = 0.35f;
    [SerializeField] private float weightBlendSpeed = 12f;

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
        if (animator == null || !animator.isHuman || motor == null || !motor.IsClimbing)
        {
            ClearHandIK();
            return;
        }

        ClimbPlan plan = motor.CurrentClimbPlan;
        float targetWeight = GetTargetWeight(plan.Mode, motor.ClimbNormalizedTime);
        currentWeight = Mathf.MoveTowards(currentWeight, targetWeight, weightBlendSpeed * Time.deltaTime);
        if (currentWeight <= 0.001f)
        {
            ClearHandIK();
            return;
        }

        Vector3 forward = plan.FacingDirection;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
        {
            ClearHandIK();
            return;
        }

        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 basePosition = plan.LedgeTopPoint
            - forward * ledgeBackOffset
            + Vector3.up * ledgeHeightOffset;
        Quaternion handRotation = Quaternion.LookRotation(forward, Vector3.up);

        ApplyHandIK(AvatarIKGoal.LeftHand, basePosition - right * (handSpacing * 0.5f), handRotation);
        ApplyHandIK(AvatarIKGoal.RightHand, basePosition + right * (handSpacing * 0.5f), handRotation);
    }

    private float GetTargetWeight(CharacterLocomotionMode mode, float normalizedTime)
    {
        float maxWeight = mode == CharacterLocomotionMode.DashVault
            ? stepClimbWeight
            : mode == CharacterLocomotionMode.HighClimb
            ? highClimbWeight
            : mode == CharacterLocomotionMode.LowClimb
                ? lowClimbWeight
                : stepClimbWeight;
        float fadeIn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.08f, 0.24f, normalizedTime));
        float fadeOut = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.78f, 0.96f, normalizedTime));
        return maxWeight * Mathf.Min(fadeIn, fadeOut);
    }

    private void ApplyHandIK(AvatarIKGoal goal, Vector3 position, Quaternion rotation)
    {
        animator.SetIKPositionWeight(goal, currentWeight);
        animator.SetIKRotationWeight(goal, currentWeight);
        animator.SetIKPosition(goal, position);
        animator.SetIKRotation(goal, rotation);
    }

    private void ClearHandIK()
    {
        currentWeight = Mathf.MoveTowards(currentWeight, 0f, weightBlendSpeed * Time.deltaTime);
        animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, currentWeight);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, currentWeight);
        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, currentWeight);
        animator.SetIKRotationWeight(AvatarIKGoal.RightHand, currentWeight);
    }
}
