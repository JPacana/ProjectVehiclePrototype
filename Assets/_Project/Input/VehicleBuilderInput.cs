using UnityEngine;
using UnityEngine.InputSystem;

public class VehicleBuilderInput : MonoBehaviour
{
    [SerializeField] private VehicleBuilder _vehicleBuilder;

    [SerializeField] private InputActionReference _rotationAction;

    private void OnEnable()
    {
        _rotationAction.action.Enable();
        _rotationAction.action.performed += RotatePerformed;
    }

    private void RotatePerformed(InputAction.CallbackContext obj)
    {
        var rotateDirection = obj.ReadValue<float>();
        _vehicleBuilder.Rotate(rotateDirection);
    }
}
