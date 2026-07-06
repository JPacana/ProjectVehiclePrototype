using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;
using UnityEngine.EventSystems;

public class VehicleComponent : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private bool _flippable;
    public bool IsFlippable => _flippable;

    private bool _flipped;
    public bool IsFlipped => _flipped;
    
    [SerializeField] private bool _rotatable;
    public bool IsRotatable => _rotatable;

    [SerializeField] private Transform _layout1;
    [SerializeField] private Transform _layout2;
    
    public InventoryItemSO Item { get; set; }
    
    public event Action<PointerEventData, VehicleComponent> BeginDrag;
    public event Action<PointerEventData, VehicleComponent> Drag;
    public event Action<PointerEventData, VehicleComponent> EndDrag;
    
    void Awake()
    {
        if (_layout1 == null)
        {
            Debug.LogWarning($"VehicleComponent {name} has no _layout1 set.");
            return;
        }
        
        if (_flippable && _layout2 == null)
        {
            Debug.LogWarning($"Flippable VehicleComponent {name} has no _layout2 set.");
            _flippable = false;
        }
        _layout1.gameObject.SetActive(true);
        if (_flippable) _layout2.gameObject.SetActive(false);
    }

    public bool Flip()
    {
        if (!_flippable) return false;
        
        _flipped = !_flipped;

        _layout1.gameObject.SetActive(!_flipped);
        _layout2.gameObject.SetActive(_flipped);
        
        return true;
    }

    public bool Rotate(float degrees)
    {
        if (_rotatable)
            transform.Rotate(transform.up, degrees);
        
        return _rotatable;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        BeginDrag?.Invoke(eventData, this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Drag?.Invoke(eventData, this);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        EndDrag?.Invoke(eventData, this);
    }
}
