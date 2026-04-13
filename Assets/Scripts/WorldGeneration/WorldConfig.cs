using UnityEngine;

/// <summary>
/// Configuration for a world type (Stone, Wood, Iron).
/// Stores theme settings, generation parameters, and difficulty-independent properties.
/// </summary>
[CreateAssetMenu(fileName = "WorldConfig_", menuName = "Gravitation/World Config", order = 1)]
public class WorldConfig : ScriptableObject
{
    [Header("World Identity")]
    [Tooltip("Name of the world type (e.g., 'stone', 'wood', 'iron').")]
    public string worldTypeName = "stone";

    [Header("Gravity")]
    [Tooltip("Default gravity strength for this world (e.g., 9.81 for Earth-like).")]
    public float defaultGravityStrength = 9.81f;

    [Tooltip("Multiplier for gravity anomaly strength in this world.")]
    [Range(0.5f, 3f)]
    public float gravityAnomalyIntensity = 1f;

    [Header("Terrain Generation")]
    [Tooltip("Scale of Perlin noise for platform generation (higher = larger features).")]
    public float noiseScale = 50f;

    [Tooltip("Frequency of Perlin noise (affects variation intensity).")]
    [Range(0.1f, 5f)]
    public float noiseFrequency = 1f;

    [Tooltip("Height range for platform placement.")]
    public float minTerrainHeight = -10f;
    public float maxTerrainHeight = 50f;

    [Tooltip("Density of platforms (0-1, where 1 is most dense).")]
    [Range(0.1f, 1f)]
    public float platformDensity = 0.6f;

    [Header("Cave Generation")]
    [Tooltip("Whether caves should generate in this world.")]
    public bool generateCaves = true;

    [Tooltip("Scale of cave noise.")]
    public float caveNoiseScale = 30f;

    [Tooltip("Threshold for cave carving (lower = more caves).")]
    [Range(0.3f, 0.7f)]
    public float caveThreshold = 0.5f;

    [Header("Gravity Anomalies")]
    [Tooltip("Number of gravity zones to generate in this world.")]
    [Range(1, 20)]
    public int gravityZoneCount = 5;

    [Tooltip("Minimum radius of gravity zones.")]
    public float minGravityZoneRadius = 5f;

    [Tooltip("Maximum radius of gravity zones.")]
    public float maxGravityZoneRadius = 15f;

    [Header("Collectibles")]
    [Tooltip("Number of health pickups to spawn.")]
    [Range(0, 50)]
    public int healthPickupCount = 10;

    [Tooltip("Health restored per pickup.")]
    public float healthPerPickup = 25f;

    [Header("Visual Theme")]
    [Tooltip("Primary color for this world type.")]
    public Color primaryColor = Color.gray;

    [Tooltip("Secondary color for this world type.")]
    public Color secondaryColor = Color.white;

    [Tooltip("Accent color for this world type (used for gravity zones, etc).")]
    public Color accentColor = Color.cyan;

    [Header("World Bounds")]
    [Tooltip("Size of the world generation area (applied as half-extents from origin).")]
    public Vector3 worldSize = new Vector3(200f, 100f, 200f);

    /// <summary>
    /// Validates the configuration and returns any warnings.
    /// </summary>
    public bool Validate(out string warning)
    {
        warning = "";

        if (string.IsNullOrEmpty(worldTypeName))
        {
            warning = "World type name is empty.";
            return false;
        }

        if (defaultGravityStrength <= 0)
        {
            warning = "Gravity strength must be positive.";
            return false;
        }

        if (noiseScale <= 0)
        {
            warning = "Noise scale must be positive.";
            return false;
        }

        if (minTerrainHeight >= maxTerrainHeight)
        {
            warning = "Min terrain height must be less than max terrain height.";
            return false;
        }

        if (minGravityZoneRadius >= maxGravityZoneRadius)
        {
            warning = "Min gravity zone radius must be less than max.";
            return false;
        }

        return true;
    }
}
