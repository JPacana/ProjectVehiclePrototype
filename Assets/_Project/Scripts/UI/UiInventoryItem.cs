using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UiInventoryItem : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [SerializeField] private Image _image;
    public Image Image => _image;
    
    public InventoryItemSO Item { get; set; }
    
    public event Action<PointerEventData, InventoryItemSO> BeginDrag;
    public event Action<PointerEventData, InventoryItemSO> Drag;
    public event Action<PointerEventData, InventoryItemSO> EndDrag;
    
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
