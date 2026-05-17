using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class CharacterLookAtPresenter : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private bool enabledWhileAirborne = true;
    [SerializeField] private float lookDistance = 18f;
    [SerializeField] private float cameraVerticalWeight = 0.2f;
    [SerializeField] private float weightBlendSpeed = 8f;
    [SerializeField] private float totalWeight = 1f;
    [SerializeField] private float bodyWeight = 0f;
    [SerializeField] private float headWeight = 0.82f;
    [SerializeField] private float eyesWeight = 0.35f;
    [SerializeField] private float clampWeight = 0.55f;
    [SerializeField] private float fullWeightFacingAngle = 135f;
    [SerializeField] private float maxFacingAngle = 165f;

    private CharacterMotor motor;
    private CharacterLockOnAimPresenter aimPresenter;
    private float currentWeight;

    private void Reset()
    {
        animator = GetComponent<Animator>();
        cameraTransform = Camera.main != null ? Camera.main.transform : null;
        motor = GetComponent<CharacterMotor>();
        aimPresenter = GetComponent<CharacterLockOnAimPresenter>();
    }

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        motor = GetComponent<CharacterMotor>();
        aimPresenter = GetComponent<CharacterLockOnAimPresenter>();
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || !animator.isHuman || cameraTransform == null)
        {
            return;
        }

        if (motor != null && motor.LockOnTarget != null)
        {
            currentWeight = 0f;
            return;
        }

        bool canLook = enabledWhileAirborne || motor == null || motor.IsGrounded;
        if (aimPresenter != null && aimPresenter.enabled)
        {
            canLook = false;
        }

        Vector3 lookTarget = cameraTransform.position + cameraTransform.forward * lookDistance;
        float neutralHeight = transform.position.y + 1.35f;
        lookTarget.y = Mathf.Lerp(neutralHeight, lookTarget.y, cameraVerticalWeight);
        float targetWeight = canLook ? totalWeight * ResolveFacingWeight(lookTarget) : 0f;
        currentWeight = Mathf.MoveTowards(currentWeight, targetWeight, weightBlendSpeed * Time.deltaTime);

        animator.SetLookAtWeight(
            currentWeight,
            bodyWeight,
            headWeight,
            eyesWeight,
            clampWeight);
        animator.SetLookAtPosition(lookTarget);
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
