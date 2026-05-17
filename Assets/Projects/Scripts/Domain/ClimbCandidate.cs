using UnityEngine;

public readonly struct ClimbCandidate
{
    public ClimbCandidate(
        float height,
        Vector3 direction,
        Vector3 topPoint,
        bool canVaultOver,
        Vector3 vaultLandingPoint,
        Vector3 actorPosition,
        float actorHeight)
    {
        Height = height;
        Direction = direction;
        TopPoint = topPoint;
        CanVaultOver = canVaultOver;
        VaultLandingPoint = vaultLandingPoint;
        ActorPosition = actorPosition;
        ActorHeight = actorHeight;
    }

    public float Height { get; }
    public Vector3 Direction { get; }
    public Vector3 TopPoint { get; }
    public bool CanVaultOver { get; }
    public Vector3 VaultLandingPoint { get; }
    public Vector3 ActorPosition { get; }
    public float ActorHeight { get; }
}
