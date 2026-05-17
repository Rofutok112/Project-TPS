using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public sealed class CharacterClimbProbe : MonoBehaviour
{
    [SerializeField] private CharacterController controller;
    [SerializeField] private LayerMask climbMask = ~0;

    private void Reset()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Awake()
    {
        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
        }
    }

    public bool TryProbe(
        CharacterClimbSettings settings,
        Vector3 probeDirection,
        out ClimbCandidate candidate)
    {
        candidate = default;
        if (settings == null || controller == null)
        {
            return false;
        }

        probeDirection.y = 0f;
        if (probeDirection.sqrMagnitude < 0.001f)
        {
            return false;
        }

        probeDirection.Normalize();
        Vector3 center = transform.TransformPoint(controller.center);
        Vector3 chestOrigin = center + Vector3.up * (controller.height * 0.12f);
        Vector3 stepOrigin = transform.position + Vector3.up * (settings.minHeight + 0.12f);
        float chestProbeRadius = Mathf.Max(0.08f, controller.radius * 0.45f);
        float stepProbeRadius = Mathf.Max(0.06f, controller.radius * 0.28f);
        if (!TryFindWall(stepOrigin, stepProbeRadius, settings.probeDistance, probeDirection, out RaycastHit wallHit)
            && !TryFindWall(chestOrigin, chestProbeRadius, settings.probeDistance, probeDirection, out wallHit))
        {
            return false;
        }

        Vector3 wallNormal = wallHit.normal;
        wallNormal.y = 0f;
        if (wallNormal.sqrMagnitude < 0.001f)
        {
            return false;
        }

        Vector3 climbDirection = -wallNormal.normalized;
        float maxProbeHeight = settings.highClimbMaxHeight + controller.height * 0.5f;
        if (!TryFindTopSurface(settings, wallHit.point, climbDirection, maxProbeHeight, out Vector3 topPoint))
        {
            return false;
        }

        float climbHeight = topPoint.y - transform.position.y;
        bool canVaultOver = TryFindVaultLanding(
            settings,
            wallHit.point,
            climbDirection,
            climbHeight,
            maxProbeHeight,
            out Vector3 vaultLandingPoint);
        candidate = new ClimbCandidate(
            climbHeight,
            climbDirection,
            topPoint,
            canVaultOver,
            vaultLandingPoint,
            transform.position,
            controller.height);
        return true;
    }

    private bool TryFindTopSurface(
        CharacterClimbSettings settings,
        Vector3 wallPoint,
        Vector3 direction,
        float maxProbeHeight,
        out Vector3 topPoint)
    {
        topPoint = default;
        bool foundTop = false;
        float bestHeight = float.NegativeInfinity;
        const int sampleCount = 6;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = sampleCount == 1 ? 1f : i / (sampleCount - 1f);
            float distance = Mathf.Lerp(settings.surfaceOffset * 0.35f, settings.forwardClearance, t);
            Vector3 origin = wallPoint + direction * distance + Vector3.up * maxProbeHeight;
            if (!Physics.Raycast(
                    origin,
                    Vector3.down,
                    out RaycastHit hit,
                    maxProbeHeight + 0.4f,
                    climbMask,
                    QueryTriggerInteraction.Ignore))
            {
                continue;
            }

            float height = hit.point.y - transform.position.y;
            if (height < settings.minHeight || height <= bestHeight)
            {
                continue;
            }

            bestHeight = height;
            topPoint = hit.point;
            foundTop = true;
        }

        return foundTop;
    }

    private bool TryFindVaultLanding(
        CharacterClimbSettings settings,
        Vector3 wallPoint,
        Vector3 direction,
        float climbHeight,
        float maxProbeHeight,
        out Vector3 landingPoint)
    {
        landingPoint = default;
        if (climbHeight > controller.height * settings.vaultOverMaxHeightRatio)
        {
            return false;
        }

        float minDistance = controller.radius + settings.surfaceOffset;
        float maxDistance = Mathf.Max(minDistance, settings.vaultOverMaxThickness + settings.vaultOverLandingOffset);
        const int sampleCount = 4;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = sampleCount == 1 ? 1f : i / (sampleCount - 1f);
            float distance = Mathf.Lerp(minDistance, maxDistance, t);
            Vector3 origin = wallPoint + direction * distance + Vector3.up * maxProbeHeight;
            if (!Physics.Raycast(
                    origin,
                    Vector3.down,
                    out RaycastHit groundHit,
                    maxProbeHeight + 0.4f,
                    climbMask,
                    QueryTriggerInteraction.Ignore))
            {
                continue;
            }

            float groundDelta = groundHit.point.y - transform.position.y;
            if (groundDelta > settings.minHeight)
            {
                continue;
            }

            landingPoint = groundHit.point + direction * settings.vaultOverLandingOffset;
            return true;
        }

        return false;
    }

    private bool TryFindWall(
        Vector3 origin,
        float radius,
        float distance,
        Vector3 direction,
        out RaycastHit hit)
    {
        return Physics.SphereCast(
            origin,
            radius,
            direction,
            out hit,
            distance,
            climbMask,
            QueryTriggerInteraction.Ignore);
    }

    public bool HasCapsuleRoom(Vector3 targetPosition)
    {
        if (controller == null)
        {
            return false;
        }

        Vector3 center = targetPosition + controller.center;
        float radius = Mathf.Max(0.02f, controller.radius - controller.skinWidth);
        float halfHeight = Mathf.Max(controller.height * 0.5f, radius);
        Vector3 bottom = center + Vector3.down * (halfHeight - radius);
        Vector3 top = center + Vector3.up * (halfHeight - radius);
        return !Physics.CheckCapsule(
            bottom,
            top,
            radius,
            climbMask,
            QueryTriggerInteraction.Ignore);
    }
}
