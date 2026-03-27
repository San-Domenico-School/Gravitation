using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Enables selecting GravityBody objects and applying a new gravity direction to them.
/// </summary>
public class GravityGun : MonoBehaviour
{
    private enum Mode
    {
        Selection,
        GravityPlacement
    }

    [Header("Input")]
    [Tooltip("Primary fire (left click). Used for selection or placing gravity depending on mode.")]
    public InputActionReference Shoot;

    [Tooltip("Secondary fire (right click). Used for deselecting in selection mode.")]
    public InputActionReference AltShoot;

    [Tooltip("Switch between selection mode and gravity placement mode.")]
    public InputActionReference SwitchMode;

    [Tooltip("Toggle player selection in selection mode.")]
    public InputActionReference SelfSelect;

    [Header("Selection")]
    [Tooltip("Currently selected GravityBody objects.")]
    public List<GravityBody> selectedBodies = new List<GravityBody>(20);

    [Tooltip("Maximum number of bodies that can be selected at once.")]
    public int maxSelectionCount = 20;

    [Header("Visuals")]
    [Tooltip("Visual indicator shown in selection mode (e.g., a cone).")]
    public GameObject selectionCone;

    [Tooltip("Crosshair UI Image shown in placement mode.")]
    public Image placementCrosshair;

    private Mode currentMode = Mode.Selection;
    private int gravityObjectLayerMask;

    private void Awake()
    {
        // Precompute the layer mask for GravityObject.
        gravityObjectLayerMask = LayerMask.GetMask("GravityObject");

        if (gravityObjectLayerMask == 0)
        {
            Debug.LogWarning("Layer 'GravityObject' not found. GravityGun raycasts will not hit anything.", this);
        }

        // Set initial visuals
        UpdateVisuals();
    }

    private void OnEnable()
    {
        WorldInputGate.LockStateChanged += OnInputLockChanged;

        if (Shoot != null)
        {
            Shoot.action.performed += OnShoot;
            Shoot.action.Enable();
        }

        if (AltShoot != null)
        {
            AltShoot.action.performed += OnAltShoot;
            AltShoot.action.Enable();
        }

        if (SwitchMode != null)
        {
            SwitchMode.action.performed += OnSwitchMode;
            SwitchMode.action.Enable();
        }

        if (SelfSelect != null)
        {
            SelfSelect.action.performed += OnSelfSelect;
            SelfSelect.action.Enable();
        }

        Debug.Log("GravityGun input actions enabled");
    }

    private void OnDisable()
    {
        WorldInputGate.LockStateChanged -= OnInputLockChanged;

        if (Shoot != null)
        {
            Shoot.action.performed -= OnShoot;
            Shoot.action.Disable();
        }

        if (AltShoot != null)
        {
            AltShoot.action.performed -= OnAltShoot;
            AltShoot.action.Disable();
        }

        if (SwitchMode != null)
        {
            SwitchMode.action.performed -= OnSwitchMode;
            SwitchMode.action.Disable();
        }

        if (SelfSelect != null)
        {
            SelfSelect.action.performed -= OnSelfSelect;
            SelfSelect.action.Disable();
        }

        Debug.Log("GravityGun input actions disabled");
    }

    private void OnShoot(InputAction.CallbackContext ctx)
    {
        if (WorldInputGate.IsUIOpen)
            return;

        Debug.Log("OnShoot triggered");
        switch (currentMode)
        {
            case Mode.Selection:
                TrySelectBody();
                break;

            case Mode.GravityPlacement:
                TryPlaceGravity();
                break;
        }
    }

    private void OnAltShoot(InputAction.CallbackContext ctx)
    {
        if (WorldInputGate.IsUIOpen)
            return;

        Debug.Log("OnAltShoot triggered");
        
        if (currentMode == Mode.Selection)
        {
            TryDeselectBody();
        }
        else if (currentMode == Mode.GravityPlacement)
        {
            TryRemoveGravity();
        }
    }

    private void OnSwitchMode(InputAction.CallbackContext ctx)
    {
        if (WorldInputGate.IsUIOpen)
            return;

        Debug.Log("OnSwitchMode triggered");
        currentMode = currentMode == Mode.Selection ? Mode.GravityPlacement : Mode.Selection;
        Debug.Log($"GravityGun mode switched to: {currentMode}");
        UpdateVisuals();
    }

    private void OnSelfSelect(InputAction.CallbackContext ctx)
    {
        if (WorldInputGate.IsUIOpen)
            return;

        Debug.Log("OnSelfSelect triggered");
        
        if (currentMode == Mode.Selection)
        {
            TryTogglePlayerSelection();
        }
    }

    private void TryTogglePlayerSelection()
    {
        GravityBody playerBody = WorldSessionManagerProxy.GetPlayerBody();
        if (playerBody == null)
        {
            Debug.LogWarning("Player gravity body not found.");
            return;
        }

        if (selectedBodies.Contains(playerBody))
        {
            selectedBodies.Remove(playerBody);
            Debug.Log("Player deselected");
        }
        else if (selectedBodies.Count < maxSelectionCount)
        {
            selectedBodies.Add(playerBody);
            Debug.Log("Player selected");
        }
        else
        {
            Debug.Log("Cannot select player: max selection count reached");
        }
    }

    private void TrySelectBody()
    {
        if (selectedBodies.Count >= maxSelectionCount)
            return;

        if (TryRaycast(out RaycastHit hit))
        {
            GravityBody body = hit.collider.GetComponentInParent<GravityBody>();
            if (body == null)
                return;

            if (!selectedBodies.Contains(body))
            {
                selectedBodies.Add(body);
                Debug.Log($"Selected body: {body.name}");
            }
        }
        else
        {
            Debug.Log("Raycast missed in selection mode");
        }
    }

    private void TryDeselectBody()
    {
        if (TryRaycast(out RaycastHit hit))
        {
            GravityBody body = hit.collider.GetComponentInParent<GravityBody>();
            if (body == null)
                return;

            if (selectedBodies.Contains(body))
                selectedBodies.Remove(body);
        }
    }

    private void TryPlaceGravity()
    {
        if (selectedBodies.Count == 0)
        {
            Debug.Log("No selected bodies for gravity placement");
            return;
        }

        if (!TryRaycastDirection(out RaycastHit hit))
        {
            Debug.Log("Raycast missed in gravity placement mode");
            return;
        }

        Vector3 gravityDirection = -hit.normal;
        GravityController.Instance?.SetGravity(selectedBodies, gravityDirection);
        Debug.Log($"Placed gravity at {hit.point} with direction {gravityDirection}");
    }

    private void TryRemoveGravity()
    {
        if (selectedBodies.Count == 0)
        {
            Debug.Log("No selected bodies to remove gravity from");
            return;
        }

        GravityController.Instance?.SetGravity(selectedBodies, Vector3.zero);
        Debug.Log($"Removed gravity from {selectedBodies.Count} bodies");
    }

    private bool TryRaycast(out RaycastHit hit)
    {
        hit = default;

        if (WorldInputGate.IsUIOpen)
            return false;

        Camera cam = Camera.main;
        if (cam == null)
            return false;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

        return Physics.Raycast(ray, out hit, 100f, gravityObjectLayerMask);
    }

    private bool TryRaycastDirection(out RaycastHit hit)
    {
        hit = default;

        if (WorldInputGate.IsUIOpen)
            return false;

        Camera cam = Camera.main;
        if (cam == null)
            return false;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

        // Raycast against all layers to find gravity direction, not just GravityObject layer
        return Physics.Raycast(ray, out hit, 100f);
    }

    private void UpdateVisuals()
    {
        Debug.Log($"UpdateVisuals called. Mode: {currentMode}, Cone: {(selectionCone != null ? "assigned" : "NULL")}, Crosshair: {(placementCrosshair != null ? "assigned" : "NULL")}");
        bool showPlacementVisuals = currentMode == Mode.GravityPlacement && !WorldInputGate.IsUIOpen;
        
        if (selectionCone != null)
        {
            selectionCone.SetActive(showPlacementVisuals);
            Debug.Log($"Selection cone set to: {showPlacementVisuals}");
        }
        else
        {
            Debug.LogWarning("Selection cone is not assigned!");
        }

        if (placementCrosshair != null)
        {
            placementCrosshair.enabled = showPlacementVisuals;
            Debug.Log($"Placement crosshair set to: {showPlacementVisuals}");
        }
        else
        {
            Debug.LogWarning("Placement crosshair is not assigned!");
        }
    }

    private void OnInputLockChanged(bool locked)
    {
        UpdateVisuals();
    }

    private static class WorldSessionManagerProxy
    {
        public static GravityBody GetPlayerBody()
        {
            if (ProceduralWorldSession.Instance != null && ProceduralWorldSession.Instance.PlayerBody != null)
                return ProceduralWorldSession.Instance.PlayerBody;

            PlayerMovement playerMovement = Object.FindFirstObjectByType<PlayerMovement>();
            return playerMovement != null ? playerMovement.GetComponent<GravityBody>() : Object.FindFirstObjectByType<GravityBody>();
        }
    }
}
