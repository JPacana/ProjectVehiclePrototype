using System.Collections.Generic;
using UnityEngine;

public class VehiclePhysicsCore : MonoBehaviour
{
    private IThrustProvider[] _thrustProviders;
    private Rigidbody _rigidbody;
    
    [SerializeField] private List<ThrusterAction> _thrusterActions;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _thrustProviders = GetComponentsInChildren<IThrustProvider>();
    }

    private float _currentTime = 0f;
    private void FixedUpdate()
    {
        Debug.Log("FixedUpdate...");
        
        Debug.Log("Applying gravity.");
        _rigidbody.AddForce(new Vector3(0f, 0f, -1f) * 9.81f, ForceMode.Acceleration);
        
        foreach (IThrustProvider thrustProvider in _thrustProviders)
        {
            var thrust = thrustProvider.Direction(_currentTime) * thrustProvider.ForceMagnitude(_currentTime);
            Debug.Log($"Adding {thrust} thrust at {thrustProvider.Position(_currentTime)}");
            _rigidbody.AddForceAtPosition(thrust, thrustProvider.Position(_currentTime));
        }
        _currentTime += Time.fixedDeltaTime;
    }
}
