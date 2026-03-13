using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player movement and jumping relative to the current gravity direction.
/// The player must have a GravityBody component for gravity handling.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(GravityBody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Input Actions")]
    [Tooltip("Movement input (WASD or left stick).")]
    public InputActionReference Move;

    [Tooltip("Look input (mouse or right stick).")]
    public InputActionReference Look;

    [Tooltip("Jump input (space or button).")]
    public InputActionReference Jump;

    [Header("Movement Settings")]
    [Tooltip("Speed of movement.")]
    public float moveSpeed = 10f;

    [Tooltip("Force applied when jumping.")]
    public float jumpForce = 5f;

    [Tooltip("Speed of rotation to align with gravity.")]
    public float rotationSpeed = 6f;

    [Tooltip("Sensitivity for looking around.")]
    public float lookSpeed = 2f;

    private Rigidbody rb;
    private GravityBody gravityBody;
    private Camera cam;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        gravityBody = GetComponent<GravityBody>();
        cam = Camera.main;

        if (cam == null)
        {
            Debug.LogError("No main camera found. PlayerMovement requires a camera.", this);
        }

        // Lock and hide the cursor for FPS-style controls.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        if (Move != null) Move.action.Enable();
        if (Look != null) Look.action.Enable();
        if (Jump != null) Jump.action.Enable();
    }

    private void OnDisable()
    {
        if (Move != null) Move.action.Disable();
        if (Look != null) Look.action.Disable();
        if (Jump != null) Jump.action.Disable();
    }

    private void Update()
    {
        // Handle look input for camera rotation.
        if (Look != null && cam != null)
        {
            Vector2 lookInput = Look.action.ReadValue<Vector2>();
            if (lookInput != Vector2.zero)
            {
                // Rotate camera around world up for yaw (horizontal look).
                cam.transform.Rotate(Vector3.up, lookInput.x * lookSpeed * Time.deltaTime, Space.World);
                // Rotate camera around its local right for pitch (vertical look).
                cam.transform.Rotate(cam.transform.right, -lookInput.y * lookSpeed * Time.deltaTime, Space.Self);
            }
        }
    }

    private void FixedUpdate()
    {
        if (gravityBody == null || cam == null)
            return;

        Vector3 gravityDir = gravityBody.gravityDirection;

        // Compute movement directions projected onto the plane perpendicular to gravity.
        Vector3 forward = Vector3.ProjectOnPlane(cam.transform.forward, gravityDir).normalized;
        Vector3 right = Vector3.ProjectOnPlane(cam.transform.right, gravityDir).normalized;

        // Get movement input.
        Vector2 moveInput = Move != null ? Move.action.ReadValue<Vector2>() : Vector2.zero;
        Vector3 moveDir = forward * moveInput.y + right * moveInput.x;

        // Apply movement force.
        if (moveDir != Vector3.zero)
        {
            rb.AddForce(moveDir * moveSpeed, ForceMode.Acceleration);
        }

        // Handle jumping.
        if (Jump != null && Jump.action.triggered)
        {
            rb.AddForce(-gravityDir * jumpForce, ForceMode.Impulse);
        }

        // Rotate the player to align local down with gravity direction.
        if (gravityDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.FromToRotation(-transform.up, gravityDir) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }
}
