using System.Collections.Generic;
using UnityEngine;

public class VehicleBuilder : MonoBehaviour
{
    [SerializeField] private GameObject _root;

    [SerializeField] private Inventory _initialInventory;

    [SerializeField] private List<InventoryItem> _inventory = new();
    public List<InventoryItem> Inventory => _inventory;
    
    [SerializeField] private List<VehicleAttachment> _attachments = new();

    [SerializeField] private LayerMask _buildSurfaceLayers;
    public LayerMask BuildSurfaceLayers => _buildSurfaceLayers;

    [SerializeField] private Material _previewGhostMaterial;

    void Awake()
    {
        foreach (var inventoryItem in _initialInventory.Items)
        {
            _inventory.Add(inventoryItem);
        }
    }

    public void SetRoot(GameObject root)
    {
        _root = root;
    }

    private GameObject _previewObject = null;
    private GameObject _previewGhostObject = null;
    public void Preview(InventoryItem item, Vector3 position)
    {
        if (_previewObject == null)
        {
            _previewObject = Instantiate(item.VehicleAttachment);
        }
        //_previewObject.transform.position = position;
        
        // Find the closest valid attachment point and instantiate / move the preview ghost there
        if (_previewGhostObject == null)
        {
            _previewGhostObject = Instantiate(item.VehicleAttachment);
        }
        var ghostMeshRenderers = _previewGhostObject.GetComponentsInChildren<MeshRenderer>();
        foreach (var renderer in ghostMeshRenderers)
        {
            renderer.material = _previewGhostMaterial;
        }
        _previewGhostObject.transform.position = position;
    }

    public void PlaceAttachment(InventoryItem item, Vector3 position)
    {
        Destroy(_previewObject.gameObject);
        Destroy(_previewGhostObject.gameObject);
        
        // Using the provided position, determine the best place to spawn the attachment
        var newAttachment = Instantiate(item.VehicleAttachment, position, Quaternion.identity, _root.transform.GetChild(0));
    }

    public bool Attach(
        AttachmentSocket targetAttachmentSocket, 
        VehicleAttachment attachment,
        AttachmentSocket attachmentSocket)
    {
        return false;
    }
}
