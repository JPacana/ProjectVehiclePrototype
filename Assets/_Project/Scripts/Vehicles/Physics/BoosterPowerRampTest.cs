using System.Collections.Generic;
using UnityEngine;

public class BoosterPowerRampTest : MonoBehaviour, IThrustProvider
{
    [SerializeField] private float _maxBoostForce = 1f;
    [SerializeField] private AnimationCurve _boostForceCurve;
    [SerializeField] private List<ThrusterAction> _actions;

    private void Awake()
    {
        _boostForceCurve = new AnimationCurve();
        foreach (var action in _actions)
        {
            Keyframe key = new Keyframe(action.Time, action.Value);
            key.inTangent = 0f;
            key.outTangent = float.PositiveInfinity;

            _boostForceCurve.AddKey(key);
        }
    }

    public Vector3 Position(float time)
    {
        return transform.position;
    }

    public Vector3 Direction(float time)
    {
        return transform.forward;
    }

    public float ForceMagnitude(float time)
    {
        if (_boostForceCurve[_boostForceCurve.length - 1].time < time) return 0f;
        return _boostForceCurve.Evaluate(time) * _maxBoostForce;
    }
}
