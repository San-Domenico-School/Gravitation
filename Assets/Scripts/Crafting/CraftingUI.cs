using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

[RequireComponent(typeof(UIDocument))]
public class CraftingUI : MonoBehaviour
{
    public static CraftingUI Instance { get; private set; }

    [SerializeField] private InputActionReference cancelAction;

    private const float CraftDuration = 2f;

    private UIDocument document;
    private VisualElement root;

    private Label crafterTitle;
    private VisualElement categoryList;
    private VisualElement recipeList;
    private VisualElement detailIcon;
    private Label detailName;
    private Label detailDesc;
    private VisualElement ingredientsList;
    private Button craftBtn;
    private VisualElement craftProgressFill;
    private VisualElement progressBg;

    private CrafterInteractable activeCrafter;
    private List<CraftingRecipe> currentRecipes = new();
    private CraftingCategory selectedCategory = CraftingCategory.BasicMaterials;
    private CraftingRecipe selectedRecipe;
    private bool isCrafting;

    public bool IsOpen => root != null && root.style.display == DisplayStyle.Flex;
    public bool IsOpenForCrafter(CrafterInteractable crafter) => IsOpen && activeCrafter == crafter;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        document = GetComponent<UIDocument>();
    }

    private void Start()
    {
        root = document.rootVisualElement.Q("crafting-root");
        crafterTitle = root.Q<Label>("crafter-title");
        categoryList = root.Q("category-list");
        recipeList = root.Q("recipe-list");
        detailIcon = root.Q("detail-icon");
        detailName = root.Q<Label>("detail-name");
        detailDesc = root.Q<Label>("detail-desc");
        ingredientsList = root.Q("ingredients-list");
        craftBtn = root.Q<Button>("craft-btn");
        craftProgressFill = root.Q("craft-progress-fill");
        progressBg = root.Q("craft-progress-bg");

        craftBtn.clicked += OnCraftClicked;
        root.Q<Button>("close-btn").clicked += Close;

        root.style.display = DisplayStyle.None;

        BuildCategoryButtons();
    }

    private void OnEnable()
    {
        if (cancelAction != null)
            cancelAction.action.performed += OnCancel;
    }

    private void OnDisable()
    {
        if (cancelAction != null)
            cancelAction.action.performed -= OnCancel;
    }

    private void OnCancel(InputAction.CallbackContext ctx)
    {
        if (IsOpen) Close();
    }

    // ── Public API ──────────────────────────────────────────────────────────

    public void Open(CrafterInteractable crafter)
    {
        activeCrafter = crafter;
        crafterTitle.text = $"TIER {crafter.tier} FABRICATOR";

        currentRecipes = CraftingSystem.Instance.GetRecipesForTier(crafter.tier);

        selectedCategory = CraftingCategory.BasicMaterials;
        selectedRecipe = null;
        isCrafting = false;
        SetProgressWidth(0f);

        RefreshCategoryButtons();
        RefreshRecipeList();
        ClearDetail();

        root.style.display = DisplayStyle.Flex;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        InventorySystem.Instance.OnInventoryChanged += OnInventoryChanged;
    }

    public void Close()
    {
        if (!IsOpen) return;

        StopAllCoroutines();
        isCrafting = false;

        root.style.display = DisplayStyle.None;
        activeCrafter = null;

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        InventorySystem.Instance.OnInventoryChanged -= OnInventoryChanged;
    }

    // ── Internal ─────────────────────────────────────────────────────────────

    private void BuildCategoryButtons()
    {
        categoryList.Clear();
        foreach (CraftingCategory cat in System.Enum.GetValues(typeof(CraftingCategory)))
        {
            var btn = new Button();
            btn.text = CategoryDisplayName(cat);
            btn.AddToClassList("category-btn");
            var captured = cat;
            btn.clicked += () => SelectCategory(captured);
            categoryList.Add(btn);
        }
    }

    private void RefreshCategoryButtons()
    {
        int i = 0;
        foreach (CraftingCategory cat in System.Enum.GetValues(typeof(CraftingCategory)))
        {
            var btn = categoryList[i] as Button;
            if (btn != null)
            {
                btn.EnableInClassList("category-btn--selected", cat == selectedCategory);
                btn.SetEnabled(HasAnyRecipesInCategory(cat));
            }
            i++;
        }
    }

    private void SelectCategory(CraftingCategory cat)
    {
        selectedCategory = cat;
        selectedRecipe = null;
        RefreshCategoryButtons();
        RefreshRecipeList();
        ClearDetail();
    }

    private void RefreshRecipeList()
    {
        recipeList.Clear();

        foreach (var recipe in currentRecipes)
        {
            if (recipe.category != selectedCategory) continue;

            var entry = new VisualElement();
            entry.AddToClassList("recipe-entry");

            var icon = new VisualElement();
            icon.AddToClassList("recipe-entry__icon");
            if (recipe.result?.icon != null)
                icon.style.backgroundImage = new StyleBackground(recipe.result.icon);

            var label = new Label(recipe.result != null ? recipe.result.itemName : "???");
            label.AddToClassList("recipe-entry__name");

            entry.Add(icon);
            entry.Add(label);

            bool canCraft = CraftingSystem.Instance.CanCraft(recipe);
            entry.EnableInClassList("recipe-entry--uncraftable", !canCraft);

            var captured = recipe;
            entry.RegisterCallback<ClickEvent>(_ => SelectRecipe(captured));

            recipeList.Add(entry);
        }
    }

    private void SelectRecipe(CraftingRecipe recipe)
    {
        selectedRecipe = recipe;
        RefreshRecipeListSelection();
        PopulateDetail(recipe);
    }

    private void RefreshRecipeListSelection()
    {
        foreach (var child in recipeList.Children())
        {
            int idx = recipeList.IndexOf(child);
            var filtered = GetFilteredRecipes();
            bool isSelected = idx < filtered.Count && filtered[idx] == selectedRecipe;
            child.EnableInClassList("recipe-entry--selected", isSelected);
        }
    }

    private void PopulateDetail(CraftingRecipe recipe)
    {
        if (recipe == null || recipe.result == null) { ClearDetail(); return; }

        detailName.text = recipe.result.itemName;
        detailDesc.text = recipe.result.description;

        if (recipe.result.icon != null)
            detailIcon.style.backgroundImage = new StyleBackground(recipe.result.icon);
        else
            detailIcon.style.backgroundImage = StyleKeyword.None;

        ingredientsList.Clear();
        foreach (var ingredient in recipe.ingredients)
        {
            if (ingredient.item == null) continue;

            var row = new VisualElement();
            row.AddToClassList("ingredient-row");

            var icon = new VisualElement();
            icon.AddToClassList("ingredient-row__icon");
            if (ingredient.item.icon != null)
                icon.style.backgroundImage = new StyleBackground(ingredient.item.icon);

            var lbl = new Label($"{ingredient.item.itemName}  ×{ingredient.count}");
            lbl.AddToClassList("ingredient-row__label");

            int have = InventorySystem.Instance.CountItems(ingredient.item);
            var check = new Label(have >= ingredient.count ? "✓" : "✗");
            check.AddToClassList("ingredient-row__check");
            check.AddToClassList(have >= ingredient.count ? "ingredient-row__check--ok" : "ingredient-row__check--missing");

            row.Add(icon);
            row.Add(lbl);
            row.Add(check);
            ingredientsList.Add(row);
        }

        bool canCraft = CraftingSystem.Instance.CanCraft(recipe);
        craftBtn.SetEnabled(canCraft && !isCrafting);
        SetProgressWidth(0f);
    }

    private void ClearDetail()
    {
        detailName.text = "";
        detailDesc.text = "";
        detailIcon.style.backgroundImage = StyleKeyword.None;
        ingredientsList.Clear();
        craftBtn.SetEnabled(false);
        SetProgressWidth(0f);
    }

    private void OnCraftClicked()
    {
        if (selectedRecipe == null || isCrafting) return;
        if (!CraftingSystem.Instance.CanCraft(selectedRecipe)) return;

        isCrafting = true;
        craftBtn.SetEnabled(false);
        StartCoroutine(CraftAnimation(selectedRecipe));
    }

    private IEnumerator CraftAnimation(CraftingRecipe recipe)
    {
        float elapsed = 0f;
        CraftingSystem.Instance.Craft(recipe, CraftDuration, OnCraftComplete);

        while (elapsed < CraftDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetProgressWidth(Mathf.Clamp01(elapsed / CraftDuration));
            yield return null;
        }
    }

    private void OnCraftComplete()
    {
        isCrafting = false;
        SetProgressWidth(0f);
        if (selectedRecipe != null) PopulateDetail(selectedRecipe);
        RefreshRecipeList();
        RefreshCategoryButtons();
    }

    private void OnInventoryChanged()
    {
        if (!IsOpen) return;
        RefreshRecipeList();
        RefreshCategoryButtons();
        if (selectedRecipe != null) PopulateDetail(selectedRecipe);
    }

    private void SetProgressWidth(float t)
    {
        if (craftProgressFill != null)
            craftProgressFill.style.width = new StyleLength(new Length(t * 100f, LengthUnit.Percent));
    }

    private List<CraftingRecipe> GetFilteredRecipes()
    {
        var result = new List<CraftingRecipe>();
        foreach (var r in currentRecipes)
            if (r.category == selectedCategory) result.Add(r);
        return result;
    }

    private bool HasAnyRecipesInCategory(CraftingCategory cat)
    {
        foreach (var r in currentRecipes)
            if (r.category == cat) return true;
        return false;
    }

    private static string CategoryDisplayName(CraftingCategory cat) => cat switch
    {
        CraftingCategory.BasicMaterials => "BASIC MATERIALS",
        CraftingCategory.Tech => "TECH",
        _ => cat.ToString().ToUpper()
    };
}
