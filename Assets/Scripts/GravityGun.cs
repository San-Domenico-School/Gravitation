using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Tier-gated gravity gun with five modes: Selection, GravityPlacement, PulseMode,
/// LockMode, and FlipMode. The installed Graviton Cell determines the effective tier
/// and which modes are accessible.
/// </summary>
public class GravityGun : MonoBehaviour
{
    private const float COST_GRAVITY_APPLY = 15f;       // place gravity
    private const float COST_PLAYER_SELF   = 25f;       // additional cost if player is in selection
    private const float COST_GRAVITY_PULSE = 30f;       // pulse fire
    private const float COST_GRAVITY_LOCK  = 20f;       // lock drain per second
    private const float COST_CORE_RESONATOR = 60f;      // flip fire

    // T4 placement-strength control (scroll wheel).
    private const float MIN_PLACEMENT_STRENGTH = 0.81f;
    private const float MAX_PLACEMENT_STRENGTH = 30f;
    private const float STRENGTH_STEP          = 1f;
    private const float CONE_SCALE_MIN         = 0.3f;
    private const float CONE_SCALE_MAX         = 2f;

    public enum Mode
    {
        Selection,
        GravityPlacement,
        PulseMode,
        LockMode,
        FlipMode
    }

    [Header("Input")]
    [Tooltip("Primary fire (left click). Dispatched on currentMode.")]
    public InputActionReference Shoot;

    [Tooltip("Secondary fire (right click). Dispatched on currentMode.")]
    public InputActionReference AltShoot;

    [Tooltip("Returns to base Selection mode from anywhere (default: F).")]
    public InputActionReference SwitchMode;

    [Tooltip("Toggles player self-selection in Selection mode (default: Q).")]
    public InputActionReference SelfSelect;

    [Tooltip("Enters Pulse mode (Tier 2+, default: G).")]
    public InputActionReference GravityPulse;

    [Tooltip("Enters Lock mode (Tier 3+, default: V).")]
    public InputActionReference GravityLockToggle;

    [Tooltip("Enters Flip mode (Tier 4, default: B).")]
    public InputActionReference GravityFlip;

    [Header("Battery")]
    [SerializeField] private GunBatterySystem batterySystem;
    [SerializeField] private AudioClip outOfChargeClip;

    [Header("Selection")]
    [Tooltip("Currently selected GravityBody objects.")]
    public List<GravityBody> selectedBodies = new List<GravityBody>(20);

    [Header("Pulse Mode")]
    [Tooltip("Radius of the impulse blast.")]
    [SerializeField] private float pulseRadius = 8f;
    [Tooltip("Explosion force applied by the impulse blast.")]
    [SerializeField] private float pulseForce = 1200f;
    [Tooltip("Upward modifier applied to AddExplosionForce.")]
    [SerializeField] private float pulseUpwardModifier = 1f;

    [Header("Flip Mode")]
    [Tooltip("Seconds to suspend gravity on the target.")]
    [SerializeField] private float flipDuration = 4f;
    [Tooltip("Upward impulse applied to make the target visibly hover.")]
    [SerializeField] private float flipHopImpulse = 4f;

    [Header("Visuals")]
    [SerializeField] private GameObject gunVisual;
    [SerializeField] private GameObject batteryUI;
    [Tooltip("Cone shown in placement mode that follows the crosshair.")]
    public GameObject selectionCone;
    [Tooltip("Crosshair UI Image shown in placement mode.")]
    public Image placementCrosshair;
    [Tooltip("Maximum raycast distance for the placement cone.")]
    [SerializeField] private float coneMaxDistance = 100f;
    [Tooltip("How far above the surface to float the cone.")]
    [SerializeField] private float coneSurfaceOffset = 0.02f;

    [Header("Testing")]
    [Tooltip("Unlocks all tiers and grants infinite charge regardless of installed cell.")]
    [SerializeField] private bool testingMode = false;

    // T4-only placement strength. Persists between placements; clamped on every change.
    private float placementStrength = 9.81f;
    private Vector3 baseConeScale = Vector3.one;
    private bool baseConeScaleCaptured = false;

    private Mode currentMode = Mode.Selection;
    private int gravityObjectLayerMask;
    private AudioSource audioSource;
    private bool isEquipped = false;

    // Lock state
    private bool isLockActive = false;
    private readonly List<LockedBodyState> lockedBodies = new List<LockedBodyState>();

    // Track previous tier so we can fire OnEffectiveTierChanged when it changes.
    private int lastEffectiveTier = -1;
    private bool lastTestingMode = false;

    private struct LockedBodyState
    {
        public GravityBody body;
        public Rigidbody rb;
        public Vector3 capturedPosition;
        public Quaternion capturedRotation;
        public bool wasKinematic;
        public Vector3 originalGravityDir;
    }

    public Mode CurrentMode => currentMode;
    public bool IsLockActive => isLockActive;
    public bool TestingMode => testingMode;

    public int EffectiveTier => testingMode
        ? 4
        : (batterySystem != null && batterySystem.CurrentCell != null ? batterySystem.CurrentCell.Tier : 1);

    public int MaxSelectionCount =>
        EffectiveTier >= 4 ? 20 :
        EffectiveTier >= 2 ? 10 :
        3;

    public float PlacementStrength => placementStrength;
    public float MinPlacementStrength => MIN_PLACEMENT_STRENGTH;
    public float MaxPlacementStrength => MAX_PLACEMENT_STRENGTH;

    public event Action<Mode> OnModeChanged;
    public event Action<int, int> OnSelectionChanged;
    public event Action<int> OnEffectiveTierChanged;

    private void Awake()
    {
        gravityObjectLayerMask = LayerMask.GetMask("GravityObject");

        if (gravityObjectLayerMask == 0)
        {
            Debug.LogWarning("Layer 'GravityObject' not found. GravityGun raycasts will not hit anything.", this);
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (batterySystem == null)
        {
            batterySystem = GetComponent<GunBatterySystem>();
            if (batterySystem == null)
            {
                Debug.LogWarning("GunBatterySystem not found on this GameObject. Gun will not charge check.", this);
            }
        }

        if (Shoot != null)             { Shoot.action.performed += OnShoot;                       Shoot.action.Enable(); }
        if (AltShoot != null)          { AltShoot.action.performed += OnAltShoot;                 AltShoot.action.Enable(); }
        if (SwitchMode != null)        { SwitchMode.action.performed += OnSwitchMode;             SwitchMode.action.Enable(); }
        if (SelfSelect != null)        { SelfSelect.action.performed += OnSelfSelect;             SelfSelect.action.Enable(); }
        if (GravityPulse != null)      { GravityPulse.action.performed += OnGravityPulse;         GravityPulse.action.Enable(); }
        if (GravityLockToggle != null) { GravityLockToggle.action.performed += OnGravityLock;     GravityLockToggle.action.Enable(); }
        if (GravityFlip != null)       { GravityFlip.action.performed += OnGravityFlip;           GravityFlip.action.Enable(); }
        Debug.Log("[GravityGun] Awake — actions registered");

        lastEffectiveTier = EffectiveTier;
        lastTestingMode = testingMode;

        if (selectionCone != null)
        {
            baseConeScale = selectionCone.transform.localScale;
            baseConeScaleCaptured = true;
        }

        if (GravityController.Instance != null)
            placementStrength = Mathf.Clamp(GravityController.Instance.gravityStrength, MIN_PLACEMENT_STRENGTH, MAX_PLACEMENT_STRENGTH);

        UpdateVisuals();
        UpdateConeScale();
    }

    private void Start()
    {
        // Re-emit at Start so listeners that subscribe in their own Awake/Start get the initial values.
        OnEffectiveTierChanged?.Invoke(EffectiveTier);
        OnSelectionChanged?.Invoke(selectedBodies.Count, MaxSelectionCount);

        // GravityController.Instance may have been null in Awake on first scene load; resync here.
        if (!baseConeScaleCaptured && selectionCone != null)
        {
            baseConeScale = selectionCone.transform.localScale;
            baseConeScaleCaptured = true;
            UpdateConeScale();
        }
    }

    public void SetEquipped(bool value)
    {
        isEquipped = value;

        if (gunVisual != null)
            foreach (var r in gunVisual.GetComponentsInChildren<Renderer>(true))
                r.enabled = value;

        if (batteryUI != null) batteryUI.SetActive(value);

        if (!value && isLockActive)
            ReleaseLock("gun unequipped");

        UpdateVisuals();
    }

    private void OnDestroy()
    {
        if (Shoot != null)             Shoot.action.performed -= OnShoot;
        if (AltShoot != null)          AltShoot.action.performed -= OnAltShoot;
        if (SwitchMode != null)        SwitchMode.action.performed -= OnSwitchMode;
        if (SelfSelect != null)        SelfSelect.action.performed -= OnSelfSelect;
        if (GravityPulse != null)      GravityPulse.action.performed -= OnGravityPulse;
        if (GravityLockToggle != null) GravityLockToggle.action.performed -= OnGravityLock;
        if (GravityFlip != null)       GravityFlip.action.performed -= OnGravityFlip;
    }

    private void Update()
    {
        // Detect tier or testing-mode flips coming from the inspector or cell installs.
        int currentTier = EffectiveTier;
        if (currentTier != lastEffectiveTier || testingMode != lastTestingMode)
        {
            lastEffectiveTier = currentTier;
            lastTestingMode = testingMode;
            OnEffectiveTierChanged?.Invoke(currentTier);
            OnSelectionChanged?.Invoke(selectedBodies.Count, MaxSelectionCount);
        }

        // Drain charge while Lock is engaged.
        if (isLockActive)
        {
            DrainLockCharge();
            ReapplyLockedTransforms();
        }

        // T4: scroll-wheel placement strength control (placement mode only).
        HandlePlacementStrengthScroll();
    }

    private void HandlePlacementStrengthScroll()
    {
        if (currentMode != Mode.GravityPlacement) return;
        if (EffectiveTier < 4) return;
        if (InputBlocked()) return;
        if (Mouse.current == null) return;

        float scrollY = Mouse.current.scroll.ReadValue().y;
        if (scrollY > 0.01f)
            AdjustPlacementStrength(STRENGTH_STEP);
        else if (scrollY < -0.01f)
            AdjustPlacementStrength(-STRENGTH_STEP);
    }

    private void AdjustPlacementStrength(float delta)
    {
        float previous = placementStrength;
        placementStrength = Mathf.Clamp(placementStrength + delta, MIN_PLACEMENT_STRENGTH, MAX_PLACEMENT_STRENGTH);
        if (!Mathf.Approximately(previous, placementStrength))
        {
            UpdateConeScale();
            Debug.Log($"[GravityGun] Placement strength: {placementStrength:F2} ({MIN_PLACEMENT_STRENGTH}–{MAX_PLACEMENT_STRENGTH})");
        }
    }

    private void UpdateConeScale()
    {
        if (selectionCone == null || !baseConeScaleCaptured) return;
        float t = Mathf.InverseLerp(MIN_PLACEMENT_STRENGTH, MAX_PLACEMENT_STRENGTH, placementStrength);
        float multiplier = Mathf.Lerp(CONE_SCALE_MIN, CONE_SCALE_MAX, t);
        selectionCone.transform.localScale = baseConeScale * multiplier;
    }

    private void LateUpdate()
    {
        if (currentMode == Mode.GravityPlacement && selectionCone != null && selectionCone.activeSelf)
        {
            PositionConeAtCrosshair();
        }
    }

    // ─── Input handlers ────────────────────────────────────────────────────────

    private bool InputBlocked()
    {
        bool invOpen = InventoryUI.Instance != null && InventoryUI.Instance.IsOpen;
        return !isEquipped || invOpen;
    }

    private void OnShoot(InputAction.CallbackContext ctx)
    {
        if (InputBlocked()) return;

        // Selection mode allows clicking even with empty charge (selection itself costs nothing).
        if (currentMode != Mode.Selection && currentMode != Mode.LockMode)
        {
            if (batterySystem != null && !batterySystem.HasCharge)
            {
                PlayOutOfChargeAudio();
                return;
            }
        }

        switch (currentMode)
        {
            case Mode.Selection:        TryToggleSelectBody(); break;
            case Mode.GravityPlacement: TryPlaceGravity();     break;
            case Mode.PulseMode:        TryFirePulse();        break;
            case Mode.LockMode:         TryToggleSelectBody(); break;
            case Mode.FlipMode:         TryFireFlip();         break;
        }
    }

    private void OnAltShoot(InputAction.CallbackContext ctx)
    {
        if (InputBlocked()) return;

        switch (currentMode)
        {
            case Mode.Selection:
                SetMode(Mode.GravityPlacement);
                break;

            case Mode.GravityPlacement:
                SetMode(Mode.Selection);
                break;

            case Mode.LockMode:
                ToggleLockEngage();
                break;

            // PulseMode and FlipMode: right click does nothing.
            case Mode.PulseMode:
            case Mode.FlipMode:
            default:
                break;
        }
    }

    private void OnSwitchMode(InputAction.CallbackContext ctx)
    {
        if (InputBlocked()) return;
        // F always returns to base Selection mode.
        SetMode(Mode.Selection);
    }

    private void OnSelfSelect(InputAction.CallbackContext ctx)
    {
        if (InputBlocked()) return;
        if (currentMode == Mode.Selection)
            TryTogglePlayerSelection();
    }

    private void OnGravityPulse(InputAction.CallbackContext ctx)
    {
        if (InputBlocked()) return;
        if (!RequireTier(2, "Pulse")) return;
        SetMode(Mode.PulseMode);
    }

    private void OnGravityLock(InputAction.CallbackContext ctx)
    {
        if (InputBlocked()) return;
        if (!RequireTier(3, "Lock")) return;
        SetMode(Mode.LockMode);
    }

    private void OnGravityFlip(InputAction.CallbackContext ctx)
    {
        if (InputBlocked()) return;
        if (!RequireTier(4, "Flip")) return;
        SetMode(Mode.FlipMode);
    }

    // ─── Mode plumbing ─────────────────────────────────────────────────────────

    private bool RequireTier(int tier, string modeName)
    {
        if (EffectiveTier >= tier) return true;
        Debug.Log($"[GravityGun] {modeName} mode locked — Tier {tier} cell required.");
        return false;
    }

    private void SetMode(Mode next)
    {
        if (next == currentMode) return;

        Mode previous = currentMode;

        // Leaving LockMode auto-releases an active lock.
        if (previous == Mode.LockMode && isLockActive)
            ReleaseLock("left Lock mode");

        currentMode = next;
        Debug.Log($"[GravityGun] Mode changed: {previous} → {currentMode} (tier {EffectiveTier})");
        UpdateVisuals();
        OnModeChanged?.Invoke(currentMode);
    }

    // ─── Selection ─────────────────────────────────────────────────────────────

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

        OnSelectionChanged?.Invoke(selectedBodies.Count, MaxSelectionCount);
    }

    private void TryToggleSelectBody()
    {
        if (!TryRaycast(out RaycastHit hit))
        {
            Debug.Log("Raycast missed in selection mode");
            return;
        }

        GravityBody body = hit.collider.GetComponent<GravityBody>();
        if (body == null) return;

        if (selectedBodies.Contains(body))
        {
            selectedBodies.Remove(body);
            Debug.Log($"Deselected body: {body.name}");
        }
        else
        {
            if (selectedBodies.Count >= MaxSelectionCount)
            {
                Debug.Log("Cannot select: max selection count reached");
                return;
            }
            selectedBodies.Add(body);
            Debug.Log($"Selected body: {body.name}");
        }

        OnSelectionChanged?.Invoke(selectedBodies.Count, MaxSelectionCount);
    }

    // ─── Placement ─────────────────────────────────────────────────────────────

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
        GameObject playerObject = GameObject.FindWithTag("Player");
        GravityBody playerBody = playerObject != null ? playerObject.GetComponent<GravityBody>() : null;
        if (playerBody != null && selectedBodies.Contains(playerBody))
            cost += COST_PLAYER_SELF;

        if (batterySystem != null && !batterySystem.TrySpendCharge(cost))
        {
            Debug.Log($"Insufficient charge. Required: {cost}, Available: {batterySystem.CurrentCharge}");
            PlayOutOfChargeAudio();
            return;
        }

        Vector3 gravityDirection = -hit.normal;

        if (EffectiveTier >= 4)
        {
            // T4: apply per-body strength to every member of the selection field.
            for (int i = 0; i < selectedBodies.Count; i++)
            {
                GravityBody body = selectedBodies[i];
                if (body == null) continue;
                body.SetGravity(gravityDirection, placementStrength);
            }
            Debug.Log($"Placed gravity at {hit.point} with direction {gravityDirection}, strength {placementStrength:F2}. Spent {cost} charge.");
        }
        else
        {
            GravityController.Instance?.SetGravity(selectedBodies, gravityDirection);
            Debug.Log($"Placed gravity at {hit.point} with direction {gravityDirection}. Spent {cost} charge.");
        }
    }

    // ─── Pulse ─────────────────────────────────────────────────────────────────

    private void TryFirePulse()
    {
        if (!TryRaycastDirection(out RaycastHit hit))
        {
            Debug.Log("Raycast missed in pulse mode");
            return;
        }

        if (batterySystem != null && !batterySystem.TrySpendCharge(COST_GRAVITY_PULSE))
        {
            Debug.Log($"Insufficient charge for Pulse. Required: {COST_GRAVITY_PULSE}, Available: {batterySystem.CurrentCharge}");
            PlayOutOfChargeAudio();
            return;
        }

        Vector3 origin = hit.point;
        Collider[] inRange = Physics.OverlapSphere(origin, pulseRadius);
        int affected = 0;
        for (int i = 0; i < inRange.Length; i++)
        {
            Rigidbody rb = inRange[i].attachedRigidbody;
            if (rb == null) continue;
            // Avoid double-applying when multiple colliders share a rigidbody.
            if (!IsFirstColliderForRigidbody(inRange, i, rb)) continue;
            rb.AddExplosionForce(pulseForce, origin, pulseRadius, pulseUpwardModifier, ForceMode.Impulse);
            affected++;
        }

        Debug.Log($"[GravityGun] Pulse fired at {origin}. Affected {affected} rigidbody(ies). Spent {COST_GRAVITY_PULSE} charge.");
    }

    private static bool IsFirstColliderForRigidbody(Collider[] arr, int index, Rigidbody rb)
    {
        for (int i = 0; i < index; i++)
        {
            if (arr[i] != null && arr[i].attachedRigidbody == rb)
                return false;
        }
        return true;
    }

    // ─── Lock ──────────────────────────────────────────────────────────────────

    private void ToggleLockEngage()
    {
        if (isLockActive)
        {
            ReleaseLock("toggled off");
        }
        else
        {
            EngageLock();
        }
    }

    private void EngageLock()
    {
        if (selectedBodies.Count == 0)
        {
            Debug.Log("[GravityGun] Lock engage failed — no selected bodies.");
            return;
        }

        if (batterySystem != null && !batterySystem.HasCharge)
        {
            Debug.Log("[GravityGun] Lock engage failed — out of charge.");
            PlayOutOfChargeAudio();
            return;
        }

        lockedBodies.Clear();
        for (int i = 0; i < selectedBodies.Count; i++)
        {
            GravityBody body = selectedBodies[i];
            if (body == null) continue;

            Rigidbody rb = body.GetComponent<Rigidbody>();
            if (rb == null) continue;

            LockedBodyState state = new LockedBodyState
            {
                body = body,
                rb = rb,
                capturedPosition = rb.position,
                capturedRotation = rb.rotation,
                wasKinematic = rb.isKinematic,
                originalGravityDir = body.gravityDirection
            };

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            lockedBodies.Add(state);
        }

        if (lockedBodies.Count == 0)
        {
            Debug.Log("[GravityGun] Lock engage failed — no valid rigidbodies in selection.");
            return;
        }

        isLockActive = true;
        Debug.Log($"[GravityGun] Lock engaged on {lockedBodies.Count} body(ies).");
        OnModeChanged?.Invoke(currentMode);
    }

    private void ReleaseLock(string reason)
    {
        if (!isLockActive) return;

        for (int i = 0; i < lockedBodies.Count; i++)
        {
            LockedBodyState state = lockedBodies[i];
            if (state.rb != null)
            {
                state.rb.isKinematic = state.wasKinematic;
            }
        }
        lockedBodies.Clear();
        isLockActive = false;
        Debug.Log($"[GravityGun] Lock released ({reason}).");
        OnModeChanged?.Invoke(currentMode);
    }

    private void DrainLockCharge()
    {
        if (batterySystem == null) return;

        float cost = COST_GRAVITY_LOCK * Time.deltaTime;
        if (!batterySystem.TrySpendCharge(cost))
        {
            ReleaseLock("charge depleted");
        }
    }

    private void ReapplyLockedTransforms()
    {
        for (int i = 0; i < lockedBodies.Count; i++)
        {
            LockedBodyState state = lockedBodies[i];
            if (state.rb == null) continue;
            state.rb.MovePosition(state.capturedPosition);
            state.rb.MoveRotation(state.capturedRotation);
        }
    }

    // ─── Flip ──────────────────────────────────────────────────────────────────

    private void TryFireFlip()
    {
        if (!TryRaycast(out RaycastHit hit))
        {
            Debug.Log("Raycast missed in flip mode");
            return;
        }

        GravityBody body = hit.collider.GetComponent<GravityBody>();
        if (body == null)
        {
            Debug.Log("Flip target is not a GravityBody.");
            return;
        }

        if (batterySystem != null && !batterySystem.TrySpendCharge(COST_CORE_RESONATOR))
        {
            Debug.Log($"Insufficient charge for Flip. Required: {COST_CORE_RESONATOR}, Available: {batterySystem.CurrentCharge}");
            PlayOutOfChargeAudio();
            return;
        }

        Debug.Log($"[GravityGun] Flip fired on {body.name} for {flipDuration}s. Spent {COST_CORE_RESONATOR} charge.");
        StartCoroutine(FlipCoroutine(body));
    }

    private System.Collections.IEnumerator FlipCoroutine(GravityBody body)
    {
        Vector3 originalDir = body.gravityDirection;
        float originalStrength = body.gravityStrength;
        Rigidbody rb = body.GetComponent<Rigidbody>();

        // Hop opposite the body's own gravity (its local "up"), not world up.
        Vector3 hopDir = originalDir.sqrMagnitude > 0.0001f
            ? -originalDir.normalized
            : Vector3.up;

        body.gravityDirection = Vector3.zero;

        if (rb != null && !rb.isKinematic)
        {
            rb.AddForce(hopDir * flipHopImpulse, ForceMode.Impulse);
        }

        float elapsed = 0f;
        while (elapsed < flipDuration)
        {
            if (body == null) yield break;
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (body != null)
        {
            body.SetGravity(originalDir, originalStrength);
        }
    }

    // ─── Cone follow ───────────────────────────────────────────────────────────

    private void PositionConeAtCrosshair()
    {
        if (selectionCone == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Ray ray = cam.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, coneMaxDistance))
        {
            selectionCone.transform.position = hit.point + hit.normal * coneSurfaceOffset;
            selectionCone.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
        }
        else
        {
            selectionCone.transform.position = ray.origin + ray.direction * coneMaxDistance;
            selectionCone.transform.rotation = Quaternion.LookRotation(ray.direction);
        }
    }

    // ─── Raycasts ──────────────────────────────────────────────────────────────

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

    // ─── Visuals ───────────────────────────────────────────────────────────────

    private void UpdateVisuals()
    {
        bool placement = currentMode == Mode.GravityPlacement;

        if (selectionCone != null)
        {
            selectionCone.SetActive(placement);
            if (placement) PositionConeAtCrosshair();
        }

        if (placementCrosshair != null)
            placementCrosshair.enabled = placement;
    }

    private void PlayOutOfChargeAudio()
    {
        if (outOfChargeClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(outOfChargeClip);
        }
    }
}
