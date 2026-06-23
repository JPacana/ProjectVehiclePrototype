using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VehicleComponent : MonoBehaviour
{
    [SerializeField] private List<AttachmentSocket> _sockets;
    public List<AttachmentSocket> Sockets => _sockets;
    
    private Dictionary<AttachmentSocket, AttachmentSocket> _socketConnections = new();

    void Awake()
    {
        foreach (var socket in _sockets)
        {
            _socketConnections.Add(socket, null);
        }
    }
    
    public List<AttachmentSocket> AvailableSockets 
    {
        get
        {
            // Get all sockets that are available with nothing connected to them
            var availableSockets = _socketConnections.Where(pair => !pair.Value)
                .Select(pair => pair.Key)
                .ToList();
            return availableSockets;
        }
    }

    public bool AttachSockets(AttachmentSocket parentSocket, AttachmentSocket childSocket)
    {
        if (AvailableSockets.Contains(parentSocket))
        {
            _socketConnections[parentSocket] = childSocket;
            return true;
        }
        return false;
    }
}
