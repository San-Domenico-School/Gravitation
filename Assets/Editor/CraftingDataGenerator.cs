#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Run via menu: Gravitas → Generate Crafting Data
/// Creates all ItemData and CraftingRecipe assets from the GDD.
/// Safe to re-run — skips assets that already exist.
/// </summary>
public static class CraftingDataGenerator
{
    private const string ItemsPath    = "Assets/data/items";
    private const string RecipesPath  = "Assets/data/recipes";

    [MenuItem("Gravitas/Generate Crafting Data")]
    public static void Generate()
    {
        EnsureFolder(ItemsPath);
        EnsureFolder(RecipesPath);

        // ── 1. Create all ItemData assets ────────────────────────────────────
        CreateItems();

        // ── 2. Create all CraftingRecipe assets ──────────────────────────────
        CreateRecipes();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CraftingDataGenerator] Done! All ItemData and CraftingRecipe assets created.");
    }

    // =========================================================================
    //  ITEM DEFINITIONS
    // =========================================================================

    private static void CreateItems()
    {
        // Base raw resources
        MakeItem("Silver Deposit",        ItemType.RawResource,    CraftingCategory.BasicMaterials, "Raw silver ore found in the world.");
        MakeItem("Copper Deposit",        ItemType.RawResource,    CraftingCategory.BasicMaterials, "Raw copper ore.");
        MakeItem("Rubber Juice",          ItemType.RawResource,    CraftingCategory.BasicMaterials, "Viscous sap harvested from rubber plants.");
        MakeItem("Metal Scrap",           ItemType.RawResource,    CraftingCategory.BasicMaterials, "Salvaged scrap metal.");
        MakeItem("Fibers",                ItemType.RawResource,    CraftingCategory.BasicMaterials, "Natural plant fibers.");
        MakeItem("Resin",                 ItemType.RawResource,    CraftingCategory.BasicMaterials, "Sticky resin gathered from trees.");
        MakeItem("Wood",                  ItemType.RawResource,    CraftingCategory.BasicMaterials, "Harvested lumber.");
        MakeItem("Gravity Crystal",       ItemType.RawResource,    CraftingCategory.BasicMaterials, "A crystal humming with gravitational energy.");
        MakeItem("Lithium",               ItemType.RawResource,    CraftingCategory.BasicMaterials, "Lightweight reactive metal.");
        MakeItem("Quartz",                ItemType.RawResource,    CraftingCategory.BasicMaterials, "Clear crystalline mineral.");

        // Tier 1 Basic crafted
        MakeItem("Wiring Kit",            ItemType.CraftedComponent, CraftingCategory.BasicMaterials, "A bundle of wires and connectors.");
        MakeItem("Copper Wire",           ItemType.CraftedComponent, CraftingCategory.BasicMaterials, "Refined copper wire.");
        MakeItem("Rubber Sheet",          ItemType.CraftedComponent, CraftingCategory.BasicMaterials, "Flat vulcanized rubber.");
        MakeItem("Metal Ingot",           ItemType.CraftedComponent, CraftingCategory.BasicMaterials, "Smelted metal bar.");
        MakeItem("Fabric",                ItemType.CraftedComponent, CraftingCategory.BasicMaterials, "Woven cloth from plant fibers.");
        MakeItem("Glue",                  ItemType.CraftedComponent, CraftingCategory.BasicMaterials, "Strong adhesive made from resin.");
        MakeItem("Stick",                 ItemType.CraftedComponent, CraftingCategory.BasicMaterials, "A simple wooden stick.");
        MakeItem("Power Cell",            ItemType.CraftedComponent, CraftingCategory.BasicMaterials, "A small rechargeable energy cell.");
        MakeItem("Health Pack",           ItemType.CraftedComponent, CraftingCategory.BasicMaterials, "A basic medical kit that restores health.");

        // Tier 1 Tech
        MakeItem("Knife",                 ItemType.Weapon,   CraftingCategory.Tech, "A simple cutting tool.");
        MakeItem("Rock Pulverizer T1",    ItemType.Tool,     CraftingCategory.Tech, "Pulverizes rock to gather resources.");
        MakeItem("Axe",                   ItemType.Tool,     CraftingCategory.Tech, "Used to harvest trees for wood.");
        MakeItem("Glide Suit",            ItemType.Equipment,CraftingCategory.Tech, "Allows the player to glide far distances.");
        MakeItem("Beacon",                ItemType.Equipment,CraftingCategory.Tech, "Places a navigational beacon in the world.");
        MakeItem("Gravity Grenade",       ItemType.Weapon,   CraftingCategory.Tech, "Launches objects and players on detonation.");
        MakeItem("Tier 1 Crafter",        ItemType.Structure,CraftingCategory.Tech, "A Tier 1 crafting station you can place anywhere.");
        MakeItem("Scanner",               ItemType.Tool,     CraftingCategory.Tech, "Scans objects for information.");

        // Tier 2 Basic crafted
        MakeItem("Control Board",         ItemType.CraftedComponent, CraftingCategory.BasicMaterials, "A printed circuit board for complex devices.");
        MakeItem("Motor",                 ItemType.CraftedComponent, CraftingCategory.BasicMaterials, "A small electric motor.");
        MakeItem("Gas Condenser",         ItemType.CraftedComponent, CraftingCategory.BasicMaterials, "Condenses gases into liquid form.");
        MakeItem("Base Small Room",       ItemType.Structure,        CraftingCategory.BasicMaterials, "A small modular base room.");
        MakeItem("Door",                  ItemType.Structure,        CraftingCategory.BasicMaterials, "A door for base modules.");
        MakeItem("Solar Panel",           ItemType.Structure,        CraftingCategory.BasicMaterials, "Generates power from sunlight.");
        MakeItem("Storage",               ItemType.Structure,        CraftingCategory.BasicMaterials, "A basic storage container.");

        // Tier 2 Tech
        MakeItem("Jetpack",               ItemType.Equipment,CraftingCategory.Tech, "Allows sustained flight using condensed gas.");
        MakeItem("Scout Drone",           ItemType.Equipment,CraftingCategory.Tech, "Scouts ahead 24/7 for enemies and loot.");
        MakeItem("Suction Grenade",       ItemType.Weapon,   CraftingCategory.Tech, "Pulls everything towards its detonation point.");
        MakeItem("Gravity Compass",       ItemType.Tool,     CraftingCategory.Tech, "Shows the direction of gravity in 3D space.");
        MakeItem("Grapple Hook",          ItemType.Equipment,CraftingCategory.Tech, "Launches a grappling hook for traversal.");
        MakeItem("Plasma Cutter",         ItemType.Weapon,   CraftingCategory.Tech, "A high-powered plasma cutting tool.");
        MakeItem("Tier 2 Crafter",        ItemType.Structure,CraftingCategory.Tech, "A Tier 2 crafting station you can place anywhere.");

        // Tier 3 Basic crafted
        MakeItem("Quantum Computing Chip",ItemType.CraftedComponent, CraftingCategory.BasicMaterials, "A chip operating on quantum principles.");
        MakeItem("Reinforced Metal Ingot",ItemType.CraftedComponent, CraftingCategory.BasicMaterials, "Incredibly dense and strong metal bar.");
        MakeItem("Vehicle Frame",         ItemType.CraftedComponent, CraftingCategory.BasicMaterials, "The chassis for a gravity vehicle.");
        MakeItem("Gravity Engine",        ItemType.CraftedComponent, CraftingCategory.BasicMaterials, "An engine that manipulates gravity.");
        MakeItem("Gravity Core",          ItemType.CraftedComponent, CraftingCategory.BasicMaterials, "A dense gravity crystal core.");
        MakeItem("Large Base Room",       ItemType.Structure,        CraftingCategory.BasicMaterials, "A large modular base room.");
        MakeItem("Quantum Power Cell",    ItemType.CraftedComponent, CraftingCategory.BasicMaterials, "A high-capacity quantum energy cell.");
        MakeItem("Sled Dock",             ItemType.Structure,        CraftingCategory.BasicMaterials, "A docking platform for the gravity sled.");
        MakeItem("Large Storage Container",ItemType.Structure,       CraftingCategory.BasicMaterials, "A large reinforced storage unit.");

        // Tier 3 Tech
        MakeItem("Scout Drone Attack Upgrade", ItemType.Equipment, CraftingCategory.Tech, "Enables the scout drone to perform a multispacial smash attack.");
        MakeItem("Teleport Pad",          ItemType.Structure,CraftingCategory.Tech, "Links two pads for instant teleportation. Requires two pads.");
        MakeItem("Vehicle",               ItemType.Equipment,CraftingCategory.Tech, "A gravity sled that adheres to surfaces.");
        MakeItem("Jetpack Length Upgrade",ItemType.Equipment,CraftingCategory.Tech, "Extends jetpack flight time.");
        MakeItem("Axe Upgrade",           ItemType.Weapon,   CraftingCategory.Tech, "Allows you to throw the axe and recall it.");
        MakeItem("Rock Pulverizer T2",    ItemType.Tool,     CraftingCategory.Tech, "Faster pulverizer that can mine harder materials.");
        MakeItem("Tier 3 Crafter",        ItemType.Structure,CraftingCategory.Tech, "A Tier 3 crafting station you can place anywhere.");
    }

    // =========================================================================
    //  RECIPE DEFINITIONS
    // =========================================================================

    private static void CreateRecipes()
    {
        // ── TIER 1 Basic ──────────────────────────────────────────────────────
        MakeRecipe("Recipe_WiringKit",        "Wiring Kit",       1, CraftingCategory.BasicMaterials, 1,
            ("Silver Deposit", 2), ("Quartz", 1));

        MakeRecipe("Recipe_CopperWire",       "Copper Wire",      1, CraftingCategory.BasicMaterials, 1,
            ("Copper Deposit", 1));

        MakeRecipe("Recipe_RubberSheet",      "Rubber Sheet",     1, CraftingCategory.BasicMaterials, 1,
            ("Rubber Juice", 2));

        MakeRecipe("Recipe_MetalIngot",       "Metal Ingot",      1, CraftingCategory.BasicMaterials, 1,
            ("Metal Scrap", 2));

        MakeRecipe("Recipe_Fabric",           "Fabric",           1, CraftingCategory.BasicMaterials, 1,
            ("Fibers", 2));

        MakeRecipe("Recipe_Glue",             "Glue",             1, CraftingCategory.BasicMaterials, 1,
            ("Resin", 3));

        MakeRecipe("Recipe_Stick",            "Stick",            4, CraftingCategory.BasicMaterials, 1,
            ("Wood", 1));

        MakeRecipe("Recipe_PowerCell",        "Power Cell",       1, CraftingCategory.BasicMaterials, 1,
            ("Copper Wire", 1), ("Lithium", 1));

        MakeRecipe("Recipe_HealthPack",       "Health Pack",      1, CraftingCategory.BasicMaterials, 1,
            ("Fabric", 2), ("Rubber Sheet", 1), ("Glue", 1));

        // ── TIER 1 Tech ───────────────────────────────────────────────────────
        MakeRecipe("Recipe_Knife",            "Knife",            1, CraftingCategory.Tech, 1,
            ("Stick", 1), ("Metal Ingot", 1));

        MakeRecipe("Recipe_RockPulverizerT1", "Rock Pulverizer T1", 1, CraftingCategory.Tech, 1,
            ("Gravity Crystal", 1), ("Metal Ingot", 2), ("Glue", 1), ("Wiring Kit", 1));

        MakeRecipe("Recipe_Axe",              "Axe",              1, CraftingCategory.Tech, 1,
            ("Stick", 2), ("Metal Ingot", 1));

        MakeRecipe("Recipe_GlideSuit",        "Glide Suit",       1, CraftingCategory.Tech, 1,
            ("Fabric", 2), ("Glue", 1));

        MakeRecipe("Recipe_Beacon",           "Beacon",           1, CraftingCategory.Tech, 1,
            ("Metal Ingot", 1), ("Power Cell", 1), ("Quartz", 1));

        MakeRecipe("Recipe_GravityGrenade",   "Gravity Grenade",  1, CraftingCategory.Tech, 1,
            ("Metal Ingot", 1), ("Gravity Crystal", 1));

        MakeRecipe("Recipe_Tier1Crafter",     "Tier 1 Crafter",   1, CraftingCategory.Tech, 1,
            ("Wiring Kit", 1), ("Copper Wire", 1), ("Metal Ingot", 1));

        MakeRecipe("Recipe_Scanner",          "Scanner",          1, CraftingCategory.Tech, 1,
            ("Power Cell", 1), ("Quartz", 1), ("Metal Ingot", 1));

        // ── TIER 2 Basic ──────────────────────────────────────────────────────
        MakeRecipe("Recipe_ControlBoard",     "Control Board",    1, CraftingCategory.BasicMaterials, 2,
            ("Wiring Kit", 1), ("Copper Wire", 2));

        MakeRecipe("Recipe_Motor",            "Motor",            1, CraftingCategory.BasicMaterials, 2,
            ("Wiring Kit", 1), ("Lithium", 1), ("Metal Ingot", 1));

        MakeRecipe("Recipe_GasCondenser",     "Gas Condenser",    1, CraftingCategory.BasicMaterials, 2,
            ("Metal Ingot", 2), ("Fibers", 1), ("Glue", 1), ("Motor", 1));

        MakeRecipe("Recipe_BaseSmallRoom",    "Base Small Room",  1, CraftingCategory.BasicMaterials, 2,
            ("Metal Ingot", 4), ("Lithium", 2));

        MakeRecipe("Recipe_Door",             "Door",             1, CraftingCategory.BasicMaterials, 2,
            ("Metal Ingot", 1), ("Quartz", 1));

        MakeRecipe("Recipe_SolarPanel",       "Solar Panel",      1, CraftingCategory.BasicMaterials, 2,
            ("Quartz", 2), ("Metal Ingot", 1), ("Copper Wire", 1));

        MakeRecipe("Recipe_Storage",          "Storage",          1, CraftingCategory.BasicMaterials, 2,
            ("Wood", 1), ("Metal Ingot", 1), ("Quartz", 1));

        // ── TIER 2 Tech ───────────────────────────────────────────────────────
        MakeRecipe("Recipe_Jetpack",          "Jetpack",          1, CraftingCategory.Tech, 2,
            ("Metal Ingot", 2), ("Control Board", 1), ("Motor", 2));

        MakeRecipe("Recipe_ScoutDrone",       "Scout Drone",      1, CraftingCategory.Tech, 2,
            ("Metal Ingot", 1), ("Power Cell", 1), ("Solar Panel", 1));

        MakeRecipe("Recipe_SuctionGrenade",   "Suction Grenade",  1, CraftingCategory.Tech, 2,
            ("Gravity Crystal", 1), ("Metal Ingot", 1));

        MakeRecipe("Recipe_GravityCompass",   "Gravity Compass",  1, CraftingCategory.Tech, 2,
            ("Metal Ingot", 1), ("Control Board", 1), ("Gravity Crystal", 1), ("Quartz", 1));

        MakeRecipe("Recipe_GrappleHook",      "Grapple Hook",     1, CraftingCategory.Tech, 2,
            ("Metal Ingot", 1), ("Rubber Sheet", 1), ("Gravity Crystal", 1));

        MakeRecipe("Recipe_PlasmaCutter",     "Plasma Cutter",    1, CraftingCategory.Tech, 2,
            ("Metal Ingot", 1), ("Lithium", 1), ("Control Board", 1));

        MakeRecipe("Recipe_Tier2Crafter",     "Tier 2 Crafter",   1, CraftingCategory.Tech, 2,
            ("Wiring Kit", 1), ("Control Board", 1), ("Tier 1 Crafter", 1));

        // ── TIER 3 Basic ──────────────────────────────────────────────────────
        MakeRecipe("Recipe_QuantumChip",      "Quantum Computing Chip", 1, CraftingCategory.BasicMaterials, 3,
            ("Control Board", 2), ("Copper Wire", 1), ("Gravity Core", 2));

        MakeRecipe("Recipe_ReinforcedIngot",  "Reinforced Metal Ingot", 1, CraftingCategory.BasicMaterials, 3,
            ("Metal Ingot", 2), ("Lithium", 1));

        MakeRecipe("Recipe_VehicleFrame",     "Vehicle Frame",    1, CraftingCategory.BasicMaterials, 3,
            ("Reinforced Metal Ingot", 4), ("Gravity Engine", 2));

        MakeRecipe("Recipe_GravityEngine",    "Gravity Engine",   1, CraftingCategory.BasicMaterials, 3,
            ("Motor", 1), ("Gravity Core", 1), ("Metal Ingot", 2));

        MakeRecipe("Recipe_GravityCore",      "Gravity Core",     1, CraftingCategory.BasicMaterials, 3,
            ("Gravity Crystal", 3), ("Wiring Kit", 1), ("Lithium", 1));

        MakeRecipe("Recipe_LargeBaseRoom",    "Large Base Room",  1, CraftingCategory.BasicMaterials, 3,
            ("Reinforced Metal Ingot", 4), ("Lithium", 4));

        MakeRecipe("Recipe_QuantumPowerCell", "Quantum Power Cell", 1, CraftingCategory.BasicMaterials, 3,
            ("Power Cell", 2), ("Gravity Core", 1));

        MakeRecipe("Recipe_SledDock",         "Sled Dock",        1, CraftingCategory.BasicMaterials, 3,
            ("Rubber Sheet", 2), ("Reinforced Metal Ingot", 2), ("Control Board", 1));

        MakeRecipe("Recipe_LargeStorage",     "Large Storage Container", 1, CraftingCategory.BasicMaterials, 3,
            ("Reinforced Metal Ingot", 2), ("Quartz", 1));

        // ── TIER 3 Tech ───────────────────────────────────────────────────────
        MakeRecipe("Recipe_DroneAttackUpgrade","Scout Drone Attack Upgrade", 1, CraftingCategory.Tech, 3,
            ("Reinforced Metal Ingot", 2), ("Control Board", 1), ("Gravity Crystal", 1));

        MakeRecipe("Recipe_TeleportPad",      "Teleport Pad",     2, CraftingCategory.Tech, 3,
            ("Metal Ingot", 2), ("Quantum Computing Chip", 2), ("Quantum Power Cell", 2));

        MakeRecipe("Recipe_Vehicle",          "Vehicle",          1, CraftingCategory.Tech, 3,
            ("Vehicle Frame", 1), ("Quartz", 3), ("Quantum Computing Chip", 1),
            ("Quantum Power Cell", 2), ("Glue", 1), ("Rubber Sheet", 2));

        MakeRecipe("Recipe_JetpackUpgrade",   "Jetpack Length Upgrade", 1, CraftingCategory.Tech, 3,
            ("Gas Condenser", 1), ("Fabric", 1), ("Glue", 1));

        MakeRecipe("Recipe_AxeUpgrade",       "Axe Upgrade",      1, CraftingCategory.Tech, 3,
            ("Gravity Crystal", 1), ("Metal Ingot", 2), ("Quantum Computing Chip", 1));

        MakeRecipe("Recipe_RockPulverizerT2", "Rock Pulverizer T2", 1, CraftingCategory.Tech, 3,
            ("Rock Pulverizer T1", 1), ("Wiring Kit", 1), ("Quantum Computing Chip", 1));

        MakeRecipe("Recipe_Tier3Crafter",     "Tier 3 Crafter",   1, CraftingCategory.Tech, 3,
            ("Quantum Computing Chip", 1), ("Gravity Core", 1), ("Tier 2 Crafter", 1));
    }

    // =========================================================================
    //  HELPERS
    // =========================================================================

    private static ItemData MakeItem(string itemName, ItemType type, CraftingCategory category, string description)
    {
        string safeName = itemName.Replace(" ", "").Replace("/", "");
        string assetPath = $"{ItemsPath}/{safeName}.asset";

        var existing = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
        if (existing != null) return existing;

        var data = ScriptableObject.CreateInstance<ItemData>();
        data.itemName = itemName;
        data.itemType = type;
        data.craftingCategory = category;
        data.description = description;
        AssetDatabase.CreateAsset(data, assetPath);
        return data;
    }

    private static void MakeRecipe(string assetName, string resultItemName, int resultCount,
        CraftingCategory category, int tier, params (string itemName, int count)[] ingredients)
    {
        string assetPath = $"{RecipesPath}/{assetName}.asset";
        if (AssetDatabase.LoadAssetAtPath<CraftingRecipe>(assetPath) != null) return;

        var resultItem = FindItem(resultItemName);
        if (resultItem == null)
        {
            Debug.LogWarning($"[CraftingDataGenerator] Result item not found: {resultItemName}");
            return;
        }

        var recipe = ScriptableObject.CreateInstance<CraftingRecipe>();
        recipe.result = resultItem;
        recipe.resultCount = resultCount;
        recipe.crafterTier = tier;
        recipe.category = category;

        var ingList = new List<CraftingIngredient>();
        foreach (var (name, count) in ingredients)
        {
            var item = FindItem(name);
            if (item == null)
            {
                Debug.LogWarning($"[CraftingDataGenerator] Ingredient not found: {name} (recipe: {assetName})");
                continue;
            }
            ingList.Add(new CraftingIngredient { item = item, count = count });
        }
        recipe.ingredients = ingList.ToArray();

        AssetDatabase.CreateAsset(recipe, assetPath);
    }

    private static ItemData FindItem(string itemName)
    {
        string safeName = itemName.Replace(" ", "").Replace("/", "");
        return AssetDatabase.LoadAssetAtPath<ItemData>($"{ItemsPath}/{safeName}.asset");
    }

    private static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            string folderName = System.IO.Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
#endif
