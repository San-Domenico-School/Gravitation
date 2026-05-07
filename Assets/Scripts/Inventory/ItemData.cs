using UnityEngine;

public enum ItemType
{
    RawResource,
    CraftedComponent,
    GravitonCell,
    Weapon,
    Tool,
    Equipment,
    Structure
}

public enum CraftingCategory
{
    BasicMaterials,
    Tech
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Gravitas/Items/ItemData")]
public class ItemData : ScriptableObject
{
    [SerializeField] public string itemName;
    [SerializeField] public ItemType itemType;
    [SerializeField] public CraftingCategory craftingCategory;
    [SerializeField] public Sprite icon;
    [TextArea] [SerializeField] public string description;
    [SerializeField] public bool isUnique;
    [SerializeField] public GravitonCell cellData;
    [Tooltip("Prefab spawned in the world when this item is dropped. Must have a WorldItem component.")]
    [SerializeField] public GameObject worldPrefab;

    [Header("Save System")]
    [Tooltip("Stable string ID used by the save system. Leave blank to default to the asset's name. Must be unique per item.")]
    [SerializeField] private string saveId;

    /// <summary>
    /// Stable ID used by the save system. Falls back to the ScriptableObject's
    /// asset name when no explicit saveId is set. Renaming the asset will break
    /// existing save files unless an explicit saveId is set first.
    /// </summary>
    public string SaveId => string.IsNullOrEmpty(saveId) ? name : saveId;
}
