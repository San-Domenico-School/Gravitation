using System;
using UnityEngine;

[CreateAssetMenu(fileName = "BiomeSpawnTable", menuName = "Gravitas/Resource Gathering/Biome Spawn Table")]
public class BiomeSpawnTable : ScriptableObject
{
    [Serializable]
    public struct ResourceWeight
    {
        public ItemData resource;
        [Tooltip("Relative weight in the roll. 0 = never spawn.")]
        public float weight;
        [ColorUsage(true, true)]
        [Tooltip("Color the crack glows when this resource is rolled. HDR — high intensity for bloom.")]
        public Color glowColor;
    }

    [Serializable]
    public class BiomeEntry
    {
        public BiomeType biome;
        public ResourceWeight[] resources;
    }

    [Tooltip("One entry per biome. Each entry holds weighted resource rolls for cracks and natural outcrops.")]
    public BiomeEntry[] biomes;

    public bool TryRoll(BiomeType biome, out ResourceWeight result)
    {
        result = default;
        BiomeEntry entry = GetEntry(biome);
        if (entry == null || entry.resources == null) return false;

        float total = 0f;
        for (int i = 0; i < entry.resources.Length; i++)
        {
            if (entry.resources[i].resource != null && entry.resources[i].weight > 0f)
                total += entry.resources[i].weight;
        }
        if (total <= 0f) return false;

        float roll = UnityEngine.Random.value * total;
        float acc = 0f;
        for (int i = 0; i < entry.resources.Length; i++)
        {
            ResourceWeight rw = entry.resources[i];
            if (rw.resource == null || rw.weight <= 0f) continue;
            acc += rw.weight;
            if (roll <= acc) { result = rw; return true; }
        }
        return false;
    }

    public BiomeEntry GetEntry(BiomeType biome)
    {
        if (biomes == null) return null;
        for (int i = 0; i < biomes.Length; i++)
            if (biomes[i] != null && biomes[i].biome == biome) return biomes[i];
        return null;
    }
}
