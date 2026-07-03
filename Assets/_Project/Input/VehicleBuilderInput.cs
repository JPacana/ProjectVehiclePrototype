using UnityEngine;
using UnityEngine.InputSystem;

public class VehicleBuilderInput : MonoBehaviour
{
    [SerializeField] private VehicleBuilder _vehicleBuilder;

    [SerializeField] private InputActionReference _rotationAction;
    [SerializeField] private InputActionReference _flipAction;

    private void OnEnable()
    {
        _rotationAction.action.Enable();
        _rotationAction.action.performed += RotatePerformed;
        
        _flipAction.action.Enable();
        _flipAction.action.performed += FlipPerformed;

    }

    private void RotatePerformed(InputAction.CallbackContext obj)
    {
        var rotateDirection = obj.ReadValue<float>();
        _vehicleBuilder.Rotate(rotateDirection);
    }

    private void FlipPerformed(InputAction.CallbackContext obj)
    {
        _vehicleBuilder.Flip();
    }
}
