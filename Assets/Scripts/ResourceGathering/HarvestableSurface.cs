using UnityEngine;

/// <summary>
/// Marker for terrain/rock that the Gravity Pulse can crack open.
/// Pulses on objects WITHOUT this component (or the matching layer) do nothing —
/// only harvestable terrain spawns cracks. Optionally overrides the biome so
/// hand-placed harvestables can yield specific resources regardless of position.
/// </summary>
public class HarvestableSurface : MonoBehaviour
{
    [Tooltip("If checked, cracks made on this surface use this biome instead of resolving via BiomeManager. Use for hand-placed mineral nodes.")]
    public bool overrideBiome = false;

    [Tooltip("Biome used when overrideBiome is true.")]
    public BiomeType biomeOverride = BiomeType.Biome1;

    public BiomeType ResolveBiome(Vector3 worldPos)
    {
        if (overrideBiome) return biomeOverride;
        if (BiomeManager.Instance != null) return BiomeManager.Instance.GetBiomeAt(worldPos);
        return BiomeType.Biome1;
    }
}
