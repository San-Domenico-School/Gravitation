using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Lightweight manual-save / load hotkeys for development. Drop this on the same GameObject
/// as <see cref="SaveManager"/> (or anywhere in the bootstrap scene). A real pause menu can
/// replace this later — it just calls the same SaveManager API.
///
/// Defaults:
///   F5  → SaveNow()
///   F9  → LoadFromDisk()
///   Shift+Delete → DeleteSave()  (held to avoid accidental wipes)
/// </summary>
public class SaveHotkeys : MonoBehaviour
{
    [Header("Toggles")]
    [SerializeField] private bool enableSaveHotkey = true;
    [SerializeField] private bool enableLoadHotkey = true;
    [SerializeField] private bool enableDeleteHotkey = true;

    [Header("Keys")]
    [SerializeField] private Key saveKey = Key.F5;
    [SerializeField] private Key loadKey = Key.F9;
    [SerializeField] private Key deleteKey = Key.Delete;
    [Tooltip("Shift must be held alongside the delete key to wipe the save file. Prevents accidents.")]
    [SerializeField] private bool requireShiftForDelete = true;

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (SaveManager.Instance == null) return;

        if (enableSaveHotkey && kb[saveKey].wasPressedThisFrame)
        {
            SaveManager.Instance.SaveNow();
            Debug.Log("[SaveHotkeys] Saved.");
        }

        if (enableLoadHotkey && kb[loadKey].wasPressedThisFrame)
        {
            SaveManager.Instance.LoadFromDisk();
            Debug.Log("[SaveHotkeys] Loaded.");
        }

        if (enableDeleteHotkey && kb[deleteKey].wasPressedThisFrame)
        {
            bool shiftHeld = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
            if (!requireShiftForDelete || shiftHeld)
            {
                SaveManager.Instance.DeleteSave();
                Debug.Log("[SaveHotkeys] Deleted save file.");
            }
        }
    }
}
