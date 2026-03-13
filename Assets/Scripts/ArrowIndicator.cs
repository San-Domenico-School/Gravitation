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

    private void Update()
    {
        // Perform a raycast from the camera to detect where gravity would be applied.
        Camera cam = Camera.main;
        if (cam == null)
        {
            gameObject.SetActive(false);
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
            gameObject.SetActive(true);
        }
        else
        {
            // No hit: hide the arrow.
            gameObject.SetActive(false);
        }
    }
}

