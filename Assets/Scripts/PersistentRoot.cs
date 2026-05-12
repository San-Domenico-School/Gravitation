using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic "make this GameObject persist across scenes" helper for things that aren't
/// already singletons (like a HUD Canvas that hosts <c>HotbarUI</c>, <c>HealthHUD</c>,
/// <c>BatteryHUD</c>, <c>GravityGunHUD</c>, etc).
///
/// Identify the root with a unique <c>id</c> string. The first one with a given ID
/// survives; later duplicates (i.e. the canvas re-authored in a biome scene) are
/// destroyed entirely.
///
/// Usage: drop this on the root of a HUD Canvas or any persistent UI hierarchy.
/// Set <c>id = "HUD"</c> (or whatever). Done.
/// </summary>
public class PersistentRoot : MonoBehaviour
{
    [Tooltip("Unique identifier. Two PersistentRoots with the same id are treated as duplicates — only the first survives.")]
    [SerializeField] private string id;

    private static readonly Dictionary<string, PersistentRoot> registry = new Dictionary<string, PersistentRoot>();

    private void Awake()
    {
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning($"[PersistentRoot] '{name}' has no id set. Pick a unique string.", this);
            return;
        }

        // Sanity check: DDOL only works on root GameObjects. If we have a parent, warn loudly.
        if (transform.parent != null)
        {
            Debug.LogError($"[PersistentRoot id='{id}'] '{name}' is NOT a scene root (parent: '{transform.parent.name}'). DontDestroyOnLoad will not work. Move this GameObject to the top of the hierarchy.", this);
            return;
        }

        if (registry.TryGetValue(id, out var existing) && existing != null && existing != this)
        {
            // Duplicate — the original wins. Destroy this duplicate.
            Debug.Log($"[PersistentRoot id='{id}'] Destroying duplicate '{name}' in scene '{gameObject.scene.name}'. Original from '{existing.gameObject.scene.name}' wins.", this);
            Destroy(gameObject);
            return;
        }

        registry[id] = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log($"[PersistentRoot id='{id}'] DDOL applied to '{name}' from scene '{gameObject.scene.name}'.", this);
    }

    private void OnDestroy()
    {
        if (!string.IsNullOrEmpty(id) && registry.TryGetValue(id, out var existing) && existing == this)
        {
            registry.Remove(id);
        }
    }
}
