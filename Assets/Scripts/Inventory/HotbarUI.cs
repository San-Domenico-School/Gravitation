using UnityEngine;
using UnityEngine.InputSystem;

public class HotbarUI : MonoBehaviour
{
    [SerializeField] private HotbarSlotUI[] slotUIs;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference hotbar1Action;
    [SerializeField] private InputActionReference hotbar2Action;
    [SerializeField] private InputActionReference hotbar3Action;
    [SerializeField] private InputActionReference hotbar4Action;
    [SerializeField] private InputActionReference hotbar5Action;

    private int selectedIndex = 0;

    private void OnEnable()
    {
        HotbarSystem.Instance.OnHotbarChanged += Refresh;
        hotbar1Action.action.performed += OnHotbar1;
        hotbar2Action.action.performed += OnHotbar2;
        hotbar3Action.action.performed += OnHotbar3;
        hotbar4Action.action.performed += OnHotbar4;
        hotbar5Action.action.performed += OnHotbar5;
    }

    private void OnDisable()
    {
        if (HotbarSystem.Instance != null) HotbarSystem.Instance.OnHotbarChanged -= Refresh;
        hotbar1Action.action.performed -= OnHotbar1;
        hotbar2Action.action.performed -= OnHotbar2;
        hotbar3Action.action.performed -= OnHotbar3;
        hotbar4Action.action.performed -= OnHotbar4;
        hotbar5Action.action.performed -= OnHotbar5;
    }

    private void Start() => Refresh();

    private void OnHotbar1(InputAction.CallbackContext ctx) => SelectSlot(0);
    private void OnHotbar2(InputAction.CallbackContext ctx) => SelectSlot(1);
    private void OnHotbar3(InputAction.CallbackContext ctx) => SelectSlot(2);
    private void OnHotbar4(InputAction.CallbackContext ctx) => SelectSlot(3);
    private void OnHotbar5(InputAction.CallbackContext ctx) => SelectSlot(4);

    private void SelectSlot(int index)
    {
        selectedIndex = index;
        UpdateSelection();
    }

    private void Refresh()
    {
        for (int i = 0; i < slotUIs.Length; i++)
            slotUIs[i].SetItem(HotbarSystem.Instance.GetHotbarItem(i), i + 1);
        UpdateSelection();
    }

    private void UpdateSelection()
    {
        for (int i = 0; i < slotUIs.Length; i++)
            slotUIs[i].SetSelected(i == selectedIndex);
    }
}
