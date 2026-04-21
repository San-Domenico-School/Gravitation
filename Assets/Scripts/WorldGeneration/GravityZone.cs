using UnityEngine;

/// <summary>
/// Represents a zone of altered gravity.
/// Bodies inside this zone experience the zone's gravity instead of the default world gravity.
/// Uses a sphere collider to define the zone's region.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class GravityZone : MonoBehaviour
{
    [Header("Gravity")]
    [Tooltip("Direction of gravity inside this zone.")]
    public Vector3 gravityDirection = Vector3.down;

    [Tooltip("Strength of gravity inside this zone.")]
    public float gravityStrength = 9.81f;

    [Header("Falloff")]
    [Tooltip("Whether gravity influence fades at the zone's edges.")]
    public bool useFalloff = true;

    [Tooltip("Falloff mode: 'Linear' fades linearly from center to edge, 'Smooth' uses smoothstep.")]
    public FalloffMode falloffMode = FalloffMode.Smooth;

    private SphereCollider zoneCollider;
    private float radius;

    /// <summary>
    /// Falloff calculation mode.
    /// </summary>
    public enum FalloffMode
    {
        Linear,
        Smooth
    }

    private void Awake()
    {
        zoneCollider = GetComponent<SphereCollider>();
        zoneCollider.isTrigger = true;
        radius = zoneCollider.radius;

        // Register with the manager.
        GravityZoneManager.Instance?.RegisterZone(this);
    }

    private void OnDestroy()
    {
        GravityZoneManager.Instance?.UnregisterZone(this);
    }

    /// <summary>
    /// Calculates the influence of this zone at a given position.
    /// Returns 0 if position is outside the zone, 1 if at the center, or a falloff value between 0-1.
    /// </summary>
    public float GetInfluenceAtPosition(Vector3 position)
    {
        Vector3 zoneCenter = transform.position;
        float distance = Vector3.Distance(position, zoneCenter);

        // Outside the zone
        if (distance > radius)
            return 0f;

        if (!useFalloff)
            return 1f;

        // Calculate normalized distance (0 at center, 1 at edge)
        float normalizedDistance = distance / radius;

        // Apply falloff
        if (falloffMode == FalloffMode.Linear)
        {
            return 1f - normalizedDistance;
        }
        else // Smooth
        {
            // Smoothstep falloff: smooth curve from 1 to 0
            return Mathf.SmoothStep(1f, 0f, normalizedDistance);
        }
    }

    /// <summary>
    /// Checks if a position is inside this zone.
    /// </summary>
    public bool IsPositionInZone(Vector3 position)
    {
        return GetInfluenceAtPosition(position) > 0f;
    }

    /// <summary>
    /// Gets the effective gravity direction and strength at a position, accounting for falloff.
    /// </summary>
    public void GetGravityAtPosition(Vector3 position, out Vector3 direction, out float strength)
    {
        float influence = GetInfluenceAtPosition(position);
        
        direction = gravityDirection.normalized;
        strength = gravityStrength * influence;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Visualize the gravity zone in the editor.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (zoneCollider == null)
        {
            zoneCollider = GetComponent<SphereCollider>();
            if (zoneCollider == null)
                return;
        }

        // Draw zone sphere
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        Gizmos.DrawWireSphere(transform.position, zoneCollider.radius);

        // Draw gravity direction arrow
        Gizmos.color = Color.cyan;
        Vector3 arrowStart = transform.position;
        Vector3 arrowEnd = arrowStart + gravityDirection.normalized * 5f;
        Gizmos.DrawLine(arrowStart, arrowEnd);

        // Draw arrow head
        Vector3 arrowHeadSize = Vector3.one * 0.5f;
        Gizmos.DrawLine(arrowEnd, arrowEnd - (gravityDirection.normalized * arrowHeadSize.z) + Vector3.right * arrowHeadSize.x);
        Gizmos.DrawLine(arrowEnd, arrowEnd - (gravityDirection.normalized * arrowHeadSize.z) - Vector3.right * arrowHeadSize.x);
    }
#endif
}
