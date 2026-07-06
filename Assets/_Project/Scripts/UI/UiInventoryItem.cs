using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(InventoryItem))]
public class UiInventoryItem : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [SerializeField] private Image _image;
    public Image Image => _image;
    
    public InventoryItem Item { get; set; }

    private void Awake()
    {
        Item = GetComponent<InventoryItem>();
    }
    
    public event Action<PointerEventData, InventoryItemSO> BeginDrag;
    public event Action<PointerEventData, InventoryItemSO> Drag;
    public event Action<PointerEventData, InventoryItemSO> EndDrag;
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        BeginDrag?.Invoke(eventData, Item.Item);
    }

    private Vector3 _currentMouseWorldPosition;
    public void OnDrag(PointerEventData eventData)
    {
        Drag?.Invoke(eventData, Item.Item);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        EndDrag?.Invoke(eventData, Item.Item);
    }
}
