using System;
using UnityEngine;

public static class WorldInputGate
{
    public static bool IsUIOpen { get; private set; }

    public static event Action<bool> LockStateChanged;

    public static void SetUIOpen(bool isOpen)
    {
        if (IsUIOpen == isOpen)
            return;

        IsUIOpen = isOpen;
        Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isOpen;
        LockStateChanged?.Invoke(isOpen);
    }
}
