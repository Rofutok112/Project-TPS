using UnityEngine;

public sealed class CharacterWeaponUseCase
{
    private float fireTimer;

    public CharacterWeaponSnapshot Snapshot { get; private set; } = CharacterWeaponSnapshot.Unarmed;

    public CharacterWeaponSnapshot Tick(
        CharacterWeaponSettings settings,
        CharacterInputSnapshot input,
        float deltaTime)
    {
        if (settings == null || !settings.weaponEquipped)
        {
            fireTimer = 0f;
            Snapshot = CharacterWeaponSnapshot.Unarmed;
            return Snapshot;
        }

        bool fireStarted = input.FirePressed;
        if (fireStarted)
        {
            fireTimer = Mathf.Max(0.01f, settings.firePoseDuration);
        }
        else
        {
            fireTimer = Mathf.Max(0f, fireTimer - deltaTime);
        }

        bool firing = fireTimer > 0f;
        bool aiming = settings.alwaysAimWhenEquipped || input.AimHeld || firing;
        CharacterWeaponMode mode = firing
            ? CharacterWeaponMode.Firing
            : aiming
                ? CharacterWeaponMode.Aiming
                : CharacterWeaponMode.Ready;

        Snapshot = new CharacterWeaponSnapshot(mode, true, aiming, firing, fireStarted);
        return Snapshot;
    }
}
