using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles camera rotation (both pitch and yaw) in a gravity-aware manner.
/// This ensures camera controls remain correct and responsive even when gravity changes the player's orientation.
/// The camera is a child of the player and independently manages look input relative to the current gravity direction.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Look input (mouse or right stick).")]
    public InputActionReference Look;

    [Header("Settings")]
    [Tooltip("Sensitivity for look input.")]
    public float lookSpeed = 2f;

    [Tooltip("Minimum pitch angle (looking down).")]
    public float minPitch = -90f;

    [Tooltip("Maximum pitch angle (looking up).")]
    public float maxPitch = 90f;

    private float pitchAngle = 0f;
    private float yawAngle = 0f;

    private void OnEnable()
    {
        if (Look != null)
        {
            Look.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (Look != null)
        {
            Look.action.Disable();
        }
    }

    private void Update()
    {
        if (Look != null)
        {
            Vector2 lookInput = Look.action.ReadValue<Vector2>();
            if (lookInput != Vector2.zero)
            {
                // Update yaw (horizontal look) - rotate around local Y axis
                yawAngle += lookInput.x * lookSpeed * Time.deltaTime;

                // Update pitch (vertical look) - rotate around local X axis
                pitchAngle += -lookInput.y * lookSpeed * Time.deltaTime;
                pitchAngle = Mathf.Clamp(pitchAngle, minPitch, maxPitch);
            }
        }

        // Apply rotation as local Euler angles
        // This automatically works with the player's rotating coordinate system
        // The parent player's rotation is already handled by GravityBody and gravity system
        transform.localEulerAngles = new Vector3(pitchAngle, yawAngle, 0f);
    }
}
