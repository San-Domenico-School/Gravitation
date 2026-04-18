using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class HotbarSlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Image selectionBorder;
    [SerializeField] private TextMeshProUGUI slotNumber;

    private int slotIndex;
    private Action<int> onLeftClick;
    private Action<int> onRightClick;

    public void Init(int index, Action<int> leftClickCb, Action<int> rightClickCb)
    {
        slotIndex = index;
        onLeftClick = leftClickCb;
        onRightClick = rightClickCb;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            onLeftClick?.Invoke(slotIndex);
        else if (eventData.button == PointerEventData.InputButton.Right)
            onRightClick?.Invoke(slotIndex);
    }

    public void SetItem(InventoryItem item, int number)
    {
        if (slotNumber != null) slotNumber.text = number.ToString();

        if (iconImage != null)
        {
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
    }

    public void SetSelected(bool selected)
    {
        if (selectionBorder != null) selectionBorder.enabled = selected;
    }
}
