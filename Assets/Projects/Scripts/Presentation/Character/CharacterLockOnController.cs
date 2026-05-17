using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CharacterLockOnController : MonoBehaviour
{
    [Header("Adapters")]
    [SerializeField] private CharacterInputReader inputReader;
    [SerializeField] private CharacterMotor motor;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private LayerMask lineOfSightMask = ~0;

    [Header("Use Case Settings")]
    [SerializeField] private CharacterLockOnSettings settings = new CharacterLockOnSettings();

    private readonly List<CharacterLockOnCandidate> candidates = new List<CharacterLockOnCandidate>(32);
    private readonly Dictionary<int, CharacterLockOnTarget> targetById = new Dictionary<int, CharacterLockOnTarget>(32);
    private CharacterLockOnUseCase useCase;

    public Transform CurrentTarget { get; private set; }

    private void Reset()
    {
        inputReader = GetComponent<CharacterInputReader>();
        motor = GetComponent<CharacterMotor>();
        cameraTransform = Camera.main != null ? Camera.main.transform : null;
    }

    private void Awake()
    {
        if (inputReader == null)
        {
            inputReader = GetComponent<CharacterInputReader>();
        }

        if (motor == null)
        {
            motor = GetComponent<CharacterMotor>();
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (settings == null)
        {
            settings = new CharacterLockOnSettings();
        }

        useCase = new CharacterLockOnUseCase();
    }

    private void Update()
    {
        if (useCase == null)
        {
            useCase = new CharacterLockOnUseCase();
        }

        CharacterInputSnapshot input = inputReader != null ? inputReader.ReadSnapshot() : CharacterInputSnapshot.None;
        BuildCandidates();
        CharacterLockOnSnapshot snapshot = useCase.Tick(settings, input, candidates);
        CurrentTarget = ResolveTarget(snapshot);

        if (snapshot.HasTarget && CurrentTarget == null)
        {
            useCase.Clear();
            CurrentTarget = null;
        }

        if (motor != null)
        {
            motor.SetLockOnTarget(CurrentTarget);
        }
    }

    private void BuildCandidates()
    {
        candidates.Clear();
        targetById.Clear();

        CharacterLockOnTarget[] targets = FindObjectsByType<CharacterLockOnTarget>(FindObjectsInactive.Exclude);
        Transform basis = cameraTransform != null ? cameraTransform : transform;
        Vector3 origin = basis.position;
        Vector3 forward = basis.forward;

        for (int i = 0; i < targets.Length; i++)
        {
            CharacterLockOnTarget target = targets[i];
            Transform aimPoint = target.AimPoint;
            if (aimPoint == null || aimPoint == transform)
            {
                continue;
            }

            Vector3 toTarget = aimPoint.position - origin;
            float distance = toTarget.magnitude;
            if (distance <= 0.001f)
            {
                continue;
            }

            float angle = Vector3.Angle(forward, toTarget);
            bool visible = HasLineOfSight(origin, aimPoint.position, target);
            int id = target.GetHashCode();
            candidates.Add(new CharacterLockOnCandidate(id, aimPoint.position, distance, angle, visible));
            targetById[id] = target;
        }
    }

    private bool HasLineOfSight(Vector3 origin, Vector3 targetPosition, CharacterLockOnTarget target)
    {
        if (!settings.requireLineOfSight)
        {
            return true;
        }

        Vector3 direction = targetPosition - origin;
        float distance = direction.magnitude;
        if (!Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance, lineOfSightMask, QueryTriggerInteraction.Ignore))
        {
            return true;
        }

        return hit.transform == target.transform || hit.transform.IsChildOf(target.transform);
    }

    private Transform ResolveTarget(CharacterLockOnSnapshot snapshot)
    {
        if (!snapshot.HasTarget || !targetById.TryGetValue(snapshot.TargetId, out CharacterLockOnTarget target))
        {
            return null;
        }

        return target.AimPoint;
    }
}
