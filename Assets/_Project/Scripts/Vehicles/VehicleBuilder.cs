using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Serialization;

public class VehicleBuilder : MonoBehaviour
{
    [SerializeField] private Plane _plane;

    [SerializeField] private Camera _buildCamera;
    
    [SerializeField] private VehicleBase _vehicleBase;

    [SerializeField] private InventorySO _initialInventory;

    [SerializeField] private List<InventoryItemSO> _inventory = new();
    public List<InventoryItemSO> Inventory => _inventory;
    
    [SerializeField] private LayerMask _buildSurfaceLayers;
    public LayerMask BuildSurfaceLayers => _buildSurfaceLayers;
    
    [SerializeField] private LayerMask _buildSocketLayers;
    private Collider[] _buildSocketColliders;
    private const int _maxBuildSocketColliders = 20;

    private GameObject _previewObject = null;

    void Awake()
    {
        foreach (var inventoryItem in _initialInventory.Items)
        {
            _inventory.Add(inventoryItem);
        }
        _buildSocketColliders = new Collider[_maxBuildSocketColliders];
    }
    
    void SnapGhostToTarget(BuildSocket targetBuildSocket)
    {
        BuildSocket bestGhostBuildSocket = null;
        float closestDistance = Mathf.Infinity;

        foreach (BuildSocket ghostSocket in _ghostSockets)
        {
            var d = (ghostSocket.transform.position - targetBuildSocket.transform.position).sqrMagnitude;
            if (d < closestDistance)
            {
                closestDistance = d;
                bestGhostBuildSocket = ghostSocket;
            }
        }

        if (bestGhostBuildSocket == null) return;
        
        // --- STEP 1: ORIENTATION (ROTATION) ---
        // Sockets connect face-to-face, meaning their forward vectors must look at each other.
        // We look in the OPPOSITE direction of the target socket's forward axis.
        Quaternion targetRotation = Quaternion.LookRotation(-targetBuildSocket.transform.forward, targetBuildSocket.transform.up);
        
        // Calculate the relative rotation deviation of our chosen ghost socket from its parent center
        Quaternion localSocketRot = Quaternion.Inverse(_previewObject.transform.rotation) * bestGhostBuildSocket.transform.rotation;

        // Apply the corrected global rotation to the ghost parent center
        _previewObject.transform.rotation = targetRotation * Quaternion.Inverse(localSocketRot);

        // --- STEP 2: POSITIONING (OFFSET) ---
        // Calculate where the ghost socket is sitting relative to the ghost's center pivot
        Vector3 localOffset = _previewObject.transform.InverseTransformPoint(bestGhostBuildSocket.transform.position);
        
        // Translate that local offset into the newly calculated world space orientation
        Vector3 worldOffset = _previewObject.transform.TransformDirection(localOffset);

        // Position the ghost center so the ghost socket perfectly hits the target socket coordinate
        _previewObject.transform.position = targetBuildSocket.transform.position - worldOffset;
    }

    private BuildSocket GetClosestAvailableWorldSocket(Vector3 mouseWorldPos)
    {
        float snapRadius = 0.25f;
        var numColliders = Physics.OverlapSphereNonAlloc(mouseWorldPos, snapRadius, _buildSocketColliders, _buildSocketLayers);
        BuildSocket closestSocket = null;
        float bestDistance = Mathf.Infinity;
        
        for (int i = 0; i < numColliders; i++)
        {
            // Ignore Build Sockets that are on the _previewObject
            if (_buildSocketColliders[i].transform.IsChildOf(_previewObject.transform)) continue;
            
            var socket = _buildSocketColliders[i].gameObject.GetComponent<BuildSocket>();
            if (socket != null && !socket.IsOccupied)
            {
                var d = (mouseWorldPos - _buildSocketColliders[i].transform.position).sqrMagnitude;
                if (d < bestDistance)
                {
                    bestDistance = d;
                    closestSocket = socket;
                }
            }
        }
        return closestSocket;
    }

    private BuildSocket[] _ghostSockets;
    private ClearanceVolume[] _ghostClearanceVolumes;
    public void StartPreview(InventoryItemSO item, Vector2 mousePosition)
    {
        if (!_previewObject) _previewObject = Instantiate(item.VehicleAttachmentPreview);
        _ghostSockets = _previewObject.GetComponentsInChildren<BuildSocket>();
        _ghostClearanceVolumes = _previewObject.GetComponentsInChildren<ClearanceVolume>();
    }
    
    public void UpdatePreview(InventoryItemSO item, Vector2 mousePosition)
    {
        if (!_vehicleBase.RootComponent)
        {
            _previewObject.transform.position = _vehicleBase.transform.position;
        }
        else
        {
            // Raycast from the camera to the mouse position
            Ray ray = _buildCamera.ScreenPointToRay(mousePosition);
            RaycastHit hit;

            _previewObject.transform.rotation = Quaternion.identity;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, _buildSurfaceLayers))
            {
                BuildSocket targetBuildSocket = GetClosestAvailableWorldSocket(hit.point);
                if (targetBuildSocket)
                {
                    SnapGhostToTarget(targetBuildSocket);
                }
                else
                {
                    _previewObject.transform.position = hit.point;
                }
            }
            
            // Check for clearances
            bool overlapsSomething = false;
            for (int i = 0; i < _ghostClearanceVolumes.Length; i++)
            {
                var overlappingClearanceVolumes = _ghostClearanceVolumes[i].OverlappedColliders
                    .Where(c => c.gameObject.layer == LayerMask.NameToLayer("Clearance Volume")).ToList().Count;
                if (overlappingClearanceVolumes > 0) overlapsSomething = true;
            }
            //if (overlapsSomething) Debug.Log("UpdatePreview: Ghost is overlapping an object.");
        }
    }

    private ClearanceVolume[] _vehicleClearanceVolumes;
    public void EndPreview(InventoryItemSO item, Vector3 position)
    {
        if (!_vehicleBase.RootComponent)
        {
            var spawnPosition = _vehicleBase.transform.position;
            var newBaseComponent = Instantiate(item.VehicleAttachment, spawnPosition, Quaternion.identity, _vehicleBase.transform).GetComponent<VehicleComponent>();
            _vehicleBase.SetRootComponent(newBaseComponent);
        }
        else
        {
            // Check for clearances
            bool overlapsSomething = false;
            for (int i = 0; i < _ghostClearanceVolumes.Length; i++)
            {
                ////var overlappingClearanceVolumes = _ghostClearanceVolumes[i].OverlappedColliders
                ////    .Where(c => c.gameObject.layer == LayerMask.GetMask("Clearance Volume")).ToList().Count;
                //var overlappingClearanceVolumes = _ghostClearanceVolumes[i].OverlappedColliders
                //    .Where(c => c.gameObject.layer == LayerMask.NameToLayer("Clearance Volume")).ToList().Count;
                //if (overlappingClearanceVolumes > 0) overlapsSomething = true;
                //overlapsSomething = _ghostClearanceVolumes[i].GetOverlappedClearanceVolumes().Length > 0;
                var overlappedClearanceVolumes = _ghostClearanceVolumes[i].GetOverlappedClearanceVolumes();
                if (overlappedClearanceVolumes.Length > 0) overlapsSomething = true;
                foreach (var overlappedClearanceVolume in overlappedClearanceVolumes)
                {
                    Debug.Log($"Ghost Clearance Volume {i} is overlapping {overlappedClearanceVolume.name}");
                }
            }
            //if (overlapsSomething) Debug.Log("EndPreview: Ghost is overlapping an object.");

            if (!overlapsSomething)
            {
                var newBaseComponent = Instantiate(
                    item.VehicleAttachment, 
                    _previewObject.transform.position, 
                    _previewObject.transform.rotation, 
                    _vehicleBase.transform).GetComponent<VehicleComponent>();
            }
        }
        
        _vehicleClearanceVolumes = _vehicleBase.GetComponentsInChildren<ClearanceVolume>();
        
        Destroy(_previewObject.gameObject);
    }
    
    private AttachmentSocket FindClosestAttachmentSocketToPosition(Vector3 position, List<AttachmentSocket> sockets)
    {
        AttachmentSocket closestAttachmentSocket = null;
        float closestDistanceSqr = Mathf.Infinity;
        foreach (var socket in sockets)
        {
            var distanceSqr = (socket.transform.position - position).sqrMagnitude;
            if (distanceSqr > closestDistanceSqr) continue;
            
            closestDistanceSqr = distanceSqr;
            closestAttachmentSocket = socket;
        }
        return closestAttachmentSocket;
    }

    //private List<AttachmentSocket> _instantiatedAttachmentSockets;
    //public void Preview(InventoryItemSO item, Vector3 position)
    //{
    //    if (_previewObject == null)
    //    {
    //        _previewObject = Instantiate(item.VehicleAttachment);
    //        _instantiatedAttachmentSockets = _previewObject.GetComponentsInChildren<AttachmentSocket>().ToList();
    //    }
    //    _previewObject.transform.position = position;
    //    
    //    if (_previewGhostObject == null)
    //    {
    //        _previewGhostObject = Instantiate(item.VehicleAttachmentPreview);
    //    }
    //    
    //    // Find the closest valid attachment point and instantiate / move the preview ghost there
    //    //if (_instantiatedAttachmentSockets.Count == 0)
    //    if (_attachmentSockets.Count == 0)
    //    {
    //        _previewGhostObject.transform.position = _vehicleBase.transform.position;
    //    }
    //    else
    //    {
    //        var closestAttachmentSocket = FindClosestTargetAttachmentSocket(position);

    //        var closestInstantiatedAttachmentSocket = FindClosestInstantiatedAttachmentSocket(closestAttachmentSocket);

    //        if (_previewGhostObject == null)
    //        {
    //            _previewGhostObject = Instantiate(item.VehicleAttachmentPreview);
    //        }
    //        _previewGhostObject.transform.position = closestAttachmentSocket.transform.position -
    //                                                 closestInstantiatedAttachmentSocket.transform.localPosition;
    //    }
    //}

    //private AttachmentSocket FindClosestInstantiatedAttachmentSocket(AttachmentSocket closestAttachmentSocket)
    //{
    //    AttachmentSocket closestInstantiatedAttachmentSocket = null;
    //    {
    //        float closestDistanceSqr = Mathf.Infinity;
    //        foreach (var instantiatedAttachmentPoint in _instantiatedAttachmentSockets)
    //        {
    //            var distanceSqr = (instantiatedAttachmentPoint.transform.position - closestAttachmentSocket.transform.position).sqrMagnitude;
    //            if (distanceSqr < closestDistanceSqr)
    //            {
    //                closestDistanceSqr = distanceSqr;
    //                closestInstantiatedAttachmentSocket = instantiatedAttachmentPoint;
    //            }
    //        }
    //    }
    //    return closestInstantiatedAttachmentSocket;
    //}

    //private AttachmentSocket FindClosestTargetAttachmentSocket(Vector3 position)
    //{
    //    AttachmentSocket closestAttachmentSocket = null;
    //    {
    //        float closestDistanceSqr = Mathf.Infinity;
    //        //foreach (var targetAttachmentSocket in _attachmentSockets)
    //        foreach (var targetAttachmentSocket in _attachmentSockets.Keys)
    //        {
    //            var distanceSqr = (targetAttachmentSocket.transform.position - position).sqrMagnitude;
    //            if (distanceSqr < closestDistanceSqr)
    //            {
    //                closestDistanceSqr = distanceSqr;
    //                closestAttachmentSocket = targetAttachmentSocket;
    //            }
    //        }
    //    }
    //    return closestAttachmentSocket;
    //}

    //public void PlaceAttachment(InventoryItemSO item, Vector3 position)
    //{
    //    Destroy(_previewObject.gameObject);
    //    Destroy(_previewGhostObject.gameObject);

    //    // Find the closest valid attachment point and instantiate / move the preview ghost there
    //    if (_attachmentSockets.Count == 0)
    //    {
    //        var spawnPosition = _vehicleBase.transform.position;
    //        Instantiate(item.VehicleAttachment, spawnPosition, Quaternion.identity, _vehicleBase.transform).GetComponent<VehicleAttachment>();
    //        
    //        var baseAttachmentSockets = _vehicleBase.GetComponentsInChildren<AttachmentSocket>().ToList();
    //        foreach (var attachmentSocket in baseAttachmentSockets)
    //        {
    //            _attachmentSockets.Add(attachmentSocket, null);
    //        }
    //    }
    //    else
    //    {
    //        var closestAttachmentSocket = FindClosestTargetAttachmentSocket(position);

    //        var closestInstantiatedAttachmentSocket = FindClosestInstantiatedAttachmentSocket(closestAttachmentSocket);
    //        
    //        Attach(closestAttachmentSocket, item, closestInstantiatedAttachmentSocket);
    //    }
    //}

    //public bool Attach(
    //    AttachmentSocket targetAttachmentSocket, 
    //    InventoryItemSO item,
    //    AttachmentSocket attachmentSocket)
    //{
    //    // Check if the AttachmentSocket is available...
    //    if (_attachmentSockets.TryGetValue(targetAttachmentSocket, out var currentAttachment))
    //    {
    //        if (currentAttachment) return false;

    //        var spawnPosition = targetAttachmentSocket.transform.position - attachmentSocket.transform.localPosition;
    //        var newAttachment = Instantiate(item.VehicleAttachment, spawnPosition, Quaternion.identity, _vehicleBase.transform.GetChild(0)).GetComponent<VehicleAttachment>();
    //        _attachmentSockets[targetAttachmentSocket] = newAttachment;

    //        var newAttachmentSockets = newAttachment.GetComponentsInChildren<AttachmentSocket>();
    //        if (newAttachment.Terminal)
    //        {
    //            // Disable all attachments if it's terminal. Nothing can be attached to this.
    //            foreach (var newAttachmentSocket in newAttachmentSockets)
    //            {
    //                newAttachmentSocket.gameObject.SetActive(false);
    //            }
    //        }
    //        else
    //        {
    //            foreach (var newAttachmentSocket in newAttachmentSockets)
    //            {
    //                if (newAttachmentSocket == attachmentSocket)
    //                {
    //                    newAttachmentSocket.gameObject.SetActive(false);
    //                }
    //                else
    //                {
    //                    _attachmentSockets.Add(newAttachmentSocket, null);
    //                }
    //            }
    //        }
    //        
    //        return true;
    //    }

    //    return false;
    //}
}