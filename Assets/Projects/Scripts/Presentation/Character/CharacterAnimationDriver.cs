using UnityEngine;

[DisallowMultipleComponent]
public sealed class CharacterAnimationDriver : MonoBehaviour
{
    [SerializeField] private CharacterMotor motor;
    [SerializeField] private Animator animator;
    [SerializeField] private float parameterDampTime = 0.08f;
    [SerializeField] private float groundedGraceTime = 0.14f;
    [SerializeField] private float groundedGraceMaxFallSpeed = 5f;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");
    private static readonly int DriveHash = Animator.StringToHash("Drive");
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int CyberSprintHash = Animator.StringToHash("CyberSprint");
    private static readonly int QuickBoostHash = Animator.StringToHash("QuickBoost");
    private static readonly int AssaultBoostHash = Animator.StringToHash("AssaultBoost");
    private static readonly int DashVaultHash = Animator.StringToHash("DashVault");
    private static readonly int LowVaultHash = Animator.StringToHash("LowVault");
    private static readonly int SpaceClimbHash = Animator.StringToHash("SpaceClimb");
    private static readonly int OverheatedHash = Animator.StringToHash("Overheated");
    private static readonly int OverheatedWalkHash = Animator.StringToHash("OverheatedWalk");
    private static readonly int HardLandingHash = Animator.StringToHash("HardLanding");

    private float ungroundedTimer;

    private void Reset()
    {
        motor = GetComponentInParent<CharacterMotor>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Awake()
    {
        if (motor == null)
        {
            motor = GetComponentInParent<CharacterMotor>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Update()
    {
        if (motor == null || animator == null)
        {
            return;
        }

        Vector3 localVelocity = transform.InverseTransformDirection(motor.Velocity);
        Vector3 horizontalVelocity = new Vector3(localVelocity.x, 0f, localVelocity.z);
        float deltaTime = Time.deltaTime;

        animator.SetFloat(SpeedHash, horizontalVelocity.magnitude, parameterDampTime, deltaTime);
        animator.SetFloat(MoveXHash, localVelocity.x, parameterDampTime, deltaTime);
        animator.SetFloat(MoveYHash, localVelocity.z, parameterDampTime, deltaTime);
        animator.SetFloat(VerticalSpeedHash, motor.Velocity.y, parameterDampTime, deltaTime);
        animator.SetFloat(DriveHash, motor.DriveNormalized, parameterDampTime, deltaTime);

        animator.SetBool(GroundedHash, ResolveAnimatorGrounded(deltaTime));
        animator.SetBool(CyberSprintHash, motor.State == CharacterLocomotionMode.CyberSprint);
        animator.SetBool(QuickBoostHash, motor.State == CharacterLocomotionMode.QuickBoost);
        animator.SetBool(AssaultBoostHash, motor.State == CharacterLocomotionMode.AssaultBoost);
        bool isDashVault = motor.State == CharacterLocomotionMode.DashVault;
        bool isLowVault = motor.State == CharacterLocomotionMode.StepClimb
            || motor.State == CharacterLocomotionMode.LowClimb;
        bool isSpaceClimb = motor.State == CharacterLocomotionMode.HighClimb;
        animator.SetBool(DashVaultHash, isDashVault);
        animator.SetBool(LowVaultHash, isLowVault);
        animator.SetBool(SpaceClimbHash, isSpaceClimb);
        bool isOverheated = motor.State == CharacterLocomotionMode.Overheated;
        animator.SetBool(OverheatedHash, isOverheated && horizontalVelocity.magnitude < 0.2f);
        animator.SetBool(OverheatedWalkHash, isOverheated && horizontalVelocity.magnitude >= 0.2f);
        animator.SetBool(HardLandingHash, motor.State == CharacterLocomotionMode.HardLanding);
    }

    private bool ResolveAnimatorGrounded(float deltaTime)
    {
        if (motor.IsGrounded)
        {
            ungroundedTimer = 0f;
            return true;
        }

        if (motor.Velocity.y > 0.1f)
        {
            ungroundedTimer = groundedGraceTime;
            return false;
        }

        ungroundedTimer += deltaTime;
        return ungroundedTimer <= groundedGraceTime
            && motor.Velocity.y >= -groundedGraceMaxFallSpeed;
    }
}
