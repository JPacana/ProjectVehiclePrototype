using UnityEngine;

[CreateAssetMenu(fileName = "InventoryItem", menuName = "Inventory/Item")]
public class InventoryItem : ScriptableObject
{
    public Sprite Sprite;
    public GameObject VehicleAttachment;
}