using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Gravitation/World Config")]
public class WorldConfig : ScriptableObject
{
    private static readonly Dictionary<WorldType, WorldConfig> Cache = new Dictionary<WorldType, WorldConfig>();

    [System.Serializable]
    public class ChunkPattern
    {
        public string name;
        public List<Vector3Int> blockOffsets = new List<Vector3Int>();
    }

    [Header("Identity")]
    public WorldType worldType;
    public string displayName;

    [Header("Physics")]
    public float gravityStrength = 9.81f;
    public float moveSpeedMultiplier = 1f;
    public float jumpForceMultiplier = 1f;

    [Header("Visuals")]
    public Color platformColor = Color.gray;
    public Color accentColor = Color.white;
    public Color skyColor = Color.black;

    [Header("Generation")]
    public float platformDensity = 0.7f;
    public float chunkStride = 8f;
    public float blockSize = 2f;
    public int worldRadiusInChunks = 4;
    public int spawnRadius = 5;
    public float spawnSurfaceY = 10f;
    public float collectibleChance = 0.1f;
    public float gravityPropChance = 0.05f;

    [Header("Patterns")]
    public List<ChunkPattern> chunkPatterns = new List<ChunkPattern>();

    public static WorldConfig GetDefault(WorldType worldType)
    {
        if (Cache.TryGetValue(worldType, out WorldConfig cached) && cached != null)
            return cached;

        WorldConfig created = CreateInstance<WorldConfig>();
        created.hideFlags = HideFlags.HideAndDontSave;
        created.ConfigureDefaults(worldType);
        Cache[worldType] = created;
        return created;
    }

    private void ConfigureDefaults(WorldType type)
    {
        worldType = type;
        displayName = WorldTypeUtility.ToDisplayName(type);

        chunkPatterns = BuildPatterns(type);

        switch (type)
        {
            case WorldType.Stone:
                gravityStrength = 11.5f;
                moveSpeedMultiplier = 0.95f;
                jumpForceMultiplier = 0.95f;
                platformDensity = 0.85f;
                platformColor = new Color(0.55f, 0.55f, 0.6f);
                accentColor = new Color(0.8f, 0.8f, 0.85f);
                skyColor = new Color(0.13f, 0.14f, 0.18f);
                collectibleChance = 0.08f;
                gravityPropChance = 0.04f;
                break;
            case WorldType.Wood:
                gravityStrength = 8.8f;
                moveSpeedMultiplier = 1.1f;
                jumpForceMultiplier = 1.05f;
                platformDensity = 0.72f;
                platformColor = new Color(0.55f, 0.36f, 0.2f);
                accentColor = new Color(0.77f, 0.62f, 0.4f);
                skyColor = new Color(0.17f, 0.21f, 0.12f);
                collectibleChance = 0.12f;
                gravityPropChance = 0.07f;
                break;
            case WorldType.Iron:
                gravityStrength = 13.2f;
                moveSpeedMultiplier = 0.9f;
                jumpForceMultiplier = 0.9f;
                platformDensity = 0.62f;
                platformColor = new Color(0.45f, 0.52f, 0.58f);
                accentColor = new Color(0.78f, 0.86f, 0.9f);
                skyColor = new Color(0.1f, 0.12f, 0.16f);
                collectibleChance = 0.1f;
                gravityPropChance = 0.09f;
                break;
        }
    }

    private static List<ChunkPattern> BuildPatterns(WorldType type)
    {
        List<ChunkPattern> patterns = new List<ChunkPattern>();

        patterns.Add(CreateFlatPattern(type + "_Flat", 3, 3));
        patterns.Add(CreateLadderPattern(type + "_Ladder", 3, 4));
        patterns.Add(CreateBridgePattern(type + "_Bridge", 4, 2));
        patterns.Add(CreateTowerPattern(type + "_Tower", 2, 4));
        patterns.Add(CreateSplitPattern(type + "_Split", 4, 3));

        return patterns;
    }

    private static ChunkPattern CreateFlatPattern(string name, int width, int depth)
    {
        ChunkPattern pattern = new ChunkPattern { name = name };
        AddFilledRect(pattern.blockOffsets, 0, 0, 0, width, 1, depth);
        return pattern;
    }

    private static ChunkPattern CreateLadderPattern(string name, int width, int steps)
    {
        ChunkPattern pattern = new ChunkPattern { name = name };
        for (int step = 0; step < steps; step++)
        {
            pattern.blockOffsets.Add(new Vector3Int(step, step, 0));
            if (step < steps - 1)
            {
                pattern.blockOffsets.Add(new Vector3Int(step, step + 1, 0));
            }
        }

        AddFilledRect(pattern.blockOffsets, 0, 0, 1, width, 1, 2);
        return pattern;
    }

    private static ChunkPattern CreateBridgePattern(string name, int width, int depth)
    {
        ChunkPattern pattern = new ChunkPattern { name = name };
        AddFilledRect(pattern.blockOffsets, 0, 0, 0, width, 1, 1);
        AddFilledRect(pattern.blockOffsets, 1, 1, 1, width - 2, 1, depth);
        return pattern;
    }

    private static ChunkPattern CreateTowerPattern(string name, int width, int height)
    {
        ChunkPattern pattern = new ChunkPattern { name = name };
        AddFilledRect(pattern.blockOffsets, 0, 0, 0, width, height, width);
        return pattern;
    }

    private static ChunkPattern CreateSplitPattern(string name, int width, int depth)
    {
        ChunkPattern pattern = new ChunkPattern { name = name };
        AddFilledRect(pattern.blockOffsets, 0, 0, 0, 2, 1, depth);
        AddFilledRect(pattern.blockOffsets, width - 2, 0, 0, 2, 1, depth);
        pattern.blockOffsets.Add(new Vector3Int(width / 2, 1, depth / 2));
        return pattern;
    }

    private static void AddFilledRect(List<Vector3Int> offsets, int startX, int startY, int startZ, int width, int height, int depth)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    offsets.Add(new Vector3Int(startX + x, startY + y, startZ + z));
                }
            }
        }
    }
}
