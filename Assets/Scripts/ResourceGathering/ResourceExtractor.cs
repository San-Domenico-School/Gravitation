using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Hotbar-equipped tool. Hold the primary fire input while aimed at a
/// ResourceCrack to extract its resource over `extractTime` seconds.
/// Tier 1 / Tier 2 differ only in extraction time and yield range —
/// configure both via the inspector or by passing a tier preset.
/// </summary>
public class ResourceExtractor : MonoBehaviour
{
    [Header("Tier")]
    [Tooltip("1 = T1 Resource Extractor, 2 = T2. Used by ResourceExtractorEquipSystem to choose which ItemData equips this tool.")]
    [SerializeField] private int extractorTier = 1;

    [Header("Extraction Tuning")]
    [Tooltip("Seconds of held input required to fully extract a crack.")]
    [SerializeField] private float extractTime = 2.0f;

    [Tooltip("Minimum resources granted per successful extraction (inclusive).")]
    [SerializeField] private int yieldMin = 1;

    [Tooltip("Maximum resources granted per successful extraction (inclusive).")]
    [SerializeField] private int yieldMax = 2;

    [Header("Targeting")]
    [Tooltip("Maximum raycast distance from camera to find a crack.")]
    [SerializeField] private float maxRange = 8f;

    [Tooltip("Layer mask used for the extractor raycast. Include the layer the ResourceCrack collider sits on.")]
    [SerializeField] private LayerMask crackLayerMask = ~0;

    [Header("Input")]
    [Tooltip("Primary fire (hold to extract). Reuses the GravityGun's Shoot action if not assigned.")]
    [SerializeField] private InputActionReference extractAction;

    [Header("Visual")]
    [SerializeField] private GameObject toolVisual;
    [SerializeField] private AudioClip extractCompleteClip;

    [Header("Debug")]
    [SerializeField] private bool logExtracts = false;

    private bool isEquipped = false;
    private bool isExtracting = false;
    private float extractProgress = 0f;
    private ResourceCrack currentTarget;
    private AudioSource audioSource;

    public int Tier => extractorTier;
    public bool IsEquipped => isEquipped;
    public bool IsExtracting => isExtracting;
    public float ExtractTime => extractTime;
    public float Progress01 => isExtracting && extractTime > 0f ? Mathf.Clamp01(extractProgress / extractTime) : 0f;
    public ResourceCrack CurrentTarget => currentTarget;

    public event Action<ResourceExtractor> OnExtractStarted;
    public event Action<ResourceExtractor> OnExtractCanceled;
    public event Action<ResourceExtractor, ResourceCrack, int> OnExtractCompleted;
    public event Action<float> OnProgressChanged;
    public event Action<bool> OnEquippedChanged;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        SetEquipped(false);
    }

    public void SetEquipped(bool value)
    {
        isEquipped = value;
        if (toolVisual != null)
            foreach (var r in toolVisual.GetComponentsInChildren<Renderer>(true))
                r.enabled = value;
        if (!value) CancelExtract("unequipped");
        OnEquippedChanged?.Invoke(value);
    }

    private void Update()
    {
        if (!isEquipped) return;
        if (extractAction == null || extractAction.action == null) return;

        bool inputBlocked = InventoryUI.Instance != null && InventoryUI.Instance.IsOpen;
        bool isHolding = !inputBlocked && extractAction.action.IsPressed();

        if (isHolding)
        {
            TickExtract();
        }
        else if (isExtracting)
        {
            CancelExtract("button released");
        }
    }

    private void OnEnable()
    {
        if (extractAction != null && extractAction.action != null)
            extractAction.action.Enable();
    }

    private void TickExtract()
    {
        if (!TryFindCrack(out ResourceCrack crack))
        {
            if (isExtracting) CancelExtract("no target");
            return;
        }

        if (!crack.CanExtract)
        {
            if (isExtracting) CancelExtract("crack regenerating");
            return;
        }

        if (currentTarget != crack)
        {
            currentTarget = crack;
            extractProgress = 0f;
            isExtracting = true;
            OnExtractStarted?.Invoke(this);
            OnProgressChanged?.Invoke(0f);
        }

        extractProgress += Time.deltaTime;
        OnProgressChanged?.Invoke(Progress01);

        if (extractProgress >= extractTime)
        {
            CompleteExtract();
        }
    }

    private bool TryFindCrack(out ResourceCrack crack)
    {
        crack = null;
        Camera cam = Camera.main;
        if (cam == null || Mouse.current == null) return false;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, maxRange, crackLayerMask, QueryTriggerInteraction.Collide))
            return false;

        crack = hit.collider.GetComponentInParent<ResourceCrack>();
        return crack != null;
    }

    private void CompleteExtract()
    {
        if (currentTarget == null) { CancelExtract("target lost"); return; }

        int amount = UnityEngine.Random.Range(yieldMin, yieldMax + 1);
        bool ok = currentTarget.TryExtract(amount, out int actuallyAdded);
        if (logExtracts)
            Debug.Log($"[ResourceExtractor] Extract complete tier={extractorTier} requested={amount} added={actuallyAdded} resource={currentTarget.Resource?.itemName}");

        if (ok && extractCompleteClip != null)
            audioSource.PlayOneShot(extractCompleteClip);

        ResourceCrack done = currentTarget;
        currentTarget = null;
        isExtracting = false;
        extractProgress = 0f;
        OnProgressChanged?.Invoke(0f);
        OnExtractCompleted?.Invoke(this, done, actuallyAdded);
    }

    public void CancelExtract(string reason)
    {
        if (!isExtracting) return;
        isExtracting = false;
        currentTarget = null;
        extractProgress = 0f;
        OnProgressChanged?.Invoke(0f);
        OnExtractCanceled?.Invoke(this);
        if (logExtracts) Debug.Log($"[ResourceExtractor] Cancel ({reason})");
    }
}
