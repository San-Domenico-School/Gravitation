using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach this component to the Player root GameObject (the one tagged "Player").
/// Marks the player <c>DontDestroyOnLoad</c> so the same Player + camera + gravity gun + battery
/// survives scene transitions instead of needing to respawn in every scene.
///
/// Behavior on scene load:
///   1. If a Player tagged duplicate exists in the destination scene (e.g. you forgot to
///      remove it after authoring), it gets destroyed so the persistent Player wins.
///   2. The persistent player teleports to the destination's spawn point. The chosen
///      spawn point comes from <see cref="PendingSpawnPointName"/> (set by
///      <see cref="SceneTransitionTrigger"/>) or falls back to <see cref="defaultSpawnPointName"/>.
///   3. If the SaveManager is currently restoring from disk, the teleport is skipped —
///      the SaveManager owns the player position in that case.
/// </summary>
public class PlayerPersistence : MonoBehaviour
{
    public static PlayerPersistence Instance { get; private set; }

    /// <summary>
    /// Set by SceneTransitionTrigger before <c>SceneManager.LoadScene(...)</c>.
    /// PlayerPersistence reads it on the next sceneLoaded and teleports there. Cleared after use.
    /// </summary>
    public static string PendingSpawnPointName;

    [Tooltip("Spawn point GameObject name to look for in each scene. The transition trigger can override this per-trigger.")]
    [SerializeField] private string defaultSpawnPointName = "PlayerSpawnpoint";

    [Tooltip("Tag the player uses. Duplicates with this tag in destination scenes will be destroyed.")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("If true, zero out the Rigidbody's velocity after teleporting. Recommended.")]
    [SerializeField] private bool zeroVelocityOnTeleport = true;

    private Rigidbody rb;
    private GravityBody gravityBody;

    private void Awake()
    {
        // Singleton — destroy any duplicate Player that comes in via a scene-baked one.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Player must be a root GameObject for DDOL to work cleanly.
        // (The Player root in your scene should already be a root — its camera is a child.)
        DontDestroyOnLoad(gameObject);

        rb = GetComponent<Rigidbody>();
        gravityBody = GetComponent<GravityBody>();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Additive) return;

        // 1. Destroy any duplicate Player tagged objects in the new scene.
        var duplicates = GameObject.FindGameObjectsWithTag(playerTag);
        foreach (var p in duplicates)
        {
            if (p != gameObject) Destroy(p);
        }

        // 2. If the SaveManager is restoring the player from a save file, don't fight it.
        if (SaveManager.Instance != null && SaveManager.Instance.IsRestoringFromDisk) return;

        // 3. Teleport to the spawn point requested by the trigger, or the scene's default.
        string targetName = !string.IsNullOrEmpty(PendingSpawnPointName)
            ? PendingSpawnPointName
            : defaultSpawnPointName;
        PendingSpawnPointName = null;

        if (string.IsNullOrEmpty(targetName)) return;

        GameObject spawn = GameObject.Find(targetName);
        if (spawn == null)
        {
            Debug.LogWarning($"[PlayerPersistence] Spawn point '{targetName}' not found in scene '{scene.name}'. Player stays at last position. Add an empty GameObject named '{targetName}' to the scene to fix.");
            return;
        }

        TeleportTo(spawn.transform.position, spawn.transform.rotation);
    }

    /// <summary>
    /// Public teleport helper — clears velocity and re-aligns gravity-rotation.
    /// </summary>
    public void TeleportTo(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
        if (zeroVelocityOnTeleport && rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        if (gravityBody != null)
        {
            // Re-apply current gravity direction so the player aligns immediately at the new spot.
            gravityBody.SetGravity(gravityBody.gravityDirection, gravityBody.gravityStrength);
        }
    }
}
