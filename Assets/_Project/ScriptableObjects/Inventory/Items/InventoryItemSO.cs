using UnityEngine;

[CreateAssetMenu(fileName = "InventoryItemSO", menuName = "Inventory/Item")]
public class InventoryItemSO : ScriptableObject
{
    public int Id;
    public string Name;
    public Sprite Sprite;
    public GameObject VehicleAttachment;
    public GameObject VehicleAttachmentPreview;
}