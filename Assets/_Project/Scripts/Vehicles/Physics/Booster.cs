using UnityEngine;

public class Booster : MonoBehaviour, IThrustProvider
{
    [SerializeField] private float _boostForce = 1f;

    public Vector3 ThrustPosition()
    {
        return transform.position;
    }

    public Vector3 CalculateThrustForce()
    {
        return transform.forward * _boostForce;
    }

    public Vector3 Position(float time)
    {
        return transform.position;
    }

    public Vector3 Direction(float time)
    {
        return transform.forward;
    }

    public float ForceMagnitude(float time) {
        return _boostForce;
    }
}
