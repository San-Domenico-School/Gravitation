using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Type of object detected by the Scout Drone's scan.
/// </summary>
public enum DroneTargetType
{
    Enemy,
    Resource,
    POI
}

/// <summary>
/// A single scan result passed from ScoutDrone to MinimapUI.
/// </summary>
public struct DroneTarget
{
    public Vector3 worldPosition;
    public DroneTargetType type;
}

/// <summary>
/// Scout Drone — Tier 1.
///
/// When active (enabled by ScoutDroneEquipSystem):
///   • A placeholder sphere orbits above the player's head.
///   • Every <see cref="scanInterval"/> seconds it fires a Physics.OverlapSphere
///     and tags all hits as Enemy / Resource / POI.
///   • Results are forwarded to MinimapUI for rendering on the radar.
///
/// Swap the sphere mesh for a real drone model later by replacing the child
/// renderer or assigning a custom mesh to the MeshFilter on this GameObject.
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class ScoutDrone : MonoBehaviour
{
    [Header("Orbit")]
    [Tooltip("Horizontal distance from the player.")]
    [SerializeField] private float orbitRadius = 1.5f;
    [Tooltip("Height above the player's pivot.")]
    [SerializeField] private float orbitHeight = 1.8f;
    [Tooltip("Degrees per second of orbit rotation.")]
    [SerializeField] private float orbitSpeed = 60f;
    [Tooltip("How snappily the drone lerps to its target orbit position.")]
    [SerializeField] private float followSharpness = 5f;

    [Header("Scan")]
    [Tooltip("Scan radius in world units. Should match MinimapUI.worldRadius.")]
    [SerializeField] private float scanRadius = 75f;
    [Tooltip("Seconds between scans.")]
    [SerializeField] private float scanInterval = 2f;

    private Transform playerTransform;
    private float orbitAngle;
    private float scanTimer;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        // Build the sphere placeholder mesh at runtime.
        var filter = GetComponent<MeshFilter>();
        if (filter.sharedMesh == null)
            filter.sharedMesh = BuildSphereMesh();

        // Teal-ish emission so it reads as sci-fi.
        var mr = GetComponent<MeshRenderer>();
        if (mr.sharedMaterial == null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.2f, 0.8f, 0.9f);
            mr.sharedMaterial = mat;
        }
    }

    private void OnEnable()
    {
        var player = GameObject.FindWithTag("Player");
        playerTransform = player != null ? player.transform : null;
        scanTimer = 0f;
        // Immediate first scan so the minimap populates right away.
        DoScan();
    }

    private void OnDisable()
    {
        // Clear minimap when drone is unequipped.
        if (MinimapUI.Instance != null)
            MinimapUI.Instance.UpdateTargets(new List<DroneTarget>());
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // --- Orbit ---
        orbitAngle += orbitSpeed * Time.deltaTime;
        float rad = orbitAngle * Mathf.Deg2Rad;
        Vector3 targetPos = playerTransform.position
            + new Vector3(Mathf.Cos(rad) * orbitRadius, orbitHeight, Mathf.Sin(rad) * orbitRadius);
        transform.position = Vector3.Lerp(transform.position, targetPos, followSharpness * Time.deltaTime);

        // Face direction of travel (looks nicer with a real mesh).
        Vector3 vel = targetPos - transform.position;
        if (vel.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(vel.normalized, Vector3.up);

        // --- Scan timer ---
        scanTimer += Time.deltaTime;
        if (scanTimer >= scanInterval)
        {
            scanTimer = 0f;
            DoScan();
        }
    }

    // -------------------------------------------------------------------------
    // Scan
    // -------------------------------------------------------------------------

    private void DoScan()
    {
        if (playerTransform == null || MinimapUI.Instance == null) return;

        var results = new List<DroneTarget>();
        var hits = Physics.OverlapSphere(playerTransform.position, scanRadius);

        foreach (var col in hits)
        {
            if (col.CompareTag("Enemy"))
                results.Add(new DroneTarget { worldPosition = col.transform.position, type = DroneTargetType.Enemy });
            else if (col.CompareTag("WorldItem"))
                results.Add(new DroneTarget { worldPosition = col.transform.position, type = DroneTargetType.Resource });
            else if (col.CompareTag("POI"))
                results.Add(new DroneTarget { worldPosition = col.transform.position, type = DroneTargetType.POI });
        }

        MinimapUI.Instance.UpdateTargets(results);
    }

    // -------------------------------------------------------------------------
    // Placeholder mesh (Unity sphere equivalent via CreatePrimitive is editor-only
    // when used at runtime in a build, so we use a simple low-poly sphere shape).
    // -------------------------------------------------------------------------

    private static Mesh BuildSphereMesh()
    {
        // Unity's built-in sphere primitive is safe to create at runtime via
        // GameObject.CreatePrimitive; we just steal the mesh.
        var tmp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var mesh = tmp.GetComponent<MeshFilter>().sharedMesh;
        Destroy(tmp);
        return mesh;
    }
}
