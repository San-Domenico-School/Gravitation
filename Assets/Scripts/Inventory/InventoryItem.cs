using System;

public class InventoryItem
{
    public ItemData data;
    public string uniqueInstanceId;

    public InventoryItem(ItemData data)
    {
        this.data = data;
        this.uniqueInstanceId = Guid.NewGuid().ToString();
    }
}
