using UnityEngine;

public readonly struct CharacterLocomotionSnapshot
{
    public CharacterLocomotionSnapshot(
        CharacterLocomotionMode mode,
        Vector3 velocity,
        Vector3 moveDirection,
        Vector3 facingDirection,
        float drive,
        float driveNormalized,
        bool isGrounded)
    {
        Mode = mode;
        Velocity = velocity;
        MoveDirection = moveDirection;
        FacingDirection = facingDirection;
        Drive = drive;
        DriveNormalized = driveNormalized;
        IsGrounded = isGrounded;
    }

    public CharacterLocomotionMode Mode { get; }
    public Vector3 Velocity { get; }
    public Vector3 MoveDirection { get; }
    public Vector3 FacingDirection { get; }
    public float Drive { get; }
    public float DriveNormalized { get; }
    public bool IsGrounded { get; }

    public static CharacterLocomotionSnapshot Idle { get; } = new CharacterLocomotionSnapshot(
        CharacterLocomotionMode.Grounded,
        Vector3.zero,
        Vector3.zero,
        Vector3.forward,
        1f,
        1f,
        true);
}
