using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Player))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    private Player player;
    private PlayerInput input;

    private void Awake()
    {
        player = GetComponent<Player>();
        input = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        InputsSet();
    }

    private void InputsSet()
    {
        input.actions["Move"].performed += OnMove;
        input.actions["Move"].canceled += OnMove;

        input.actions["Jump"].performed += OnJump;
    }

    private void OnDisable()
    {
        input.actions["Move"].performed -= OnMove;
        input.actions["Move"].canceled -= OnMove;
        input.actions["Jump"].performed -= OnJump;
    }

    #region InputHandlers
    private void OnMove(InputAction.CallbackContext context)
    {
        player.Move(context.ReadValue<Vector2>());
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        player.Jump();
    }
    #endregion

}
