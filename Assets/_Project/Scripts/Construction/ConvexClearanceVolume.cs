using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(MeshCollider))]
public class ConvexClearanceVolume : MonoBehaviour, IClearanceVolume
{
    private MeshCollider _collider;
    private readonly HashSet<Collider> _overlappedColliders = new();

    public HashSet<Collider> OverlappedColliders => _overlappedColliders;

    [SerializeField] private LayerMask _clearanceVolumeLayerMask;
    
    private Collider[] _hitColliders;
    private const int _maxHitColliders = 20;

    private float _allowablePenetrationDistance = 0.0001f;
    
    void Awake()
    {
        _collider = GetComponent<MeshCollider>();
        _hitColliders = new Collider[_maxHitColliders];
        
        if (!_collider.convex)
        {
            Debug.LogWarning($"ConvexClearanceVolume does not have a convex collider. Automatically setting it to convex.");
            _collider.convex = true;
        }
    }
    
    public Collider[] GetOverlappedClearanceVolumes()
    {
        // First get all nearby colliders...
        Bounds bounds = _collider.bounds;
        
        // 1. Calculate the center in World Space
        //Vector3 worldCenter = _collider.transform.TransformPoint(bounds.center);
        Vector3 worldCenter = bounds.center;

        // 2. Calculate the half-extents (half of the lossy scale multiplied by collider size)
        Vector3 lossyScale = _collider.transform.lossyScale;
        Vector3 halfExtents = new Vector3(
            (bounds.size.x * lossyScale.x) / 2f,
            (bounds.size.y * lossyScale.y) / 2f,
            (bounds.size.z * lossyScale.z) / 2f
        );

        // 3. Get the world rotation of the transform
        Quaternion worldRotation = _collider.transform.rotation;

        // 4. Pass the parameters into the physics check
        Physics.OverlapBoxNonAlloc(worldCenter, halfExtents, _hitColliders, worldRotation, _clearanceVolumeLayerMask);
        
        // Now that we have a set of nearby colliders, we can perform penetration testing on this subset.
        List<Collider> hitColliders = new(_hitColliders.Length);
        
        foreach (var collider in _hitColliders)
        {
            if (collider == null) continue;
            if (Physics.ComputePenetration(_collider, _collider.transform.position, _collider.transform.rotation,
                    collider, collider.transform.position, collider.transform.rotation,
                    out _, out var distance))
            {
                if (distance < _allowablePenetrationDistance)
                {
                    Debug.Log($"ConvexClearanceVolume hit {collider.transform.parent.parent.name} but penetration distance was acceptable ({distance} < {_allowablePenetrationDistance}).");
                    continue;
                }
                Debug.Log($"ConvexClearanceVolume hit {collider.transform.parent.parent.name} with distance {distance}.");
                hitColliders.Add(collider);
            }
        }
            
        //return _hitColliders.Where(c => !c.transform.IsChildOf(_collider.transform.parent)).ToArray();
        return hitColliders.Where(c => !c.transform.IsChildOf(_collider.transform.parent)).ToArray();
    }
}
