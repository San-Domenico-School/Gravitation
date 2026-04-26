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
    [SerializeField] private InputActionReference clickAction;   // bind to UI/Click  (<Mouse>/leftButton)
    [SerializeField] private InputActionReference pointAction;   // bind to UI/Point  (<Mouse>/position)
    [SerializeField] private PlayerInput playerInput;

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
    private VisualElement craftBtnEl;
    private Label craftBtnLabel;
    private VisualElement craftProgressFill;
    private VisualElement closeEl;

    private CrafterInteractable activeCrafter;
    private List<CraftingRecipe> currentRecipes = new();
    private CraftingCategory selectedCategory = CraftingCategory.BasicMaterials;
    private CraftingRecipe selectedRecipe;
    private bool isCrafting;
    private bool craftBtnEnabled;

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
        var docRoot = document.rootVisualElement;
        docRoot.style.width  = new StyleLength(new Length(100, LengthUnit.Percent));
        docRoot.style.height = new StyleLength(new Length(100, LengthUnit.Percent));

        root             = docRoot.Q("crafting-root");
        crafterTitle     = root.Q<Label>("crafter-title");
        categoryList     = root.Q("category-list");
        recipeList       = root.Q("recipe-list");
        detailIcon       = root.Q("detail-icon");
        detailName       = root.Q<Label>("detail-name");
        detailDesc       = root.Q<Label>("detail-desc");
        ingredientsList  = root.Q("ingredients-list");
        craftProgressFill = root.Q("craft-progress-fill");
        craftBtnEl       = root.Q("craft-btn-container");
        craftBtnLabel    = root.Q<Label>("craft-btn-label");
        closeEl          = root.Q("close-btn");

        root.style.display = DisplayStyle.None;
        BuildCategoryButtons();
    }

    private void OnEnable()
    {
        if (cancelAction != null) cancelAction.action.performed += OnCancel;
        if (clickAction  != null) clickAction.action.performed  += OnClick;
    }

    private void OnDisable()
    {
        if (cancelAction != null) cancelAction.action.performed -= OnCancel;
        if (clickAction  != null) clickAction.action.performed  -= OnClick;
    }

    private void OnCancel(InputAction.CallbackContext ctx)
    {
        if (IsOpen) Close();
    }

    private void OnClick(InputAction.CallbackContext ctx)
    {
        Debug.Log("[CraftingUI] OnClick callback FIRED");
        if (!IsOpen) { Debug.Log("[CraftingUI] OnClick: not open, ignoring"); return; }
        HandleClick();
    }

    // ── Click detection via InputAction callback ──────────────────────────────

    private void HandleClick()
    {
        // Read raw screen position
        Vector2 screenPos;
        if (pointAction != null)
            screenPos = pointAction.action.ReadValue<Vector2>();
        else if (Mouse.current != null)
            screenPos = Mouse.current.position.ReadValue();
        else { Debug.LogWarning("[CraftingUI] No mouse position source!"); return; }

        // --- Coordinate diagnostics ---
        var docRoot = document.rootVisualElement;
        float panelW = docRoot.resolvedStyle.width;
        float panelH = docRoot.resolvedStyle.height;
        var window   = root.Q("crafter-window");

        // Method A: RuntimePanelUtils
        Vector2 posA = root.panel != null
            ? RuntimePanelUtils.ScreenToPanel(root.panel, screenPos)
            : new Vector2(screenPos.x, Screen.height - screenPos.y);

        // Method B: Manual scale (handles Retina/DPI mismatch)
        Vector2 posB = new Vector2(
            screenPos.x * panelW / Screen.width,
            (Screen.height - screenPos.y) * panelH / Screen.height
        );

        Debug.Log($"[CraftingUI] Screen={Screen.width}x{Screen.height}  Panel={panelW}x{panelH}  screenPos={screenPos}");
        Debug.Log($"[CraftingUI] posA(RuntimePanel)={posA}   posB(manual)={posB}");
        Debug.Log($"[CraftingUI] window.worldBound={window?.worldBound}");
        if (categoryList.childCount > 0) Debug.Log($"[CraftingUI] category[0].worldBound={categoryList[0].worldBound}");
        int rc = 0; foreach (var c in recipeList.Children()) { Debug.Log($"[CraftingUI] recipe[{rc}].worldBound={c.worldBound}"); rc++; if (rc >= 3) break; }

        // Use whichever method lands inside the window; if neither does, we have a DPI bug
        Vector2 panelPos = window != null && window.worldBound.Contains(posB) ? posB : posA;
        Debug.Log($"[CraftingUI] Using panelPos={panelPos}  windowBound={window?.worldBound}  contains={window?.worldBound.Contains(panelPos)}");

        // Hit test
        if (closeEl   != null && closeEl.worldBound.Contains(panelPos))   { Close(); return; }
        if (craftBtnEl != null && craftBtnEl.worldBound.Contains(panelPos)){ if (craftBtnEnabled) OnCraftClicked(); return; }

        for (int i = 0; i < categoryList.childCount; i++)
            if (categoryList[i].worldBound.Contains(panelPos)) { SelectCategory((CraftingCategory)i); return; }

        var filtered = GetFilteredRecipes();
        int idx = 0;
        foreach (var child in recipeList.Children())
        {
            if (child.worldBound.Contains(panelPos)) { if (idx < filtered.Count) SelectRecipe(filtered[idx]); return; }
            idx++;
        }

        Debug.Log($"[CraftingUI] Hit NOTHING at panelPos={panelPos}");
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open(CrafterInteractable crafter)
    {
        activeCrafter    = crafter;
        crafterTitle.text = $"TIER {crafter.tier} FABRICATOR";
        currentRecipes   = CraftingSystem.Instance.GetRecipesForTier(crafter.tier);

        selectedCategory = CraftingCategory.BasicMaterials;
        selectedRecipe   = null;
        isCrafting       = false;
        SetCraftBtnEnabled(false);
        SetProgressWidth(0f);

        RefreshCategoryButtons();
        RefreshRecipeList();
        ClearDetail();

        root.style.display = DisplayStyle.Flex;
        Time.timeScale = 0f;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible   = true;

        playerInput?.actions.FindActionMap("GravityGun")?.Disable();
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
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible   = false;

        playerInput?.actions.FindActionMap("GravityGun")?.Enable();
        InventorySystem.Instance.OnInventoryChanged -= OnInventoryChanged;
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void BuildCategoryButtons()
    {
        categoryList.Clear();
        foreach (CraftingCategory cat in System.Enum.GetValues(typeof(CraftingCategory)))
        {
            var el = new Label(CategoryDisplayName(cat));
            el.AddToClassList("category-btn");
            categoryList.Add(el);
        }
    }

    private void RefreshCategoryButtons()
    {
        int i = 0;
        foreach (CraftingCategory cat in System.Enum.GetValues(typeof(CraftingCategory)))
        {
            if (i >= categoryList.childCount) break;
            var el = categoryList[i];
            el.EnableInClassList("category-btn--selected", cat == selectedCategory);
            el.EnableInClassList("category-btn--empty",   !HasAnyRecipesInCategory(cat));
            i++;
        }
    }

    private void SelectCategory(CraftingCategory cat)
    {
        selectedCategory = cat;
        selectedRecipe   = null;
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
        int idx = 0;
        var filtered = GetFilteredRecipes();
        foreach (var child in recipeList.Children())
        {
            child.EnableInClassList("recipe-entry--selected", idx < filtered.Count && filtered[idx] == selectedRecipe);
            idx++;
        }
    }

    private void PopulateDetail(CraftingRecipe recipe)
    {
        if (recipe == null || recipe.result == null) { ClearDetail(); return; }

        detailName.text = recipe.result.itemName;
        detailDesc.text = recipe.result.description;
        detailIcon.style.backgroundImage = recipe.result.icon != null
            ? new StyleBackground(recipe.result.icon)
            : StyleKeyword.None;

        ingredientsList.Clear();
        foreach (var ingredient in recipe.ingredients)
        {
            if (ingredient.item == null) continue;

            var row   = new VisualElement();
            row.AddToClassList("ingredient-row");

            var icon  = new VisualElement();
            icon.AddToClassList("ingredient-row__icon");
            if (ingredient.item.icon != null)
                icon.style.backgroundImage = new StyleBackground(ingredient.item.icon);

            int  have     = InventorySystem.Instance.CountItems(ingredient.item);
            bool hasEnough = have >= ingredient.count;

            var lbl = new Label($"{ingredient.item.itemName}  ×{ingredient.count}");
            lbl.AddToClassList("ingredient-row__label");

            var check = new Label(hasEnough ? "✓" : "✗");
            check.AddToClassList("ingredient-row__check");
            check.AddToClassList(hasEnough ? "ingredient-row__check--ok" : "ingredient-row__check--missing");

            row.Add(icon); row.Add(lbl); row.Add(check);
            ingredientsList.Add(row);
        }

        SetCraftBtnEnabled(CraftingSystem.Instance.CanCraft(recipe) && !isCrafting);
        SetProgressWidth(0f);
    }

    private void ClearDetail()
    {
        detailName.text = "";
        detailDesc.text = "";
        detailIcon.style.backgroundImage = StyleKeyword.None;
        ingredientsList.Clear();
        SetCraftBtnEnabled(false);
        SetProgressWidth(0f);
    }

    private void OnCraftClicked()
    {
        if (selectedRecipe == null || isCrafting) return;
        if (!CraftingSystem.Instance.CanCraft(selectedRecipe)) return;

        isCrafting = true;
        SetCraftBtnEnabled(false);
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

    private void SetCraftBtnEnabled(bool enabled)
    {
        craftBtnEnabled = enabled;
        var target = craftBtnLabel ?? craftBtnEl;
        if (target == null) return;
        target.EnableInClassList("craft-btn--disabled", !enabled);
        target.EnableInClassList("craft-btn--enabled",   enabled);
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
        CraftingCategory.Tech           => "TECH",
        _                               => cat.ToString().ToUpper()
    };
}
