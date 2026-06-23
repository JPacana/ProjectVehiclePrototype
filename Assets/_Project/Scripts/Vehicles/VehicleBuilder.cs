using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class VehicleBuilder : MonoBehaviour
{
    [SerializeField] private VehicleBase _vehicleBase;

    [SerializeField] private InventorySO _initialInventory;

    [SerializeField] private List<InventoryItemSO> _inventory = new();
    public List<InventoryItemSO> Inventory => _inventory;
    
    [SerializeField] private LayerMask _buildSurfaceLayers;
    public LayerMask BuildSurfaceLayers => _buildSurfaceLayers;

    private GameObject _previewObject = null;
    
    private GameObject _previewGhostObject = null;
    
    private Dictionary<AttachmentSocket, VehicleAttachment> _attachmentSockets = new();

    void Awake()
    {
        foreach (var inventoryItem in _initialInventory.Items)
        {
            _inventory.Add(inventoryItem);
        }
    }

    private List<AttachmentSocket> _instantiatedSockets;
    public void StartPreview(InventoryItemSO item, Vector3 position)
    {
        
        // Create Preview object
        if (!_previewObject) _previewObject = Instantiate(item.VehicleAttachment);
        _instantiatedSockets = _previewObject.GetComponentsInChildren<AttachmentSocket>().ToList();
        
        // Create Preview Ghost object
        if (!_previewGhostObject) _previewGhostObject = Instantiate(item.VehicleAttachmentPreview);
        
        _previewObject.transform.position = position;
        if (!_vehicleBase.RootComponent)
        {
            // Does VehicleBase exist? If not, show the preview at the origin.
            _previewGhostObject.transform.position = _vehicleBase.transform.position;
        }
        else
        {
            // Otherwise, show the preview at the potential location.
            _previewGhostObject.transform.position = _vehicleBase.transform.position;
        }
    }
    
    public void UpdatePreview(InventoryItemSO item, Vector3 position)
    {
        _previewObject.transform.position = position;
        if (!_vehicleBase.RootComponent)
        {
            _previewGhostObject.transform.position = _vehicleBase.transform.position;
        }
        else
        {
            // Find the closest available socket to the position provided
            var closestAttachmentSocket = FindClosestAttachmentSocketToPosition(position, _vehicleBase.AvailableSockets);
            
            // Find a compatible socket on the instantiated object
            foreach (var instantiatedSocket in _instantiatedSockets)
            {
                if (instantiatedSocket.Forward == -closestAttachmentSocket.Forward)
                {
                    _previewGhostObject.transform.position = closestAttachmentSocket.transform.position -
                                                             instantiatedSocket.transform.localPosition;
                }
            }
        }
    }
    
    public void EndPreview(InventoryItemSO item, Vector3 position)
    {
        Destroy(_previewObject.gameObject);
        Destroy(_previewGhostObject.gameObject);

        if (!_vehicleBase.RootComponent)
        {
            var spawnPosition = _vehicleBase.transform.position;
            var newBaseComponent = Instantiate(item.VehicleAttachment, spawnPosition, Quaternion.identity, _vehicleBase.transform).GetComponent<VehicleComponent>();
            _vehicleBase.SetRootComponent(newBaseComponent);
        }
        else
        {
            var spawnPosition = _vehicleBase.transform.position;
            
            var newBaseComponent = Instantiate(item.VehicleAttachment, spawnPosition, Quaternion.identity, _vehicleBase.transform).GetComponent<VehicleComponent>();
            
            // Find the closest available socket to the position provided
            var closestAttachmentSocket = FindClosestAttachmentSocketToPosition(position, _vehicleBase.AvailableSockets);
            
            // Find a compatible socket on the instantiated object
            var foundValidSocket = false;
            foreach (var instantiatedSocket in newBaseComponent.AvailableSockets)
            {
                if (instantiatedSocket.Forward == -closestAttachmentSocket.Forward)
                {
                    foundValidSocket = true;
                    newBaseComponent.transform.position = closestAttachmentSocket.transform.position -
                                                             instantiatedSocket.transform.localPosition;
                    _vehicleBase.AddVehicleComponent(newBaseComponent, instantiatedSocket, closestAttachmentSocket);
                }
            }
            if (!foundValidSocket) Destroy(newBaseComponent.gameObject);
        }
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