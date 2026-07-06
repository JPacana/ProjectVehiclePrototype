using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    public InventoryItemSO Item { get; set; }
    
    public event Action<PointerEventData, InventoryItem> BeginDrag;
    public event Action<PointerEventData, InventoryItem> Drag;
    public event Action<PointerEventData, InventoryItem> EndDrag;
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        BeginDrag?.Invoke(eventData, this);
    }

    private Vector3 _currentMouseWorldPosition;
    public void OnDrag(PointerEventData eventData)
    {
        Drag?.Invoke(eventData, this);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        EndDrag?.Invoke(eventData, this);
    }
}
