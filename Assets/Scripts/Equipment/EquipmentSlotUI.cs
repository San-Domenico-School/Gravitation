using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// A single equipment/upgrade slot in the 3-slot panel.
/// Supports left-click (equip/unequip via drag) and right-click (unequip to bag).
/// </summary>
public class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image iconImage;

    public int SlotIndex { get; private set; }

    private Action<int> onLeftClick;
    private Action<int> onRightClick;

    private void Awake()
    {
        // Ensure there is a raycast-target background so pointer events fire.
        var bg = GetComponent<Image>();
        if (bg == null)
        {
            bg = gameObject.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.05f);
        }
        bg.raycastTarget = true;
    }

    public void Init(int slotIndex, Action<int> leftClick, Action<int> rightClick = null)
    {
        SlotIndex = slotIndex;
        onLeftClick = leftClick;
        onRightClick = rightClick;
    }

    public void SetItem(InventoryItem item)
    {
        if (iconImage == null) return;

        if (item != null && item.data.icon != null)
        {
            iconImage.sprite = item.data.icon;
            iconImage.color = Color.white;
            iconImage.enabled = true;
        }
        else
        {
            iconImage.sprite = null;
            iconImage.color = Color.clear;
            iconImage.enabled = true; // keep enabled so slot background shows
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            onLeftClick?.Invoke(SlotIndex);
        else if (eventData.button == PointerEventData.InputButton.Right)
            onRightClick?.Invoke(SlotIndex);
    }

    public void OnPointerEnter(PointerEventData eventData) { }
    public void OnPointerExit(PointerEventData eventData) { }
}
