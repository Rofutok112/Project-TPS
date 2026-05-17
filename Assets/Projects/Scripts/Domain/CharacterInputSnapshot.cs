using UnityEngine;

public readonly struct CharacterInputSnapshot
{
    public CharacterInputSnapshot(
        Vector2 move,
        Vector2 look,
        bool jumpPressed,
        bool sprintHeld,
        bool quickBoostPressed,
        bool assaultBoostHeld,
        bool aimHeld = false,
        bool firePressed = false,
        bool lockOnPressed = false)
    {
        Move = Vector2.ClampMagnitude(move, 1f);
        Look = look;
        JumpPressed = jumpPressed;
        SprintHeld = sprintHeld;
        QuickBoostPressed = quickBoostPressed;
        AssaultBoostHeld = assaultBoostHeld;
        AimHeld = aimHeld;
        FirePressed = firePressed;
        LockOnPressed = lockOnPressed;
    }

    public Vector2 Move { get; }
    public Vector2 Look { get; }
    public bool JumpPressed { get; }
    public bool SprintHeld { get; }
    public bool QuickBoostPressed { get; }
    public bool AssaultBoostHeld { get; }
    public bool AimHeld { get; }
    public bool FirePressed { get; }
    public bool LockOnPressed { get; }

    public static CharacterInputSnapshot None { get; } = new CharacterInputSnapshot(
        Vector2.zero,
        Vector2.zero,
        false,
        false,
        false,
        false);
}
