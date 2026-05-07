using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton that resolves "what biome is at this world position?".
/// Currently backed by hand-placed BiomeZone trigger volumes — swap in a
/// world-gen-aware provider later by setting BiomeManager.Provider.
/// </summary>
public class BiomeManager : MonoBehaviour
{
    public interface IBiomeProvider
    {
        BiomeType GetBiomeAt(Vector3 worldPos);
    }

    public static BiomeManager Instance { get; private set; }

    [Tooltip("Returned when no zone or provider matches.")]
    [SerializeField] private BiomeType defaultBiome = BiomeType.Biome1;

    [Tooltip("Tag used to find the player for CurrentBiome tracking.")]
    [SerializeField] private string playerTag = "Player";

    private static readonly List<BiomeZone> zones = new List<BiomeZone>(8);
    private Transform player;
    private BiomeType currentBiome;

    public static IBiomeProvider Provider { get; set; }

    public BiomeType DefaultBiome => defaultBiome;
    public BiomeType CurrentBiome => currentBiome;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        currentBiome = defaultBiome;
    }

    private void Start()
    {
        var p = GameObject.FindGameObjectWithTag(playerTag);
        if (p != null) player = p.transform;
    }

    private void Update()
    {
        if (player == null) return;
        currentBiome = GetBiomeAt(player.position);
    }

    public BiomeType GetBiomeAt(Vector3 worldPos)
    {
        if (Provider != null)
        {
            BiomeType fromProvider = Provider.GetBiomeAt(worldPos);
            if (fromProvider != BiomeType.None) return fromProvider;
        }

        BiomeZone best = null;
        float bestVolume = float.MaxValue;
        for (int i = 0; i < zones.Count; i++)
        {
            BiomeZone z = zones[i];
            if (z == null) continue;
            if (!z.ContainsPoint(worldPos)) continue;

            float v = z.BoundsVolume();
            if (v < bestVolume)
            {
                bestVolume = v;
                best = z;
            }
        }
        return best != null ? best.biome : defaultBiome;
    }

    public static void RegisterZone(BiomeZone zone)
    {
        if (zone == null) return;
        if (!zones.Contains(zone)) zones.Add(zone);
    }

    public static void UnregisterZone(BiomeZone zone)
    {
        zones.Remove(zone);
    }
}
