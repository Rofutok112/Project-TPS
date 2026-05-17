using UnityEngine;

[DisallowMultipleComponent]
public sealed class CharacterLockOnTarget : MonoBehaviour
{
    [SerializeField] private Transform aimPoint;

    public Transform AimPoint => aimPoint != null ? aimPoint : transform;

    private void Reset()
    {
        aimPoint = transform;
    }
}
