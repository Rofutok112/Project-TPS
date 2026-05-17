using UnityEngine;

public readonly struct CharacterLockOnCandidate
{
    public CharacterLockOnCandidate(
        int id,
        Vector3 position,
        float distance,
        float viewAngle,
        bool visible)
    {
        Id = id;
        Position = position;
        Distance = distance;
        ViewAngle = viewAngle;
        Visible = visible;
    }

    public int Id { get; }
    public Vector3 Position { get; }
    public float Distance { get; }
    public float ViewAngle { get; }
    public bool Visible { get; }
}
