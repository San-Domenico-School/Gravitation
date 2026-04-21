using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

public class CraftingUI : MonoBehaviour
{
    public static CraftingUI Instance { get; private set; }

    [Header("Panel References")]
    [SerializeField] private GameObject craftingPanel;
    [SerializeField] private Transform recipeContainer;
    [SerializeField] private GameObject recipeRowPrefab;
    [SerializeField] private Button closeButton;

    [Header("Tooltip")]
    [SerializeField] private GameObject tooltip;
    [SerializeField] private TextMeshProUGUI tooltipName;
    [SerializeField] private TextMeshProUGUI tooltipDesc;

    [Header("Progress")]
    [SerializeField] private GameObject progressBarRoot;
    [SerializeField] private Image progressFill;
    [SerializeField] private TextMeshProUGUI craftingLabel;

    [Header("Input")]
    [SerializeField] private InputActionReference closeAction;

    [Header("Settings")]
    [SerializeField] public float craftTime = 2f;

    public bool IsOpen => isOpen;

    private bool isOpen;
    private bool isCrafting;
    private readonly List<RecipeRowUI> spawnedRows = new List<RecipeRowUI>();
    private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>();
    private RecipeRowUI hoveredRow;
    private Coroutine tooltipCoroutine;
    private Coroutine progressCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        closeAction.action.performed += OnCloseInput;
        closeAction.action.Enable();

        if (craftingPanel != null) craftingPanel.SetActive(false);
        if (tooltip != null) tooltip.SetActive(false);
        if (progressBarRoot != null) progressBarRoot.SetActive(false);
        if (craftingLabel != null) craftingLabel.gameObject.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    private void Start()
    {
        InventorySystem.Instance.OnInventoryChanged += OnInventoryChanged;
    }

    private void OnDestroy()
    {
        closeAction.action.performed -= OnCloseInput;
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnInventoryChanged -= OnInventoryChanged;
    }

    // Called by Crafter when the player interacts with a crafting station.
    public void Open(CraftingTier tier)
    {
        if (isOpen)
        {
            // Already open — just switch to the new tier's recipe list.
            PopulateRecipes(tier);
            return;
        }

        isOpen = true;
        craftingPanel.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Same pattern as InventoryUI: do NOT disable action maps.

        PopulateRecipes(tier);
    }

    public void Close()
    {
        if (!isOpen) return;

        isOpen = false;
        isCrafting = false;
        craftingPanel.SetActive(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (tooltip != null) tooltip.SetActive(false);
        if (tooltipCoroutine != null) { StopCoroutine(tooltipCoroutine); tooltipCoroutine = null; }
        if (progressCoroutine != null) { StopCoroutine(progressCoroutine); progressCoroutine = null; }
        if (progressBarRoot != null) progressBarRoot.SetActive(false);
        if (craftingLabel != null) craftingLabel.gameObject.SetActive(false);
    }

    private void OnCloseInput(InputAction.CallbackContext ctx)
    {
        if (isOpen) Close();
    }

    private void PopulateRecipes(CraftingTier tier)
    {
        foreach (var row in spawnedRows)
            if (row != null) Destroy(row.gameObject);
        spawnedRows.Clear();

        var items = CraftingSystem.Instance.GetCraftableItemsForTier(tier);
        foreach (var item in items)
        {
            var go = Instantiate(recipeRowPrefab, recipeContainer);
            var row = go.GetComponent<RecipeRowUI>();
            row.Setup(item, OnCraftClicked);
            spawnedRows.Add(row);
        }
    }

    private void OnInventoryChanged()
    {
        if (!isOpen) return;
        foreach (var row in spawnedRows)
            row.Refresh();
    }

    private void OnCraftClicked(ItemData item)
    {
        if (isCrafting) return;

        bool started = CraftingSystem.Instance.TryCraft(item, craftTime, OnCraftComplete);
        if (!started) return;

        isCrafting = true;
        SetCraftingLock(true);

        if (progressCoroutine != null) StopCoroutine(progressCoroutine);
        progressCoroutine = StartCoroutine(ShowProgress(craftTime));
    }

    private void OnCraftComplete()
    {
        isCrafting = false;

        if (progressCoroutine != null) { StopCoroutine(progressCoroutine); progressCoroutine = null; }
        if (progressBarRoot != null) progressBarRoot.SetActive(false);
        if (craftingLabel != null) craftingLabel.gameObject.SetActive(false);

        SetCraftingLock(false);

        // Rows also get refreshed by OnInventoryChanged when the ingredient/result items change,
        // but call explicitly here in case the callback fires before that event.
        if (isOpen)
            foreach (var row in spawnedRows) row.Refresh();
    }

    private void SetCraftingLock(bool locked)
    {
        foreach (var row in spawnedRows)
            row.SetCraftingLock(locked);
    }

    private IEnumerator ShowProgress(float duration)
    {
        if (progressBarRoot != null) progressBarRoot.SetActive(true);
        if (craftingLabel != null) craftingLabel.gameObject.SetActive(true);
        if (progressFill != null) progressFill.fillAmount = 0f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (progressFill != null)
                progressFill.fillAmount = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        if (progressFill != null) progressFill.fillAmount = 1f;
        progressCoroutine = null;
    }

    private void Update()
    {
        if (!isOpen) return;
        PollHoveredRow();
    }

    // Mirrors InventoryUI hover polling: EventSystem.RaycastAll every frame because
    // OnPointerEnter is unreliable with timeScale = 0 under the New Input System.
    private void PollHoveredRow()
    {
        _raycastResults.Clear();
        var pointerData = new PointerEventData(EventSystem.current)
            { position = Mouse.current.position.ReadValue() };
        EventSystem.current.RaycastAll(pointerData, _raycastResults);

        RecipeRowUI newHovered = null;
        foreach (var result in _raycastResults)
        {
            var row = result.gameObject.GetComponent<RecipeRowUI>()
                      ?? result.gameObject.GetComponentInParent<RecipeRowUI>();
            if (row != null) { newHovered = row; break; }
        }

        if (newHovered == hoveredRow) return;

        hoveredRow = newHovered;

        if (tooltipCoroutine != null) { StopCoroutine(tooltipCoroutine); tooltipCoroutine = null; }

        if (hoveredRow != null && tooltip != null)
            tooltipCoroutine = StartCoroutine(ShowTooltipDelayed(hoveredRow.ItemData, Mouse.current.position.ReadValue()));
        else if (tooltip != null)
            tooltip.SetActive(false);
    }

    // Mirrors InventoryUI's WaitForSecondsRealtime pattern so timeScale = 0 doesn't break delay.
    private IEnumerator ShowTooltipDelayed(ItemData item, Vector2 pos)
    {
        yield return new WaitForSecondsRealtime(0.5f);
        if (tooltip == null || item == null) yield break;
        if (tooltipName != null) tooltipName.text = item.itemName;
        if (tooltipDesc != null) tooltipDesc.text = item.description;
        tooltip.SetActive(true);
        tooltip.transform.position = (Vector3)pos + new Vector3(10f, -10f, 0f);
        tooltipCoroutine = null;
    }
}
