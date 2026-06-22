using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryDrag : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [SerializeField] private InventoryItem _inventoryItem;
    public InventoryItem InventoryItem => _inventoryItem;
    
    private List<AttachmentSocket> _attachmentPoints;

    //public event Action PointerDown;
    //public event Action PointerClick;
    //public event Action PointerUp;

    public event Action<PointerEventData, InventoryItem> BeginDrag;
    public event Action<PointerEventData, InventoryItem> Drag;
    public event Action<PointerEventData, InventoryItem> EndDrag;

    public void OnPointerDown(PointerEventData eventData)
    {
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
    }

    public void OnPointerUp(PointerEventData eventData)
    {
    }

    private GameObject _instantiatedItem;
    private List<AttachmentSocket> _instantiatedAttachmentPoints;
    public void OnBeginDrag(PointerEventData eventData)
    {
        BeginDrag?.Invoke(eventData, _inventoryItem);
        
        //_attachmentPoints = FindObjectsByType<AttachmentSocket>(FindObjectsInactive.Exclude).ToList();
        //_instantiatedItem = Instantiate(_inventoryItem);
        //_instantiatedItem.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        //_instantiatedAttachmentPoints = _instantiatedItem.GetComponentsInChildren<AttachmentSocket>(true).ToList(); 
        //Debug.Log($"Total instantiated attachment points: {_instantiatedAttachmentPoints.Count}");
    }

    private Vector3 _currentMouseWorldPosition;
    public void OnDrag(PointerEventData eventData)
    {
        Drag?.Invoke(eventData, _inventoryItem);
        
        //// Raycast from the camera to the mouse position
        //Ray ray = Camera.main.ScreenPointToRay(eventData.position);
        //RaycastHit hit;
        //
        //if (Physics.Raycast(ray, out hit))
        //{
        //    //Debug.Log($"Hit: {hit.transform.gameObject.name}");
        //    // Spawn the 3D item at the world position where the raycast hit
        //    Vector3 spawnPosition = new Vector3(hit.point.x, hit.point.y, 0f);
        //    _currentMouseWorldPosition = spawnPosition; 

        //    _instantiatedItem.transform.position = spawnPosition;
        //    
        //    //Debug.Log($"Instantiated positions: {spawnPosition}");
        //}
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        EndDrag?.Invoke(eventData, _inventoryItem);
        
        //AttachmentSocket closestAttachmentSocket = null;
        //{
        //    float closestDistanceSqr = Mathf.Infinity;
        //    foreach (var targetAttachmentPoint in _attachmentPoints)
        //    {
        //        var distanceSqr = (targetAttachmentPoint.transform.position - _instantiatedItem.transform.position).sqrMagnitude;
        //        if (distanceSqr < closestDistanceSqr)
        //        {
        //            closestDistanceSqr = distanceSqr;
        //            closestAttachmentSocket = targetAttachmentPoint;
        //        }
        //    }
        //}
        //
        //AttachmentSocket closestInstantiatedAttachmentSocket = null;
        //{
        //    float closestDistanceSqr = Mathf.Infinity;
        //    foreach (var instantiatedAttachmentPoint in _instantiatedAttachmentPoints)
        //    {
        //        var distanceSqr = (instantiatedAttachmentPoint.transform.position - closestAttachmentSocket.transform.position).sqrMagnitude;
        //        if (distanceSqr < closestDistanceSqr)
        //        {
        //            closestDistanceSqr = distanceSqr;
        //            closestInstantiatedAttachmentSocket = instantiatedAttachmentPoint;
        //        }
        //    }
        //}
        //
        //// Figure out how to attach the two parts by calculating the offset
        //var finalSpawnedItem = Instantiate(_inventoryItem);
        ////finalSpawnedItem.transform.position = closestInstantiatedAttachmentSocket.transform.position - _instantiatedItem.transform.localPosition;
        //finalSpawnedItem.transform.position = closestAttachmentSocket.transform.position - closestInstantiatedAttachmentSocket.transform.localPosition;
        //
        //Destroy(_instantiatedItem);
    }

    private void OnDrawGizmos()
    {
        //if (_instantiatedItem == null) return;

        //AttachmentSocket closestAttachmentSocket = null;
        //{
        //    float closestDistanceSqr = Mathf.Infinity;
        //    foreach (var targetAttachmentPoint in _attachmentPoints)
        //    {
        //        var distanceSqr = (targetAttachmentPoint.transform.position - _instantiatedItem.transform.position).sqrMagnitude;
        //        if (distanceSqr < closestDistanceSqr)
        //        {
        //            closestDistanceSqr = distanceSqr;
        //            closestAttachmentSocket = targetAttachmentPoint;
        //        }
        //    }
        //}
        //
        //AttachmentSocket closestInstantiatedAttachmentSocket = null;
        //{
        //    float closestDistanceSqr = Mathf.Infinity;
        //    foreach (var instantiatedAttachmentPoint in _instantiatedAttachmentPoints)
        //    {
        //        var distanceSqr = (instantiatedAttachmentPoint.transform.position - closestAttachmentSocket.transform.position).sqrMagnitude;
        //        if (distanceSqr < closestDistanceSqr)
        //        {
        //            closestDistanceSqr = distanceSqr;
        //            closestInstantiatedAttachmentSocket = instantiatedAttachmentPoint;
        //        }
        //    }
        //}
        //
        //Debug.Log($"Closest Attachment Point: {closestAttachmentSocket}");
        //Debug.Log($"Closest Instantiated Attachment Point: {closestInstantiatedAttachmentSocket}");
        //
        //Gizmos.color = Color.green;
        //if (closestAttachmentSocket != null)
        //{
        //    Gizmos.DrawSphere(closestAttachmentSocket.transform.position, 0.1f);
        //}
        //
        //if (closestInstantiatedAttachmentSocket != null)
        //{
        //    Gizmos.DrawLine(closestInstantiatedAttachmentSocket.transform.position, closestInstantiatedAttachmentSocket.transform.position);
        //}
    
        //Gizmos.color = Color.yellowGreen;
        //if (closestInstantiatedAttachmentSocket != null)
        //{
        //    Gizmos.DrawSphere(closestInstantiatedAttachmentSocket.transform.position, 0.1f);
        //}

        ////foreach (var instantiatedAttachmentPoint in _instantiatedAttachmentPoints)
        ////{
        ////    foreach (var targetAttachmentSocket in _attachmentPoints)
        ////    {
        ////        
        ////    }
        ////}
    }
}
