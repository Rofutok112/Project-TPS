using System;
using UnityEngine;

[Serializable]
public sealed class CharacterLockOnSettings
{
    [Min(1f)]
    public float maxDistance = 24f;

    [Range(1f, 180f)]
    public float maxViewAngle = 70f;

    [Min(0f)]
    public float distanceScoreWeight = 0.035f;

    [Min(0f)]
    public float angleScoreWeight = 1f;

    public bool requireLineOfSight = false;
}
