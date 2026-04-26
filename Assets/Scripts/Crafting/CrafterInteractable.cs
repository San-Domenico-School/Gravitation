using UnityEngine;
using UnityEngine.InputSystem;

public class CrafterInteractable : MonoBehaviour
{
    [SerializeField] public int tier = 1;
    [SerializeField] private float interactRadius = 3f;
    [SerializeField] private InputActionReference interactAction;

    private Transform playerTransform;
    private bool playerInRange;

    private void Start()
    {
        var player = GameObject.FindWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    private void OnEnable()
    {
        if (interactAction != null)
            interactAction.action.performed += OnInteract;
    }

    private void OnDisable()
    {
        if (interactAction != null)
            interactAction.action.performed -= OnInteract;
        playerInRange = false;
    }

    private void Update()
    {
        if (playerTransform == null) return;

        bool inRange = Vector3.Distance(transform.position, playerTransform.position) <= interactRadius;

        if (inRange != playerInRange)
        {
            playerInRange = inRange;
            if (!inRange && CraftingUI.Instance != null && CraftingUI.Instance.IsOpenForCrafter(this))
                CraftingUI.Instance.Close();
        }

        if (playerInRange && !(CraftingUI.Instance != null && CraftingUI.Instance.IsOpen))
            PickupPromptUI.ShowMessage($"Press E to use Tier {tier} Fabricator", 0.2f);
    }

    private void OnInteract(InputAction.CallbackContext ctx)
    {
        if (!playerInRange) return;
        if (CraftingUI.Instance == null) return;

        if (CraftingUI.Instance.IsOpenForCrafter(this))
            CraftingUI.Instance.Close();
        else if (!CraftingUI.Instance.IsOpen)
            CraftingUI.Instance.Open(this);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
