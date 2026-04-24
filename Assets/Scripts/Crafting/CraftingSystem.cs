using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraftingSystem : MonoBehaviour
{
    public static CraftingSystem Instance { get; private set; }

    [SerializeField] private CraftingRecipe[] allRecipes;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        Debug.Log("awake on crafting system");
    }

    public List<CraftingRecipe> GetRecipesForTier(int tier)
    {
        var result = new List<CraftingRecipe>();
        foreach (var recipe in allRecipes)
            if (recipe.crafterTier == tier) result.Add(recipe);
        return result;
    }

    public bool CanCraft(CraftingRecipe recipe)
    {
        foreach (var ingredient in recipe.ingredients)
        {
            if (InventorySystem.Instance.CountItems(ingredient.item) < ingredient.count)
                return false;
        }
        return true;
    }

    public void Craft(CraftingRecipe recipe, float duration, Action onComplete)
    {
        StartCoroutine(CraftCoroutine(recipe, duration, onComplete));
    }

    private IEnumerator CraftCoroutine(CraftingRecipe recipe, float duration, Action onComplete)
    {
        foreach (var ingredient in recipe.ingredients)
            InventorySystem.Instance.TryConsumeItems(ingredient.item, ingredient.count);

        yield return new WaitForSecondsRealtime(duration);

        for (int i = 0; i < recipe.resultCount; i++)
            InventorySystem.Instance.TryAddItem(new InventoryItem(recipe.result));

        onComplete?.Invoke();
    }
}
