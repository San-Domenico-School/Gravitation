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
        interactAction.action.performed += OnInteract;
        interactAction.action.Enable();
    }

    private void Start()
    {
        var player = GameObject.FindWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    private void Update()
    {
        if (playerTransform == null) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        bool nowInRange = dist <= interactionRange;

        if (nowInRange && !isInRange)
        {
            isInRange = true;
            OnCrafterInRange?.Invoke(this);
        }
        else if (!nowInRange && isInRange)
        {
            isInRange = false;
            OnCrafterOutOfRange?.Invoke(this);
        }
    }

    private void OnInteract(InputAction.CallbackContext ctx)
    {
        if (isInRange)
            CraftingUI.Instance.Open(crafterTier);
    }

    private void OnDestroy()
    {
        interactAction.action.performed -= OnInteract;
        if (isInRange) OnCrafterOutOfRange?.Invoke(this);
    }
}
