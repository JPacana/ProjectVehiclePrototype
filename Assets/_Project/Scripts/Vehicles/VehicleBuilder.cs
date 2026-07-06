using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.EventSystems;
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

    private GameObject _ghostObject = null;
    private GameObject _dragObject = null;
    
    void Awake()
    {
        foreach (var inventoryItem in _initialInventory.Items)
        {
            _inventory.Add(inventoryItem);
        }
        _buildSocketColliders = new Collider[_maxBuildSocketColliders];
    }
    
    private bool GetClosestAvailableWorldSocket(Vector3 worldPosition, out BuildSocket closestSocket, out float distanceSqr)
    {
        float snapRadius = 0.7071f;
        var numColliders = Physics.OverlapSphereNonAlloc(worldPosition, snapRadius, _buildSocketColliders, _buildSocketLayers);
        
        closestSocket = null;
        distanceSqr = Mathf.Infinity;
        
        for (int i = 0; i < numColliders; i++)
        {
            // Ignore Build Sockets that are on the _ghostObject
            if (_buildSocketColliders[i].transform.IsChildOf(_ghostObject.transform)) continue;
            if (_buildSocketColliders[i].transform.IsChildOf(_dragObject.transform)) continue;
            
            var socket = _buildSocketColliders[i].gameObject.GetComponent<BuildSocket>();
            if (socket != null && !socket.IsOccupied)
            {
                var d = (worldPosition - _buildSocketColliders[i].transform.position).sqrMagnitude;
                if (d < distanceSqr)
                {
                    distanceSqr = d;
                    closestSocket = socket;
                }
            }
        }
        return (closestSocket != null);
    }

    private BuildSocket GetClosestAvailableWorldSocket(Vector3 mouseWorldPos)
    {
        //float snapRadius = 0.25f;
        float snapRadius = 0.7071f;
        var numColliders = Physics.OverlapSphereNonAlloc(mouseWorldPos, snapRadius, _buildSocketColliders, _buildSocketLayers);
        BuildSocket closestSocket = null;
        float bestDistance = Mathf.Infinity;
        
        for (int i = 0; i < numColliders; i++)
        {
            // Ignore Build Sockets that are on the _ghostObject
            if (_buildSocketColliders[i].transform.IsChildOf(_ghostObject.transform)) continue;
            
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
    private IClearanceVolume[] _ghostClearanceVolumes;
    private BuildSocket[] _dragSockets;
    private IClearanceVolume[] _dragClearanceVolumes;
    private InventoryItemSO _currentItem;
    private Vector2 _lastMousePosition;
    public void StartPreview(InventoryItemSO item, bool flipped = false)
    {
        _currentItem = item;
        
        if (!_dragObject) _dragObject = Instantiate(item.VehicleAttachmentDrag);
        _dragSockets = _dragObject.GetComponentsInChildren<BuildSocket>();
        _dragClearanceVolumes = _dragObject.GetComponentsInChildren<IClearanceVolume>();
        
        if (!_ghostObject) _ghostObject = Instantiate(item.VehicleAttachmentPreview);
        _ghostSockets = _ghostObject.GetComponentsInChildren<BuildSocket>();
        _ghostClearanceVolumes = _ghostObject.GetComponentsInChildren<IClearanceVolume>();
    }
    
    public void UpdatePreview(InventoryItemSO item, Vector2 mousePosition)
    {
        _lastMousePosition = mousePosition;
        
        // Raycast from the camera to the mouse position
        Ray ray = _buildCamera.ScreenPointToRay(mousePosition);
        RaycastHit hit;

        var validMouseHit = Physics.Raycast(ray, out hit, Mathf.Infinity, _buildSurfaceLayers);
        
        if (validMouseHit)
            _dragObject.transform.position = hit.point;

        if (!_vehicleBase.RootComponent)
        {
            _ghostObject.transform.position = _vehicleBase.transform.position;
            _ghostObject.transform.rotation = _dragObject.transform.rotation;
        }
        else
        {
            //_ghostObject.transform.rotation = Quaternion.identity;
            //_dragObject.transform.rotation = Quaternion.identity;
            
            if (validMouseHit)
            { 
                // For each socket on the _dragObject, find the closest socket among nearby build socket colliders
                BuildSocket closestDragSocket = null;
                BuildSocket closestTargetSocket = null;
                float closestDistance = Mathf.Infinity;
            
                for (int i = 0; i < _dragSockets.Length; i++)
                {
                    if (GetClosestAvailableWorldSocket(
                            _dragSockets[i].transform.position, 
                            out BuildSocket closestSocket,
                            out float distanceSqr))
                    {
                        if (distanceSqr < closestDistance)
                        {
                            closestDragSocket = _dragSockets[i];
                            closestTargetSocket = closestSocket;
                            closestDistance = distanceSqr;
                        }
                    }
                }
            
                // By this point, we should know which 2 sockets are closest to each other
                
                if (closestDragSocket != null && closestTargetSocket != null)
                {
                    var originalDragObjectPosition = _dragObject.transform.position;
                    var originalDragObjectRotation = _dragObject.transform.rotation;
                    TransformObjectToConnectSockets(_dragObject, closestDragSocket, closestTargetSocket);
                    _ghostObject.transform.SetPositionAndRotation(_dragObject.transform.position, _dragObject.transform.rotation);
                    _dragObject.transform.SetPositionAndRotation(originalDragObjectPosition, originalDragObjectRotation);
                }
                else
                {
                    _ghostObject.transform.SetPositionAndRotation(_dragObject.transform.position, _dragObject.transform.rotation);
                }
            }

            //_ghostObject.transform.rotation = Quaternion.identity;
            //if (Physics.Raycast(ray, out hit, Mathf.Infinity, _buildSurfaceLayers))
            //{
            //    BuildSocket targetBuildSocket = GetClosestAvailableWorldSocket(hit.point);
            //    if (targetBuildSocket)
            //    {
            //        SnapGhostToTarget(targetBuildSocket);
            //    }
            //    else
            //    {
            //        _ghostObject.transform.position = hit.point;
            //    }
            //}
            //
            //// Check for clearances
            //bool overlapsSomething = false;
            //for (int i = 0; i < _ghostClearanceVolumes.Length; i++)
            //{
            //    var overlappingClearanceVolumes = _ghostClearanceVolumes[i].OverlappedColliders
            //        .Where(c => c.gameObject.layer == LayerMask.NameToLayer("Clearance Volume")).ToList().Count;
            //    if (overlappingClearanceVolumes > 0) overlapsSomething = true;
            //}
            ////if (overlapsSomething) Debug.Log("UpdatePreview: Ghost is overlapping an object.");
        }
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
        Quaternion localSocketRot = Quaternion.Inverse(_ghostObject.transform.rotation) * bestGhostBuildSocket.transform.rotation;

        // Apply the corrected global rotation to the ghost parent center
        _ghostObject.transform.rotation = targetRotation * Quaternion.Inverse(localSocketRot);

        // --- STEP 2: POSITIONING (OFFSET) ---
        // Calculate where the ghost socket is sitting relative to the ghost's center pivot
        Vector3 localOffset = _ghostObject.transform.InverseTransformPoint(bestGhostBuildSocket.transform.position);
        
        // Translate that local offset into the newly calculated world space orientation
        Vector3 worldOffset = _ghostObject.transform.TransformDirection(localOffset);

        // Position the ghost center so the ghost socket perfectly hits the target socket coordinate
        _ghostObject.transform.position = targetBuildSocket.transform.position - worldOffset;
    }

    private void TransformObjectToConnectSockets(GameObject sourceObject, BuildSocket sourceSocket, BuildSocket destinationSocket)
    {
        // --- STEP 1: ORIENTATION (ROTATION) ---
        // Sockets connect face-to-face, meaning their forward vectors must look at each other.
        // We look in the OPPOSITE direction of the destination socket's forward axis.
        Quaternion targetRotation = Quaternion.LookRotation(-destinationSocket.transform.forward, destinationSocket.transform.up);
        
        // Calculate the relative rotation deviation of our chosen ghost socket from its parent center
        Quaternion localSocketRot = Quaternion.Inverse(sourceObject.transform.rotation) * sourceSocket.transform.rotation;
        //Debug.Log($"localSocketRot = {localSocketRot.eulerAngles}");

        // Apply the corrected global rotation to the ghost parent center
        sourceObject.transform.rotation = targetRotation * Quaternion.Inverse(localSocketRot);

        // --- STEP 2: POSITIONING (OFFSET) ---
        // Calculate where the ghost socket is sitting relative to the ghost's center pivot
        Vector3 localOffset = sourceObject.transform.InverseTransformPoint(sourceSocket.transform.position);
        
        // Translate that local offset into the newly calculated world space orientation
        Vector3 worldOffset = sourceObject.transform.TransformDirection(localOffset);

        // Position the ghost center so the ghost socket perfectly hits the target socket coordinate
        sourceObject.transform.position = destinationSocket.transform.position - worldOffset;
    }

    private IClearanceVolume[] _vehicleClearanceVolumes;
    public void EndPreview(InventoryItemSO item, Vector3 position)
    {
        ConstructVehicleComponent(item);
        //if (!_vehicleBase.RootComponent)
        //{
        //    var spawnPosition = _vehicleBase.transform.position;
        //    var newBaseComponent = Instantiate(item.VehicleAttachment, spawnPosition, _dragObject.transform.rotation, _vehicleBase.transform).GetComponent<VehicleComponent>();
        //    
        //    if (_dragObject.GetComponent<VehicleComponent>().IsFlipped)
        //        newBaseComponent.Flip();
        //    
        //    _vehicleBase.SetRootComponent(newBaseComponent);
        //}
        //else
        //{
        //    // Check for clearances
        //    bool overlapsSomething = false;
        //    for (int i = 0; i < _ghostClearanceVolumes.Length; i++)
        //    {
        //        var overlappedClearanceVolumes = _ghostClearanceVolumes[i].GetOverlappedClearanceVolumes();
        //        
        //        // Eliminate any detected collisions that occur with the _dragObject since we don't care about those
        //        //var filteredOverlappedClearanceVolumes = overlappedClearanceVolumes.Where(c => !c.transform.IsChildOf(_dragObject.transform)).ToArray();
        //        
        //        //if (filteredOverlappedClearanceVolumes.Length > 0) overlapsSomething = true;
        //        if (overlappedClearanceVolumes.Length > 0) overlapsSomething = true;
        //        foreach (var overlappedClearanceVolume in overlappedClearanceVolumes)
        //        {
        //            Debug.Log($"Ghost Clearance Volume {i} is overlapping {overlappedClearanceVolume.name}");
        //        }
        //    }
        //    //if (overlapsSomething) Debug.Log("EndPreview: Ghost is overlapping an object.");

        //    if (!overlapsSomething)
        //    {
        //        var newBaseComponent = Instantiate(
        //            item.VehicleAttachment, 
        //            _ghostObject.transform.position, 
        //            _ghostObject.transform.rotation, 
        //            _vehicleBase.transform).GetComponent<VehicleComponent>();
        //        
        //        if (_dragObject.GetComponent<VehicleComponent>().IsFlipped)
        //            newBaseComponent.Flip();

        //        var vc = newBaseComponent.GetComponent<VehicleComponent>();
        //        
        //        vc.BeginDrag += VehicleComponent_OnBeginDrag;
        //        vc.Drag += VehicleComponent_OnDrag;
        //        vc.EndDrag += VehicleComponent_OnEndDrag;
        //    }
        //}
        //
        //_vehicleClearanceVolumes = _vehicleBase.GetComponentsInChildren<IClearanceVolume>();
        
        Destroy(_ghostObject.gameObject);
        Destroy(_dragObject.gameObject);
    }

    public void ConstructVehicleComponent(InventoryItemSO item)
    {
        if (!_vehicleBase.RootComponent)
        {
            var spawnPosition = _vehicleBase.transform.position;
            var newBaseComponent = Instantiate(item.VehicleAttachment, spawnPosition, _dragObject.transform.rotation, _vehicleBase.transform).GetComponent<VehicleComponent>();
            newBaseComponent.Item = item;
            
            if (_dragObject.GetComponent<VehicleComponent>().IsFlipped)
                newBaseComponent.Flip();
            
            _vehicleBase.SetRootComponent(newBaseComponent);
        }
        else
        {
            // Check for clearances
            bool overlapsSomething = false;
            for (int i = 0; i < _ghostClearanceVolumes.Length; i++)
            {
                var overlappedClearanceVolumes = _ghostClearanceVolumes[i].GetOverlappedClearanceVolumes();
                
                // Eliminate any detected collisions that occur with the _dragObject since we don't care about those
                //var filteredOverlappedClearanceVolumes = overlappedClearanceVolumes.Where(c => !c.transform.IsChildOf(_dragObject.transform)).ToArray();
                
                //if (filteredOverlappedClearanceVolumes.Length > 0) overlapsSomething = true;
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
                    _ghostObject.transform.position, 
                    _ghostObject.transform.rotation, 
                    _vehicleBase.transform).GetComponent<VehicleComponent>();
                
                newBaseComponent.Item = item;
                
                if (_dragObject.GetComponent<VehicleComponent>().IsFlipped)
                    newBaseComponent.Flip();

                var vc = newBaseComponent.GetComponent<VehicleComponent>();
                
                vc.BeginDrag += VehicleComponent_OnBeginDrag;
                vc.Drag += VehicleComponent_OnDrag;
                vc.EndDrag += VehicleComponent_OnEndDrag;
            }
        }
        
        _vehicleClearanceVolumes = _vehicleBase.GetComponentsInChildren<IClearanceVolume>();
    }

    public void MoveVehicleComponent(VehicleComponent vehicleComponent)
    {
        // Check for clearances
        bool overlapsSomething = false;
        for (int i = 0; i < _ghostClearanceVolumes.Length; i++)
        {
            var overlappedClearanceVolumes = _ghostClearanceVolumes[i].GetOverlappedClearanceVolumes();
                
            // Eliminate any detected collisions that occur with the _dragObject since we don't care about those
            //var filteredOverlappedClearanceVolumes = overlappedClearanceVolumes.Where(c => !c.transform.IsChildOf(_dragObject.transform)).ToArray();
                
            //if (filteredOverlappedClearanceVolumes.Length > 0) overlapsSomething = true;
            if (overlappedClearanceVolumes.Length > 0) overlapsSomething = true;
            foreach (var overlappedClearanceVolume in overlappedClearanceVolumes)
            {
                Debug.Log($"Ghost Clearance Volume {i} is overlapping {overlappedClearanceVolume.name}");
            }
        }

        if (!overlapsSomething)
        {
            vehicleComponent.transform.SetPositionAndRotation(_ghostObject.transform.position, _ghostObject.transform.rotation);
                
            if (_dragObject.GetComponent<VehicleComponent>().IsFlipped)
                vehicleComponent.Flip();
        }
        
        _vehicleClearanceVolumes = _vehicleBase.GetComponentsInChildren<IClearanceVolume>();
    }

    private void VehicleComponent_OnBeginDrag(PointerEventData arg1, VehicleComponent vehicleComponent)
    {
        StartPreview(vehicleComponent.Item);
    }

    private void VehicleComponent_OnDrag(PointerEventData arg1, VehicleComponent vehicleComponent)
    {
        UpdatePreview(vehicleComponent.Item, arg1.position);
    }

    private void VehicleComponent_OnEndDrag(PointerEventData arg1, VehicleComponent vehicleComponent)
    {
        MoveVehicleComponent(vehicleComponent);
        
        Destroy(_ghostObject.gameObject);
        Destroy(_dragObject.gameObject);
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
    
    // Build Controls
    private const float _rotationAmount = 45f;
    public void Rotate(float direction)
    {
        var rotated = _dragObject.GetComponent<VehicleComponent>().Rotate(direction * _rotationAmount);
        
        UpdatePreview(null, _lastMousePosition);
        
        Debug.Log($"Rotation successful: {rotated}");
        
        //if (_dragObject != null)
        //    _dragObject.transform.Rotate(_dragObject.transform.up, direction * _rotationAmount);
    }

    private bool _flipped = false;
    public void Flip()
    {
        if (_dragObject == null) return;
        
        Debug.Log("TODO: Figure out a good way to flip a component.");

        if (_currentItem == null) return;
        
        _dragObject.GetComponent<VehicleComponent>().Flip();
        _ghostObject.GetComponent<VehicleComponent>().Flip();
        
        _dragSockets = _dragObject.GetComponentsInChildren<BuildSocket>();
        _dragClearanceVolumes = _dragObject.GetComponentsInChildren<IClearanceVolume>();
        
        UpdatePreview(null, _lastMousePosition);
        
        //_flipped = !_flipped;
        //
        //Destroy(_dragObject);
        //_dragObject = null;
        //
        //Destroy(_ghostObject);
        //_ghostObject = null;

        //StartPreview(_currentItem, _flipped);

        //_dragObject.transform.Rotate(_dragObject.transform.forward, 180f);

        //var currentScale = _dragObject.transform.localScale;
        //_dragObject.transform.localScale = new Vector3(-1f * currentScale.x, 1f * currentScale.y, 1f * currentScale.z);
        //_ghostObject.transform.localScale = _dragObject.transform.localScale;
    }
}