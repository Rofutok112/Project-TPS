using UnityEngine;

public readonly struct ClimbPlan
{
    public ClimbPlan(
        CharacterLocomotionMode mode,
        Vector3 startPosition,
        Vector3 liftPosition,
        Vector3 ledgePosition,
        Vector3 targetPosition,
        Vector3 ledgeTopPoint,
        Vector3 facingDirection,
        float duration)
    {
        Mode = mode;
        StartPosition = startPosition;
        LiftPosition = liftPosition;
        LedgePosition = ledgePosition;
        TargetPosition = targetPosition;
        LedgeTopPoint = ledgeTopPoint;
        FacingDirection = facingDirection;
        Duration = duration;
    }

    public CharacterLocomotionMode Mode { get; }
    public Vector3 StartPosition { get; }
    public Vector3 LiftPosition { get; }
    public Vector3 LedgePosition { get; }
    public Vector3 TargetPosition { get; }
    public Vector3 LedgeTopPoint { get; }
    public Vector3 FacingDirection { get; }
    public float Duration { get; }
}
