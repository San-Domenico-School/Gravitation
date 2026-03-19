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

    [Header("Ground Detection")]
    [Tooltip("Distance below the player to check for ground.")]
    public float groundCheckDistance = 0.5f;

    private Rigidbody rb;
    private GravityBody gravityBody;
    private Camera cam;
    private float pitchAngle = 0f;
    private bool isGrounded = false;
    private int groundLayerMask;
    private bool jumpRequested = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        gravityBody = GetComponent<GravityBody>();
        cam = Camera.main;

        if (cam == null)
        {
            Debug.LogError("No main camera found. PlayerMovement requires a camera.", this);
        }
        else
        {
            // Make camera a child of the player for proper orientation
            cam.transform.SetParent(transform);
            cam.transform.localPosition = Vector3.zero;
            cam.transform.localRotation = Quaternion.identity;
        }

        // Get the Ground layer mask
        groundLayerMask = LayerMask.GetMask("Ground");
        if (groundLayerMask == 0)
        {
            Debug.LogWarning("Layer 'Ground' not found. Ground detection will not work.", this);
        }

        // Lock and hide the cursor for FPS-style controls.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        if (Move != null)
        {
            Move.action.Enable();
    //        Debug.Log($"Move action enabled: {Move.action.enabled}");
        }
        if (Look != null)
        {
            Look.action.Enable();
     //       Debug.Log($"Look action enabled: {Look.action.enabled}");
        }
        if (Jump != null)
        {
            Jump.action.Enable();
     //       Debug.Log($"Jump action enabled: {Jump.action.enabled}");
        }
    }

    private void OnDisable()
    {
        if (Move != null) Move.action.Disable();
        if (Look != null) Look.action.Disable();
        if (Jump != null) Jump.action.Disable();
    }

    private void Update()
    {
        // Capture jump input (do this in Update for consistent frame detection)
        if (Jump != null && Jump.action.triggered)
        {
            jumpRequested = true;
            Debug.Log("Jump input captured");
        }

        // Handle look input for camera rotation.
        if (Look != null && cam != null)
        {
            Vector2 lookInput = Look.action.ReadValue<Vector2>();
            if (lookInput != Vector2.zero)
            {
                // Rotate player around its up (aligned with gravity) for yaw
                transform.Rotate(transform.up, lookInput.x * lookSpeed * Time.deltaTime, Space.Self);

                // Rotate camera for pitch, clamped to prevent flipping
                pitchAngle += -lookInput.y * lookSpeed * Time.deltaTime;
                pitchAngle = Mathf.Clamp(pitchAngle, -90f, 90f);
                cam.transform.localEulerAngles = new Vector3(pitchAngle, 0f, 0f);
            }
        }
    }

    private void FixedUpdate()
    {
        if (gravityBody == null || cam == null)
            return;

        Vector3 gravityDir = gravityBody.gravityDirection;

        // Check if grounded
        CheckGrounded(gravityDir);

        // Compute movement directions projected onto the plane perpendicular to gravity.
        Vector3 forward = Vector3.ProjectOnPlane(cam.transform.forward, gravityDir).normalized;
        Vector3 right = Vector3.ProjectOnPlane(cam.transform.right, gravityDir).normalized;

        // Get movement input.
        Vector2 moveInput = Vector2.zero;
        if (Move != null && Move.action.enabled)
        {
            try
            {
                moveInput = Move.action.ReadValue<Vector2>();
                if (moveInput != Vector2.zero)
                {
        //            Debug.Log($"Movement input: {moveInput}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error reading movement input: {ex.Message}");
            }
        }
        else if (Move != null)
        {
            Debug.LogWarning("Move action is not enabled or null");
        }

        Vector3 moveDir = forward * moveInput.y + right * moveInput.x;

        // Apply movement force.
        if (moveDir != Vector3.zero)
        {
            rb.AddForce(moveDir * moveSpeed, ForceMode.Acceleration);
        }

        // Handle jumping - process jump request if grounded.
        if (jumpRequested)
        {
            Debug.Log($"Processing jump request. Grounded: {isGrounded}");
            if (isGrounded)
            {
                rb.AddForce(-gravityDir * jumpForce, ForceMode.Impulse);
                isGrounded = false; // Prevent double jump
                Debug.Log("✓ Jump executed!");
            }
            else
            {
                Debug.Log("✗ Jump requested but not grounded");
            }
            jumpRequested = false; // Clear the request regardless
        }

        // Rotate the player to align local down with gravity direction.
        if (gravityDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.FromToRotation(-transform.up, gravityDir) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    private void CheckGrounded(Vector3 gravityDir)
    {
        // Raycast starting point - offset slightly back to avoid starting inside collider
        Vector3 rayStart = transform.position + (gravityDir * 0.1f);
        Vector3 rayEnd = rayStart + (gravityDir * groundCheckDistance);

        // Raycast in the direction of gravity to check for ground
        bool hit = Physics.Raycast(
            rayStart,
            gravityDir,
            out RaycastHit hitInfo,
            groundCheckDistance,
            groundLayerMask
        );

        // Also try without layer mask to see if anything is being hit
        bool hitAny = Physics.Raycast(
            rayStart,
            gravityDir,
            out RaycastHit hitInfoAny,
            groundCheckDistance
        );

        isGrounded = hit;

        // Debug visualization - show the full raycast line
        Debug.DrawLine(rayStart, rayEnd, isGrounded ? Color.green : Color.red, 0f, false);

        if (hit)
        {
            Debug.Log($"✓ GROUNDED! Hit: {hitInfo.collider.gameObject.name}, Distance: {hitInfo.distance}");
        }
        else if (hitAny)
        {
            Debug.LogError($"✗ Hit something but NOT on Ground layer! Hit: {hitInfoAny.collider.gameObject.name} (Layer: {LayerMask.LayerToName(hitInfoAny.collider.gameObject.layer)}) at distance {hitInfoAny.distance}");
        }
        else
        {
            Debug.LogWarning($"✗ No hit at all. Start: {rayStart}, End: {rayEnd}, Direction: {gravityDir}, Distance: {groundCheckDistance}");
        }
    }
}
