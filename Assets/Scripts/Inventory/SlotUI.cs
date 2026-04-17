using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image iconImage;

    private int slotIndex;
    private Action<int> onClick;
    private Action<int> onHoverEnter;
    private Action<int> onHoverExit;
    private Action<int, KeyCode> onHotkey;

    private bool isHovered;

    private static readonly KeyCode[] hotkeyKeys =
    {
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5
    };

    public void Init(int index, Action<int> clickCb, Action<int> enterCb, Action<int> exitCb, Action<int, KeyCode> hotkeyCb)
    {
        slotIndex = index;
        onClick = clickCb;
        onHoverEnter = enterCb;
        onHoverExit = exitCb;
        onHotkey = hotkeyCb;
    }

    public void SetItem(InventoryItem item)
    {
        if (iconImage == null) return;
        if (item != null && item.data.icon != null)
        {
            iconImage.sprite = item.data.icon;
            iconImage.enabled = true;
        }
        else
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }
    }

    private void Update()
    {
        if (!isHovered) return;
        foreach (var key in hotkeyKeys)
        {
            if (Input.GetKeyDown(key))
                onHotkey?.Invoke(slotIndex, key);
        }
    }

    public void OnPointerClick(PointerEventData eventData) => onClick?.Invoke(slotIndex);
    public void OnPointerEnter(PointerEventData eventData) { isHovered = true; onHoverEnter?.Invoke(slotIndex); }
    public void OnPointerExit(PointerEventData eventData) { isHovered = false; onHoverExit?.Invoke(slotIndex); }
}
