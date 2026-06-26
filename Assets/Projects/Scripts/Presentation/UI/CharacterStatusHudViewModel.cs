public readonly struct CharacterStatusHudViewModel
{
    public CharacterStatusHudViewModel(
        string modeText,
        string groundedText,
        string lockOnText,
        string speedText,
        string climbText,
        float driveNormalized,
        bool isOverheated,
        bool isClimbing,
        bool hasLockOnTarget)
    {
        ModeText = modeText;
        GroundedText = groundedText;
        LockOnText = lockOnText;
        SpeedText = speedText;
        ClimbText = climbText;
        DriveNormalized = driveNormalized;
        IsOverheated = isOverheated;
        IsClimbing = isClimbing;
        HasLockOnTarget = hasLockOnTarget;
    }

    public string ModeText { get; }
    public string GroundedText { get; }
    public string LockOnText { get; }
    public string SpeedText { get; }
    public string ClimbText { get; }
    public float DriveNormalized { get; }
    public bool IsOverheated { get; }
    public bool IsClimbing { get; }
    public bool HasLockOnTarget { get; }
}
