using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class CharacterInputReader : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Player";
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string lookActionName = "Look";
    [SerializeField] private string jumpActionName = "Jump";
    [SerializeField] private string sprintActionName = "Sprint";
    [SerializeField] private string quickBoostActionName = "QuickBoost";
    [SerializeField] private string assaultBoostActionName = "AssaultBoost";
    [SerializeField] private string aimActionName = "Aim";
    [SerializeField] private string fireActionName = "Attack";
    [SerializeField] private string lockOnActionName = "LockOn";
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference lookAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference sprintAction;
    [SerializeField] private InputActionReference quickBoostAction;
    [SerializeField] private InputActionReference assaultBoostAction;
    [SerializeField] private InputActionReference aimAction;
    [SerializeField] private InputActionReference fireAction;
    [SerializeField] private InputActionReference lockOnAction;

    private InputAction cachedMoveAction;
    private InputAction cachedLookAction;
    private InputAction cachedJumpAction;
    private InputAction cachedSprintAction;
    private InputAction cachedQuickBoostAction;
    private InputAction cachedAssaultBoostAction;
    private InputAction cachedAimAction;
    private InputAction cachedFireAction;
    private InputAction cachedLockOnAction;

    public CharacterInputSnapshot ReadSnapshot()
    {
        Vector2 move = ReadVector2(moveAction, ref cachedMoveAction, moveActionName);
        return new CharacterInputSnapshot(
            move.sqrMagnitude > 0.001f ? move : ReadKeyboardMove(),
            ReadVector2(lookAction, ref cachedLookAction, lookActionName),
            WasPressedThisFrame(jumpAction, ref cachedJumpAction, jumpActionName) || WasPressedThisFrame(Key.Space),
            IsPressed(sprintAction, ref cachedSprintAction, sprintActionName) || IsPressed(Key.LeftShift) || IsPressed(Key.RightShift),
            WasPressedThisFrame(quickBoostAction, ref cachedQuickBoostAction, quickBoostActionName) || WasPressedThisFrame(Key.E),
            IsPressed(assaultBoostAction, ref cachedAssaultBoostAction, assaultBoostActionName) || IsPressed(Key.Q),
            IsPressed(aimAction, ref cachedAimAction, aimActionName) || IsRightMousePressed(),
            WasPressedThisFrame(fireAction, ref cachedFireAction, fireActionName) || WasLeftMousePressedThisFrame(),
            WasPressedThisFrame(lockOnAction, ref cachedLockOnAction, lockOnActionName) || WasPressedThisFrame(Key.Tab) || WasMiddleMousePressedThisFrame());
    }

    private void OnEnable()
    {
        EnableAction(moveAction, ref cachedMoveAction, moveActionName);
        EnableAction(lookAction, ref cachedLookAction, lookActionName);
        EnableAction(jumpAction, ref cachedJumpAction, jumpActionName);
        EnableAction(sprintAction, ref cachedSprintAction, sprintActionName);
        EnableAction(quickBoostAction, ref cachedQuickBoostAction, quickBoostActionName);
        EnableAction(assaultBoostAction, ref cachedAssaultBoostAction, assaultBoostActionName);
        EnableAction(aimAction, ref cachedAimAction, aimActionName);
        EnableAction(fireAction, ref cachedFireAction, fireActionName);
        EnableAction(lockOnAction, ref cachedLockOnAction, lockOnActionName);
    }

    private void OnDisable()
    {
        DisableAction(moveAction, ref cachedMoveAction, moveActionName);
        DisableAction(lookAction, ref cachedLookAction, lookActionName);
        DisableAction(jumpAction, ref cachedJumpAction, jumpActionName);
        DisableAction(sprintAction, ref cachedSprintAction, sprintActionName);
        DisableAction(quickBoostAction, ref cachedQuickBoostAction, quickBoostActionName);
        DisableAction(assaultBoostAction, ref cachedAssaultBoostAction, assaultBoostActionName);
        DisableAction(aimAction, ref cachedAimAction, aimActionName);
        DisableAction(fireAction, ref cachedFireAction, fireActionName);
        DisableAction(lockOnAction, ref cachedLockOnAction, lockOnActionName);
    }

    private InputAction ResolveAction(InputActionReference actionReference, ref InputAction cachedAction, string actionName)
    {
        if (actionReference != null && actionReference.action != null)
        {
            return actionReference.action;
        }

        if (cachedAction != null)
        {
            return cachedAction;
        }

        if (inputActions == null || string.IsNullOrWhiteSpace(actionName))
        {
            return null;
        }

        string actionPath = string.IsNullOrWhiteSpace(actionMapName)
            ? actionName
            : $"{actionMapName}/{actionName}";
        cachedAction = inputActions.FindAction(actionPath, false);
        return cachedAction;
    }

    private Vector2 ReadVector2(InputActionReference actionReference, ref InputAction cachedAction, string actionName)
    {
        InputAction action = ResolveAction(actionReference, ref cachedAction, actionName);
        return action != null ? action.ReadValue<Vector2>() : Vector2.zero;
    }

    private void EnableAction(InputActionReference actionReference, ref InputAction cachedAction, string actionName)
    {
        InputAction action = ResolveAction(actionReference, ref cachedAction, actionName);
        if (action != null)
        {
            action.Enable();
        }
    }

    private void DisableAction(InputActionReference actionReference, ref InputAction cachedAction, string actionName)
    {
        InputAction action = ResolveAction(actionReference, ref cachedAction, actionName);
        if (action != null)
        {
            action.Disable();
        }
    }

    private bool IsPressed(InputActionReference actionReference, ref InputAction cachedAction, string actionName)
    {
        InputAction action = ResolveAction(actionReference, ref cachedAction, actionName);
        return action != null && action.IsPressed();
    }

    private bool WasPressedThisFrame(InputActionReference actionReference, ref InputAction cachedAction, string actionName)
    {
        InputAction action = ResolveAction(actionReference, ref cachedAction, actionName);
        return action != null && action.WasPressedThisFrame();
    }

    private static Vector2 ReadKeyboardMove()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return Vector2.zero;
        }

        Vector2 move = Vector2.zero;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
        {
            move.y += 1f;
        }

        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
        {
            move.y -= 1f;
        }

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            move.x += 1f;
        }

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            move.x -= 1f;
        }

        return Vector2.ClampMagnitude(move, 1f);
    }

    private static bool IsPressed(Key key)
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard[key].isPressed;
    }

    private static bool WasPressedThisFrame(Key key)
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard[key].wasPressedThisFrame;
    }

    private static bool IsRightMousePressed()
    {
        Mouse mouse = Mouse.current;
        return mouse != null && mouse.rightButton.isPressed;
    }

    private static bool WasLeftMousePressedThisFrame()
    {
        Mouse mouse = Mouse.current;
        return mouse != null && mouse.leftButton.wasPressedThisFrame;
    }

    private static bool WasMiddleMousePressedThisFrame()
    {
        Mouse mouse = Mouse.current;
        return mouse != null && mouse.middleButton.wasPressedThisFrame;
    }
}
