using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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

    [Header("Selection")]
    [Tooltip("Currently selected GravityBody objects.")]
    public List<GravityBody> selectedBodies = new List<GravityBody>(20);

    [Tooltip("Maximum number of bodies that can be selected at once.")]
    public int maxSelectionCount = 20;

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
    }

    private void OnEnable()
    {
        if (Shoot != null)
            Shoot.action.performed += OnShoot;

        if (AltShoot != null)
            AltShoot.action.performed += OnAltShoot;

        if (SwitchMode != null)
            SwitchMode.action.performed += OnSwitchMode;
    }

    private void OnDisable()
    {
        if (Shoot != null)
            Shoot.action.performed -= OnShoot;

        if (AltShoot != null)
            AltShoot.action.performed -= OnAltShoot;

        if (SwitchMode != null)
            SwitchMode.action.performed -= OnSwitchMode;
    }

    private void OnShoot(InputAction.CallbackContext ctx)
    {
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
        if (currentMode != Mode.Selection)
            return;

        TryDeselectBody();
    }

    private void OnSwitchMode(InputAction.CallbackContext ctx)
    {
        currentMode = currentMode == Mode.Selection ? Mode.GravityPlacement : Mode.Selection;
        Debug.Log($"GravityGun mode switched to: {currentMode}");
    }

    private void TrySelectBody()
    {
        if (selectedBodies.Count >= maxSelectionCount)
            return;

        if (TryRaycast(out RaycastHit hit))
        {
            GravityBody body = hit.collider.GetComponent<GravityBody>();
            if (body == null)
                return;

            if (!selectedBodies.Contains(body))
                selectedBodies.Add(body);
        }
    }

    private void TryDeselectBody()
    {
        if (TryRaycast(out RaycastHit hit))
        {
            GravityBody body = hit.collider.GetComponent<GravityBody>();
            if (body == null)
                return;

            if (selectedBodies.Contains(body))
                selectedBodies.Remove(body);
        }
    }

    private void TryPlaceGravity()
    {
        if (selectedBodies.Count == 0)
            return;

        if (!TryRaycast(out RaycastHit hit))
            return;

        Vector3 gravityDirection = -hit.normal;
        GravityController.Instance?.SetGravity(selectedBodies, gravityDirection);
    }

    private bool TryRaycast(out RaycastHit hit)
    {
        hit = default;

        Camera cam = Camera.main;
        if (cam == null)
            return false;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

        return Physics.Raycast(ray, out hit, 100f, gravityObjectLayerMask);
    }
}
