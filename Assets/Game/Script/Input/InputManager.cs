using System;
using Unity.VisualScripting;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    private PlayerInputActions inputActions;
    public Action<Vector2> OnMoveInput;
    public Action<bool> OnSprintInput;
    public Action OnJumpStartedInput;
    public Action OnClimbStartedInput;
    public Action OnCancelClimb;
    public Action OnHideShowCursor;
    public Action OnChangePOV;
    public Action OnCrouching;
    public Action OnGliding;
    public Action OnAttack;
    public Action OnBackToMainMenu;
    public Action<Vector2> OnLookInput;
    private void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void Update()
    {
        CheckJumpInput();
        CheckCrouchInput();
        CheckSprintInput();
        CheckChangePOVInput();
        CheckClimbInput();
        CheckGlideInput();
        CheckCancelClimbAndGlideInput();
        CheckAttackInput();
        // CheckMoveForwardInput();
        // CheckMoveBackwardInput();
        // CheckMoveLeftInput();
        // CheckMoveRightInput();
        CheckGoToMenuInput();
        CheckVerticalAxisInput();
        CheckLookInput();
        HideShowCursorInput();

    }

    private void CheckVerticalAxisInput()
    {
        Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();

        OnMoveInput?.Invoke(moveInput);

    }

    private void CheckGoToMenuInput()
    {
        if (inputActions.Menu.GoToMenu.triggered)
        {
            OnBackToMainMenu?.Invoke();
        }
    }

    private void CheckJumpInput()
    {
        if (inputActions.Player.Jump.WasPressedThisFrame())
        {
            OnJumpStartedInput?.Invoke();
        }

    }

    private void CheckCrouchInput()
    {
        if (inputActions.Player.Crouch.triggered) OnCrouching?.Invoke();
    }
    private void CheckSprintInput()
    {
        // if (inputActions.Player.Sprint.triggered)
        // {
        //     OnSprintInput(true);
        // }
        // else
        // {
        //     OnSprintInput(false);
        // }
        // use bool isPressed = inputActions.Player.Sprint.ReadValue<float>() > 0.5f; if want threshold
        bool isPressed = inputActions.Player.Sprint.IsPressed();
        OnSprintInput?.Invoke(isPressed);
    }

    private void CheckChangePOVInput()
    {
        if (inputActions.Player.ChangePOV.triggered)
        {
            OnChangePOV();
        }
    }
    private void HideShowCursorInput()
    {
        if (inputActions.Player.CursorHideAndShow.triggered)
        {
            OnHideShowCursor();
        }
    }

    private void CheckClimbInput()
    {
        if (inputActions.Player.Climb.triggered)
        {
            OnClimbStartedInput();
        }
    }
    private void CheckGlideInput()
    {
        if (inputActions.Player.Glide.triggered) OnGliding?.Invoke();

    }

    private void CheckCancelClimbAndGlideInput()
    {
        if (inputActions.Player.CancelClimbGlide.triggered)
        {
            OnCancelClimb();
        }
    }

    private void CheckAttackInput()
    {
        if (inputActions.Player.Attack.triggered)
        {
            OnAttack?.Invoke();
        }
    }

    private void CheckLookInput()
    {
        Vector2 lookInput = inputActions.Player.Look.ReadValue<Vector2>();
        if (lookInput != Vector2.zero)
        {
            OnLookInput?.Invoke(lookInput);
        }
    }

}
