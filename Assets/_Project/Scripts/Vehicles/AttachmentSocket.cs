using JetBrains.Annotations;
using UnityEngine;

public class AttachmentSocket : MonoBehaviour
{
    [SerializeField] private Vector3 _normal;

    [CanBeNull] private VehicleAttachment _attachedComponent;
    [CanBeNull] public VehicleAttachment AttachedComponent => _attachedComponent;

    public bool Occupied => _attachedComponent != null;

    public bool Attach(VehicleAttachment potentialAttachment)
    {
        if (Occupied) return false;
        _attachedComponent = potentialAttachment;
        return true;
    }
}
