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

    private void Awake()
    {
        var bg = GetComponent<Image>();
        if (bg == null) { bg = gameObject.AddComponent<Image>(); bg.color = Color.clear; }
        bg.raycastTarget = true;
    }

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
        if (iconImage == null) return;
        iconImage.enabled = true;
        if (item != null && item.data.icon != null)
        {
            iconImage.sprite = item.data.icon;
            iconImage.color = Color.white;
        }
        else
        {
            iconImage.sprite = null;
            iconImage.color = Color.clear;
        }
    }

    public void SetSelected(bool selected)
    {
        if (selectionBorder != null) selectionBorder.enabled = selected;
    }
}
