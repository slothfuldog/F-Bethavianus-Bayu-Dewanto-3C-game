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
    }

    private void CheckVerticalAxisInput()
    {
        Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();

        if (moveInput != null)
        {
            OnMoveInput(moveInput);   
        }
    }

    private void CheckGoToMenuInput()
    {
        if (inputActions.Menu.GoToMenu.triggered)
        {
            Debug.Log("Go To Menu");
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
        if (inputActions.Player.Crouch.triggered)
        {
            Debug.Log("Crouch");
        }
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
            Debug.Log("Became first person POV");
        }
    }

    private void CheckClimbInput()
    {
        if (inputActions.Player.Climb.triggered)
        {
            OnClimbStartedInput();
            Debug.Log("climb");
        }
    }
    private void CheckGlideInput()
    {
        if (inputActions.Player.Glide.triggered)
        {
            Debug.Log("Gliding");
        }
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
            Debug.Log("Attack");
        }
    }

}
