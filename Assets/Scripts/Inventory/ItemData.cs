using System.Collections.Generic;
using UnityEngine;

public enum ItemType
{
    RawResource,
    CraftedComponent,
    GravitonCell,
    Weapon
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Gravitas/Items/ItemData")]
public class ItemData : ScriptableObject
{
    [SerializeField] public string itemName;
    [SerializeField] public ItemType itemType;
    [SerializeField] public Sprite icon;
    [TextArea] [SerializeField] public string description;
    [SerializeField] public bool isUnique;
    [SerializeField] public GravitonCell cellData;
    [Tooltip("Prefab spawned in the world when this item is dropped. Must have a WorldItem component.")]
    [SerializeField] public GameObject worldPrefab;

    // --- NEW: Crafting ---
    [Header("Crafting")]
    [Tooltip("None = uncraftable. Determines which crafter tier(s) can produce this item.")]
    [SerializeField] public CraftingTier tier = CraftingTier.None;
    [Tooltip("Ingredients required. Leave empty for uncraftable items.")]
    [SerializeField] public List<Ingredient> recipe = new List<Ingredient>();
    [Tooltip("Reserved for a future placement system. No effect on crafting behaviour.")]
    [SerializeField] public bool isPlaceable = false;
}
