using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton that spawns ResourceCrack objects when the Gravity Pulse hits
/// a harvestable surface. Enforces a minimum distance between cracks to
/// prevent spam, and rolls the resource via the BiomeSpawnTable.
/// </summary>
public class CrackSpawner : MonoBehaviour
{
    public static CrackSpawner Instance { get; private set; }

    [Header("References")]
    [Tooltip("Per-biome resource weights and glow colors.")]
    [SerializeField] private BiomeSpawnTable spawnTable;

    [Tooltip("Prefab containing a ResourceCrack component. If null, an empty GameObject is built at runtime.")]
    [SerializeField] private GameObject crackPrefab;

    [Header("Layer Filter")]
    [Tooltip("Cracks only spawn on colliders in this layer mask. Set to your Harvestable layer.")]
    [SerializeField] private LayerMask harvestableMask;

    [Tooltip("If true, also accept any collider with a HarvestableSurface component, regardless of layer.")]
    [SerializeField] private bool acceptHarvestableComponent = true;

    [Header("Spacing")]
    [Tooltip("New cracks cannot spawn within this distance of an existing crack.")]
    [SerializeField] private float minCrackSpacing = 2f;

    [Header("Debug")]
    [SerializeField] private bool logSpawns = false;

    private readonly List<ResourceCrack> liveCracks = new List<ResourceCrack>(64);

    public BiomeSpawnTable SpawnTable => spawnTable;
    public float MinCrackSpacing => minCrackSpacing;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    public bool TrySpawn(Vector3 hitPoint, Vector3 hitNormal, Collider hitCollider)
    {
        if (hitCollider == null) return false;
        if (!IsHarvestable(hitCollider))
        {
            if (logSpawns) Debug.Log($"[CrackSpawner] Pulse hit non-harvestable collider {hitCollider.name}.");
            return false;
        }

        if (HasNearbyCrack(hitPoint))
        {
            if (logSpawns) Debug.Log($"[CrackSpawner] Spawn blocked — existing crack within {minCrackSpacing}m.");
            return false;
        }

        if (spawnTable == null)
        {
            Debug.LogWarning("[CrackSpawner] No BiomeSpawnTable assigned — cannot determine resource.");
            return false;
        }

        HarvestableSurface surface = hitCollider.GetComponentInParent<HarvestableSurface>();
        BiomeType biome = surface != null
            ? surface.ResolveBiome(hitPoint)
            : (BiomeManager.Instance != null ? BiomeManager.Instance.GetBiomeAt(hitPoint) : BiomeType.Biome1);

        if (!spawnTable.TryRoll(biome, out BiomeSpawnTable.ResourceWeight rolled))
        {
            if (logSpawns) Debug.Log($"[CrackSpawner] No resources defined for biome {biome}; no crack spawned.");
            return false;
        }

        ResourceCrack crack = SpawnCrackInstance();
        Vector3 normal = hitNormal.sqrMagnitude > 0.0001f ? hitNormal.normalized : Vector3.up;
        crack.transform.position = hitPoint + normal * crack.SurfaceLift;
        crack.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal);
        crack.Initialize(rolled.resource, rolled.glowColor, biome);
        liveCracks.Add(crack);

        if (logSpawns) Debug.Log($"[CrackSpawner] Spawned crack: biome={biome}, resource={rolled.resource.itemName}, color={rolled.glowColor}.");
        return true;
    }

    private bool IsHarvestable(Collider col)
    {
        bool layerMatches = ((1 << col.gameObject.layer) & harvestableMask.value) != 0;
        if (layerMatches) return true;
        if (acceptHarvestableComponent && col.GetComponentInParent<HarvestableSurface>() != null) return true;
        return false;
    }

    private bool HasNearbyCrack(Vector3 worldPos)
    {
        float sqr = minCrackSpacing * minCrackSpacing;
        for (int i = liveCracks.Count - 1; i >= 0; i--)
        {
            if (liveCracks[i] == null) { liveCracks.RemoveAt(i); continue; }
            if ((liveCracks[i].transform.position - worldPos).sqrMagnitude < sqr) return true;
        }
        return false;
    }

    private ResourceCrack SpawnCrackInstance()
    {
        GameObject go;
        if (crackPrefab != null)
        {
            go = Instantiate(crackPrefab);
        }
        else
        {
            go = new GameObject("ResourceCrack");
            go.AddComponent<ResourceCrack>();
        }
        go.name = "ResourceCrack";
        return go.GetComponent<ResourceCrack>() ?? go.AddComponent<ResourceCrack>();
    }

    /// <summary>Read-only view of all live cracks in the scene. Used by the save system.</summary>
    public IReadOnlyList<ResourceCrack> LiveCracks => liveCracks;

    /// <summary>
    /// Save-system entry point: spawn a crack at an exact pose with given resource/state.
    /// Bypasses biome/spacing/harvestable checks since we're restoring from a known-good save.
    /// </summary>
    public ResourceCrack SpawnFromSave(Vector3 position, Quaternion rotation, ItemData resource, Color glowColor, BiomeType biome, bool active, float regenRemaining)
    {
        ResourceCrack crack = SpawnCrackInstance();
        crack.transform.position = position;
        crack.transform.rotation = rotation;
        crack.RestoreFromSave(resource, glowColor, biome, active, regenRemaining);
        liveCracks.Add(crack);
        return crack;
    }

    /// <summary>Destroy every live crack and clear the registry. Used before applying saved state.</summary>
    public void DestroyAllLiveCracks()
    {
        for (int i = liveCracks.Count - 1; i >= 0; i--)
        {
            if (liveCracks[i] != null) Destroy(liveCracks[i].gameObject);
        }
        liveCracks.Clear();
    }
}
