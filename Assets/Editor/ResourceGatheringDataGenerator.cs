#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Run via menu: Gravitas → Generate Resource Gathering Data
/// Creates a BiomeSpawnTable asset pre-populated from the GDD distribution chart,
/// with default glow colors per resource. Skips creation if the asset already exists.
/// </summary>
public static class ResourceGatheringDataGenerator
{
    private const string OutputFolder = "Assets/data/ResourceGathering";
    private const string TableAssetPath = OutputFolder + "/BiomeSpawnTable.asset";

    [MenuItem("Gravitas/Generate Resource Gathering Data")]
    public static void Generate()
    {
        EnsureFolder(OutputFolder);

        var table = AssetDatabase.LoadAssetAtPath<BiomeSpawnTable>(TableAssetPath);
        if (table == null)
        {
            table = ScriptableObject.CreateInstance<BiomeSpawnTable>();
            AssetDatabase.CreateAsset(table, TableAssetPath);
            Debug.Log($"[ResourceGathering] Created {TableAssetPath}");
        }
        else
        {
            Debug.Log("[ResourceGathering] BiomeSpawnTable already exists — repopulating with defaults.");
        }

        var entries = new List<BiomeSpawnTable.BiomeEntry>();
        entries.Add(BuildBiome1Entry());
        entries.Add(BuildBiome2Entry());
        entries.Add(BuildBiome3Entry());
        entries.Add(BuildBiome4Entry());
        entries.Add(BuildBiome5Entry());

        table.biomes = entries.ToArray();
        EditorUtility.SetDirty(table);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ResourceGathering] Done. Open the BiomeSpawnTable asset to tune weights/colors.");
    }

    // Rarity → weight conversion (matches GDD chart legend).
    private const float W_VRare    = 1f;
    private const float W_Rare     = 3f;
    private const float W_Uncommon = 6f;
    private const float W_Common   = 12f;

    // HDR-intense glow colors. Multipliers >1 push into HDR for bloom.
    private static readonly Color GlowCopper    = HDR(1.00f, 0.55f, 0.20f, 1.5f);
    private static readonly Color GlowSilver    = HDR(0.85f, 0.92f, 1.00f, 1.5f);
    private static readonly Color GlowQuartz    = HDR(0.85f, 0.95f, 1.00f, 1.2f);
    private static readonly Color GlowGravity   = HDR(0.20f, 1.00f, 0.80f, 2.0f);
    private static readonly Color GlowLithium   = HDR(0.70f, 0.40f, 1.00f, 1.5f);
    private static readonly Color GlowMetal     = HDR(1.00f, 0.85f, 0.40f, 1.2f);
    private static readonly Color GlowAlien     = HDR(0.30f, 1.00f, 1.00f, 2.5f);

    private static Color HDR(float r, float g, float b, float intensity)
    {
        return new Color(r * intensity, g * intensity, b * intensity, 1f);
    }

    private static BiomeSpawnTable.BiomeEntry BuildBiome1Entry()
    {
        // Biome 1 is plant-resource territory; no minerals here by default.
        return new BiomeSpawnTable.BiomeEntry
        {
            biome = BiomeType.Biome1,
            resources = new BiomeSpawnTable.ResourceWeight[0],
        };
    }

    private static BiomeSpawnTable.BiomeEntry BuildBiome2Entry()
    {
        return new BiomeSpawnTable.BiomeEntry
        {
            biome = BiomeType.Biome2,
            resources = new[]
            {
                Entry("Copper deposit", W_Rare,     GlowCopper),
                Entry("Silver deposit", W_Uncommon, GlowSilver),
                Entry("Quartz",         W_Rare,     GlowQuartz),
                Entry("Metal Scrap",    W_Rare,     GlowMetal),
            },
        };
    }

    private static BiomeSpawnTable.BiomeEntry BuildBiome3Entry()
    {
        return new BiomeSpawnTable.BiomeEntry
        {
            biome = BiomeType.Biome3,
            resources = new[]
            {
                Entry("Copper deposit",  W_Uncommon, GlowCopper),
                Entry("Silver deposit",  W_Rare,     GlowSilver),
                Entry("Quartz",          W_Uncommon, GlowQuartz),
                Entry("Gravity Crystal", W_VRare,    GlowGravity),
                Entry("Lithium",         W_VRare,    GlowLithium),
                Entry("Metal Scrap",     W_Rare,     GlowMetal),
            },
        };
    }

    private static BiomeSpawnTable.BiomeEntry BuildBiome4Entry()
    {
        return new BiomeSpawnTable.BiomeEntry
        {
            biome = BiomeType.Biome4,
            resources = new[]
            {
                Entry("Silver deposit",  W_VRare,    GlowSilver),
                Entry("Copper deposit",  W_VRare,    GlowCopper),
                Entry("Quartz",          W_Rare,     GlowQuartz),
                Entry("Gravity Crystal", W_VRare,    GlowGravity),
                Entry("Lithium",         W_Rare,     GlowLithium),
                Entry("Metal Scrap",     W_Uncommon, GlowMetal),
            },
        };
    }

    private static BiomeSpawnTable.BiomeEntry BuildBiome5Entry()
    {
        return new BiomeSpawnTable.BiomeEntry
        {
            biome = BiomeType.Biome5,
            resources = new[]
            {
                Entry("Silver deposit",  W_VRare,    GlowSilver),
                Entry("Copper deposit",  W_Rare,     GlowCopper),
                Entry("Quartz",          W_Uncommon, GlowQuartz),
                Entry("Gravity Crystal", W_Uncommon, GlowGravity),
                Entry("Lithium",         W_VRare,    GlowLithium),
                Entry("Metal Scrap",     W_Rare,     GlowMetal),
            },
        };
    }

    private static BiomeSpawnTable.ResourceWeight Entry(string itemName, float weight, Color glow)
    {
        return new BiomeSpawnTable.ResourceWeight
        {
            resource = FindItemDataByName(itemName),
            weight = weight,
            glowColor = glow,
        };
    }

    private static ItemData FindItemDataByName(string itemName)
    {
        string target = Normalize(itemName);
        string[] guids = AssetDatabase.FindAssets("t:ItemData");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var data = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (data == null) continue;
            if (Normalize(data.itemName) == target) return data;
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            if (Normalize(fileName) == target) return data;
        }
        Debug.LogWarning($"[ResourceGathering] ItemData '{itemName}' not found. Make sure the resource ItemData assets exist (e.g., 'Copper deposit', 'Silver deposit', 'Quartz', 'Gravity Crystal', 'Lithium', 'Metal Scrap').");
        return null;
    }

    private static string Normalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Trim().ToLowerInvariant();
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string folder = System.IO.Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }
}
#endif
