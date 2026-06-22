using UnityEngine;

public class PhysicalProperties : MonoBehaviour
{
    [SerializeField] private float _mass;
    public float Mass => _mass;
    
    [SerializeField] private Vector3 _centerOfMassOffset;
    public Vector3 CenterOfMassOffset => _centerOfMassOffset;
}
