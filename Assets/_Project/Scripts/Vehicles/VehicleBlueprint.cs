using System.Collections.Generic;
using UnityEngine;

public class VehicleBlueprint : MonoBehaviour
{
    [SerializeField] private GameObject _root;
    public GameObject Root => _root;
    
    private List<VehicleAttachment> _vehicleAttachments;
    
    
}
