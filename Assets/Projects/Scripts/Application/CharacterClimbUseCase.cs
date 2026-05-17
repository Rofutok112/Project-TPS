using UnityEngine;

public sealed class CharacterClimbUseCase
{
    public bool TryCreatePlan(
        CharacterClimbSettings settings,
        ClimbCandidate candidate,
        out ClimbPlan plan)
    {
        return TryCreatePlan(settings, candidate, false, out plan);
    }

    public bool TryCreatePlan(
        CharacterClimbSettings settings,
        ClimbCandidate candidate,
        bool forceDashVault,
        out ClimbPlan plan)
    {
        plan = default;
        if (settings == null)
        {
            return false;
        }

        if (candidate.Height < settings.minHeight || candidate.Height > settings.highClimbMaxHeight)
        {
            return false;
        }

        Vector3 direction = candidate.Direction;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
        {
            return false;
        }

        direction.Normalize();
        CharacterLocomotionMode mode = forceDashVault
            ? CharacterLocomotionMode.DashVault
            : ResolveMode(settings, candidate);
        bool vaultOverObstacle = candidate.CanVaultOver
            && mode != CharacterLocomotionMode.DashVault
            && mode != CharacterLocomotionMode.HighClimb;
        Vector3 targetPosition = vaultOverObstacle
            ? candidate.VaultLandingPoint
            : candidate.TopPoint + Vector3.up * settings.topHeightOffset + direction * settings.surfaceOffset;
        Vector3 liftPosition = ResolveLiftPosition(settings, mode, candidate, targetPosition, direction);
        Vector3 ledgePosition = ResolveLedgePosition(settings, mode, liftPosition, targetPosition, direction);
        float duration = ResolveDuration(settings, mode);

        plan = new ClimbPlan(
            mode,
            candidate.ActorPosition,
            liftPosition,
            ledgePosition,
            targetPosition,
            candidate.TopPoint,
            direction,
            duration);
        return true;
    }

    public Vector3 EvaluatePosition(ClimbPlan plan, float normalizedTime)
    {
        if (plan.Mode == CharacterLocomotionMode.DashVault)
        {
            return QuadraticBezier(
                plan.StartPosition,
                plan.LiftPosition,
                plan.TargetPosition,
                SmoothStep(normalizedTime));
        }

        if (plan.Mode == CharacterLocomotionMode.StepClimb)
        {
            return QuadraticBezier(
                plan.StartPosition,
                plan.LiftPosition,
                plan.TargetPosition,
                SmoothStep(normalizedTime));
        }

        if (plan.Mode == CharacterLocomotionMode.LowClimb)
        {
            return QuadraticBezier(
                plan.StartPosition,
                plan.LiftPosition,
                plan.TargetPosition,
                SmoothStep(normalizedTime));
        }

        if (normalizedTime < 0.45f)
        {
            float t = SmoothStep(normalizedTime / 0.45f);
            return Vector3.Lerp(plan.StartPosition, plan.LiftPosition, t);
        }

        if (normalizedTime < 0.82f)
        {
            float t = SmoothStep((normalizedTime - 0.45f) / 0.37f);
            Vector3 pullUpControl = Vector3.Lerp(plan.LiftPosition, plan.LedgePosition, 0.65f) + Vector3.up * 0.12f;
            return QuadraticBezier(plan.LiftPosition, pullUpControl, plan.LedgePosition, t);
        }

        return Vector3.Lerp(plan.LedgePosition, plan.TargetPosition, SmoothStep((normalizedTime - 0.82f) / 0.18f));
    }

    public Vector3 ResolveExitVelocity(CharacterMovementSettings settings, ClimbPlan plan)
    {
        if (settings == null || plan.Mode != CharacterLocomotionMode.DashVault)
        {
            return Vector3.zero;
        }

        return plan.FacingDirection.normalized * settings.sprintMaxSpeed;
    }

    private static CharacterLocomotionMode ResolveMode(
        CharacterClimbSettings settings,
        ClimbCandidate candidate)
    {
        if (candidate.Height < candidate.ActorHeight * settings.stepClimbMaxHeightRatio)
        {
            return CharacterLocomotionMode.StepClimb;
        }

        return candidate.Height < candidate.ActorHeight * settings.lowClimbMaxHeightRatio
            ? CharacterLocomotionMode.LowClimb
            : CharacterLocomotionMode.HighClimb;
    }

    private static float ResolveDuration(CharacterClimbSettings settings, CharacterLocomotionMode mode)
    {
        if (mode == CharacterLocomotionMode.DashVault)
        {
            return settings.dashVaultDuration;
        }

        if (mode == CharacterLocomotionMode.StepClimb)
        {
            return settings.stepClimbDuration;
        }

        return mode == CharacterLocomotionMode.LowClimb
            ? settings.lowClimbDuration
            : settings.highClimbDuration;
    }

    private static Vector3 ResolveLiftPosition(
        CharacterClimbSettings settings,
        CharacterLocomotionMode mode,
        ClimbCandidate candidate,
        Vector3 targetPosition,
        Vector3 direction)
    {
        if (mode == CharacterLocomotionMode.DashVault)
        {
            return Vector3.Lerp(candidate.ActorPosition, targetPosition, 0.7f)
                + Vector3.up * Mathf.Min(0.16f, candidate.Height * 0.22f);
        }

        if (mode == CharacterLocomotionMode.StepClimb)
        {
            return Vector3.Lerp(candidate.ActorPosition, targetPosition, 0.55f)
                + Vector3.up * Mathf.Min(0.22f, candidate.Height * 0.28f);
        }

        if (mode == CharacterLocomotionMode.LowClimb)
        {
            return Vector3.Lerp(candidate.ActorPosition, targetPosition, 0.45f)
                + Vector3.up * Mathf.Min(0.35f, candidate.Height * 0.35f);
        }

        return candidate.ActorPosition
            + Vector3.up * Mathf.Max(candidate.ActorHeight * 0.55f, candidate.Height * 0.55f)
            - direction * settings.hangBackDistance;
    }

    private static Vector3 ResolveLedgePosition(
        CharacterClimbSettings settings,
        CharacterLocomotionMode mode,
        Vector3 liftPosition,
        Vector3 targetPosition,
        Vector3 direction)
    {
        if (mode == CharacterLocomotionMode.DashVault)
        {
            return Vector3.Lerp(liftPosition, targetPosition, 0.92f);
        }

        if (mode == CharacterLocomotionMode.StepClimb)
        {
            return Vector3.Lerp(liftPosition, targetPosition, 0.85f);
        }

        if (mode == CharacterLocomotionMode.LowClimb)
        {
            return Vector3.Lerp(liftPosition, targetPosition, 0.75f);
        }

        return targetPosition - direction * settings.hangBackDistance + Vector3.up * 0.08f;
    }

    private static Vector3 QuadraticBezier(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        return Vector3.Lerp(Vector3.Lerp(a, b, t), Vector3.Lerp(b, c, t), t);
    }

    private static float SmoothStep(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }
}
