using UnityEngine;

/// <summary>
/// Place a Collider on this object (set isTrigger = true) and assign a biome.
/// Any world point inside the collider's bounds is treated as that biome.
/// Multiple overlapping zones: smaller bounds wins (most-specific overrides).
/// Stand-in until biomes are baked into the procedural world generator —
/// at that point this component is replaced by a chunk-aware biome lookup.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BiomeZone : MonoBehaviour
{
    public BiomeType biome = BiomeType.Biome1;

    private Collider zoneCollider;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnEnable()
    {
        zoneCollider = GetComponent<Collider>();
        BiomeManager.RegisterZone(this);
    }

    private void OnDisable()
    {
        BiomeManager.UnregisterZone(this);
    }

    public bool ContainsPoint(Vector3 worldPos)
    {
        if (zoneCollider == null) return false;
        return zoneCollider.bounds.Contains(worldPos);
    }

    public float BoundsVolume()
    {
        if (zoneCollider == null) return float.MaxValue;
        Vector3 size = zoneCollider.bounds.size;
        return size.x * size.y * size.z;
    }

    private void OnDrawGizmosSelected()
    {
        var col = GetComponent<Collider>();
        if (col == null) return;
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
        Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 1f);
        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }
}
