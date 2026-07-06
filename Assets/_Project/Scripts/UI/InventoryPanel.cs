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
            
            //// Trying to generate a thumbnail
            //var thumbnailGenerator = FindAnyObjectByType<ThumbnailGenerator>();
            //var thumbnail = thumbnailGenerator.Renderer.GetThumbnail(inventoryItem.Id);
            //var thumbnailReady = !thumbnailGenerator.Renderer.IsPending(inventoryItem.Id);
            //Debug.Log($"Item Id = {inventoryItem.Id} pending -> {thumbnailReady}");
            ////var sprite = Sprite.Create(thumbnail, new Rect(0, 0, 1024, 1024), new Vector2(0.5f, 0.5f));
            ////uiInventoryItem.Image.sprite = sprite;
            //uiInventoryItem.Image.texture = thumbnail;
            ////uiInventoryItem.Image.sprite = inventoryItem.Sprite;
             
            uiInventoryItem.Image.sprite = inventoryItem.Sprite;
            
            uiInventoryItem.Item.Item = inventoryItem;
            
            uiInventoryItem.Drag += OnDrag;
            uiInventoryItem.BeginDrag += OnBeginDrag;
            uiInventoryItem.EndDrag += OnEndDrag;
        }
    }
    
    private void OnBeginDrag(PointerEventData eventData, InventoryItemSO item)
    {
        //// Raycast from the camera to the mouse position
        //Ray ray = Camera.main.ScreenPointToRay(eventData.position);
        //RaycastHit hit;
        //
        //if (Physics.Raycast(ray, out hit))
        //{
        //    Vector3 spawnPosition = new Vector3(hit.point.x, hit.point.y, 0f);
        //    _builder.StartPreviewOfExistingVehicleComponent(obj, spawnPosition);
        //}
        // Raycast from the camera to the mouse position

        //_builder.StartPreviewOfExistingVehicleComponent(obj, false);
        _builder.StartPreviewOfInventoryItem(item);
    }

    private void OnDrag(PointerEventData eventData, InventoryItemSO item)
    {
        //// Raycast from the camera to the mouse position
        //Ray ray = Camera.main.ScreenPointToRay(eventData.position);
        //RaycastHit hit;
        //
        //if (Physics.Raycast(ray, out hit, Mathf.Infinity, _builder.BuildSurfaceLayers))
        //{
        //    Vector3 spawnPosition = new Vector3(hit.point.x, hit.point.y, 0f);
        //    _builder.UpdatePreviewOfExistingVehicleComponent(obj, spawnPosition);
        //}
        //_builder.UpdatePreviewOfExistingVehicleComponent(obj, eventData.position);
        _builder.UpdatePreviewOfInventoryItem(eventData.position);
    }

    private void OnEndDrag(PointerEventData eventData, InventoryItemSO item)
    {
        Ray ray = Camera.main.ScreenPointToRay(eventData.position);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, _builder.BuildSurfaceLayers))
        {
            Vector3 spawnPosition = new Vector3(hit.point.x, hit.point.y, 0f);
            //_builder.PlaceAttachment(obj, spawnPosition);
            //_builder.EndPreviewOfExistingVehicleComponent(obj, spawnPosition);
            _builder.EndPreviewOfInventoryItem(item);
        }
    }
}
