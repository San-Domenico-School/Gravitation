using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CraftingDatabase", menuName = "Gravitas/Crafting/CraftingDatabase")]
public class CraftingDatabase : ScriptableObject
{
    [Tooltip("Every craftable ItemData in the project. Add new items here to make them available in crafters.")]
    public List<ItemData> allCraftableItems = new List<ItemData>();
}
