using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Crafter : MonoBehaviour
{
    [SerializeField] private CraftingTier crafterTier = CraftingTier.Tier1;
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private InputActionReference interactAction;

    public static event Action<Crafter> OnCrafterInRange;
    public static event Action<Crafter> OnCrafterOutOfRange;

    public CraftingTier Tier => crafterTier;

    private Transform playerTransform;
    private bool isInRange;

    private void Awake()
    {
        if (interactAction == null) { Debug.LogError($"[Crafter] {gameObject.name}: interactAction is not assigned!"); return; }
        interactAction.action.performed += OnInteract;
        interactAction.action.Enable();
        Debug.Log($"[Crafter] {gameObject.name}: Awake OK — tier={crafterTier}, range={interactionRange}");
    }

    private void Start()
    {
        var player = GameObject.FindWithTag("Player");
        if (player != null) playerTransform = player.transform;
        else Debug.LogError($"[Crafter] {gameObject.name}: No GameObject with tag 'Player' found!");
        Debug.Log($"[Crafter] {gameObject.name}: Start — playerFound={playerTransform != null}");
    }

    private void Update()
    {
        if (playerTransform == null) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        bool nowInRange = dist <= interactionRange;

        if (nowInRange && !isInRange)
        {
            isInRange = true;
            Debug.Log($"[Crafter] {gameObject.name}: Player IN range (dist={dist:F1})");
            OnCrafterInRange?.Invoke(this);
        }
        else if (!nowInRange && isInRange)
        {
            isInRange = false;
            Debug.Log($"[Crafter] {gameObject.name}: Player OUT of range (dist={dist:F1})");
            OnCrafterOutOfRange?.Invoke(this);
        }
    }

    private void OnInteract(InputAction.CallbackContext ctx)
    {
        Debug.Log($"[Crafter] {gameObject.name}: Interact fired — isInRange={isInRange}, CraftingUI.Instance={CraftingUI.Instance != null}");
        if (isInRange)
            CraftingUI.Instance.Open(crafterTier);
    }

    private void OnDestroy()
    {
        interactAction.action.performed -= OnInteract;
        if (isInRange) OnCrafterOutOfRange?.Invoke(this);
    }
}
