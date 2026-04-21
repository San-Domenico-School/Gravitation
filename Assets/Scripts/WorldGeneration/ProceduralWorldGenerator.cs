using UnityEngine;

/// <summary>
/// Main orchestrator for procedural world generation.
/// Coordinates all generation systems: terrain, caves, gravity zones, collectibles.
/// Uses seed-based generation for reproducibility.
/// </summary>
public class ProceduralWorldGenerator : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Folder path to load world configs from (e.g., 'Assets/Resources/WorldConfigs').")]
    [SerializeField]
    private string configFolder = "WorldConfigs";

    [Tooltip("List of available world types (stone, wood, iron, etc).")]
    [SerializeField]
    private string[] availableWorldTypes = new string[] { "stone", "wood", "iron" };

    [Header("References")]
    [Tooltip("Parent transform under which all generated objects will be placed.")]
    [SerializeField]
    private Transform generationRoot;

    [Header("Debug")]
    [SerializeField]
    private bool logGenerationSteps = true;

    private WorldConfig currentConfig;
    private string currentSeed;

    private PlatformGenerator platformGenerator;
    private CaveGenerator caveGenerator;
    private GravityZoneGenerator gravityZoneGenerator;
    private CollectibleSpawner collectibleSpawner;

    private void Awake()
    {
        // Ensure we have a generation root.
        if (generationRoot == null)
        {
            generationRoot = transform;
        }

        // Initialize sub-generators.
        platformGenerator = GetComponent<PlatformGenerator>();
        if (platformGenerator == null)
            platformGenerator = gameObject.AddComponent<PlatformGenerator>();

        caveGenerator = GetComponent<CaveGenerator>();
        if (caveGenerator == null)
            caveGenerator = gameObject.AddComponent<CaveGenerator>();

        gravityZoneGenerator = GetComponent<GravityZoneGenerator>();
        if (gravityZoneGenerator == null)
            gravityZoneGenerator = gameObject.AddComponent<GravityZoneGenerator>();

        collectibleSpawner = GetComponent<CollectibleSpawner>();
        if (collectibleSpawner == null)
            collectibleSpawner = gameObject.AddComponent<CollectibleSpawner>();
    }

    /// <summary>
    /// Generates a world of the given type.
    /// </summary>
    /// <param name="worldType">The world type (e.g., "stone", "wood", "iron").</param>
    /// <returns>True if generation succeeded, false otherwise.</returns>
    public bool GenerateWorld(string worldType)
    {
        if (string.IsNullOrEmpty(worldType))
        {
            Debug.LogError("World type cannot be empty.");
            return false;
        }

        currentSeed = worldType.ToLower();

        // Load the world config for this type.
        if (!LoadWorldConfig(currentSeed))
        {
            Debug.LogError($"Failed to load world config for type '{worldType}'.");
            return false;
        }

        // Validate config.
        if (!currentConfig.Validate(out string warning))
        {
            Debug.LogError($"World config validation failed: {warning}");
            return false;
        }

        Log($"[ProceduralWorldGenerator] Starting generation for world type: {worldType}");

        // Seed the random number generator for reproducibility.
        Random.InitState(currentSeed.GetHashCode());

        // Clear any existing gravity zones.
        GravityZoneManager.Instance?.ClearAllZones();

        // Run generation pipeline.
        if (!GenerationPipeline())
        {
            Debug.LogError("World generation pipeline failed.");
            return false;
        }

        Log($"[ProceduralWorldGenerator] World generation completed successfully.");
        return true;
    }

    /// <summary>
    /// Main generation pipeline - coordinates all generation systems.
    /// </summary>
    private bool GenerationPipeline()
    {
        Log("Starting platform generation...");
        if (!platformGenerator.Generate(generationRoot, currentConfig, currentSeed))
        {
            return false;
        }
        Log("Platform generation completed.");

        if (currentConfig.generateCaves)
        {
            Log("Starting cave generation...");
            if (!caveGenerator.Generate(generationRoot, currentConfig, currentSeed))
            {
                return false;
            }
            Log("Cave generation completed.");
        }

        Log("Starting gravity zone generation...");
        if (!gravityZoneGenerator.Generate(generationRoot, currentConfig, currentSeed))
        {
            return false;
        }
        Log("Gravity zone generation completed.");

        Log("Starting collectible spawning...");
        if (!collectibleSpawner.Spawn(generationRoot, currentConfig, currentSeed))
        {
            return false;
        }
        Log("Collectible spawning completed.");

        return true;
    }

    /// <summary>
    /// Loads the world config for the given seed/type.
    /// First tries to find a matching WorldConfig asset, then creates a default if needed.
    /// </summary>
    private bool LoadWorldConfig(string seed)
    {
        // Try to load from Resources folder first.
        string configName = $"{seed.ToLower()}_config";
        currentConfig = Resources.Load<WorldConfig>($"{configFolder}/{configName}");

        if (currentConfig != null)
        {
            Log($"Loaded WorldConfig from Resources: {configName}");
            return true;
        }

        // Try alternate naming (e.g., "WorldConfig_Stone").
        configName = $"WorldConfig_{char.ToUpper(seed[0]) + seed.Substring(1)}";
        currentConfig = Resources.Load<WorldConfig>($"{configFolder}/{configName}");

        if (currentConfig != null)
        {
            Log($"Loaded WorldConfig from Resources: {configName}");
            return true;
        }

        // If no config found, create a default one.
        Log($"No WorldConfig found for '{seed}'. Creating default...");
        currentConfig = CreateDefaultConfig(seed);
        return true;
    }

    /// <summary>
    /// Creates a default WorldConfig for a given seed.
    /// This allows world generation to work without pre-made configs.
    /// </summary>
    private WorldConfig CreateDefaultConfig(string seed)
    {
        WorldConfig config = ScriptableObject.CreateInstance<WorldConfig>();
        config.worldTypeName = seed.ToLower();

        // Customize based on world type.
        switch (seed.ToLower())
        {
            case "stone":
                config.defaultGravityStrength = 10f;
                config.primaryColor = new Color(0.5f, 0.5f, 0.5f);
                config.secondaryColor = new Color(0.7f, 0.7f, 0.7f);
                config.platformDensity = 0.6f;
                break;

            case "wood":
                config.defaultGravityStrength = 9f;
                config.primaryColor = new Color(0.6f, 0.4f, 0.2f);
                config.secondaryColor = new Color(0.8f, 0.6f, 0.4f);
                config.platformDensity = 0.55f;
                break;

            case "iron":
                config.defaultGravityStrength = 11f;
                config.primaryColor = new Color(0.3f, 0.3f, 0.3f);
                config.secondaryColor = new Color(0.5f, 0.5f, 0.5f);
                config.platformDensity = 0.7f;
                break;

            default:
                // Generic config for unknown types.
                config.primaryColor = Color.gray;
                config.platformDensity = 0.6f;
                break;
        }

        return config;
    }

    /// <summary>
    /// Gets the current world config.
    /// </summary>
    public WorldConfig GetCurrentConfig()
    {
        return currentConfig;
    }

    /// <summary>
    /// Gets the current seed.
    /// </summary>
    public string GetCurrentSeed()
    {
        return currentSeed;
    }

    /// <summary>
    /// Gets available world types.
    /// </summary>
    public string[] GetAvailableWorldTypes()
    {
        return availableWorldTypes;
    }

    /// <summary>
    /// Logging helper.
    /// </summary>
    private void Log(string message)
    {
        if (logGenerationSteps)
            Debug.Log(message);
    }
}
