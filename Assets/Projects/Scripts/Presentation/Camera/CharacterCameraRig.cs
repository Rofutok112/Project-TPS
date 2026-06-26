using UnityEngine;
using Unity.Cinemachine;
using System.Reflection;

[DefaultExecutionOrder(-100)]
public sealed class CharacterCameraRig : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private CharacterInputReader inputReader;
    [SerializeField] private CharacterLockOnController lockOnController;
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.45f, 0f);
    [SerializeField] private float distance = 5.5f;
    [SerializeField] private float height = 1.1f;
    [SerializeField] private Vector3 shoulderOffset = new Vector3(0.45f, -0.25f, 0f);
    [SerializeField, Range(0f, 1f)] private float cameraSide = 0.7f;
    [SerializeField] private float yawSensitivity = 0.12f;
    [SerializeField] private float pitchSensitivity = 0.1f;
    [SerializeField] private float minPitch = -25f;
    [SerializeField] private float maxPitch = 65f;
    [SerializeField] private float positionSharpness = 18f;
    [SerializeField] private float rotationSharpness = 24f;
    [SerializeField] private bool avoidObstacles = true;
    [SerializeField] private LayerMask obstacleMask = ~0;
    [SerializeField] private float obstacleRadius = 0.25f;
    [SerializeField] private float dampingIntoObstacle = 0.05f;
    [SerializeField] private float dampingFromObstacle = 0.35f;
    [SerializeField] private bool lockOnFocusEnabled = true;
    [SerializeField, Range(0f, 1f)] private float lockOnFocusStrength = 0.28f;
    [SerializeField] private float lockOnFocusSharpness = 4f;
    [SerializeField] private float lockOnFocusMaxDegreesPerSecond = 80f;
    [SerializeField] private float lockOnFocusReleaseAngle = 95f;
    [SerializeField] private float lockOnFocusResumeAngle = 75f;
    [SerializeField] private float lockOnFocusVerticalWeight = 0.45f;
    [SerializeField] private float manualLookFocusSuppression = 0.65f;
    [SerializeField] private bool autoCreateCinemachineRig = true;

    private float yaw;
    private float pitch = 18f;
    private bool hasInitializedPose;
    private bool lockOnFocusActive;
    private CinemachineThirdPersonFollow thirdPersonFollow;

    private void Awake()
    {
        EnsureCinemachineRig();
        InitializeAnglesFromCamera();
    }

    private void OnValidate()
    {
        distance = Mathf.Max(0.1f, distance);
        minPitch = Mathf.Min(minPitch, maxPitch);
        positionSharpness = Mathf.Max(0.01f, positionSharpness);
        rotationSharpness = Mathf.Max(0.01f, rotationSharpness);
        obstacleRadius = Mathf.Max(0.01f, obstacleRadius);
        dampingIntoObstacle = Mathf.Max(0f, dampingIntoObstacle);
        dampingFromObstacle = Mathf.Max(0f, dampingFromObstacle);
        lockOnFocusSharpness = Mathf.Max(0f, lockOnFocusSharpness);
        lockOnFocusMaxDegreesPerSecond = Mathf.Max(0f, lockOnFocusMaxDegreesPerSecond);
        lockOnFocusReleaseAngle = Mathf.Clamp(lockOnFocusReleaseAngle, 1f, 180f);
        lockOnFocusResumeAngle = Mathf.Clamp(lockOnFocusResumeAngle, 1f, lockOnFocusReleaseAngle);
        lockOnFocusVerticalWeight = Mathf.Max(0f, lockOnFocusVerticalWeight);
        manualLookFocusSuppression = Mathf.Max(0f, manualLookFocusSuppression);
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        EnsureCinemachineRig();

        CharacterInputSnapshot input = inputReader != null ? inputReader.ReadSnapshot() : CharacterInputSnapshot.None;
        Vector2 look = input.Look;
        yaw += look.x * yawSensitivity;
        pitch = Mathf.Clamp(pitch - look.y * pitchSensitivity, minPitch, maxPitch);
        ApplyLockOnFocus(look);

        UpdateFollowTarget();
        ApplyCinemachineSettings();
    }

    private void EnsureCinemachineRig()
    {
        if (!autoCreateCinemachineRig)
        {
            return;
        }

        CinemachineBrain brain = GetComponent<CinemachineBrain>();
        if (brain == null)
        {
            brain = gameObject.AddComponent<CinemachineBrain>();
            brain.UpdateMethod = CinemachineBrain.UpdateMethods.SmartUpdate;
            brain.BlendUpdateMethod = CinemachineBrain.BrainUpdateMethods.LateUpdate;
        }

        if (lockOnController == null && target != null)
        {
            lockOnController = target.GetComponent<CharacterLockOnController>();
        }

        if (followTarget == null)
        {
            GameObject followTargetObject = new GameObject("TPS_CameraFollowTarget");
            followTarget = followTargetObject.transform;
        }

        if (cinemachineCamera == null)
        {
            GameObject cameraObject = new GameObject("TPS_CinemachineCamera");
            cinemachineCamera = cameraObject.GetComponent<CinemachineCamera>();
            if (cinemachineCamera == null)
            {
                cinemachineCamera = cameraObject.AddComponent<CinemachineCamera>();
            }
        }

        cinemachineCamera.Follow = followTarget;
        cinemachineCamera.LookAt = followTarget;

        thirdPersonFollow = cinemachineCamera.GetComponent<CinemachineThirdPersonFollow>();
        if (thirdPersonFollow == null)
        {
            thirdPersonFollow = cinemachineCamera.gameObject.AddComponent<CinemachineThirdPersonFollow>();
        }
    }

    private void InitializeAnglesFromCamera()
    {
        Vector3 cameraEuler = transform.rotation.eulerAngles;
        yaw = cameraEuler.y;
        pitch = NormalizeAngle(cameraEuler.x);
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private void UpdateFollowTarget()
    {
        if (followTarget == null)
        {
            return;
        }

        Quaternion desiredRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPosition = target.position + targetOffset + Vector3.up * height;

        if (!hasInitializedPose)
        {
            followTarget.SetPositionAndRotation(desiredPosition, desiredRotation);
            hasInitializedPose = true;
            return;
        }

        float rotationT = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);
        followTarget.position = desiredPosition;
        followTarget.rotation = Quaternion.Slerp(followTarget.rotation, desiredRotation, rotationT);
    }

    private void ApplyLockOnFocus(Vector2 manualLook)
    {
        if (!lockOnFocusEnabled || lockOnController == null || lockOnController.CurrentTarget == null)
        {
            lockOnFocusActive = false;
            return;
        }

        Vector3 origin = target != null ? target.position + targetOffset + Vector3.up * height : transform.position;
        Vector3 toTarget = lockOnController.CurrentTarget.position - origin;
        if (toTarget.sqrMagnitude <= 0.001f)
        {
            lockOnFocusActive = false;
            return;
        }

        Vector3 currentForward = Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward;
        float angle = Vector3.Angle(currentForward, toTarget);
        if (lockOnFocusActive && angle > lockOnFocusReleaseAngle)
        {
            lockOnFocusActive = false;
        }
        else if (!lockOnFocusActive && angle < lockOnFocusResumeAngle)
        {
            lockOnFocusActive = true;
        }

        if (!lockOnFocusActive)
        {
            return;
        }

        Vector3 flatDirection = Vector3.ProjectOnPlane(toTarget, Vector3.up);
        if (flatDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        float targetYaw = Quaternion.LookRotation(flatDirection.normalized, Vector3.up).eulerAngles.y;
        float targetPitch = -Mathf.Asin(Mathf.Clamp(toTarget.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;
        targetPitch = Mathf.Clamp(targetPitch * lockOnFocusVerticalWeight, minPitch, maxPitch);

        float manualSuppression = 1f / (1f + manualLook.magnitude * manualLookFocusSuppression);
        float focusT = (1f - Mathf.Exp(-lockOnFocusSharpness * Time.deltaTime)) * lockOnFocusStrength * manualSuppression;
        float maxStep = lockOnFocusMaxDegreesPerSecond * Time.deltaTime * manualSuppression;

        float yawDelta = Mathf.DeltaAngle(yaw, targetYaw);
        float pitchDelta = Mathf.DeltaAngle(pitch, targetPitch);
        yaw += Mathf.Clamp(yawDelta * focusT, -maxStep, maxStep);
        pitch = Mathf.Clamp(pitch + Mathf.Clamp(pitchDelta * focusT, -maxStep, maxStep), minPitch, maxPitch);
    }

    private void ApplyCinemachineSettings()
    {
        if (thirdPersonFollow == null)
        {
            return;
        }

        thirdPersonFollow.ShoulderOffset = shoulderOffset;
        thirdPersonFollow.VerticalArmLength = 0f;
        thirdPersonFollow.CameraSide = cameraSide;
        thirdPersonFollow.CameraDistance = distance;

        float damping = Mathf.Clamp(1f / positionSharpness, 0f, 1f);
        thirdPersonFollow.Damping = new Vector3(damping, damping, damping);

        ApplyObstacleAvoidance();
    }

    private void ApplyObstacleAvoidance()
    {
        FieldInfo avoidObstaclesField = typeof(CinemachineThirdPersonFollow).GetField("AvoidObstacles", BindingFlags.Instance | BindingFlags.Public);
        if (avoidObstaclesField == null)
        {
            return;
        }

        object obstacles = avoidObstaclesField.GetValue(thirdPersonFollow);
        if (obstacles == null)
        {
            return;
        }

        SetObstacleField(obstacles, "Enabled", avoidObstacles);
        SetObstacleField(obstacles, "CollisionFilter", obstacleMask);
        SetObstacleField(obstacles, "IgnoreTag", GetIgnoredObstacleTag());
        SetObstacleField(obstacles, "CameraRadius", obstacleRadius);
        SetObstacleField(obstacles, "DampingIntoCollision", dampingIntoObstacle);
        SetObstacleField(obstacles, "DampingFromCollision", dampingFromObstacle);
        avoidObstaclesField.SetValue(thirdPersonFollow, obstacles);
    }

    private string GetIgnoredObstacleTag()
    {
        if (target == null || string.IsNullOrEmpty(target.tag) || target.tag == "Untagged")
        {
            return string.Empty;
        }

        return target.tag;
    }

    private static void SetObstacleField(object obstacles, string fieldName, object value)
    {
        FieldInfo field = obstacles.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
        if (field != null)
        {
            field.SetValue(obstacles, value);
        }
    }

    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        hasInitializedPose = false;
    }

    public void SetInputReader(CharacterInputReader newInputReader)
    {
        inputReader = newInputReader;
    }
}
