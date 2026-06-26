using UnityEngine;

[DisallowMultipleComponent]
public sealed class CharacterStatusHudPresenter : MonoBehaviour
{
    [SerializeField] private CharacterMotor motor;
    [SerializeField] private CharacterLockOnController lockOnController;
    [SerializeField] private CharacterStatusHudView view;

    private void Reset()
    {
        view = GetComponent<CharacterStatusHudView>();
    }

    private void Awake()
    {
        if (view == null)
        {
            view = GetComponent<CharacterStatusHudView>();
        }
    }

    private void LateUpdate()
    {
        if (motor == null || view == null)
        {
            return;
        }

        CharacterStatusHudViewModel model = BuildViewModel();
        view.Render(model);
    }

    private CharacterStatusHudViewModel BuildViewModel()
    {
        CharacterLocomotionMode mode = motor.State;
        bool isClimbing = motor.IsClimbing;
        bool isOverheated = mode == CharacterLocomotionMode.Overheated;
        bool hasLockOnTarget = lockOnController != null && lockOnController.CurrentTarget != null;
        float horizontalSpeed = new Vector3(motor.Velocity.x, 0f, motor.Velocity.z).magnitude;

        return new CharacterStatusHudViewModel(
            mode.ToString(),
            motor.IsGrounded ? "Grounded" : "Airborne",
            hasLockOnTarget ? "Lock-On" : "No Target",
            $"{horizontalSpeed:0.0} m/s",
            isClimbing ? $"{motor.CurrentClimbPlan.Mode} {motor.ClimbNormalizedTime:P0}" : "None",
            Mathf.Clamp01(motor.DriveNormalized),
            isOverheated,
            isClimbing,
            hasLockOnTarget);
    }
}
