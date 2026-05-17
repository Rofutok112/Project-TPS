using UnityEngine;

[System.Serializable]
public sealed class CharacterMovementSettings
{
    [Header("Human Movement")]
    public float humanMaxSpeed = 6.4f;
    public float humanAcceleration = 32f;
    public float groundFriction = 18f;
    public float rotationSharpness = 16f;

    [Header("Cyber Drive")]
    public float driveMax = 100f;
    public float driveRecoveryPerSecond = 28f;
    public float driveRecoveryDelay = 0.45f;
    public float sprintMaxSpeed = 11f;
    public float sprintAcceleration = 52f;
    public float sprintDrivePerSecond = 18f;
    public float overheatDuration = 1.2f;
    public float overheatMaxSpeed = 2.2f;
    public float overheatAcceleration = 12f;

    [Header("Boost")]
    public float quickBoostSpeed = 18f;
    public float quickBoostDuration = 0.16f;
    public float quickBoostDriveCost = 24f;
    public float quickBoostTurnSharpness = 24f;
    public float assaultBoostSpeed = 20f;
    public float assaultBoostAcceleration = 72f;
    public float assaultBoostDrivePerSecond = 36f;

    [Header("Air Control")]
    public float jumpSpeed = 7.5f;
    public float jumpMomentumPreserveDuration = 0.18f;
    public float airAcceleration = 18f;
    public float airDrag = 3.5f;
    public float gravity = 28f;
    public float maxFallSpeed = 32f;
    public float hardLandingSpeed = 18f;
    public float hardLandingDuration = 0.18f;

    [Header("Climb")]
    public CharacterClimbSettings climb = new CharacterClimbSettings();
}
