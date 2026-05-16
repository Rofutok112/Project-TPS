using UnityEngine;

[DisallowMultipleComponent]
public sealed class CharacterAnimationDriver : MonoBehaviour
{
    [SerializeField] private CharacterMotor motor;
    [SerializeField] private Animator animator;
    [SerializeField] private float parameterDampTime = 0.08f;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");
    private static readonly int DriveHash = Animator.StringToHash("Drive");
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int CyberSprintHash = Animator.StringToHash("CyberSprint");
    private static readonly int QuickBoostHash = Animator.StringToHash("QuickBoost");
    private static readonly int AssaultBoostHash = Animator.StringToHash("AssaultBoost");
    private static readonly int OverheatedHash = Animator.StringToHash("Overheated");
    private static readonly int HardLandingHash = Animator.StringToHash("HardLanding");

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

        animator.SetBool(GroundedHash, motor.IsGrounded);
        animator.SetBool(CyberSprintHash, motor.State == CharacterMotor.LocomotionState.CyberSprint);
        animator.SetBool(QuickBoostHash, motor.State == CharacterMotor.LocomotionState.QuickBoost);
        animator.SetBool(AssaultBoostHash, motor.State == CharacterMotor.LocomotionState.AssaultBoost);
        animator.SetBool(OverheatedHash, motor.State == CharacterMotor.LocomotionState.Overheated);
        animator.SetBool(HardLandingHash, motor.State == CharacterMotor.LocomotionState.HardLanding);
    }
}
