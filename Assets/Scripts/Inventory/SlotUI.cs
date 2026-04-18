using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image iconImage;

    private int slotIndex;
    private Action<int> onClick;
    private Action<int> onRightClick;
    private Action<int> onHoverEnter;
    private Action<int> onHoverExit;

    public bool IsHovered { get; private set; }

    private void Awake()
    {
        // Guarantee this slot always intercepts raycasts even when the icon image is hidden
        var bg = GetComponent<Image>();
        if (bg == null)
        {
            bg = gameObject.AddComponent<Image>();
            bg.color = Color.clear;
        }
        bg.raycastTarget = true;
    }

    public void Init(int index, Action<int> clickCb, Action<int> rightClickCb, Action<int> enterCb, Action<int> exitCb)
    {
        slotIndex = index;
        onClick = clickCb;
        onRightClick = rightClickCb;
        onHoverEnter = enterCb;
        onHoverExit = exitCb;
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

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            onClick?.Invoke(slotIndex);
        else if (eventData.button == PointerEventData.InputButton.Right)
            onRightClick?.Invoke(slotIndex);
    }
    public void OnPointerEnter(PointerEventData eventData) { IsHovered = true; onHoverEnter?.Invoke(slotIndex); }
    public void OnPointerExit(PointerEventData eventData) { IsHovered = false; onHoverExit?.Invoke(slotIndex); }
}
