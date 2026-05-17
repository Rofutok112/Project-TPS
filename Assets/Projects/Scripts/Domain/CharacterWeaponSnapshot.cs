public readonly struct CharacterWeaponSnapshot
{
    public CharacterWeaponSnapshot(
        CharacterWeaponMode mode,
        bool weaponEquipped,
        bool aimHeld,
        bool firing,
        bool fireStarted)
    {
        Mode = mode;
        WeaponEquipped = weaponEquipped;
        AimHeld = aimHeld;
        Firing = firing;
        FireStarted = fireStarted;
    }

    public CharacterWeaponMode Mode { get; }
    public bool WeaponEquipped { get; }
    public bool AimHeld { get; }
    public bool Firing { get; }
    public bool FireStarted { get; }

    public static CharacterWeaponSnapshot Unarmed { get; } = new CharacterWeaponSnapshot(
        CharacterWeaponMode.Unarmed,
        false,
        false,
        false,
        false);
}
