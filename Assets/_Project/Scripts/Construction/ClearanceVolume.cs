using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ClearanceVolume : MonoBehaviour
{
    private Collider _collider;
    private readonly HashSet<Collider> _overlappedColliders = new();
    public HashSet<Collider> OverlappedColliders => _overlappedColliders;

    [SerializeField] private LayerMask _clearanceVolumeLayerMask;

    void Awake()
    {
        _collider = GetComponent<Collider>();
    }
    
    //void OnTriggerEnter(Collider other)
    //{
    //    if (other.transform.IsChildOf(transform.parent)) return;
    //    _overlappedColliders.Add(other);
    //}

    //void OnTriggerExit(Collider other)
    //{
    //    if (other.transform.IsChildOf(transform.parent)) return;
    //    _overlappedColliders.Remove(other);
    //}
    
    public Collider[] GetOverlappedClearanceVolumes()
    {
        if (_collider is BoxCollider targetBoxCollider)
        {
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
            
            // TODO: Eliminate ClearanceVolumes that are either this volume or a sibling
            return hitColliders.Where(c => c.transform.IsChildOf(targetBoxCollider.transform.parent)).ToArray();
            //Debug.Log($"Physics.OverlapBox({worldCenter}, {halfExtents}, {worldRotation}, {_clearanceVolumeLayerMask.value})");
            //return hitColliders;
            
            //Debug.Log($"Transform position: {transform.position}");
            //Debug.Log($"collider.bounds.center: {collider.bounds.center}");
            //Debug.Log($"collider.center: {collider.center}");
            //Debug.Log($"Checking for overlaps at {transform.TransformPoint(collider.bounds.center)} and extents {collider.bounds.extents}");
            //return Physics.OverlapBox(transform.TransformPoint(collider.bounds.center), collider.bounds.extents, Quaternion.identity, LayerMask.GetMask("Clearance Volume"));
        }
        return Array.Empty<Collider>();
    }
}
