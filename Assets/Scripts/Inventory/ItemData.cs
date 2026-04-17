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
}
