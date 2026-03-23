using UnityEngine;
/// <summary>
/// Controls gravity for a Rigidbody manually.
/// </summary>
[RequireComponent(typeof(Rigidbody))]

public class GravityBody : MonoBehaviour
{
    [Header("Gravity Settings")]
    [Tooltip("Direction that gravity is applied in (will be normalized).")]
    public Vector3 gravityDirection = Vector3.down;

    [Tooltip("Strength of the gravity applied to this body.")]
    public float gravityStrength = 9.81f;

    [Tooltip("Whether this body is currently selected (project-specific usage).")]
    public bool isSelected;

    [Header("Rotation")]
    [Tooltip("How quickly the object rotates to align its local down with gravity.")]
    [SerializeField]
    private float rotationSpeed = 6f;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // We'll drive gravity manually.

        // The system expects gravity-controlled objects to be on a specific layer.
        int requiredLayer = LayerMask.NameToLayer("GravityObject");
        if (requiredLayer == -1)
        {
            Debug.LogWarning("Layer 'GravityObject' does not exist. Create it in the Tags & Layers settings.", gameObject);
        }
        else if (gameObject.layer != requiredLayer)
        {
            Debug.LogWarning($"{name} is not on layer 'GravityObject'. Gravity handling may be inconsistent.", gameObject);
        }

        GravityController.Instance?.RegisterBody(this);
    }

    private void OnDestroy()
    {
        GravityController.Instance?.RemoveBody(this);
    }

    private void FixedUpdate()
    {
        // Apply manual gravity.
        rb.AddForce(gravityDirection * gravityStrength, ForceMode.Acceleration);

        // Rotate so the local "down" aligns with gravityDirection.
        if (gravityDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.FromToRotation(-transform.up, gravityDirection) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    /// <summary>
    /// Updates the gravity direction and strength for this body.
    /// </summary>
    /// <param name="newDirection">New gravity direction (will be normalized).</param>
    /// <param name="strength">Gravity strength to use.</param>
    public void SetGravity(Vector3 newDirection, float strength)
    {
        gravityDirection = newDirection.normalized;
        gravityStrength = strength;
    }
}
