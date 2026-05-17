using System;
using UnityEngine;

[Serializable]
public sealed class CharacterWeaponSettings
{
    [Tooltip("Equip the configured weapon when the character starts.")]
    public bool weaponEquipped = true;

    [Tooltip("Selected weapon pose and prefab socket.")]
    public CharacterWeaponKind weaponKind = CharacterWeaponKind.Pistol;

    [Tooltip("Keep the upper body in an aimed pose while the weapon is equipped.")]
    public bool alwaysAimWhenEquipped = false;

    [Tooltip("How long the firing animation parameter stays active after one fire input.")]
    [Min(0.01f)]
    public float firePoseDuration = 0.18f;
}
