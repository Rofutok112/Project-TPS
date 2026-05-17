using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public sealed class CharacterMotor : MonoBehaviour
{
    [Header("Adapters")]
    [SerializeField] private CharacterInputReader inputReader;
    [SerializeField] private CharacterClimbProbe climbProbe;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Transform lockOnTarget;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundProbeDistance = 0.18f;

    [Header("Use Case Settings")]
    [SerializeField] private CharacterMovementSettings movementSettings = new CharacterMovementSettings();

    public CharacterLocomotionSnapshot Snapshot { get; private set; } = CharacterLocomotionSnapshot.Idle;
    public CharacterLocomotionMode State => Snapshot.Mode;
    public Vector3 Velocity => Snapshot.Velocity;
    public float Drive => Snapshot.Drive;
    public float DriveNormalized => Snapshot.DriveNormalized;
    public bool IsGrounded => Snapshot.IsGrounded;
    public bool IsClimbing => isClimbing;
    public Transform LockOnTarget => lockOnTarget;
    public ClimbPlan CurrentClimbPlan => climbPlan;
    public float ClimbNormalizedTime => isClimbing && climbPlan.Duration > 0f
        ? Mathf.Clamp01(climbTimer / climbPlan.Duration)
        : 0f;

    private CharacterController controller;
    private CharacterLocomotionUseCase locomotionUseCase;
    private CharacterClimbUseCase climbUseCase;
    private bool isClimbing;
    private bool disabledControllerForClimb;
    private ClimbPlan climbPlan;
    private float climbTimer;

    private void OnDisable()
    {
        RestoreControllerAfterClimb();
    }

    public void SetLockOnTarget(Transform target)
    {
        lockOnTarget = target;
    }

    private void Reset()
    {
        inputReader = GetComponent<CharacterInputReader>();
        climbProbe = GetComponent<CharacterClimbProbe>();
        visualRoot = transform;
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (inputReader == null)
        {
            inputReader = GetComponent<CharacterInputReader>();
        }

        if (climbProbe == null)
        {
            climbProbe = GetComponent<CharacterClimbProbe>();
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (movementSettings == null)
        {
            movementSettings = new CharacterMovementSettings();
        }

        if (movementSettings.climb == null)
        {
            movementSettings.climb = new CharacterClimbSettings();
        }

        locomotionUseCase = new CharacterLocomotionUseCase(movementSettings);
        climbUseCase = new CharacterClimbUseCase();
        Snapshot = locomotionUseCase.Snapshot;
    }

    private void Update()
    {
        if (locomotionUseCase == null)
        {
            locomotionUseCase = new CharacterLocomotionUseCase(movementSettings);
        }

        if (climbUseCase == null)
        {
            climbUseCase = new CharacterClimbUseCase();
        }

        float deltaTime = Time.deltaTime;
        CharacterInputSnapshot input = inputReader != null ? inputReader.ReadSnapshot() : CharacterInputSnapshot.None;
        Vector3 moveDirection = ResolveCameraRelativeDirection(input.Move);
        Vector3 facingOverride = Vector3.zero;

        Transform facingRoot = visualRoot != null ? visualRoot : transform;
        bool groundedBeforeMove = controller.isGrounded || ProbeGround();

        if (isClimbing)
        {
            UpdateClimb(deltaTime);
            return;
        }

        if (input.JumpPressed && TryStartClimb(moveDirection, facingRoot.forward, false))
        {
            UpdateClimb(0f);
            return;
        }

        if (CanAutoStepClimb(movementSettings.climb, input, moveDirection, groundedBeforeMove)
            && TryStartClimb(moveDirection, facingRoot.forward, true))
        {
            UpdateClimb(0f);
            return;
        }

        CharacterLocomotionSnapshot plannedSnapshot = locomotionUseCase.Tick(
            movementSettings,
            input,
            groundedBeforeMove,
            moveDirection,
            facingRoot.forward,
            facingOverride,
            deltaTime);

        if (CanAutoStepClimb(movementSettings.climb, plannedSnapshot, input, moveDirection, groundedBeforeMove)
            && TryStartClimb(moveDirection, facingRoot.forward, true))
        {
            UpdateClimb(0f);
            return;
        }

        CollisionFlags flags = controller.Move(plannedSnapshot.Velocity * deltaTime);
        Vector3 resolvedVelocity = plannedSnapshot.Velocity;
        bool groundedAfterMove = controller.isGrounded || (flags & CollisionFlags.Below) != 0 || ProbeGround();
        if (groundedAfterMove && resolvedVelocity.y < 0f)
        {
            resolvedVelocity.y = -2f;
        }

        locomotionUseCase.AcceptControllerResult(resolvedVelocity, groundedAfterMove);
        Snapshot = locomotionUseCase.Snapshot;
        RotateVisual(Snapshot.FacingDirection, deltaTime);
    }

    private Vector3 ResolveCameraRelativeDirection(Vector2 input)
    {
        input = Vector2.ClampMagnitude(input, 1f);
        if (input.sqrMagnitude < 0.001f)
        {
            return Vector3.zero;
        }

        Transform basis = cameraTransform != null ? cameraTransform : transform;
        Vector3 forward = basis.forward;
        Vector3 right = basis.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        return Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);
    }

    private bool ProbeGround()
    {
        if (controller == null)
        {
            return false;
        }

        Vector3 center = transform.TransformPoint(controller.center);
        float radius = Mathf.Max(0.02f, controller.radius - controller.skinWidth);
        float halfHeight = Mathf.Max(controller.height * 0.5f, radius);
        Vector3 sphereOrigin = center + Vector3.down * (halfHeight - radius);
        return Physics.SphereCast(
            sphereOrigin + Vector3.up * 0.04f,
            radius,
            Vector3.down,
            out _,
            groundProbeDistance + 0.04f,
            groundMask,
            QueryTriggerInteraction.Ignore);
    }

    private bool TryStartClimb(Vector3 moveDirection, Vector3 fallbackForward, bool dashVaultOnly)
    {
        Vector3 probeDirection = moveDirection.sqrMagnitude > 0.001f ? moveDirection : fallbackForward;
        probeDirection.y = 0f;
        if (probeDirection.sqrMagnitude < 0.001f)
        {
            return false;
        }

        probeDirection.Normalize();
        CharacterClimbSettings climbSettings = movementSettings.climb;
        if (climbProbe == null
            || !climbProbe.TryProbe(climbSettings, probeDirection, out ClimbCandidate candidate)
            || (dashVaultOnly && !CanUseAutoStepClimb(climbSettings, candidate)))
        {
            return false;
        }

        bool forceDashVault = dashVaultOnly;
        if (!climbUseCase.TryCreatePlan(climbSettings, candidate, forceDashVault, out ClimbPlan plan)
            || !climbProbe.HasCapsuleRoom(plan.TargetPosition))
        {
            return false;
        }

        isClimbing = true;
        climbPlan = plan;
        climbTimer = 0f;
        DisableControllerForClimb();
        Snapshot = new CharacterLocomotionSnapshot(
            climbPlan.Mode,
            Vector3.zero,
            climbPlan.FacingDirection,
            climbPlan.FacingDirection,
            Snapshot.Drive,
            Snapshot.DriveNormalized,
            false);
        return true;
    }

    private static bool CanUseAutoStepClimb(CharacterClimbSettings settings, ClimbCandidate candidate)
    {
        return settings != null
            && candidate.Height >= settings.minHeight
            && candidate.Height <= candidate.ActorHeight * settings.autoStepClimbMaxHeightRatio;
    }

    private static bool CanAutoStepClimb(
        CharacterClimbSettings settings,
        CharacterLocomotionSnapshot snapshot,
        CharacterInputSnapshot input,
        Vector3 moveDirection,
        bool grounded)
    {
        if (settings == null || !settings.enableDashVault)
        {
            return false;
        }

        if (!grounded && !snapshot.IsGrounded)
        {
            return false;
        }

        return CanAutoStepClimb(settings, input, moveDirection, true)
            || snapshot.Mode == CharacterLocomotionMode.CyberSprint
            || snapshot.Mode == CharacterLocomotionMode.QuickBoost
            || snapshot.Mode == CharacterLocomotionMode.AssaultBoost;
    }

    private static bool CanAutoStepClimb(
        CharacterClimbSettings settings,
        CharacterInputSnapshot input,
        Vector3 moveDirection,
        bool grounded)
    {
        return settings != null
            && settings.enableDashVault
            && grounded
            && input.SprintHeld
            && input.Move.y > 0.25f
            && moveDirection.sqrMagnitude >= 0.25f;
    }

    private void UpdateClimb(float deltaTime)
    {
        climbTimer += deltaTime;
        float normalizedTime = climbPlan.Duration > 0f ? Mathf.Clamp01(climbTimer / climbPlan.Duration) : 1f;
        Vector3 nextPosition = climbUseCase.EvaluatePosition(climbPlan, normalizedTime);
        ApplyClimbPosition(nextPosition);
        RotateVisual(climbPlan.FacingDirection, deltaTime);

        bool finished = normalizedTime >= 1f;
        Vector3 exitVelocity = finished
            ? climbUseCase.ResolveExitVelocity(movementSettings, climbPlan)
            : Vector3.zero;
        Vector3 velocity = finished
            ? exitVelocity
            : (climbPlan.TargetPosition - climbPlan.StartPosition) / Mathf.Max(climbPlan.Duration, 0.01f);
        Snapshot = new CharacterLocomotionSnapshot(
            finished ? CharacterLocomotionMode.Grounded : climbPlan.Mode,
            velocity,
            climbPlan.FacingDirection,
            climbPlan.FacingDirection,
            Snapshot.Drive,
            Snapshot.DriveNormalized,
            finished);

        if (!finished)
        {
            return;
        }

        isClimbing = false;
        RestoreControllerAfterClimb();
        locomotionUseCase.AcceptControllerResult(exitVelocity, true);
        Snapshot = locomotionUseCase.Snapshot;
    }

    private void DisableControllerForClimb()
    {
        if (controller == null || !controller.enabled)
        {
            return;
        }

        controller.enabled = false;
        disabledControllerForClimb = true;
    }

    private void RestoreControllerAfterClimb()
    {
        if (!disabledControllerForClimb || controller == null)
        {
            return;
        }

        controller.enabled = true;
        disabledControllerForClimb = false;
    }

    private void ApplyClimbPosition(Vector3 nextPosition)
    {
        if (disabledControllerForClimb)
        {
            transform.position = nextPosition;
            return;
        }

        controller.Move(nextPosition - transform.position);
    }

    private void RotateVisual(Vector3 facingDirection, float deltaTime)
    {
        if (facingDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        float sharpness = State == CharacterLocomotionMode.QuickBoost
            ? movementSettings.quickBoostTurnSharpness
            : movementSettings.rotationSharpness;
        Quaternion targetRotation = Quaternion.LookRotation(facingDirection.normalized, Vector3.up);
        Transform root = visualRoot != null ? visualRoot : transform;
        root.rotation = Quaternion.Slerp(root.rotation, targetRotation, 1f - Mathf.Exp(-sharpness * deltaTime));
    }
}
