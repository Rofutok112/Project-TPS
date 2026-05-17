using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
[DefaultExecutionOrder(100)]
public sealed class CharacterLockOnAimPresenter : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterMotor motor;
    [SerializeField] private CharacterLockOnController lockOnController;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private bool useCameraWhenUnlocked = true;
    [SerializeField] private float cameraLookDistance = 18f;
    [SerializeField] private float cameraVerticalWeight = 0.2f;
    [SerializeField] private float lookWeight = 1f;
    [SerializeField] private float bodyWeight = 0f;
    [SerializeField] private float headWeight = 0f;
    [SerializeField] private float eyesWeight = 0f;
    [SerializeField] private float cameraHeadWeight = 0f;
    [SerializeField] private float cameraEyesWeight = 0f;
    [SerializeField] private float clampWeight = 0.35f;
    [SerializeField] private float blendSpeed = 10f;
    [SerializeField] private float spineWeight = 0.35f;
    [SerializeField] private float chestWeight = 0.75f;
    [SerializeField] private float maxUpperBodyAngle = 85f;
    [SerializeField] private float fullWeightFacingAngle = 135f;
    [SerializeField] private float maxFacingAngle = 165f;

    private float currentWeight;
    private Transform spine;
    private Transform chest;

    private void Reset()
    {
        animator = GetComponent<Animator>();
        motor = GetComponent<CharacterMotor>();
        lockOnController = GetComponent<CharacterLockOnController>();
        cameraTransform = Camera.main != null ? Camera.main.transform : null;
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

        if (lockOnController == null)
        {
            lockOnController = GetComponent<CharacterLockOnController>();
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        CacheBones();
    }

    private void LateUpdate()
    {
        if (animator == null || !animator.isHuman)
        {
            return;
        }

        Vector3 targetPosition = ResolveTargetPosition(out bool hasTarget, out _);
        float targetWeight = hasTarget ? lookWeight * ResolveFacingWeight(targetPosition) : 0f;
        currentWeight = Mathf.MoveTowards(currentWeight, targetWeight, blendSpeed * Time.deltaTime);
        if (currentWeight <= 0.001f || !hasTarget)
        {
            return;
        }

        ApplyUpperBodyAim(spine, targetPosition, spineWeight);
        ApplyUpperBodyAim(chest, targetPosition, chestWeight);
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || !animator.isHuman)
        {
            return;
        }

        Vector3 targetPosition = ResolveTargetPosition(out bool hasTarget, out bool isCameraTarget);
        if (currentWeight <= 0f || !hasTarget)
        {
            return;
        }

        float resolvedHeadWeight = isCameraTarget ? cameraHeadWeight : headWeight;
        float resolvedEyesWeight = isCameraTarget ? cameraEyesWeight : eyesWeight;
        animator.SetLookAtWeight(currentWeight, bodyWeight, resolvedHeadWeight, resolvedEyesWeight, clampWeight);
        animator.SetLookAtPosition(targetPosition);
    }

    private Vector3 ResolveTargetPosition(out bool hasTarget, out bool isCameraTarget)
    {
        isCameraTarget = false;

        if (lockOnController != null && lockOnController.CurrentTarget != null)
        {
            hasTarget = true;
            return lockOnController.CurrentTarget.position;
        }

        if (motor != null && motor.LockOnTarget != null)
        {
            hasTarget = true;
            return motor.LockOnTarget.position;
        }

        if (useCameraWhenUnlocked && cameraTransform != null && (motor == null || motor.LockOnTarget == null))
        {
            hasTarget = true;
            isCameraTarget = true;
            Vector3 targetPosition = cameraTransform.position + cameraTransform.forward * cameraLookDistance;
            float neutralHeight = chest != null
                ? chest.position.y
                : transform.position.y + 1.35f;
            targetPosition.y = Mathf.Lerp(neutralHeight, targetPosition.y, cameraVerticalWeight);
            return targetPosition;
        }

        hasTarget = false;
        return Vector3.zero;
    }

    private void CacheBones()
    {
        if (animator == null || !animator.isHuman)
        {
            return;
        }

        spine = animator.GetBoneTransform(HumanBodyBones.Spine);
        chest = animator.GetBoneTransform(HumanBodyBones.Chest);
    }

    private void ApplyUpperBodyAim(Transform bone, Vector3 targetPosition, float weight)
    {
        if (bone == null || weight <= 0f)
        {
            return;
        }

        Vector3 direction = targetPosition - bone.position;
        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion delta = Quaternion.FromToRotation(bone.forward, direction.normalized);
        delta.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f)
        {
            angle -= 360f;
        }

        angle = Mathf.Clamp(angle, -maxUpperBodyAngle, maxUpperBodyAngle);
        Quaternion limitedDelta = Quaternion.AngleAxis(angle * weight * currentWeight, axis);
        bone.rotation = limitedDelta * bone.rotation;
    }

    private float ResolveFacingWeight(Vector3 targetPosition)
    {
        Transform basis = motor != null ? motor.transform : transform;
        Vector3 forward = basis.forward;
        Vector3 direction = targetPosition - basis.position;
        forward.y = 0f;
        direction.y = 0f;
        if (forward.sqrMagnitude < 0.001f || direction.sqrMagnitude < 0.001f)
        {
            return 1f;
        }

        float angle = Vector3.Angle(forward.normalized, direction.normalized);
        if (angle <= fullWeightFacingAngle)
        {
            return 1f;
        }

        if (angle >= maxFacingAngle)
        {
            return 0f;
        }

        return Mathf.InverseLerp(maxFacingAngle, fullWeightFacingAngle, angle);
    }
}
