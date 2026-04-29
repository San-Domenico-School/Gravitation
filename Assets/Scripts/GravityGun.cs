using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Enables selecting GravityBody objects and applying a new gravity direction to them.
/// Higher-tier cells unlock additional modes: Pulse (T2), Lock (T3), Flip (T4).
/// </summary>
public class GravityGun : MonoBehaviour
{
    // Charge costs for gun actions
    private const float COST_GRAVITY_APPLY = 15f;      // Cost to apply gravity to selected objects
    private const float COST_GRAVITY_REMOVE = 8f;      // Cost to remove gravity (legacy)
    private const float COST_PLAYER_SELF = 25f;        // Additional cost when player is in selection
    private const float COST_GRAVITY_PULSE = 30f;      // Gravity Pulse fire (T2)
    private const float COST_GRAVITY_LOCK = 20f;       // Gravity Lock drain per second (T3)
    private const float COST_CORE_RESONATOR = 60f;     // Gravity Flip fire (T4)

    public enum Mode
    {
        Selection,
        GravityPlacement,
        PulseMode,
        LockMode,
        FlipMode
    }

    [Header("Input")]
    [Tooltip("Primary fire (left click). Behavior depends on current mode.")]
    public InputActionReference Shoot;

    [Tooltip("Secondary fire (right click). Behavior depends on current mode.")]
    public InputActionReference AltShoot;

    [Tooltip("Switch back to basic Selection mode (default key: F).")]
    public InputActionReference SwitchMode;

    [Tooltip("Toggle player selection in selection mode.")]
    public InputActionReference SelfSelect;

    [Tooltip("Enter Pulse mode (Tier 2).")]
    public InputActionReference GravityPulse;

    [Tooltip("Enter Lock mode (Tier 3).")]
    public InputActionReference GravityLockToggle;

    [Tooltip("Enter Flip mode (Tier 4).")]
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
    [Tooltip("Duration in seconds that gravity is suspended for a flipped body.")]
    [SerializeField] private float flipDuration = 4f;
    [Tooltip("Upward impulse applied to a flipped body so it visibly hovers off the ground.")]
    [SerializeField] private float flipHoverImpulse = 6f;

    [Header("Testing")]
    [Tooltip("Unlocks all tiers and grants infinite charge regardless of installed cell.")]
    [SerializeField] private bool testingMode = false;

    private Mode currentMode = Mode.Selection;
    private int gravityObjectLayerMask;
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

    // ---- Public state for UI ----
    public bool TestingMode => testingMode;
    public Mode CurrentMode => currentMode;
    public bool IsLockActive => isLockActive;

    /// <summary>Fired when the gun's mode changes.</summary>
    public event Action<Mode> OnModeChanged;

    /// <summary>Fired when the selection list changes. Passes (currentCount, maxCount).</summary>
    public event Action<int, int> OnSelectionChanged;

    /// <summary>Fired when the effective tier changes (e.g. cell installed, testing mode toggled).</summary>
    public event Action<int> OnEffectiveTierChanged;

    private int lastEffectiveTier = -1;

    /// <summary>
    /// Highest tier whose abilities are currently available.
    /// 1 = base, 2 = Pulse, 3 = Lock, 4 = Flip.
    /// </summary>
    public int EffectiveTier
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
        gravityObjectLayerMask = LayerMask.GetMask("GravityObject");
        if (gravityObjectLayerMask == 0)
            Debug.LogWarning("Layer 'GravityObject' not found. GravityGun raycasts will not hit anything.", this);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (batterySystem == null)
        {
            batterySystem = GetComponent<GunBatterySystem>();
            if (batterySystem == null)
                Debug.LogWarning("GunBatterySystem not found on this GameObject. Gun will not charge check.", this);
        }

        if (Shoot != null)              { Shoot.action.performed += OnShoot;                          Shoot.action.Enable(); }
        if (AltShoot != null)           { AltShoot.action.performed += OnAltShoot;                    AltShoot.action.Enable(); }
        if (SwitchMode != null)         { SwitchMode.action.performed += OnSwitchToBasic;             SwitchMode.action.Enable(); }
        if (SelfSelect != null)         { SelfSelect.action.performed += OnSelfSelect;                SelfSelect.action.Enable(); }
        if (GravityPulse != null)       { GravityPulse.action.performed += OnEnterPulseMode;          GravityPulse.action.Enable(); }
        if (GravityLockToggle != null)  { GravityLockToggle.action.performed += OnEnterLockMode;      GravityLockToggle.action.Enable(); }
        if (GravityFlip != null)        { GravityFlip.action.performed += OnEnterFlipMode;            GravityFlip.action.Enable(); }
        Debug.Log("[GravityGun] Awake — actions registered");

        UpdateVisuals();
    }

    private void Start()
    {
        if (batterySystem != null)
            batterySystem.OnChargeChanged += OnBatteryCharge;

        // Fire initial UI events.
        FireSelectionEvent();
        FireTierEventIfChanged();
        OnModeChanged?.Invoke(currentMode);
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
        if (SwitchMode != null)         SwitchMode.action.performed -= OnSwitchToBasic;
        if (SelfSelect != null)         SelfSelect.action.performed -= OnSelfSelect;
        if (GravityPulse != null)       GravityPulse.action.performed -= OnEnterPulseMode;
        if (GravityLockToggle != null)  GravityLockToggle.action.performed -= OnEnterLockMode;
        if (GravityFlip != null)        GravityFlip.action.performed -= OnEnterFlipMode;

        if (batterySystem != null)
            batterySystem.OnChargeChanged -= OnBatteryCharge;
    }

    private void Update()
    {
        if (isLockActive)
            HandleLockMaintenance();

        // Catch tier changes (e.g. cell installed at runtime, testingMode toggled in inspector).
        FireTierEventIfChanged();
    }

    private bool IsBlockedByUI()
    {
        return InventoryUI.Instance != null && InventoryUI.Instance.IsOpen;
    }

    // ---- Mode change helpers ----

    private void SetMode(Mode newMode)
    {
        if (currentMode == newMode) return;
        Mode previous = currentMode;

        // Leaving Lock mode while a lock is engaged → release the lock.
        if (previous == Mode.LockMode && isLockActive)
            EndGravityLock();

        currentMode = newMode;
        Debug.Log($"[GravityGun] Mode changed: {previous} → {currentMode} (tier {EffectiveTier})");
        UpdateVisuals();
        OnModeChanged?.Invoke(currentMode);
    }

    private void FireSelectionEvent()
    {
        OnSelectionChanged?.Invoke(selectedBodies.Count, MaxSelectionCount);
    }

    private void FireTierEventIfChanged()
    {
        int tier = EffectiveTier;
        if (tier != lastEffectiveTier)
        {
            lastEffectiveTier = tier;
            Debug.Log($"[GravityGun] Effective tier is now {tier} (testingMode={testingMode}).");
            OnEffectiveTierChanged?.Invoke(tier);
            // Selection cap may have changed.
            FireSelectionEvent();
        }
    }

    private void OnBatteryCharge(float current, float max)
    {
        // Charge changes can correspond to cell swaps; recheck tier.
        FireTierEventIfChanged();
    }

    // ---- Mode-entry input handlers ----

    private void OnSwitchToBasic(InputAction.CallbackContext ctx)
    {
        if (!isEquipped || IsBlockedByUI()) return;
        SetMode(Mode.Selection);
    }

    private void OnEnterPulseMode(InputAction.CallbackContext ctx)
    {
        if (!isEquipped || IsBlockedByUI()) return;
        if (EffectiveTier < 2)
        {
            Debug.Log("[GravityGun] Pulse mode locked — Tier 2 cell required.");
            return;
        }
        SetMode(Mode.PulseMode);
    }

    private void OnEnterLockMode(InputAction.CallbackContext ctx)
    {
        if (!isEquipped || IsBlockedByUI()) return;
        if (EffectiveTier < 3)
        {
            Debug.Log("[GravityGun] Lock mode locked — Tier 3 cell required.");
            return;
        }
        SetMode(Mode.LockMode);
    }

    private void OnEnterFlipMode(InputAction.CallbackContext ctx)
    {
        if (!isEquipped || IsBlockedByUI()) return;
        if (EffectiveTier < 4)
        {
            Debug.Log("[GravityGun] Flip mode locked — Tier 4 cell required.");
            return;
        }
        SetMode(Mode.FlipMode);
    }

    // ---- Click dispatch ----

    private void OnShoot(InputAction.CallbackContext ctx)
    {
        if (!isEquipped || IsBlockedByUI()) return;

        switch (currentMode)
        {
            case Mode.Selection:
            case Mode.LockMode:
                ToggleBodySelection();
                break;

            case Mode.GravityPlacement:
                if (RequireCharge(0f)) TryPlaceGravity();
                break;

            case Mode.PulseMode:
                FirePulse();
                break;

            case Mode.FlipMode:
                FireFlipSingle();
                break;
        }
    }

    private void OnAltShoot(InputAction.CallbackContext ctx)
    {
        if (!isEquipped || IsBlockedByUI()) return;

        switch (currentMode)
        {
            case Mode.Selection:
                SetMode(Mode.GravityPlacement);
                break;

            case Mode.GravityPlacement:
                SetMode(Mode.Selection);
                break;

            case Mode.LockMode:
                ToggleLockEffect();
                break;

            case Mode.PulseMode:
            case Mode.FlipMode:
                // Right click intentionally does nothing in these modes.
                break;
        }
    }

    private void OnSelfSelect(InputAction.CallbackContext ctx)
    {
        if (!isEquipped) return;
        if (currentMode == Mode.Selection || currentMode == Mode.LockMode)
            TryTogglePlayerSelection();
    }

    /// <summary>Quick "do we have any charge for placement?" gate.</summary>
    private bool RequireCharge(float _)
    {
        if (batterySystem != null && !batterySystem.HasCharge)
        {
            PlayOutOfChargeAudio();
            return false;
        }
        return true;
    }

    // ---- Mode actions ----

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
            FireSelectionEvent();
        }
        else if (selectedBodies.Count < MaxSelectionCount)
        {
            selectedBodies.Add(playerBody);
            Debug.Log("Player selected");
            FireSelectionEvent();
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
            FireSelectionEvent();
        }
        else if (selectedBodies.Count < MaxSelectionCount)
        {
            selectedBodies.Add(body);
            Debug.Log($"Selected body: {body.name}");
            FireSelectionEvent();
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

        float cost = COST_GRAVITY_APPLY;
        bool playerInSelection = selectedBodies.Contains(GameObject.FindWithTag("Player")?.GetComponent<GravityBody>());
        if (playerInSelection)
            cost += COST_PLAYER_SELF;

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

    // ---- Pulse ----

    private void FirePulse()
    {
        if (batterySystem != null && !batterySystem.TrySpendCharge(COST_GRAVITY_PULSE))
        {
            PlayOutOfChargeAudio();
            return;
        }

        if (!TryRaycastDirection(out RaycastHit hit))
            return;

        Collider[] hits = Physics.OverlapSphere(hit.point, pulseRadius);
        int affected = 0;
        foreach (var c in hits)
        {
            Rigidbody rb = c.attachedRigidbody;
            if (rb == null || rb.isKinematic) continue;
            rb.AddExplosionForce(pulseForce, hit.point, pulseRadius, pulseUpward, ForceMode.Impulse);
            affected++;
        }
        Debug.Log($"[GravityGun] Pulse fired at {hit.point}. Launched {affected} rigidbodies.");
    }

    // ---- Flip (single target with hover) ----

    private void FireFlipSingle()
    {
        if (!TryRaycast(out RaycastHit hit))
        {
            Debug.Log("[GravityGun] Flip raycast missed a GravityObject.");
            return;
        }

        GravityBody body = hit.collider.GetComponent<GravityBody>();
        if (body == null) return;

        if (batterySystem != null && !batterySystem.TrySpendCharge(COST_CORE_RESONATOR))
        {
            PlayOutOfChargeAudio();
            return;
        }

        Vector3 originalDir = body.gravityDirection;
        GravityController.Instance?.SetGravity(body, Vector3.zero);

        Rigidbody rb = body.GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            // Lift opposite of the body's previous gravity. If gravity was unset, default to world up.
            Vector3 hoverDir = originalDir.sqrMagnitude > 0.0001f ? -originalDir.normalized : Vector3.up;
            rb.AddForce(hoverDir * flipHoverImpulse, ForceMode.Impulse);
        }

        StartCoroutine(RestoreGravityAfter(body, originalDir, flipDuration));
        Debug.Log($"[GravityGun] Flip applied to {body.name} for {flipDuration}s; hovering and zero-G.");
    }

    private IEnumerator RestoreGravityAfter(GravityBody body, Vector3 direction, float duration)
    {
        yield return new WaitForSeconds(duration);
        if (body == null || GravityController.Instance == null) yield break;
        GravityController.Instance.SetGravity(body, direction);
        Debug.Log($"[GravityGun] Flip restored on {body.name}.");
    }

    // ---- Lock ----

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

        for (int i = 0; i < lockedBodies.Count; i++)
        {
            var lb = lockedBodies[i];
            if (lb.rb == null) continue;
            lb.rb.MovePosition(lb.position);
            lb.rb.MoveRotation(lb.rotation);
        }
    }

    // ---- Raycast helpers ----

    private bool TryRaycast(out RaycastHit hit)
    {
        hit = default;
        Camera cam = Camera.main;
        if (cam == null) return false;
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        return Physics.Raycast(ray, out hit, 100f, gravityObjectLayerMask);
    }

    private bool TryRaycastDirection(out RaycastHit hit)
    {
        hit = default;
        Camera cam = Camera.main;
        if (cam == null) return false;
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
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
            audioSource.PlayOneShot(outOfChargeClip);
    }
}
