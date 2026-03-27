using UnityEngine;

/// <summary>
/// Visual indicator that shows a preview of the gravity direction the player is aiming at.
/// </summary>
public class ArrowIndicator : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Maximum distance to raycast for gravity preview.")]
    [SerializeField]
    private float maxRaycastDistance = 100f;

    [Tooltip("Reference to the GravityGun to check if we're in placement mode.")]
    [SerializeField]
    private GravityGun gravityGun;

    private void Update()
    {
        if (WorldInputGate.IsUIOpen)
            return;

        // Only update arrow when GravityGun is in GravityPlacement mode
        if (gravityGun == null || !gameObject.activeSelf)
        {
            return;
        }

        // Perform a raycast from the camera to detect where gravity would be applied.
        Camera cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxRaycastDistance))
        {
            // Hit something: position the arrow at the hit point and rotate it to show gravity direction.
            transform.position = hit.point;
            Vector3 gravityDirection = -hit.normal;
            // Assuming the arrow is a cone pointing upwards, rotate so its local up points in gravity direction.
            transform.rotation = Quaternion.FromToRotation(Vector3.up, gravityDirection);
        }
    }
}
