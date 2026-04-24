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
}
