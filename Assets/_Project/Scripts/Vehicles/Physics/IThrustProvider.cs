using UnityEngine;

public interface IThrustProvider
{
    public Vector3 Position(float time);
    public Vector3 Direction(float time);
    public float ForceMagnitude(float time);
}
