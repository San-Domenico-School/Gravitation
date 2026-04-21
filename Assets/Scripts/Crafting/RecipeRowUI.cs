using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RecipeRowUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI ingredientsText;
    [SerializeField] private Button craftButton;
    [SerializeField] private CanvasGroup canvasGroup;

    public ItemData ItemData { get; private set; }

    private Action<ItemData> onCraftClicked;

    public void Setup(ItemData data, Action<ItemData> craftCallback)
    {
        ItemData = data;
        onCraftClicked = craftCallback;

        if (iconImage != null) iconImage.sprite = data.icon;
        if (nameText != null) nameText.text = data.itemName;

        craftButton.onClick.AddListener(() => onCraftClicked?.Invoke(ItemData));

        Refresh();
    }

    // Re-evaluates ingredient counts and can-craft state. Called whenever inventory changes.
    public void Refresh()
    {
        bool canCraft = CraftingSystem.Instance.CanCraft(ItemData);

        craftButton.interactable = canCraft;

        if (canvasGroup != null)
            canvasGroup.alpha = canCraft ? 1f : 0.5f;

        if (ingredientsText != null)
        {
            var sb = new StringBuilder();
            foreach (var ingredient in ItemData.recipe)
            {
                if (ingredient.itemData == null) continue;
                int have = CraftingSystem.Instance.CountItemsWithData(ingredient.itemData);
                int need = ingredient.amount;
                sb.AppendLine($"{ingredient.itemData.itemName}  {have}/{need}");
            }
            ingredientsText.text = sb.ToString().TrimEnd();
        }
    }

    // Called by CraftingUI to block/unblock all buttons while a craft is in progress.
    public void SetCraftingLock(bool locked)
    {
        craftButton.interactable = !locked && CraftingSystem.Instance.CanCraft(ItemData);
    }
}
