using JetBrains.Annotations;
using UnityEngine;

public class AttachmentSocket : MonoBehaviour
{
    [CanBeNull] private VehicleAttachment _attachedComponent;
    [CanBeNull] public VehicleAttachment AttachedComponent => _attachedComponent;
}
