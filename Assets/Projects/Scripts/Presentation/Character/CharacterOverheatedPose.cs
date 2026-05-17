using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class CharacterOverheatedPose : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterMotor motor;
    [SerializeField] private float movingSpeedThreshold = 0.2f;
    [SerializeField] private float blendSpeed = 5f;
    [SerializeField] private Vector3 spineOffset = new Vector3(8f, 0f, 0f);
    [SerializeField] private Vector3 chestOffset = new Vector3(10f, 0f, 0f);
    [SerializeField] private Vector3 headOffset = new Vector3(12f, 0f, 0f);
    [SerializeField] private Vector3 leftUpperArmOffset = new Vector3(0f, 0f, -10f);
    [SerializeField] private Vector3 rightUpperArmOffset = new Vector3(0f, 0f, 10f);

    private Transform spine;
    private Transform chest;
    private Transform head;
    private Transform leftUpperArm;
    private Transform rightUpperArm;
    private float currentWeight;

    private void Reset()
    {
        animator = GetComponent<Animator>();
        motor = GetComponentInParent<CharacterMotor>();
    }

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (motor == null)
        {
            motor = GetComponentInParent<CharacterMotor>();
        }

        CacheBones();
    }

    private void LateUpdate()
    {
        if (animator == null || !animator.isHuman || motor == null)
        {
            currentWeight = 0f;
            return;
        }

        float horizontalSpeed = new Vector3(motor.Velocity.x, 0f, motor.Velocity.z).magnitude;
        bool shouldPose = motor.State == CharacterLocomotionMode.Overheated
            && horizontalSpeed >= movingSpeedThreshold
            && !motor.IsClimbing;
        float targetWeight = shouldPose ? 1f : 0f;
        currentWeight = Mathf.MoveTowards(currentWeight, targetWeight, blendSpeed * Time.deltaTime);
        if (currentWeight <= 0.001f)
        {
            return;
        }

        ApplyLocalOffset(spine, spineOffset);
        ApplyLocalOffset(chest, chestOffset);
        ApplyLocalOffset(head, headOffset);
        ApplyLocalOffset(leftUpperArm, leftUpperArmOffset);
        ApplyLocalOffset(rightUpperArm, rightUpperArmOffset);
    }

    private void CacheBones()
    {
        if (animator == null || !animator.isHuman)
        {
            return;
        }

        spine = animator.GetBoneTransform(HumanBodyBones.Spine);
        chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        head = animator.GetBoneTransform(HumanBodyBones.Head);
        leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
    }

    private void ApplyLocalOffset(Transform bone, Vector3 eulerOffset)
    {
        if (bone == null)
        {
            return;
        }

        bone.localRotation *= Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(eulerOffset), currentWeight);
    }
}
