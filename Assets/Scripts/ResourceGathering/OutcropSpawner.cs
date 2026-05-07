using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Subnautica-style natural outcrop scatter. Picks random points inside its
/// volume, raycasts down to a stable surface, and instantiates the item's
/// worldPrefab there. Resource selection per spawn is biome-weighted via
/// the supplied BiomeSpawnTable, just like the crack system.
///
/// Place one OutcropSpawner per biome region (or per scene), set its volume,
/// and the spawn count. Outcrops are loose, limited supply — they do NOT
/// regenerate. Use cracks for the main, renewable resource path.
/// </summary>
public class OutcropSpawner : MonoBehaviour
{
    [Header("Source Tables")]
    [SerializeField] private BiomeSpawnTable spawnTable;

    [Tooltip("Biome used to roll outcrop resources. Set per spawner.")]
    [SerializeField] private BiomeType biome = BiomeType.Biome1;

    [Header("Volume")]
    [Tooltip("Half-extents of the spawn box centered on this transform.")]
    [SerializeField] private Vector3 halfExtents = new Vector3(40f, 20f, 40f);

    [Header("Scatter")]
    [Tooltip("Number of outcrops to attempt to place when Spawn() is called.")]
    [SerializeField] private int outcropCount = 30;

    [Tooltip("Maximum random attempts per outcrop before giving up.")]
    [SerializeField] private int maxAttemptsPerOutcrop = 8;

    [Tooltip("Layer mask used to raycast down for surfaces. Should include ground/platforms.")]
    [SerializeField] private LayerMask surfaceLayerMask = ~0;

    [Tooltip("Maximum downward raycast distance to find a surface from a sampled point.")]
    [SerializeField] private float maxRayDistance = 200f;

    [Tooltip("Reject surfaces steeper than this angle (degrees).")]
    [Range(0f, 90f)] [SerializeField] private float maxSlopeAngle = 45f;

    [Tooltip("Vertical offset above the surface where the prefab is placed.")]
    [SerializeField] private float surfaceLift = 0.05f;

    [Tooltip("Minimum distance between any two outcrops.")]
    [SerializeField] private float minSpacing = 2.5f;

    [Header("Behavior")]
    [Tooltip("Spawn on Start automatically.")]
    [SerializeField] private bool spawnOnStart = true;

    [Tooltip("Random seed for reproducible scatters. 0 = use Time.")]
    [SerializeField] private int seed = 0;

    [Tooltip("Parent transform for spawned outcrops. Defaults to this transform.")]
    [SerializeField] private Transform spawnParent;

    private readonly List<Vector3> placedPositions = new List<Vector3>(64);

    private void Start()
    {
        if (spawnOnStart) Spawn();
    }

    public void Spawn()
    {
        if (spawnTable == null) { Debug.LogWarning("[OutcropSpawner] No BiomeSpawnTable assigned."); return; }

        Random.State prevState = Random.state;
        if (seed != 0) Random.InitState(seed);

        if (spawnParent == null) spawnParent = transform;
        placedPositions.Clear();

        int placed = 0;
        for (int i = 0; i < outcropCount; i++)
        {
            if (TryPlaceOne()) placed++;
        }

        Debug.Log($"[OutcropSpawner] biome={biome} placed={placed}/{outcropCount}.");

        if (seed != 0) Random.state = prevState;
    }

    private bool TryPlaceOne()
    {
        for (int attempt = 0; attempt < maxAttemptsPerOutcrop; attempt++)
        {
            Vector3 sample = transform.position + new Vector3(
                Random.Range(-halfExtents.x, halfExtents.x),
                Random.Range(-halfExtents.y, halfExtents.y) + halfExtents.y, // sample biased toward upper half so down-cast works
                Random.Range(-halfExtents.z, halfExtents.z));

            if (!Physics.Raycast(sample, Vector3.down, out RaycastHit hit, maxRayDistance, surfaceLayerMask, QueryTriggerInteraction.Ignore))
                continue;

            float slope = Vector3.Angle(hit.normal, Vector3.up);
            if (slope > maxSlopeAngle) continue;

            Vector3 placement = hit.point + Vector3.up * surfaceLift;

            if (TooClose(placement)) continue;

            if (!spawnTable.TryRoll(biome, out BiomeSpawnTable.ResourceWeight rolled)) return false;
            if (rolled.resource == null) return false;

            GameObject prefab = rolled.resource.worldPrefab;
            if (prefab == null)
            {
                Debug.LogWarning($"[OutcropSpawner] {rolled.resource.itemName} has no worldPrefab — assign one on the ItemData asset.");
                return false;
            }

            GameObject go = Instantiate(prefab, placement, Quaternion.FromToRotation(Vector3.up, hit.normal), spawnParent);
            go.name = $"Outcrop_{rolled.resource.itemName}";
            EnsureWorldItem(go, rolled.resource);
            placedPositions.Add(placement);
            return true;
        }
        return false;
    }

    private static void EnsureWorldItem(GameObject go, ItemData data)
    {
        var wi = go.GetComponent<WorldItem>();
        if (wi == null) wi = go.AddComponent<WorldItem>();
        if (wi.itemData == null) wi.itemData = data;
    }

    private bool TooClose(Vector3 worldPos)
    {
        float sqr = minSpacing * minSpacing;
        for (int i = 0; i < placedPositions.Count; i++)
            if ((placedPositions[i] - worldPos).sqrMagnitude < sqr) return true;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.2f);
        Gizmos.DrawCube(transform.position, halfExtents * 2f);
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 1f);
        Gizmos.DrawWireCube(transform.position, halfExtents * 2f);
    }
}
