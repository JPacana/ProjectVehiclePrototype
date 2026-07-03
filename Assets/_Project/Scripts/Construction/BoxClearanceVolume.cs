using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class BoxClearanceVolume : MonoBehaviour, IClearanceVolume
{
    private BoxCollider _collider;
    private readonly HashSet<Collider> _overlappedColliders = new();

    public HashSet<Collider> OverlappedColliders => _overlappedColliders;

    [SerializeField] private LayerMask _clearanceVolumeLayerMask;
    
    private Collider[] _hitColliders;
    private const int _maxHitColliders = 20;

    void Awake()
    {
        _collider = GetComponent<BoxCollider>();
        _hitColliders = new Collider[_maxHitColliders];
    }
    
    public Collider[] GetOverlappedClearanceVolumes()
    {
        // 1. Calculate the center in World Space
        Vector3 worldCenter = _collider.transform.TransformPoint(_collider.center);

        // 2. Calculate the half-extents (half of the lossy scale multiplied by collider size)
        Vector3 lossyScale = _collider.transform.lossyScale;
        Vector3 halfExtents = new Vector3(
            (_collider.size.x * lossyScale.x) / 2f,
            (_collider.size.y * lossyScale.y) / 2f,
            (_collider.size.z * lossyScale.z) / 2f
        );

        // 3. Get the world rotation of the transform
        Quaternion worldRotation = _collider.transform.rotation;

        // 4. Pass the parameters into the physics check
        Physics.OverlapBoxNonAlloc(worldCenter, halfExtents, _hitColliders, worldRotation, _clearanceVolumeLayerMask);
        
        if (_hitColliders == null) Debug.Log("_hitColliders is null!");
        if (_hitColliders.Length == 0) Debug.Log("_hitColliders is empty!");
            
        return _hitColliders.Where(c => c != null && !c.transform.IsChildOf(_collider.transform.parent)).ToArray();
    }
    
    
    public Collider[] GetOverlappedClearanceVolumes_BoxCollider()
    {
        if (_collider is not BoxCollider targetBoxCollider) return Array.Empty<Collider>();
        
        // 1. Calculate the center in World Space
        Vector3 worldCenter = targetBoxCollider.transform.TransformPoint(targetBoxCollider.center);

        // 2. Calculate the half-extents (half of the lossy scale multiplied by collider size)
        Vector3 lossyScale = targetBoxCollider.transform.lossyScale;
        Vector3 halfExtents = new Vector3(
            (targetBoxCollider.size.x * lossyScale.x) / 2f,
            (targetBoxCollider.size.y * lossyScale.y) / 2f,
            (targetBoxCollider.size.z * lossyScale.z) / 2f
        );

        // 3. Get the world rotation of the transform
        Quaternion worldRotation = targetBoxCollider.transform.rotation;

        // 4. Pass the parameters into the physics check
        //Collider[] hitColliders = Physics.OverlapBox(worldCenter, halfExtents, worldRotation, _clearanceVolumeLayerMask);
            
        Collider[] hitColliders = Physics.OverlapBox(worldCenter, halfExtents, worldRotation, _clearanceVolumeLayerMask);

        //// Works
        //List<Collider> returnColliders = new();
        //foreach (var hitCollider in hitColliders)
        //{
        //    Debug.Log($"{hitCollider.gameObject.name} (layer={hitCollider.gameObject.layer}) is overlapped by Clearance Volume on {targetBoxCollider.name} (layer={targetBoxCollider.gameObject.layer})");
        //    Debug.Log($"{hitCollider.transform.name}.IsChildOf({targetBoxCollider.transform.parent.name})");
        //    if (hitCollider.transform.IsChildOf(targetBoxCollider.transform.parent)) continue;
        //    returnColliders.Add(hitCollider);
        //}
        //return returnColliders.ToArray();
            
        return hitColliders.Where(c => !c.transform.IsChildOf(targetBoxCollider.transform.parent)).ToArray();
            
        // TODO: Eliminate ClearanceVolumes that are either this volume or a sibling
        //return hitColliders.Where(c => c.transform.IsChildOf(targetBoxCollider.transform.parent)).ToArray();
        //return hitColliders.Where(c => c.transform.IsChildOf(targetBoxCollider.transform.parent)).ToArray();
        //Debug.Log($"Physics.OverlapBox({worldCenter}, {halfExtents}, {worldRotation}, {_clearanceVolumeLayerMask.value})");
        //return hitColliders;
            
        //Debug.Log($"Transform position: {transform.position}");
        //Debug.Log($"collider.bounds.center: {collider.bounds.center}");
        //Debug.Log($"collider.center: {collider.center}");
        //Debug.Log($"Checking for overlaps at {transform.TransformPoint(collider.bounds.center)} and extents {collider.bounds.extents}");
        //return Physics.OverlapBox(transform.TransformPoint(collider.bounds.center), collider.bounds.extents, Quaternion.identity, LayerMask.GetMask("Clearance Volume"));
    }
}
