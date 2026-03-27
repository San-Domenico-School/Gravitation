using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class PortalInteractable : MonoBehaviour
{
    [SerializeField]
    private ProceduralWorldSession worldSession;

    [SerializeField]
    private Vector3 uiOffset = new Vector3(0f, 2f, 0f);

    private bool playerInside;

    private void Awake()
    {
        EnsureSetup();
    }

    private void Update()
    {
        EnsureSetup();

        if (!playerInside || worldSession == null || WorldInputGate.IsUIOpen)
            return;

        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            worldSession.OpenPortalUi();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        EnsureSetup();

        if (worldSession != null && worldSession.IsPlayerCollider(other))
        {
            playerInside = true;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        EnsureSetup();

        if (worldSession == null || !worldSession.IsPlayerCollider(other))
            return;

        playerInside = true;

        if (!WorldInputGate.IsUIOpen && Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            worldSession.OpenPortalUi();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        EnsureSetup();

        if (worldSession != null && worldSession.IsPlayerCollider(other))
            playerInside = false;
    }

    private void EnsureSetup()
    {
        Collider collider = GetComponent<Collider>();
        if (collider != null)
            collider.isTrigger = true;

        if (worldSession == null)
            worldSession = FindFirstObjectByType<ProceduralWorldSession>();
    }
}
