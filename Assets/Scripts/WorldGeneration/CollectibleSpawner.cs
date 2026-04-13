using UnityEngine;

/// <summary>
/// Spawns collectibles (health pickups) throughout the world.
/// Ensures collectibles are placed on stable positions (platforms, ground).
/// </summary>
public class CollectibleSpawner : MonoBehaviour
{
    [Tooltip("Prefab to instantiate for health pickups (should have HealthPickup component).")]
    [SerializeField]
    private GameObject healthPickupPrefab;

    /// <summary>
    /// Spawns collectibles in the world.
    /// </summary>
    /// <param name="root">Parent transform for generated objects.</param>
    /// <param name="config">World configuration.</param>
    /// <param name="seed">Seed string for reproducibility.</param>
    /// <returns>True if spawning succeeded.</returns>
    public bool Spawn(Transform root, WorldConfig config, string seed)
    {
        if (config == null)
        {
            Debug.LogError("WorldConfig is null in CollectibleSpawner.");
            return false;
        }

        // TODO: Implement collectible spawning
        // 1. Generate random positions within world bounds
        // 2. For each position, raycast down to find stable surface (platform)
        // 3. If surface found, instantiate health pickup prefab there
        // 4. Ensure pickups don't spawn inside walls (use Physics.OverlapBox)

        Debug.Log($"[CollectibleSpawner] Stub: Would spawn {config.healthPickupCount} health pickups");
        return true;
    }
}
