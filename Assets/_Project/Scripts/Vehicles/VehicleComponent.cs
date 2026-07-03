using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;

public class VehicleComponent : MonoBehaviour
{
    [SerializeField] private bool _flippable;
    public bool IsFlippable => _flippable;

    private bool _flipped;
    public bool IsFlipped => _flipped;
    
    [SerializeField] private bool _rotatable;
    public bool IsRotatable => _rotatable;

    [SerializeField] private Transform _layout1;
    [SerializeField] private Transform _layout2;
    
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
}
