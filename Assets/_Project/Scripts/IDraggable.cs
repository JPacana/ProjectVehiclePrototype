using System;
using UnityEngine;
using UnityEngine.EventSystems;

public interface IDraggable
{
    public event Action<PointerEventData> BeginDrag;
    public event Action<PointerEventData> Drag;
    public event Action<PointerEventData> EndDrag;
}
