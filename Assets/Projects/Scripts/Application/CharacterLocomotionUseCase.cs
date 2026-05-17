using UnityEngine;

public sealed class CharacterLocomotionUseCase
{
    private Vector3 velocity;
    private Vector3 quickBoostDirection;
    private CharacterLocomotionMode mode;
    private float drive;
    private float driveRecoveryTimer;
    private float quickBoostTimer;
    private float jumpMomentumTimer;
    private float overheatTimer;
    private float hardLandingTimer;
    private bool wasGrounded;

    public CharacterLocomotionUseCase(CharacterMovementSettings settings)
    {
        Reset(settings);
    }

    public CharacterLocomotionSnapshot Snapshot { get; private set; } = CharacterLocomotionSnapshot.Idle;

    public void Reset(CharacterMovementSettings settings)
    {
        velocity = Vector3.zero;
        quickBoostDirection = Vector3.forward;
        mode = CharacterLocomotionMode.Grounded;
        drive = settings != null ? settings.driveMax : 0f;
        driveRecoveryTimer = 0f;
        quickBoostTimer = 0f;
        jumpMomentumTimer = 0f;
        overheatTimer = 0f;
        hardLandingTimer = 0f;
        wasGrounded = false;
        Snapshot = new CharacterLocomotionSnapshot(mode, velocity, Vector3.zero, Vector3.forward, drive, 1f, true);
    }

    public CharacterLocomotionSnapshot Tick(
        CharacterMovementSettings settings,
        CharacterInputSnapshot input,
        bool grounded,
        Vector3 moveDirection,
        Vector3 fallbackForward,
        Vector3 facingOverride,
        float deltaTime)
    {
        UpdateTimers(deltaTime);
        UpdateGrounding(settings, grounded);

        if (CanAct && input.QuickBoostPressed && TrySpendDrive(settings, settings.quickBoostDriveCost))
        {
            StartQuickBoost(settings, moveDirection, fallbackForward);
        }

        if (CanAct && grounded && input.JumpPressed)
        {
            velocity.y = settings.jumpSpeed;
            jumpMomentumTimer = settings.jumpMomentumPreserveDuration;
            grounded = false;
        }

        if (quickBoostTimer > 0f)
        {
            ApplyQuickBoost(settings, deltaTime, grounded);
        }
        else if (CanAct && input.AssaultBoostHeld && moveDirection.sqrMagnitude > 0.001f && drive > 0f)
        {
            ApplyAssaultBoost(settings, moveDirection, deltaTime);
        }
        else
        {
            ApplyRegularMovement(settings, moveDirection, input.SprintHeld, grounded, deltaTime);
        }

        ApplyVerticalMovement(settings, grounded, deltaTime);
        RecoverDrive(settings, deltaTime);
        UpdateMode(settings, grounded, input.SprintHeld, input.AssaultBoostHeld);

        Vector3 facingDirection = ResolveFacingDirection(moveDirection, fallbackForward, facingOverride);
        Snapshot = new CharacterLocomotionSnapshot(
            mode,
            velocity,
            moveDirection,
            facingDirection,
            drive,
            settings.driveMax > 0f ? drive / settings.driveMax : 0f,
            grounded);

        return Snapshot;
    }

    public void AcceptControllerResult(Vector3 movedVelocity, bool groundedAfterMove)
    {
        velocity = movedVelocity;
        Snapshot = new CharacterLocomotionSnapshot(
            Snapshot.Mode,
            velocity,
            Snapshot.MoveDirection,
            Snapshot.FacingDirection,
            Snapshot.Drive,
            Snapshot.DriveNormalized,
            groundedAfterMove);
    }

    private bool CanAct => overheatTimer <= 0f && hardLandingTimer <= 0f;

    private void ApplyRegularMovement(
        CharacterMovementSettings settings,
        Vector3 moveDirection,
        bool sprintHeld,
        bool grounded,
        float deltaTime)
    {
        bool wantsSprint = sprintHeld && grounded && moveDirection.sqrMagnitude > 0.001f && drive > 0f;
        if (!grounded && jumpMomentumTimer > 0f)
        {
            return;
        }

        bool isOverheated = overheatTimer > 0f;
        float maxSpeed = isOverheated
            ? settings.overheatMaxSpeed
            : wantsSprint ? settings.sprintMaxSpeed : settings.humanMaxSpeed;
        float acceleration = isOverheated
            ? settings.overheatAcceleration
            : grounded
            ? (wantsSprint ? settings.sprintAcceleration : settings.humanAcceleration)
            : settings.airAcceleration;

        if (wantsSprint)
        {
            SpendDrive(settings, settings.sprintDrivePerSecond * deltaTime);
        }

        Vector3 horizontalVelocity = GetHorizontalVelocity();
        Vector3 targetVelocity = moveDirection * maxSpeed;
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, acceleration * deltaTime);

        float friction = grounded ? settings.groundFriction : settings.airDrag;
        if (moveDirection.sqrMagnitude < 0.001f || !grounded)
        {
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, Vector3.zero, friction * deltaTime);
        }

        velocity.x = horizontalVelocity.x;
        velocity.z = horizontalVelocity.z;
    }

    private void ApplyQuickBoost(CharacterMovementSettings settings, float deltaTime, bool grounded)
    {
        quickBoostTimer -= deltaTime;
        Vector3 horizontalVelocity = quickBoostDirection * settings.quickBoostSpeed;
        velocity.x = horizontalVelocity.x;
        velocity.z = horizontalVelocity.z;

        if (!grounded)
        {
            velocity.y = Mathf.Max(velocity.y, -2f);
        }
    }

    private void ApplyAssaultBoost(CharacterMovementSettings settings, Vector3 moveDirection, float deltaTime)
    {
        SpendDrive(settings, settings.assaultBoostDrivePerSecond * deltaTime);
        Vector3 targetVelocity = moveDirection * settings.assaultBoostSpeed;
        Vector3 horizontalVelocity = Vector3.MoveTowards(
            GetHorizontalVelocity(),
            targetVelocity,
            settings.assaultBoostAcceleration * deltaTime);
        velocity.x = horizontalVelocity.x;
        velocity.z = horizontalVelocity.z;
    }

    private void ApplyVerticalMovement(CharacterMovementSettings settings, bool grounded, float deltaTime)
    {
        if (grounded && velocity.y < 0f)
        {
            velocity.y = -2f;
            return;
        }

        velocity.y = Mathf.Max(velocity.y - settings.gravity * deltaTime, -settings.maxFallSpeed);
    }

    private void StartQuickBoost(CharacterMovementSettings settings, Vector3 moveDirection, Vector3 fallbackForward)
    {
        quickBoostDirection = moveDirection.sqrMagnitude > 0.001f ? moveDirection.normalized : fallbackForward.normalized;
        if (quickBoostDirection.sqrMagnitude < 0.001f)
        {
            quickBoostDirection = Vector3.forward;
        }

        quickBoostTimer = settings.quickBoostDuration;
    }

    private void UpdateGrounding(CharacterMovementSettings settings, bool grounded)
    {
        if (grounded && !wasGrounded)
        {
            float impactSpeed = Mathf.Abs(Mathf.Min(velocity.y, 0f));
            if (impactSpeed >= settings.hardLandingSpeed)
            {
                hardLandingTimer = settings.hardLandingDuration;
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

        if (jumpMomentumTimer > 0f)
        {
            jumpMomentumTimer -= deltaTime;
        }

        if (driveRecoveryTimer > 0f)
        {
            driveRecoveryTimer -= deltaTime;
        }
    }

    private void RecoverDrive(CharacterMovementSettings settings, float deltaTime)
    {
        if (driveRecoveryTimer > 0f || quickBoostTimer > 0f || overheatTimer > 0f)
        {
            return;
        }

        drive = Mathf.Min(drive + settings.driveRecoveryPerSecond * deltaTime, settings.driveMax);
    }

    private bool TrySpendDrive(CharacterMovementSettings settings, float amount)
    {
        if (drive < amount || overheatTimer > 0f)
        {
            return false;
        }

        SpendDrive(settings, amount);
        return true;
    }

    private void SpendDrive(CharacterMovementSettings settings, float amount)
    {
        drive = Mathf.Max(0f, drive - amount);
        driveRecoveryTimer = settings.driveRecoveryDelay;

        if (drive <= 0f)
        {
            overheatTimer = settings.overheatDuration;
        }
    }

    private void UpdateMode(
        CharacterMovementSettings settings,
        bool grounded,
        bool sprintHeld,
        bool assaultHeld)
    {
        if (overheatTimer > 0f)
        {
            mode = CharacterLocomotionMode.Overheated;
        }
        else if (hardLandingTimer > 0f)
        {
            mode = CharacterLocomotionMode.HardLanding;
        }
        else if (quickBoostTimer > 0f)
        {
            mode = CharacterLocomotionMode.QuickBoost;
        }
        else if (assaultHeld && drive > 0f && GetHorizontalVelocity().sqrMagnitude > settings.humanMaxSpeed * settings.humanMaxSpeed)
        {
            mode = CharacterLocomotionMode.AssaultBoost;
        }
        else if (!grounded)
        {
            mode = CharacterLocomotionMode.Airborne;
        }
        else if (sprintHeld && GetHorizontalVelocity().sqrMagnitude > settings.humanMaxSpeed * settings.humanMaxSpeed * 0.8f)
        {
            mode = CharacterLocomotionMode.CyberSprint;
        }
        else
        {
            mode = CharacterLocomotionMode.Grounded;
        }
    }

    private Vector3 ResolveFacingDirection(Vector3 moveDirection, Vector3 fallbackForward, Vector3 facingOverride)
    {
        Vector3 facingDirection = moveDirection;
        if (facingOverride.sqrMagnitude > 0.001f)
        {
            facingDirection = facingOverride;
        }
        else if (quickBoostTimer > 0f)
        {
            facingDirection = quickBoostDirection;
        }

        if (facingDirection.sqrMagnitude < 0.001f)
        {
            facingDirection = fallbackForward;
        }

        facingDirection.y = 0f;
        return facingDirection.sqrMagnitude > 0.001f ? facingDirection.normalized : Vector3.forward;
    }

    private Vector3 GetHorizontalVelocity()
    {
        return new Vector3(velocity.x, 0f, velocity.z);
    }
}
