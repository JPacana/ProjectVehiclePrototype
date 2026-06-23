using JetBrains.Annotations;
using UnityEngine;

public class AttachmentSocket : MonoBehaviour
{
    public Vector3 Forward => transform.forward;
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward);
    }
}
