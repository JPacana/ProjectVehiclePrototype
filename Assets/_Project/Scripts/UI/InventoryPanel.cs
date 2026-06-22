using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryPanel : MonoBehaviour
{
    [SerializeField] private VehicleBuilder _builder;

    [SerializeField] private Transform _contentContainer;

    [SerializeField] private GameObject _inventoryItemPrefab;

    void Start()
    {
        foreach (var inventoryItem in _builder.Inventory)
        {
            var uiInventoryItem = Instantiate(_inventoryItemPrefab, _contentContainer).GetComponent<UiInventoryItem>();
            uiInventoryItem.Image.sprite = inventoryItem.Sprite;
            uiInventoryItem.Item = inventoryItem;
            
            uiInventoryItem.Drag += OnDrag;
            uiInventoryItem.BeginDrag += OnBeginDrag;
            uiInventoryItem.EndDrag += OnEndDrag;
        }
    }
    
    private void OnBeginDrag(PointerEventData eventData, InventoryItem obj)
    {
        // Raycast from the camera to the mouse position
        Ray ray = Camera.main.ScreenPointToRay(eventData.position);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit))
        {
            Vector3 spawnPosition = new Vector3(hit.point.x, hit.point.y, 0f);
            _builder.Preview(obj, spawnPosition);
        }
    }

    private void OnDrag(PointerEventData eventData, InventoryItem obj)
    {
        // Raycast from the camera to the mouse position
        Ray ray = Camera.main.ScreenPointToRay(eventData.position);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, _builder.BuildSurfaceLayers))
        {
            Vector3 spawnPosition = new Vector3(hit.point.x, hit.point.y, 0f);
            _builder.Preview(obj, spawnPosition);
        }
    }

    private void OnEndDrag(PointerEventData eventData, InventoryItem obj)
    {
        Ray ray = Camera.main.ScreenPointToRay(eventData.position);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, _builder.BuildSurfaceLayers))
        {
            Vector3 spawnPosition = new Vector3(hit.point.x, hit.point.y, 0f);
            _builder.PlaceAttachment(obj, spawnPosition);
        }
    }
}
