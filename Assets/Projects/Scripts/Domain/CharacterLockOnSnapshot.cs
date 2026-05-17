public readonly struct CharacterLockOnSnapshot
{
    public CharacterLockOnSnapshot(bool hasTarget, int targetId)
    {
        HasTarget = hasTarget;
        TargetId = targetId;
    }

    public bool HasTarget { get; }
    public int TargetId { get; }

    public static CharacterLockOnSnapshot None { get; } = new CharacterLockOnSnapshot(false, 0);
}
