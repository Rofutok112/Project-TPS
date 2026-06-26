using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CharacterStatusHudView : MonoBehaviour
{
    [SerializeField] private Image driveFill;
    [SerializeField] private RectTransform driveFillRect;
    [SerializeField] private Text driveText;
    [SerializeField] private Text modeText;
    [SerializeField] private Text groundedText;
    [SerializeField] private Text lockOnText;
    [SerializeField] private Text speedText;
    [SerializeField] private Text climbText;
    [SerializeField] private Color normalDriveColor = new Color(0.2f, 0.75f, 1f);
    [SerializeField] private Color overheatedDriveColor = new Color(1f, 0.35f, 0.2f);
    [SerializeField] private Color activeColor = new Color(0.35f, 1f, 0.55f);
    [SerializeField] private Color inactiveColor = new Color(0.82f, 0.86f, 0.9f);

    public void Render(CharacterStatusHudViewModel model)
    {
        float driveNormalized = Mathf.Clamp01(model.DriveNormalized);
        if (driveFill != null)
        {
            driveFill.color = model.IsOverheated ? overheatedDriveColor : normalDriveColor;
        }

        if (driveFillRect != null)
        {
            driveFillRect.localScale = new Vector3(driveNormalized, 1f, 1f);
        }

        SetText(driveText, $"Energy {(driveNormalized * 100f):0}%");
        SetText(modeText, $"State  {model.ModeText}");
        SetText(groundedText, model.GroundedText);
        SetText(lockOnText, model.LockOnText);
        SetText(speedText, $"Speed  {model.SpeedText}");
        SetText(climbText, $"Climb  {model.ClimbText}");

        if (groundedText != null)
        {
            groundedText.color = model.IsClimbing ? activeColor : inactiveColor;
        }

        if (lockOnText != null)
        {
            lockOnText.color = model.HasLockOnTarget ? activeColor : inactiveColor;
        }
    }

    private static void SetText(Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }
}
