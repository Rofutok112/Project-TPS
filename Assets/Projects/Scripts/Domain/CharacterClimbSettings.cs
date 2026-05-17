using UnityEngine;

[System.Serializable]
public sealed class CharacterClimbSettings
{
    public bool enableDashVault;
    public float probeDistance = 0.85f;
    public float minHeight = 0.18f;
    public float stepClimbMaxHeightRatio = 0.22f;
    public float autoStepClimbMaxHeightRatio = 0.5f;
    public float vaultOverMaxHeightRatio = 0.65f;
    public float vaultOverMaxThickness = 0.85f;
    public float vaultOverLandingOffset = 0.35f;
    public float lowClimbMaxHeightRatio = 0.92f;
    public float highClimbMaxHeight = 2.6f;
    public float forwardClearance = 0.7f;
    public float surfaceOffset = 0.15f;
    public float topHeightOffset = 0.06f;
    public float hangBackDistance = 0.35f;
    public float dashVaultDuration = 0.82f;
    public float stepClimbDuration = 0.7f;
    public float lowClimbDuration = 0.62f;
    public float highClimbDuration = 1.15f;
}
