using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    public static InputHandler Singleton { get; private set; }

    private PlayerControls _controls;

    // --- POLLING (Continuous Values) ---
    public Vector2 MoveInput => _controls.Key.Move.ReadValue<Vector2>();
    public Vector2 MousePosition => _controls.Key.Pointer.ReadValue<Vector2>();
    
    public bool IsSprinting => _controls.Key.Sprint.IsPressed();

    // --- OBSERVER (Pulse Events) ---
    public event Action OnJumpTriggered;
    public event Action OnInteractTriggered;
    public event Action OnInventoryTriggered;
    public event Action<bool> OnSprintToggled; 

    private void Awake()
    {
        if (Singleton == null)
        {
            Singleton = this;
            _controls = new PlayerControls();
            
            BindActions();

            _controls.Enable();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void BindActions()
    {
        _controls.Key.Jump.performed += ctx => OnJumpTriggered?.Invoke();

        _controls.Key.Interact.performed += ctx => OnInteractTriggered?.Invoke();

        _controls.Key.Inventory.performed += ctx => OnInventoryTriggered?.Invoke();

        _controls.Key.Sprint.performed += ctx => OnSprintToggled?.Invoke(true);
        _controls.Key.Sprint.canceled += ctx => OnSprintToggled?.Invoke(false);
    }

    private void OnDestroy()
    {
        if (_controls != null)
        {
            _controls.Disable();
            _controls.Dispose();
        }
    }
}
