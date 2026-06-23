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
    
    public List<AttachmentSocket> Sockets 
    {
        // Get all Sockets from all VehicleComponents
        get
        {
            var sockets = new List<AttachmentSocket>();
            foreach (var component in _components)
            {
                sockets.AddRange(component.Sockets);
            }
            return sockets;
        }
    }

    public List<AttachmentSocket> AvailableSockets 
    {
        // Get all AvailableSockets from all VehicleComponents
        get
        {
            var availableSockets = new List<AttachmentSocket>();
            foreach (var component in _components)
            {
                availableSockets.AddRange(component.AvailableSockets);
            }
            return availableSockets;
        }
    }

    public bool AddVehicleComponent(
        VehicleComponent component, 
        AttachmentSocket newAttachmentSocket,
        AttachmentSocket parentAttachmentSocket)
    {
        if (AvailableSockets.Contains(parentAttachmentSocket))
        {
            _rootComponent.AttachSockets(parentAttachmentSocket, newAttachmentSocket);
            _components.Add(component);
            return true;
        }
        return false;
    }
}
