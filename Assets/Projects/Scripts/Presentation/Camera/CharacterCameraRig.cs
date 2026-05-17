using UnityEngine;

public sealed class CharacterCameraRig : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private CharacterInputReader inputReader;
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.45f, 0f);
    [SerializeField] private float distance = 5.5f;
    [SerializeField] private float height = 1.1f;
    [SerializeField] private float yawSensitivity = 0.12f;
    [SerializeField] private float pitchSensitivity = 0.1f;
    [SerializeField] private float minPitch = -25f;
    [SerializeField] private float maxPitch = 65f;
    [SerializeField] private float positionSharpness = 18f;
    [SerializeField] private float rotationSharpness = 24f;

    private float yaw;
    private float pitch = 18f;

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        CharacterInputSnapshot input = inputReader != null ? inputReader.ReadSnapshot() : CharacterInputSnapshot.None;
        Vector2 look = input.Look;
        yaw += look.x * yawSensitivity;
        pitch = Mathf.Clamp(pitch - look.y * pitchSensitivity, minPitch, maxPitch);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 focusPoint = target.position + targetOffset;
        Vector3 desiredPosition = focusPoint + Vector3.up * height - rotation * Vector3.forward * distance;

        float positionT = 1f - Mathf.Exp(-positionSharpness * Time.deltaTime);
        float rotationT = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, positionT);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotationT);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SetInputReader(CharacterInputReader newInputReader)
    {
        inputReader = newInputReader;
    }
}
