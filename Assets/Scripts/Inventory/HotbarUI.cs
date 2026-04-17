using UnityEngine;
using UnityEngine.UI;

public class HotbarUI : MonoBehaviour
{
    [SerializeField] private HotbarSlotUI[] slotUIs;

    private int selectedIndex = 0;

    private void OnEnable()
    {
        HotbarSystem.Instance.OnHotbarChanged += Refresh;
    }

    private void OnDisable()
    {
        if (HotbarSystem.Instance != null)
            HotbarSystem.Instance.OnHotbarChanged -= Refresh;
    }

    private void Start() => Refresh();

    private void Update()
    {
        for (int i = 0; i < 5; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                selectedIndex = i;
                UpdateSelection();
            }
        }
    }

    private void Refresh()
    {
        for (int i = 0; i < slotUIs.Length; i++)
        {
            var item = HotbarSystem.Instance.GetHotbarItem(i);
            slotUIs[i].SetItem(item, i + 1);
        }
        UpdateSelection();
    }

    private void UpdateSelection()
    {
        for (int i = 0; i < slotUIs.Length; i++)
            slotUIs[i].SetSelected(i == selectedIndex);
    }
}
