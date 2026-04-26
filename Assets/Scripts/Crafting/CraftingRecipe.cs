using UnityEngine;

[System.Serializable]
public struct CraftingIngredient
{
    public ItemData item;
    public int count;
}

[CreateAssetMenu(fileName = "NewRecipe", menuName = "Gravitas/Crafting/Recipe")]
public class CraftingRecipe : ScriptableObject
{
    public ItemData result;
    public int resultCount = 1;
    public CraftingIngredient[] ingredients;
    public int crafterTier;
    public CraftingCategory category;
}
