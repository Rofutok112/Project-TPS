using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class CharacterMotor : MonoBehaviour
{
    public enum LocomotionState
    {
        Grounded,
        Airborne,
        CyberSprint,
        QuickBoost,
        AssaultBoost,
        Overheated,
        HardLanding
    }

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Transform lockOnTarget;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference lookAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference sprintAction;
    [SerializeField] private InputActionReference quickBoostAction;
    [SerializeField] private InputActionReference assaultBoostAction;

    [Header("Human Movement")]
    [SerializeField] private float humanMaxSpeed = 5.5f;
    [SerializeField] private float humanAcceleration = 32f;
    [SerializeField] private float groundFriction = 18f;
    [SerializeField] private float rotationSharpness = 16f;

    [Header("Cyber Drive")]
    [SerializeField] private float driveMax = 100f;
    [SerializeField] private float driveRecoveryPerSecond = 28f;
    [SerializeField] private float driveRecoveryDelay = 0.45f;
    [SerializeField] private float sprintMaxSpeed = 11f;
    [SerializeField] private float sprintAcceleration = 52f;
    [SerializeField] private float sprintDrivePerSecond = 18f;
    [SerializeField] private float overheatDuration = 1.2f;

    [Header("Boost")]
    [SerializeField] private float quickBoostSpeed = 18f;
    [SerializeField] private float quickBoostDuration = 0.16f;
    [SerializeField] private float quickBoostDriveCost = 24f;
    [SerializeField] private float quickBoostTurnSharpness = 24f;
    [SerializeField] private float assaultBoostSpeed = 20f;
    [SerializeField] private float assaultBoostAcceleration = 72f;
    [SerializeField] private float assaultBoostDrivePerSecond = 36f;

    [Header("Air Control")]
    [SerializeField] private float jumpSpeed = 7.5f;
    [SerializeField] private float airAcceleration = 18f;
    [SerializeField] private float airDrag = 3.5f;
    [SerializeField] private float gravity = 28f;
    [SerializeField] private float maxFallSpeed = 32f;
    [SerializeField] private float hardLandingSpeed = 18f;
    [SerializeField] private float hardLandingDuration = 0.18f;

    public LocomotionState State { get; private set; }
    public Vector3 Velocity => velocity;
    public float Drive => drive;
    public float DriveNormalized => driveMax > 0f ? drive / driveMax : 0f;
    public bool IsGrounded => controller != null && controller.isGrounded;

    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 quickBoostDirection;
    private float drive;
    private float driveRecoveryTimer;
    private float quickBoostTimer;
    private float overheatTimer;
    private float hardLandingTimer;
    private bool wasGrounded;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        drive = driveMax;

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void OnEnable()
    {
        EnableAction(moveAction);
        EnableAction(lookAction);
        EnableAction(jumpAction);
        EnableAction(sprintAction);
        EnableAction(quickBoostAction);
        EnableAction(assaultBoostAction);
    }

    private void OnDisable()
    {
        DisableAction(moveAction);
        DisableAction(lookAction);
        DisableAction(jumpAction);
        DisableAction(sprintAction);
        DisableAction(quickBoostAction);
        DisableAction(assaultBoostAction);
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        Vector2 moveInput = ReadMove();
        Vector3 moveDirection = GetCameraRelativeDirection(moveInput);
        bool grounded = controller.isGrounded;

        UpdateTimers(deltaTime);
        UpdateGrounding(grounded);

        if (CanAct && WasPressedThisFrame(quickBoostAction) && TrySpendDrive(quickBoostDriveCost))
        {
            StartQuickBoost(moveDirection);
        }

        if (CanAct && grounded && WasPressedThisFrame(jumpAction))
        {
            velocity.y = jumpSpeed;
            grounded = false;
        }

        bool sprintHeld = IsPressed(sprintAction);
        bool assaultHeld = IsPressed(assaultBoostAction);

        if (quickBoostTimer > 0f)
        {
            ApplyQuickBoost(deltaTime);
        }
        else if (CanAct && assaultHeld && moveDirection.sqrMagnitude > 0.001f && drive > 0f)
        {
            ApplyAssaultBoost(moveDirection, deltaTime);
        }
        else
        {
            ApplyRegularMovement(moveDirection, sprintHeld, grounded, deltaTime);
        }

        ApplyVerticalMovement(grounded, deltaTime);
        controller.Move(velocity * deltaTime);
        RotateBody(moveDirection, deltaTime);
        RecoverDrive(deltaTime);
        UpdateState(grounded, sprintHeld, assaultHeld);
    }

    private bool CanAct => overheatTimer <= 0f && hardLandingTimer <= 0f;

    private void ApplyRegularMovement(Vector3 moveDirection, bool sprintHeld, bool grounded, float deltaTime)
    {
        bool wantsSprint = sprintHeld && grounded && moveDirection.sqrMagnitude > 0.001f && drive > 0f;
        float maxSpeed = wantsSprint ? sprintMaxSpeed : humanMaxSpeed;
        float acceleration = grounded ? (wantsSprint ? sprintAcceleration : humanAcceleration) : airAcceleration;

        if (wantsSprint)
        {
            SpendDrive(sprintDrivePerSecond * deltaTime);
        }

        Vector3 horizontalVelocity = GetHorizontalVelocity();
        Vector3 targetVelocity = moveDirection * maxSpeed;
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, acceleration * deltaTime);

        float friction = grounded ? groundFriction : airDrag;
        if (moveDirection.sqrMagnitude < 0.001f || !grounded)
        {
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, Vector3.zero, friction * deltaTime);
        }

        velocity.x = horizontalVelocity.x;
        velocity.z = horizontalVelocity.z;
    }

    private void ApplyQuickBoost(float deltaTime)
    {
        quickBoostTimer -= deltaTime;
        Vector3 horizontalVelocity = quickBoostDirection * quickBoostSpeed;
        velocity.x = horizontalVelocity.x;
        velocity.z = horizontalVelocity.z;

        if (!controller.isGrounded)
        {
            velocity.y = Mathf.Max(velocity.y, -2f);
        }
    }

    private void ApplyAssaultBoost(Vector3 moveDirection, float deltaTime)
    {
        SpendDrive(assaultBoostDrivePerSecond * deltaTime);
        Vector3 targetVelocity = moveDirection * assaultBoostSpeed;
        Vector3 horizontalVelocity = Vector3.MoveTowards(GetHorizontalVelocity(), targetVelocity, assaultBoostAcceleration * deltaTime);
        velocity.x = horizontalVelocity.x;
        velocity.z = horizontalVelocity.z;
    }

    private void ApplyVerticalMovement(bool grounded, float deltaTime)
    {
        if (grounded && velocity.y < 0f)
        {
            velocity.y = -2f;
            return;
        }

        velocity.y = Mathf.Max(velocity.y - gravity * deltaTime, -maxFallSpeed);
    }

    private void StartQuickBoost(Vector3 moveDirection)
    {
        quickBoostDirection = moveDirection.sqrMagnitude > 0.001f ? moveDirection : transform.forward;
        quickBoostTimer = quickBoostDuration;
    }

    private void RotateBody(Vector3 moveDirection, float deltaTime)
    {
        Vector3 facingDirection = moveDirection;

        if (lockOnTarget != null)
        {
            facingDirection = lockOnTarget.position - transform.position;
            facingDirection.y = 0f;
        }
        else if (quickBoostTimer > 0f)
        {
            facingDirection = quickBoostDirection;
        }

        if (facingDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        float sharpness = quickBoostTimer > 0f ? quickBoostTurnSharpness : rotationSharpness;
        Quaternion targetRotation = Quaternion.LookRotation(facingDirection.normalized, Vector3.up);
        Transform root = visualRoot != null ? visualRoot : transform;
        root.rotation = Quaternion.Slerp(root.rotation, targetRotation, 1f - Mathf.Exp(-sharpness * deltaTime));
    }

    private void UpdateGrounding(bool grounded)
    {
        if (grounded && !wasGrounded)
        {
            float impactSpeed = Mathf.Abs(Mathf.Min(velocity.y, 0f));
            if (impactSpeed >= hardLandingSpeed)
            {
                hardLandingTimer = hardLandingDuration;
            }
        }

        wasGrounded = grounded;
    }

    private void UpdateTimers(float deltaTime)
    {
        if (overheatTimer > 0f)
        {
            overheatTimer -= deltaTime;
        }

        if (hardLandingTimer > 0f)
        {
            hardLandingTimer -= deltaTime;
        }

        if (driveRecoveryTimer > 0f)
        {
            driveRecoveryTimer -= deltaTime;
        }
    }

    private void RecoverDrive(float deltaTime)
    {
        if (driveRecoveryTimer > 0f || quickBoostTimer > 0f || overheatTimer > 0f)
        {
            return;
        }

        drive = Mathf.Min(drive + driveRecoveryPerSecond * deltaTime, driveMax);
    }

    private bool TrySpendDrive(float amount)
    {
        if (drive < amount || overheatTimer > 0f)
        {
            return false;
        }

        SpendDrive(amount);
        return true;
    }

    private void SpendDrive(float amount)
    {
        drive = Mathf.Max(0f, drive - amount);
        driveRecoveryTimer = driveRecoveryDelay;

        if (drive <= 0f)
        {
            overheatTimer = overheatDuration;
        }
    }

    private void UpdateState(bool grounded, bool sprintHeld, bool assaultHeld)
    {
        if (overheatTimer > 0f)
        {
            State = LocomotionState.Overheated;
        }
        else if (hardLandingTimer > 0f)
        {
            State = LocomotionState.HardLanding;
        }
        else if (quickBoostTimer > 0f)
        {
            State = LocomotionState.QuickBoost;
        }
        else if (assaultHeld && drive > 0f && GetHorizontalVelocity().sqrMagnitude > humanMaxSpeed * humanMaxSpeed)
        {
            State = LocomotionState.AssaultBoost;
        }
        else if (!grounded)
        {
            State = LocomotionState.Airborne;
        }
        else if (sprintHeld && GetHorizontalVelocity().sqrMagnitude > humanMaxSpeed * humanMaxSpeed * 0.8f)
        {
            State = LocomotionState.CyberSprint;
        }
        else
        {
            State = LocomotionState.Grounded;
        }
    }

    private Vector3 GetCameraRelativeDirection(Vector2 input)
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

    private Vector3 GetHorizontalVelocity()
    {
        return new Vector3(velocity.x, 0f, velocity.z);
    }

    private Vector2 ReadMove()
    {
        return moveAction != null && moveAction.action != null
            ? Vector2.ClampMagnitude(moveAction.action.ReadValue<Vector2>(), 1f)
            : Vector2.zero;
    }

    private static void EnableAction(InputActionReference actionReference)
    {
        if (actionReference != null && actionReference.action != null)
        {
            actionReference.action.Enable();
        }
    }

    private static void DisableAction(InputActionReference actionReference)
    {
        if (actionReference != null && actionReference.action != null)
        {
            actionReference.action.Disable();
        }
    }

    private static bool IsPressed(InputActionReference actionReference)
    {
        return actionReference != null
            && actionReference.action != null
            && actionReference.action.IsPressed();
    }

    private static bool WasPressedThisFrame(InputActionReference actionReference)
    {
        return actionReference != null
            && actionReference.action != null
            && actionReference.action.WasPressedThisFrame();
    }
}
