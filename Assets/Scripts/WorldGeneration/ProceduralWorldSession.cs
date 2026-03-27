using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ProceduralWorldSession : MonoBehaviour
{
    private static readonly Dictionary<Color32, Material> MaterialCache = new Dictionary<Color32, Material>();

    private static ProceduralWorldSession instance;

    public static ProceduralWorldSession Instance => instance;

    [SerializeField]
    private string defaultSeed = "default";

    [SerializeField]
    private WorldType defaultWorldType = WorldType.Stone;

    private Scene activeWorldScene;
    private GameObject activeWorldRoot;
    private WorldConfig activeConfig;
    private string activeWorldKey;
    private string activeSeedText;
    private WorldSaveData activeSaveData;
    private bool worldTransitionInProgress;

    private GravityBody playerBody;
    private PlayerMovement playerMovement;
    private GravityController gravityController;
    private PlayerHealth playerHealth;
    private PortalWorldUI portalUi;

    private readonly Dictionary<string, WorldPersistentObject> activeObjects = new Dictionary<string, WorldPersistentObject>();

    public GravityBody PlayerBody => playerBody;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        CacheSceneReferences();
        EnsureGravityController();
        EnsurePlayerSetup();
        EnsurePortalUi();

        if (!worldTransitionInProgress)
        {
            BeginWorldLoad(defaultWorldType, defaultSeed);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public void OpenPortalUi()
    {
        if (portalUi == null)
            EnsurePortalUi();

        if (portalUi == null)
            return;

        portalUi.Open(activeSeedText ?? defaultSeed, activeConfig != null ? activeConfig.worldType : defaultWorldType, BeginWorldLoad);
    }

    public bool IsPlayerCollider(Collider other)
    {
        if (other == null || playerBody == null)
            return false;

        return other.GetComponentInParent<GravityBody>() == playerBody;
    }

    public void BeginWorldLoad(WorldType worldType, string seedText)
    {
        if (worldTransitionInProgress)
            return;

        StartCoroutine(LoadWorldRoutine(worldType, seedText));
    }

    public void SaveCurrentWorldState()
    {
        if (string.IsNullOrEmpty(activeWorldKey) || playerBody == null)
            return;

        WorldSaveData saveData = new WorldSaveData
        {
            worldKey = activeWorldKey,
            worldType = activeConfig != null ? activeConfig.worldType : defaultWorldType,
            seedText = activeSeedText ?? defaultSeed,
            playerPosition = SerializableVector3.From(playerBody.transform.position),
            playerRotation = SerializableQuaternion.From(playerBody.transform.rotation),
            playerHealth = playerHealth != null ? playerHealth.CurrentHealth : 0f
        };

        foreach (WorldPersistentObject persistentObject in FindObjectsOfType<WorldPersistentObject>(true))
        {
            if (persistentObject == null || string.IsNullOrEmpty(persistentObject.PersistentId))
                continue;

            saveData.objects.Add(new PersistentObjectSaveData
            {
                persistentId = persistentObject.PersistentId,
                kind = persistentObject.Kind,
                active = persistentObject.gameObject.activeSelf,
                position = SerializableVector3.From(persistentObject.transform.position),
                rotation = SerializableQuaternion.From(persistentObject.transform.rotation)
            });
        }

        WorldSaveService.Save(saveData);
    }

    private IEnumerator LoadWorldRoutine(WorldType worldType, string seedText)
    {
        worldTransitionInProgress = true;

        if (portalUi != null && portalUi.IsOpen)
            portalUi.Close();
        else
            WorldInputGate.SetUIOpen(false);

        EnsureGravityController();
        EnsurePlayerSetup();
        SaveCurrentWorldState();

        if (activeWorldRoot != null)
        {
            Destroy(activeWorldRoot);
            activeWorldRoot = null;
        }

        if (activeWorldScene.IsValid())
        {
            yield return SceneManager.UnloadSceneAsync(activeWorldScene);
        }

        activeSeedText = string.IsNullOrWhiteSpace(seedText) ? defaultSeed : seedText.Trim();
        activeConfig = WorldConfig.GetDefault(worldType);
        activeWorldKey = activeConfig.worldType + ":" + activeSeedText;
        activeSaveData = null;

        if (WorldSaveService.TryLoad(activeWorldKey, out WorldSaveData loadedSave))
        {
            activeSaveData = loadedSave;
        }

        activeWorldScene = SceneManager.CreateScene(BuildSceneName(activeWorldKey));
        SceneManager.SetActiveScene(activeWorldScene);

        activeWorldRoot = new GameObject("WorldRoot");
        SceneManager.MoveGameObjectToScene(activeWorldRoot, activeWorldScene);

        if (gravityController == null)
            gravityController = FindFirstObjectByType<GravityController>();

        GenerateWorld(activeWorldRoot.transform, activeConfig, activeSeedText);

        if (gravityController != null)
        {
            gravityController.gravityStrength = activeConfig.gravityStrength;
            gravityController.ApplyGravityStrengthToActiveBodies(activeConfig.gravityStrength);
        }

        ApplyWorldTuning();
        RestoreSavedWorldState();
        SpawnPlayerAtSafePosition();

        worldTransitionInProgress = false;
    }

    private void CacheSceneReferences()
    {
        gravityController = FindFirstObjectByType<GravityController>();
        playerBody = FindFirstObjectByType<GravityBody>();
        playerMovement = playerBody != null ? playerBody.GetComponent<PlayerMovement>() : null;
        playerHealth = playerBody != null ? playerBody.GetComponent<PlayerHealth>() : null;
    }

    private void EnsureGravityController()
    {
        if (gravityController != null)
            return;

        gravityController = FindFirstObjectByType<GravityController>();
        if (gravityController == null)
        {
            GameObject controllerObject = new GameObject("GameManager");
            gravityController = controllerObject.AddComponent<GravityController>();
            DontDestroyOnLoad(controllerObject);
        }
    }

    private void EnsurePlayerSetup()
    {
        if (playerBody == null)
        {
            playerBody = FindFirstObjectByType<GravityBody>();
        }

        if (playerBody != null)
        {
            playerMovement = playerBody.GetComponent<PlayerMovement>();
            playerHealth = playerBody.GetComponent<PlayerHealth>();
            if (playerHealth == null)
                playerHealth = playerBody.gameObject.AddComponent<PlayerHealth>();

            if (gravityController != null)
                gravityController.RegisterBody(playerBody);
        }
    }

    private void EnsurePortalUi()
    {
        if (portalUi != null)
            return;

        GameObject uiObject = new GameObject("PortalWorldUI");
        uiObject.transform.SetParent(transform, false);
        portalUi = uiObject.AddComponent<PortalWorldUI>();
    }

    private void ApplyWorldTuning()
    {
        if (playerMovement != null && activeConfig != null)
        {
            playerMovement.ApplyWorldTuning(activeConfig.moveSpeedMultiplier, activeConfig.jumpForceMultiplier);
        }
    }

    private void RestoreSavedWorldState()
    {
        activeObjects.Clear();

        foreach (WorldPersistentObject persistentObject in FindObjectsOfType<WorldPersistentObject>(true))
        {
            if (persistentObject == null || string.IsNullOrEmpty(persistentObject.PersistentId))
                continue;

            activeObjects[persistentObject.PersistentId] = persistentObject;
        }

        if (activeSaveData == null)
            return;

        if (playerBody != null)
        {
            playerBody.transform.SetPositionAndRotation(activeSaveData.playerPosition.ToVector3(), activeSaveData.playerRotation.ToQuaternion());
            Rigidbody rb = playerBody.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        if (playerHealth != null)
            playerHealth.SetHealth(activeSaveData.playerHealth);

        for (int i = 0; i < activeSaveData.objects.Count; i++)
        {
            PersistentObjectSaveData saved = activeSaveData.objects[i];
            if (!activeObjects.TryGetValue(saved.persistentId, out WorldPersistentObject runtimeObject) || runtimeObject == null)
                continue;

            runtimeObject.transform.SetPositionAndRotation(saved.position.ToVector3(), saved.rotation.ToQuaternion());
            runtimeObject.gameObject.SetActive(saved.active);
        }
    }

    private void SpawnPlayerAtSafePosition()
    {
        if (playerBody == null || activeConfig == null)
            return;

        if (activeSaveData != null)
            return;

        Vector3 center = new Vector3(0f, activeConfig.spawnSurfaceY + 1.5f, 10f);
        float offsetX = Random.Range(-activeConfig.spawnRadius, activeConfig.spawnRadius);
        float offsetZ = Random.Range(-activeConfig.spawnRadius, activeConfig.spawnRadius);
        Vector3 spawn = new Vector3(center.x + offsetX, center.y, center.z + offsetZ);
        playerBody.transform.position = spawn;
        playerBody.transform.rotation = Quaternion.identity;

        Rigidbody rb = playerBody.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void GenerateWorld(Transform root, WorldConfig config, string seedText)
    {
        Vector2 noiseOffset = WorldTypeUtility.GetNoiseOffset(config.worldType + ":" + seedText);
        System.Random random = new System.Random(WorldTypeUtility.GetStableSeed(config.worldType + ":" + seedText));

        if (Camera.main != null)
            Camera.main.backgroundColor = config.skyColor;

        BuildSpawnPlatform(root, config);
        SpawnPortal(root, config);

        int radius = config.worldRadiusInChunks;
        float chunkStride = config.chunkStride;

        for (int x = -radius; x <= radius; x++)
        {
            for (int z = -radius; z <= radius; z++)
            {
                if (x == 0 && z == 0)
                    continue;

                float densitySample = Mathf.PerlinNoise((x + noiseOffset.x) * 0.23f, (z + noiseOffset.y) * 0.23f);
                if (densitySample < config.platformDensity)
                    continue;

                int heightOffset = Mathf.RoundToInt((Mathf.PerlinNoise((x + noiseOffset.y) * 0.13f, (z + noiseOffset.x) * 0.13f) - 0.5f) * 4f);
                Vector3 chunkOrigin = new Vector3(x * chunkStride, config.spawnSurfaceY + heightOffset, z * chunkStride);
                SpawnChunk(root, config, chunkOrigin, random, x, z);

                TrySpawnCollectible(root, config, random, x, z, chunkOrigin);
                TrySpawnGravityProp(root, config, random, x, z, chunkOrigin);
            }
        }
    }

    private void BuildSpawnPlatform(Transform root, WorldConfig config)
    {
        float blockSize = config.blockSize;
        int half = Mathf.CeilToInt(config.spawnRadius);
        Vector3 origin = new Vector3(0f, config.spawnSurfaceY - blockSize * 0.5f, 10f);

        for (int x = -half; x <= half; x++)
        {
            for (int z = -half; z <= half; z++)
            {
                CreatePlatformBlock(root, origin + new Vector3(x * blockSize, 0f, z * blockSize), blockSize, config.platformColor, "SpawnFloor");
            }
        }
    }

    private void SpawnPortal(Transform root, WorldConfig config)
    {
        GameObject portal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        portal.name = "Portal";
        portal.transform.SetParent(root, false);
        portal.transform.position = new Vector3(6f, config.spawnSurfaceY + 1f, 10f);
        portal.transform.localScale = new Vector3(2f, 2.5f, 2f);

        Collider collider = portal.GetComponent<Collider>();
        collider.isTrigger = true;

        Renderer renderer = portal.GetComponent<Renderer>();
        renderer.sharedMaterial = BuildMaterial(config.accentColor);

        PortalInteractable interactable = portal.AddComponent<PortalInteractable>();
        interactable.name = "PortalInteractable";

        WorldPersistentObject persistentObject = portal.AddComponent<WorldPersistentObject>();
        persistentObject.Initialize(activeWorldKey + ":portal", PersistentObjectKind.GravityProp);
    }

    private void SpawnChunk(Transform root, WorldConfig config, Vector3 chunkOrigin, System.Random random, int cellX, int cellZ)
    {
        int patternIndex = Mathf.Abs((cellX * 92821) ^ (cellZ * 68917) ^ random.Next()) % config.chunkPatterns.Count;
        WorldConfig.ChunkPattern pattern = config.chunkPatterns[patternIndex];

        for (int i = 0; i < pattern.blockOffsets.Count; i++)
        {
            Vector3Int offset = pattern.blockOffsets[i];
            Vector3 position = chunkOrigin + new Vector3(offset.x * config.blockSize, offset.y * config.blockSize, offset.z * config.blockSize);
            CreatePlatformBlock(root, position, config.blockSize, config.platformColor, $"{pattern.name}_{i}");
        }
    }

    private void TrySpawnCollectible(Transform root, WorldConfig config, System.Random random, int cellX, int cellZ, Vector3 chunkOrigin)
    {
        if (random.NextDouble() > config.collectibleChance)
            return;

        GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pickup.name = $"HealthPickup_{cellX}_{cellZ}";
        pickup.transform.SetParent(root, false);
        pickup.transform.position = chunkOrigin + new Vector3(0f, config.blockSize * 1.25f, 0f);
        pickup.transform.localScale = Vector3.one * 0.75f;

        Renderer renderer = pickup.GetComponent<Renderer>();
        renderer.sharedMaterial = BuildMaterial(config.accentColor);

        Rigidbody rb = pickup.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;

        pickup.AddComponent<HealthPickup>();

        WorldPersistentObject persistentObject = pickup.AddComponent<WorldPersistentObject>();
        persistentObject.Initialize(activeWorldKey + $":pickup:{cellX}:{cellZ}", PersistentObjectKind.Pickup);
    }

    private void TrySpawnGravityProp(Transform root, WorldConfig config, System.Random random, int cellX, int cellZ, Vector3 chunkOrigin)
    {
        if (random.NextDouble() > config.gravityPropChance)
            return;

        GameObject prop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        prop.name = $"GravityProp_{cellX}_{cellZ}";
        prop.transform.SetParent(root, false);
        prop.transform.position = chunkOrigin + new Vector3(0f, config.blockSize * 1.75f, 0f);
        prop.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
        prop.layer = LayerMask.NameToLayer("GravityObject");

        Renderer renderer = prop.GetComponent<Renderer>();
        renderer.sharedMaterial = BuildMaterial(config.accentColor * 0.8f);

        Rigidbody rb = prop.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        GravityBody gravityBody = prop.AddComponent<GravityBody>();
        gravityBody.gravityStrength = config.gravityStrength;

        WorldPersistentObject persistentObject = prop.AddComponent<WorldPersistentObject>();
        persistentObject.Initialize(activeWorldKey + $":prop:{cellX}:{cellZ}", PersistentObjectKind.GravityProp);
    }

    private static void CreatePlatformBlock(Transform root, Vector3 position, float size, Color color, string name)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.SetParent(root, false);
        block.transform.position = position;
        block.transform.localScale = new Vector3(size, size, size);
        block.layer = LayerMask.NameToLayer("Ground");

        Renderer renderer = block.GetComponent<Renderer>();
        renderer.sharedMaterial = BuildMaterial(color);
    }

    private static Material BuildMaterial(Color color)
    {
        Color32 key = color;
        if (MaterialCache.TryGetValue(key, out Material cached) && cached != null)
            return cached;

        Shader shader = Shader.Find("Standard");
        Material material = new Material(shader);
        material.color = color;
        MaterialCache[key] = material;
        return material;
    }

    private static string BuildSceneName(string worldKey)
    {
        return "World_" + Sanitize(worldKey);
    }

    private static string Sanitize(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "World";

        char[] chars = value.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (!char.IsLetterOrDigit(chars[i]))
                chars[i] = '_';
        }

        return new string(chars);
    }
}
