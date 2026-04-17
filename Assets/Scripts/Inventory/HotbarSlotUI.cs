using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HotbarSlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Image selectionBorder;
    [SerializeField] private TextMeshProUGUI slotNumber;

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
