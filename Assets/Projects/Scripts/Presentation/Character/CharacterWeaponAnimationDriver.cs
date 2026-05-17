using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
[DefaultExecutionOrder(200)]
public sealed class CharacterWeaponAnimationDriver : MonoBehaviour
{
    private const string GeneratedAssaultRifleSocketName = "WeaponSocket_AssaultRifle";
    private const string GeneratedPistolSocketName = "WeaponSocket_Pistol";
    private const string GeneratedLeftGripName = "LeftHandGrip";

    [Header("Adapters")]
    [SerializeField] private CharacterInputReader inputReader;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private CharacterMotor motor;
    [SerializeField] private CharacterLockOnController lockOnController;

    [Header("Use Case Settings")]
    [SerializeField] private CharacterWeaponSettings weaponSettings = new CharacterWeaponSettings();

    [Header("Weapon")]
    [SerializeField] private GameObject assaultRiflePrefab;
    [SerializeField] private GameObject pistolPrefab;
    [SerializeField] private GameObject weaponPrefab;
    [SerializeField] private Transform weaponSocket;
    [SerializeField] private Transform pistolSocket;
    [SerializeField] private Transform leftHandGrip;
    [SerializeField] private bool instantiateWeaponOnAwake = true;
    [SerializeField] private bool compensateHandScale = true;

    [Header("Animation")]
    [SerializeField] private string weaponLayerName = "Weapon Upper Body";
    [SerializeField] private float layerBlendSpeed = 8f;
    [SerializeField] private float leftHandIKWeight = 0.9f;
    [SerializeField] private bool aimWeaponRoot = true;
    [SerializeField] private float lockOnWeaponAimWeight = 1f;
    [SerializeField] private float cameraWeaponAimWeight = 1f;
    [SerializeField] private float weaponAimBlendSpeed = 12f;
    [SerializeField] private float maxWeaponAimAngle = 140f;
    [SerializeField] private float cameraAimDistance = 24f;
    [SerializeField] private float cameraVerticalAimWeight = 0.35f;
    [SerializeField] private float fullWeightFacingAngle = 145f;
    [SerializeField] private float maxFacingAngle = 165f;

    private static readonly int WeaponEquippedHash = Animator.StringToHash("WeaponEquipped");
    private static readonly int WeaponAimingHash = Animator.StringToHash("WeaponAiming");
    private static readonly int WeaponFiringHash = Animator.StringToHash("WeaponFiring");
    private static readonly int WeaponPistolHash = Animator.StringToHash("WeaponPistol");

    private CharacterWeaponUseCase useCase;
    private CharacterWeaponSnapshot snapshot = CharacterWeaponSnapshot.Unarmed;
    private int weaponLayerIndex = -1;
    private float layerWeight;
    private float weaponAimWeight;

    private void Reset()
    {
        inputReader = GetComponent<CharacterInputReader>();
        animator = GetComponent<Animator>();
        cameraTransform = Camera.main != null ? Camera.main.transform : null;
        motor = GetComponent<CharacterMotor>();
        lockOnController = GetComponent<CharacterLockOnController>();
    }

    private void Awake()
    {
        if (inputReader == null)
        {
            inputReader = GetComponent<CharacterInputReader>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (motor == null)
        {
            motor = GetComponent<CharacterMotor>();
        }

        if (lockOnController == null)
        {
            lockOnController = GetComponent<CharacterLockOnController>();
        }

        if (weaponSettings == null)
        {
            weaponSettings = new CharacterWeaponSettings();
        }

        useCase = new CharacterWeaponUseCase();
        weaponLayerIndex = animator != null ? animator.GetLayerIndex(weaponLayerName) : -1;

        if (instantiateWeaponOnAwake)
        {
            EnsureWeaponInstance();
        }
    }

    private void Update()
    {
        if (animator == null || useCase == null)
        {
            return;
        }

        CharacterInputSnapshot input = inputReader != null ? inputReader.ReadSnapshot() : CharacterInputSnapshot.None;
        if (IsLockOnActive() && !input.AimHeld)
        {
            input = new CharacterInputSnapshot(
                input.Move,
                input.Look,
                input.JumpPressed,
                input.SprintHeld,
                input.QuickBoostPressed,
                input.AssaultBoostHeld,
                true,
                input.FirePressed,
                input.LockOnPressed);
        }

        snapshot = useCase.Tick(weaponSettings, input, Time.deltaTime);

        animator.SetBool(WeaponEquippedHash, snapshot.WeaponEquipped);
        animator.SetBool(WeaponAimingHash, snapshot.AimHeld);
        animator.SetBool(WeaponFiringHash, snapshot.Firing);
        animator.SetBool(WeaponPistolHash, weaponSettings.weaponKind == CharacterWeaponKind.Pistol);

        float targetLayerWeight = snapshot.WeaponEquipped ? 1f : 0f;
        layerWeight = Mathf.MoveTowards(layerWeight, targetLayerWeight, layerBlendSpeed * Time.deltaTime);
        if (weaponLayerIndex >= 0)
        {
            animator.SetLayerWeight(weaponLayerIndex, layerWeight);
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || !animator.isHuman || leftHandGrip == null)
        {
            return;
        }

        float weight = layerWeight * leftHandIKWeight;
        if (!snapshot.WeaponEquipped || weaponSettings.weaponKind != CharacterWeaponKind.AssaultRifle)
        {
            weight = 0f;
        }

        animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, weight);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, weight);
        animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandGrip.position);
        animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandGrip.rotation);
    }

    private void LateUpdate()
    {
        if (!aimWeaponRoot || !snapshot.WeaponEquipped)
        {
            weaponAimWeight = Mathf.MoveTowards(weaponAimWeight, 0f, weaponAimBlendSpeed * Time.deltaTime);
            return;
        }

        Transform weaponRoot = ResolveActiveWeaponRoot();
        if (weaponRoot == null || !ResolveAimTargetPosition(out Vector3 targetPosition, out bool locked))
        {
            weaponAimWeight = Mathf.MoveTowards(weaponAimWeight, 0f, weaponAimBlendSpeed * Time.deltaTime);
            return;
        }

        ResetWeaponRootPose(weaponRoot);
        if (!locked)
        {
            targetPosition.y = Mathf.Lerp(weaponRoot.position.y, targetPosition.y, cameraVerticalAimWeight);
        }

        float facingWeight = ResolveFacingWeight(targetPosition);
        float targetWeight = (locked ? lockOnWeaponAimWeight : cameraWeaponAimWeight) * facingWeight;
        weaponAimWeight = Mathf.MoveTowards(weaponAimWeight, targetWeight, weaponAimBlendSpeed * Time.deltaTime);
        if (weaponAimWeight <= 0.001f)
        {
            return;
        }

        Vector3 direction = targetPosition - weaponRoot.position;
        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion delta = Quaternion.FromToRotation(weaponRoot.forward, direction.normalized);
        delta.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f)
        {
            angle -= 360f;
        }

        angle = Mathf.Clamp(angle, -maxWeaponAimAngle, maxWeaponAimAngle);
        weaponRoot.rotation = Quaternion.AngleAxis(angle * weaponAimWeight, axis) * weaponRoot.rotation;
    }

    private void EnsureWeaponInstance()
    {
        if (animator == null)
        {
            return;
        }

        Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        if (rightHand == null)
        {
            return;
        }

        if (assaultRiflePrefab == null)
        {
            assaultRiflePrefab = weaponPrefab;
        }

        float weaponScale = compensateHandScale ? ResolveCompensatedScale(rightHand) : 1f;
        Transform assaultRifleRoot = null;
        if (assaultRiflePrefab != null)
        {
            weaponSocket = EnsureSocket(
                rightHand,
                weaponSocket,
                GeneratedAssaultRifleSocketName,
                new Vector3(0f, 0.11f, 0.02f),
                new Vector3(357.87f, 0f, 85.09f));
            assaultRifleRoot = EnsureWeaponChild(
                weaponSocket,
                assaultRiflePrefab,
                assaultRiflePrefab.name,
                new Vector3(0f, 0.02f, 0f),
                new Vector3(0f, 90f, 0f),
                weaponScale);
        }

        Transform pistolRoot = null;
        if (pistolPrefab != null)
        {
            pistolSocket = EnsureSocket(
                rightHand,
                pistolSocket,
                GeneratedPistolSocketName,
                new Vector3(0f, 0.11f, 0.02f),
                new Vector3(357.87f, 0f, 85.09f));
            pistolRoot = EnsureWeaponChild(
                pistolSocket,
                pistolPrefab,
                pistolPrefab.name,
                new Vector3(0f, 0.02f, 0f),
                new Vector3(0f, 90f, 90f),
                weaponScale);
        }

        bool usePistol = weaponSettings.weaponKind == CharacterWeaponKind.Pistol;
        if (assaultRifleRoot != null)
        {
            assaultRifleRoot.gameObject.SetActive(!usePistol);
        }

        if (pistolRoot != null)
        {
            pistolRoot.gameObject.SetActive(usePistol);
        }

        if (leftHandGrip == null && assaultRifleRoot != null)
        {
            Transform existingGrip = assaultRifleRoot.Find(GeneratedLeftGripName);
            leftHandGrip = existingGrip != null
                ? existingGrip
                : new GameObject(GeneratedLeftGripName).transform;
            leftHandGrip.SetParent(assaultRifleRoot, false);
            leftHandGrip.localPosition = new Vector3(0f, -0.02f, -1.39f);
            leftHandGrip.localEulerAngles = new Vector3(0f, -90f, 0f);
            leftHandGrip.localScale = Vector3.one;
        }
    }

    private bool IsLockOnActive()
    {
        if (lockOnController != null && lockOnController.CurrentTarget != null)
        {
            return true;
        }

        return motor != null && motor.LockOnTarget != null;
    }

    private bool ResolveAimTargetPosition(out Vector3 targetPosition, out bool locked)
    {
        Transform lockTarget = lockOnController != null && lockOnController.CurrentTarget != null
            ? lockOnController.CurrentTarget
            : motor != null
                ? motor.LockOnTarget
                : null;
        if (lockTarget != null)
        {
            targetPosition = lockTarget.position;
            locked = true;
            return true;
        }

        if (cameraTransform != null)
        {
            targetPosition = cameraTransform.position + cameraTransform.forward * cameraAimDistance;
            locked = false;
            return true;
        }

        targetPosition = Vector3.zero;
        locked = false;
        return false;
    }

    private Transform ResolveActiveWeaponRoot()
    {
        if (weaponSettings.weaponKind == CharacterWeaponKind.Pistol)
        {
            return pistolSocket != null ? pistolSocket.Find(pistolPrefab != null ? pistolPrefab.name : "Human_Gun") : null;
        }

        return weaponSocket != null ? weaponSocket.Find(assaultRiflePrefab != null ? assaultRiflePrefab.name : "Human_AssaultRifle") : null;
    }

    private void ResetWeaponRootPose(Transform weaponRoot)
    {
        weaponRoot.localEulerAngles = weaponSettings.weaponKind == CharacterWeaponKind.Pistol
            ? new Vector3(0f, 90f, 90f)
            : new Vector3(0f, 90f, 0f);
    }

    private float ResolveFacingWeight(Vector3 targetPosition)
    {
        Transform basis = motor != null ? motor.transform : transform;
        Vector3 forward = basis.forward;
        Vector3 direction = targetPosition - basis.position;
        forward.y = 0f;
        direction.y = 0f;
        if (forward.sqrMagnitude < 0.001f || direction.sqrMagnitude < 0.001f)
        {
            return 1f;
        }

        float angle = Vector3.Angle(forward.normalized, direction.normalized);
        if (angle <= fullWeightFacingAngle)
        {
            return 1f;
        }

        if (angle >= maxFacingAngle)
        {
            return 0f;
        }

        return Mathf.InverseLerp(maxFacingAngle, fullWeightFacingAngle, angle);
    }

    private static Transform EnsureSocket(
        Transform rightHand,
        Transform socket,
        string socketName,
        Vector3 localPosition,
        Vector3 localEulerAngles)
    {
        if (socket == null)
        {
            Transform existingSocket = rightHand.Find(socketName);
            socket = existingSocket != null
                ? existingSocket
                : new GameObject(socketName).transform;
        }

        socket.SetParent(rightHand, false);
        socket.localPosition = localPosition;
        socket.localEulerAngles = localEulerAngles;
        socket.localScale = Vector3.one;
        return socket;
    }

    private static Transform EnsureWeaponChild(
        Transform socket,
        GameObject prefab,
        string instanceName,
        Vector3 localPosition,
        Vector3 localEulerAngles,
        float localScale)
    {
        Transform weaponRoot = socket.Find(instanceName);
        if (weaponRoot == null)
        {
            GameObject instance = Instantiate(prefab, socket);
            instance.name = instanceName;
            weaponRoot = instance.transform;
        }

        weaponRoot.localPosition = localPosition;
        weaponRoot.localEulerAngles = localEulerAngles;
        weaponRoot.localScale = Vector3.one * localScale;
        return weaponRoot;
    }

    private static float ResolveCompensatedScale(Transform parent)
    {
        Vector3 scale = parent.lossyScale;
        float averageScale = (Mathf.Abs(scale.x) + Mathf.Abs(scale.y) + Mathf.Abs(scale.z)) / 3f;
        if (averageScale <= 0.0001f)
        {
            return 1f;
        }

        return Mathf.Clamp(1f / averageScale, 1f, 100f);
    }
}
