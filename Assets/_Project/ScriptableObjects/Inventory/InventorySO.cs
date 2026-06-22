using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InventorySO", menuName = "Inventory/Inventory")]
public class InventorySO : ScriptableObject
{
   public List<InventoryItemSO> Items;
   public List<int> Quantity;
}
