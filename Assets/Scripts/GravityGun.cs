using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Enables selecting GravityBody objects and applying a new gravity direction to them.
/// Higher-tier cells unlock additional abilities: Gravity Pulse (T2), Gravity Lock (T3),
/// and AoE Gravity Flip (T4).
/// </summary>
public class GravityGun : MonoBehaviour
{
    // Charge costs for gun actions
    private const float COST_GRAVITY_APPLY = 15f;      // Cost to apply gravity to selected objects
    private const float COST_GRAVITY_REMOVE = 8f;      // Cost to remove gravity
    private const float COST_PLAYER_SELF = 25f;        // Additional cost when player is in selection
    private const float COST_GRAVITY_PULSE = 30f;      // Gravity Pulse (T2)
    private const float COST_GRAVITY_LOCK = 20f;       // Gravity Lock drain per second (T3)
    private const float COST_CORE_RESONATOR = 60f;     // AoE Gravity Flip (T4)

    private enum Mode
    {
        Selection,
        GravityPlacement,
        GravityLockActive
    }

    [Header("Input")]
    [Tooltip("Primary fire (left click). Selection: toggle select. Placement: place gravity. Lock: toggle select.")]
    public InputActionReference Shoot;

    [Tooltip("Secondary fire (right click). Selection: enter Placement. Placement: back to Selection. Lock: toggle lock effect.")]
    public InputActionReference AltShoot;

    [Tooltip("(Deprecated) Mode swap is now handled by right click; this binding is no longer wired up.")]
    public InputActionReference SwitchMode;

    [Tooltip("Toggle player selection in selection mode.")]
    public InputActionReference SelfSelect;

    [Tooltip("Fire Gravity Pulse (Tier 2).")]
    public InputActionReference GravityPulse;

    [Tooltip("Toggle Gravity Lock mode (Tier 3).")]
    public InputActionReference GravityLockToggle;

    [Tooltip("Fire AoE Gravity Flip / zero-G burst (Tier 4).")]
    public InputActionReference GravityFlip;

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

    [Header("Visuals")]
    [Tooltip("The gun model shown only when equipped in the hotbar.")]
    [SerializeField] private GameObject gunVisual;

    [Tooltip("Battery HUD shown only when the gun is equipped.")]
    [SerializeField] private GameObject batteryUI;

    [Tooltip("Visual indicator shown in selection mode (e.g., a cone).")]
    public GameObject selectionCone;

    [Tooltip("Crosshair UI Image shown in placement mode.")]
    public Image placementCrosshair;

    [Header("Ability Tuning")]
    [Tooltip("Radius of the Gravity Pulse blast at the impact point.")]
    [SerializeField] private float pulseRadius = 8f;
    [Tooltip("Force magnitude applied by Gravity Pulse to each rigidbody.")]
    [SerializeField] private float pulseForce = 1500f;
    [Tooltip("Upward bias for Gravity Pulse (Unity ExplosionForce upwardsModifier).")]
    [SerializeField] private float pulseUpward = 0.5f;
    [Tooltip("Radius of the AoE Gravity Flip / zero-G burst.")]
    [SerializeField] private float flipRadius = 10f;
    [Tooltip("Duration in seconds that gravity is suspended for affected bodies.")]
    [SerializeField] private float flipDuration = 4f;

    [Header("Testing")]
    [Tooltip("Unlocks all tiers and grants infinite charge regardless of installed cell.")]
    [SerializeField] private bool testingMode = false;

    private Mode currentMode = Mode.Selection;
    private int gravityObjectLayerMask;
    private bool isDrained = false; // Flag to avoid spamming low charge warning
    private AudioSource audioSource;

    // Lock state
    private bool isLockActive;
    private readonly List<LockedBody> lockedBodies = new List<LockedBody>();

    private struct LockedBody
    {
        public Rigidbody rb;
        public bool wasKinematic;
        public Vector3 position;
        public Quaternion rotation;
    }

    public bool TestingMode => testingMode;

    /// <summary>
    /// Highest tier whose abilities are currently available.
    /// 1 = base, 2 = Pulse, 3 = Lock, 4 = AoE Flip.
    /// </summary>
    private int EffectiveTier
    {
        get
        {
            if (testingMode) return 4;
            if (batterySystem == null || batterySystem.CurrentCell == null) return 1;
            return batterySystem.CurrentCell.Tier;
        }
    }

    /// <summary>
    /// Maximum number of bodies that can be selected at once. 3 at base, 10 once Tier 2 is unlocked.
    /// </summary>
    public int MaxSelectionCount => EffectiveTier >= 2 ? 10 : 3;

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
        if (Shoot != null)              { Shoot.action.performed += OnShoot;                          Shoot.action.Enable(); }
        if (AltShoot != null)           { AltShoot.action.performed += OnAltShoot;                    AltShoot.action.Enable(); }
        if (SelfSelect != null)         { SelfSelect.action.performed += OnSelfSelect;                SelfSelect.action.Enable(); }
        if (GravityPulse != null)       { GravityPulse.action.performed += OnGravityPulse;            GravityPulse.action.Enable(); }
        if (GravityLockToggle != null)  { GravityLockToggle.action.performed += OnGravityLockToggle;  GravityLockToggle.action.Enable(); }
        if (GravityFlip != null)        { GravityFlip.action.performed += OnGravityFlip;              GravityFlip.action.Enable(); }
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

        // Stowing the gun mid-lock would leave bodies frozen — release them.
        if (!isEquipped && isLockActive)
            EndGravityLock();

        UpdateVisuals();
    }

    private void OnDestroy()
    {
        if (Shoot != null)              Shoot.action.performed -= OnShoot;
        if (AltShoot != null)           AltShoot.action.performed -= OnAltShoot;
        if (SelfSelect != null)         SelfSelect.action.performed -= OnSelfSelect;
        if (GravityPulse != null)       GravityPulse.action.performed -= OnGravityPulse;
        if (GravityLockToggle != null)  GravityLockToggle.action.performed -= OnGravityLockToggle;
        if (GravityFlip != null)        GravityFlip.action.performed -= OnGravityFlip;
        // NOTE: not calling action.Disable() — shared actions; disabling would break any other listener.
    }

    private void Update()
    {
        if (isLockActive)
            HandleLockMaintenance();
    }

    private bool IsBlockedByUI()
    {
        return InventoryUI.Instance != null && InventoryUI.Instance.IsOpen;
    }

    private void OnShoot(InputAction.CallbackContext ctx)
    {
        if (!isEquipped || IsBlockedByUI()) return;

        // No-charge fail behavior depends on mode (Selection lets you still toggle picks).
        if (batterySystem != null && !batterySystem.HasCharge && currentMode == Mode.GravityPlacement)
        {
            PlayOutOfChargeAudio();
            return;
        }

        switch (currentMode)
        {
            case Mode.Selection:
            case Mode.GravityLockActive:
                ToggleBodySelection();
                break;

            case Mode.GravityPlacement:
                TryPlaceGravity();
                break;
        }
    }

    private void OnAltShoot(InputAction.CallbackContext ctx)
    {
        if (!isEquipped || IsBlockedByUI()) return;

        switch (currentMode)
        {
            case Mode.Selection:
                currentMode = Mode.GravityPlacement;
                UpdateVisuals();
                break;

            case Mode.GravityPlacement:
                // Placement-mode right click also acts as a "remove gravity" / cancel:
                // rather than two confusable behaviors, return to Selection so the input
                // scheme stays symmetrical with the user's request.
                currentMode = Mode.Selection;
                UpdateVisuals();
                break;

            case Mode.GravityLockActive:
                ToggleLockEffect();
                break;
        }
    }

    private void OnSelfSelect(InputAction.CallbackContext ctx)
    {
        if (!isEquipped) return;
        if (currentMode == Mode.Selection || currentMode == Mode.GravityLockActive)
        {
            TryTogglePlayerSelection();
        }
    }

    private void OnGravityPulse(InputAction.CallbackContext ctx)
    {
        if (!isEquipped || IsBlockedByUI()) return;
        if (EffectiveTier < 2) return;

        if (batterySystem != null && !batterySystem.TrySpendCharge(COST_GRAVITY_PULSE))
        {
            PlayOutOfChargeAudio();
            return;
        }

        if (!TryRaycastDirection(out RaycastHit hit))
            return;

        Collider[] hits = Physics.OverlapSphere(hit.point, pulseRadius);
        foreach (var c in hits)
        {
            Rigidbody rb = c.attachedRigidbody;
            if (rb == null || rb.isKinematic) continue;
            rb.AddExplosionForce(pulseForce, hit.point, pulseRadius, pulseUpward, ForceMode.Impulse);
        }
        Debug.Log($"[GravityGun] Gravity Pulse fired at {hit.point}. Affected {hits.Length} colliders.");
    }

    private void OnGravityLockToggle(InputAction.CallbackContext ctx)
    {
        if (!isEquipped || IsBlockedByUI()) return;
        if (EffectiveTier < 3) return;

        if (currentMode == Mode.GravityLockActive)
        {
            // Leave Lock mode (and end any active lock).
            if (isLockActive)
                EndGravityLock();
            currentMode = Mode.Selection;
        }
        else
        {
            currentMode = Mode.GravityLockActive;
        }
        UpdateVisuals();
    }

    private void OnGravityFlip(InputAction.CallbackContext ctx)
    {
        if (!isEquipped || IsBlockedByUI()) return;
        if (EffectiveTier < 4) return;

        if (batterySystem != null && !batterySystem.TrySpendCharge(COST_CORE_RESONATOR))
        {
            PlayOutOfChargeAudio();
            return;
        }

        if (!TryRaycastDirection(out RaycastHit hit))
            return;

        Collider[] hits = Physics.OverlapSphere(hit.point, flipRadius, gravityObjectLayerMask);
        var affected = new List<GravityBody>();
        var originalDirs = new List<Vector3>();
        foreach (var c in hits)
        {
            GravityBody body = c.GetComponent<GravityBody>();
            if (body == null) continue;
            if (affected.Contains(body)) continue;
            affected.Add(body);
            originalDirs.Add(body.gravityDirection);
        }

        if (affected.Count == 0) return;

        GravityController.Instance?.SetGravity(affected, Vector3.zero);
        StartCoroutine(RestoreGravityAfter(affected, originalDirs, flipDuration));
        Debug.Log($"[GravityGun] AoE Flip fired at {hit.point}. Suspended gravity for {affected.Count} bodies for {flipDuration}s.");
    }

    private IEnumerator RestoreGravityAfter(List<GravityBody> bodies, List<Vector3> directions, float duration)
    {
        yield return new WaitForSeconds(duration);
        if (GravityController.Instance == null) yield break;
        for (int i = 0; i < bodies.Count; i++)
        {
            GravityBody body = bodies[i];
            if (body == null) continue;
            GravityController.Instance.SetGravity(body, directions[i]);
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
        else if (selectedBodies.Count < MaxSelectionCount)
        {
            selectedBodies.Add(playerBody);
            Debug.Log("Player selected");
        }
        else
        {
            Debug.Log("Cannot select player: max selection count reached");
        }
    }

    private void ToggleBodySelection()
    {
        if (!TryRaycast(out RaycastHit hit))
            return;

        GravityBody body = hit.collider.GetComponent<GravityBody>();
        if (body == null)
            return;

        if (selectedBodies.Contains(body))
        {
            selectedBodies.Remove(body);
            Debug.Log($"Deselected body: {body.name}");
        }
        else if (selectedBodies.Count < MaxSelectionCount)
        {
            selectedBodies.Add(body);
            Debug.Log($"Selected body: {body.name}");
        }
        else
        {
            Debug.Log($"Cannot select: max ({MaxSelectionCount}) reached.");
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

    private void ToggleLockEffect()
    {
        if (isLockActive)
        {
            EndGravityLock();
            return;
        }

        if (selectedBodies.Count == 0)
        {
            Debug.Log("[GravityGun] Lock toggle: no bodies selected.");
            return;
        }

        lockedBodies.Clear();
        foreach (var body in selectedBodies)
        {
            if (body == null) continue;
            Rigidbody rb = body.GetComponent<Rigidbody>();
            if (rb == null) continue;

            lockedBodies.Add(new LockedBody
            {
                rb = rb,
                wasKinematic = rb.isKinematic,
                position = rb.position,
                rotation = rb.rotation
            });

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (lockedBodies.Count == 0)
        {
            Debug.Log("[GravityGun] Lock toggle: no rigidbodies among selection.");
            return;
        }

        isLockActive = true;
        Debug.Log($"[GravityGun] Gravity Lock engaged on {lockedBodies.Count} bodies.");
    }

    private void EndGravityLock()
    {
        for (int i = 0; i < lockedBodies.Count; i++)
        {
            var lb = lockedBodies[i];
            if (lb.rb != null)
                lb.rb.isKinematic = lb.wasKinematic;
        }
        lockedBodies.Clear();
        isLockActive = false;
        Debug.Log("[GravityGun] Gravity Lock released.");
    }

    private void HandleLockMaintenance()
    {
        if (batterySystem == null) return;

        float drain = COST_GRAVITY_LOCK * Time.deltaTime;
        if (!batterySystem.TrySpendCharge(drain))
        {
            Debug.Log("[GravityGun] Lock auto-released — out of charge.");
            EndGravityLock();
            return;
        }

        // Re-pin transforms each frame in case external code moved them.
        for (int i = 0; i < lockedBodies.Count; i++)
        {
            var lb = lockedBodies[i];
            if (lb.rb == null) continue;
            lb.rb.MovePosition(lb.position);
            lb.rb.MoveRotation(lb.rotation);
        }
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
