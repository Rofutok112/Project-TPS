using System.Collections.Generic;

public sealed class CharacterLockOnUseCase
{
    public CharacterLockOnSnapshot Snapshot { get; private set; } = CharacterLockOnSnapshot.None;

    public CharacterLockOnSnapshot Tick(
        CharacterLockOnSettings settings,
        CharacterInputSnapshot input,
        IReadOnlyList<CharacterLockOnCandidate> candidates)
    {
        if (settings == null || !input.LockOnPressed)
        {
            return Snapshot;
        }

        if (Snapshot.HasTarget)
        {
            Snapshot = CharacterLockOnSnapshot.None;
            return Snapshot;
        }

        int selectedId = 0;
        float bestScore = float.PositiveInfinity;
        for (int i = 0; i < candidates.Count; i++)
        {
            CharacterLockOnCandidate candidate = candidates[i];
            if (candidate.Distance > settings.maxDistance
                || candidate.ViewAngle > settings.maxViewAngle
                || (settings.requireLineOfSight && !candidate.Visible))
            {
                continue;
            }

            float normalizedAngle = candidate.ViewAngle / settings.maxViewAngle;
            float score = normalizedAngle * settings.angleScoreWeight
                + candidate.Distance * settings.distanceScoreWeight;
            if (score >= bestScore)
            {
                continue;
            }

            bestScore = score;
            selectedId = candidate.Id;
        }

        Snapshot = selectedId != 0
            ? new CharacterLockOnSnapshot(true, selectedId)
            : CharacterLockOnSnapshot.None;
        return Snapshot;
    }

    public void Clear()
    {
        Snapshot = CharacterLockOnSnapshot.None;
    }
}
