using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Enables selecting GravityBody objects and applying a new gravity direction to them.
/// </summary>
public class GravityGun : MonoBehaviour
{
    // Charge costs for gun actions
    private const float COST_GRAVITY_APPLY = 15f;      // Cost to apply gravity to selected objects
    private const float COST_GRAVITY_REMOVE = 8f;      // Cost to remove gravity
    private const float COST_PLAYER_SELF = 25f;        // Additional cost when player is in selection
    private const float COST_GRAVITY_PULSE = 30f;      // Reserved for future Gravity Pulse ability
    private const float COST_GRAVITY_LOCK = 20f;       // Reserved for future Gravity Lock ability
    private const float COST_CORE_RESONATOR = 60f;     // Reserved for future Core Resonator ability

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

    [Header("Battery")]
    [Tooltip("Reference to the battery system (typically on this same GameObject).")]
    [SerializeField]
    private GunBatterySystem batterySystem;

    [Tooltip("Audio clip played when gun is out of charge.")]
    [SerializeField]
    private AudioClip outOfChargeClip;

    [Header("Selection")]
    [Tooltip("Currently selected GravityBody objects.")]
    public List<GravityBody> selectedBodies = new List<GravityBody>(20);

    [Tooltip("Maximum number of bodies that can be selected at once.")]
    public int maxSelectionCount = 20;

    [Header("Visuals")]
    [Tooltip("The gun model shown only when equipped in the hotbar.")]
    [SerializeField] private GameObject gunVisual;

    [Tooltip("Battery HUD shown only when the gun is equipped.")]
    [SerializeField] private GameObject batteryUI;

    [Tooltip("Visual indicator shown in selection mode (e.g., a cone).")]
    public GameObject selectionCone;

    [Tooltip("Crosshair UI Image shown in placement mode.")]
    public Image placementCrosshair;

    private Mode currentMode = Mode.Selection;
    private int gravityObjectLayerMask;
    private bool isDrained = false; // Flag to avoid spamming low charge warning
    private AudioSource audioSource;

    private void Awake()
    {
        // Precompute the layer mask for GravityObject.
        gravityObjectLayerMask = LayerMask.GetMask("GravityObject");

        if (gravityObjectLayerMask == 0)
        {
            Debug.LogWarning("Layer 'GravityObject' not found. GravityGun raycasts will not hit anything.", this);
        }

        // Get or create AudioSource for charge cues
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Find battery system if not assigned
        if (batterySystem == null)
        {
            batterySystem = GetComponent<GunBatterySystem>();
            if (batterySystem == null)
            {
                Debug.LogWarning("GunBatterySystem not found on this GameObject. Gun will not charge check.", this);
            }
        }

        // Register input actions here so they work even if the component starts disabled.
        // Awake runs on disabled components; OnEnable does not.
        if (Shoot != null)     { Shoot.action.performed += OnShoot;          Shoot.action.Enable(); }
        if (AltShoot != null)  { AltShoot.action.performed += OnAltShoot;    AltShoot.action.Enable(); }
        if (SwitchMode != null){ SwitchMode.action.performed += OnSwitchMode; SwitchMode.action.Enable(); }
        if (SelfSelect != null){ SelfSelect.action.performed += OnSelfSelect; SelfSelect.action.Enable(); }
        Debug.Log("[GravityGun] Awake — actions registered");

        // Set initial visuals
        UpdateVisuals();
    }

    private bool isEquipped = false;

    public void SetEquipped(bool value)
    {
        isEquipped = value;
        if (gunVisual != null)
            foreach (var r in gunVisual.GetComponentsInChildren<Renderer>(true))
                r.enabled = value;
        if (batteryUI != null) batteryUI.SetActive(value);
        UpdateVisuals();
    }

    private void OnDestroy()
    {
        if (Shoot != null)     Shoot.action.performed -= OnShoot;
        if (AltShoot != null)  AltShoot.action.performed -= OnAltShoot;
        if (SwitchMode != null) SwitchMode.action.performed -= OnSwitchMode;
        if (SelfSelect != null) SelfSelect.action.performed -= OnSelfSelect;
        // NOTE: not calling action.Disable() — shared actions; disabling would break any other listener.
    }

    private void OnShoot(InputAction.CallbackContext ctx)
    {
        bool invOpen = InventoryUI.Instance != null && InventoryUI.Instance.IsOpen;
        Debug.Log($"[GravityGun] OnShoot — equipped={isEquipped} inventoryOpen={invOpen} hasCharge={batterySystem?.HasCharge}");
        if (!isEquipped || invOpen) return;

        // Check if gun is disabled due to no charge
        if (batterySystem != null && !batterySystem.HasCharge)
        {
            PlayOutOfChargeAudio();
            return;
        }

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
        if (!isEquipped || (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen)) return;

        // Check if gun is disabled due to no charge (only blocks removal in placement mode)
        if (batterySystem != null && !batterySystem.HasCharge && currentMode == Mode.GravityPlacement)
        {
            PlayOutOfChargeAudio();
            return;
        }

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
        if (!isEquipped || (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen)) return;

        // Check if gun is disabled due to no charge
        if (batterySystem != null && !batterySystem.HasCharge)
        {
            PlayOutOfChargeAudio();
            return;
        }

        currentMode = currentMode == Mode.Selection ? Mode.GravityPlacement : Mode.Selection;
        UpdateVisuals();
    }

    private void OnSelfSelect(InputAction.CallbackContext ctx)
    {
        if (!isEquipped) return;
        if (currentMode == Mode.Selection)
        {
            TryTogglePlayerSelection();
        }
    }

    private void TryTogglePlayerSelection()
    {
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject == null)
        {
            Debug.LogWarning("Player object with 'Player' tag not found.");
            return;
        }

        GravityBody playerBody = playerObject.GetComponent<GravityBody>();
        if (playerBody == null)
        {
            Debug.LogWarning("GravityBody component not found on player object.");
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
            GravityBody body = hit.collider.GetComponent<GravityBody>();
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
        {
            Debug.Log("No selected bodies for gravity placement");
            return;
        }

        if (!TryRaycastDirection(out RaycastHit hit))
        {
            Debug.Log("Raycast missed in gravity placement mode");
            return;
        }

        // Calculate charge cost
        float cost = COST_GRAVITY_APPLY;
        bool playerInSelection = selectedBodies.Contains(GameObject.FindWithTag("Player")?.GetComponent<GravityBody>());
        if (playerInSelection)
        {
            cost += COST_PLAYER_SELF;
        }

        // Try to spend charge
        if (batterySystem != null && !batterySystem.TrySpendCharge(cost))
        {
            Debug.Log($"Insufficient charge. Required: {cost}, Available: {batterySystem.CurrentCharge}");
            PlayOutOfChargeAudio();
            return;
        }

        Vector3 gravityDirection = -hit.normal;
        GravityController.Instance?.SetGravity(selectedBodies, gravityDirection);
        Debug.Log($"Placed gravity at {hit.point} with direction {gravityDirection}. Spent {cost} charge.");
    }

    private void TryRemoveGravity()
    {
        if (selectedBodies.Count == 0)
        {
            Debug.Log("No selected bodies to remove gravity from");
            return;
        }

        // Try to spend charge for removal
        if (batterySystem != null && !batterySystem.TrySpendCharge(COST_GRAVITY_REMOVE))
        {
            Debug.Log($"Insufficient charge. Required: {COST_GRAVITY_REMOVE}, Available: {batterySystem.CurrentCharge}");
            PlayOutOfChargeAudio();
            return;
        }

        GravityController.Instance?.SetGravity(selectedBodies, Vector3.zero);
        Debug.Log($"Removed gravity from {selectedBodies.Count} bodies. Spent {COST_GRAVITY_REMOVE} charge.");
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

    private bool TryRaycastDirection(out RaycastHit hit)
    {
        hit = default;

        Camera cam = Camera.main;
        if (cam == null)
            return false;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

        // Raycast against all layers to find gravity direction, not just GravityObject layer
        return Physics.Raycast(ray, out hit, 100f);
    }

    private void UpdateVisuals()
    {
        if (selectionCone != null)
            selectionCone.SetActive(currentMode == Mode.GravityPlacement);

        if (placementCrosshair != null)
            placementCrosshair.enabled = (currentMode == Mode.GravityPlacement);
    }

    private void PlayOutOfChargeAudio()
    {
        if (outOfChargeClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(outOfChargeClip);
        }
    }
}
