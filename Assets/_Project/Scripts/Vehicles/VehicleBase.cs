using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VehicleBase : MonoBehaviour
{
    private VehicleComponent _rootComponent;
    public VehicleComponent RootComponent => _rootComponent;

    private HashSet<VehicleComponent> _components = new();

    public bool SetRootComponent(VehicleComponent bodyRoot)
    {
        if (_rootComponent) return false;
        
        _rootComponent = bodyRoot;

        // Set components
        _components.Clear();
        _components.Add(bodyRoot);
        
        return true;
    }
}
