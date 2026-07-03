using System.Collections.Generic;
using UnityEngine;

public interface IClearanceVolume
{
    public HashSet<Collider> OverlappedColliders { get; }
    public Collider[] GetOverlappedClearanceVolumes();
}
