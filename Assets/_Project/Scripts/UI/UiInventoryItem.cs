using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UiInventoryItem : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [SerializeField] private Image _image;
    public Image Image => _image;
    
    public InventoryItem Item { get; set; }
    
    public event Action<PointerEventData, InventoryItem> BeginDrag;
    public event Action<PointerEventData, InventoryItem> Drag;
    public event Action<PointerEventData, InventoryItem> EndDrag;
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        BeginDrag?.Invoke(eventData, Item);
    }

    private Vector3 _currentMouseWorldPosition;
    public void OnDrag(PointerEventData eventData)
    {
        Drag?.Invoke(eventData, Item);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        EndDrag?.Invoke(eventData, Item);
    }
}
