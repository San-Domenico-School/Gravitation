using System;
using UnityEngine;

/// <summary>
/// A glowing crack in the ground spawned by the Gravity Pulse.
/// Holds the resource it yields and its glow color. Switches to a dim
/// "spent" state on extraction and regenerates after a delay.
///
/// The visual is a flat Quad with a runtime-generated procedural crack texture
/// + URP transparent material. No imported assets required, but you can
/// override the material/mesh in the prefab if you want a custom look.
/// </summary>
public class ResourceCrack : MonoBehaviour
{
    [Header("Visual")]
    [Tooltip("Optional override material. If unset, a procedural transparent material is built at runtime.")]
    [SerializeField] private Material overrideMaterial;
    [Tooltip("World-space size of the crack quad (square).")]
    [SerializeField] private float crackSize = 1.6f;
    [Tooltip("Distance lifted above the surface to avoid z-fighting.")]
    [SerializeField] private float surfaceLift = 0.02f;

    [Header("Glow Intensity")]
    [Tooltip("Multiplier applied to glowColor when the crack is active.")]
    [SerializeField] private float activeIntensity = 3f;
    [Tooltip("Multiplier applied to glowColor when the crack has been extracted (regenerating).")]
    [SerializeField] private float dimIntensity = 0.25f;

    [Header("Regeneration")]
    [Tooltip("Seconds after extraction before the crack glows again and can be re-extracted.")]
    [SerializeField] private float regenSeconds = 30f;

    [Header("Collision")]
    [Tooltip("Optional collider used as the extractor's hit target. Auto-added if missing.")]
    [SerializeField] private BoxCollider hitCollider;

    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private Material runtimeMaterial;
    private Texture2D runtimeTexture;

    private ItemData resource;
    private Color glowColor;
    private BiomeType biome;
    private bool isActive = true;
    private float regenStartTime;

    public ItemData Resource => resource;
    public Color GlowColor => glowColor;
    public BiomeType Biome => biome;
    public bool CanExtract => isActive && resource != null;
    public bool IsActive => isActive;
    public float RegenSeconds => regenSeconds;
    /// <summary>Seconds left on the regen timer, or 0 if currently active.</summary>
    public float RegenRemaining => isActive ? 0f : Mathf.Max(0f, regenSeconds - (Time.time - regenStartTime));
    public float RegenProgress => isActive ? 1f : Mathf.Clamp01((Time.time - regenStartTime) / Mathf.Max(0.001f, regenSeconds));

    public event Action<ResourceCrack> OnExtracted;
    public event Action<ResourceCrack> OnRegenerated;

    public void Initialize(ItemData resource, Color glowColor, BiomeType biome)
    {
        this.resource = resource;
        this.glowColor = glowColor;
        this.biome = biome;
        BuildVisualIfNeeded();
        ApplyState(true);
    }

    /// <summary>
    /// Save-system entry point: initialize the crack and force it into a specific
    /// active/regenerating state with a specific remaining regen time.
    /// </summary>
    public void RestoreFromSave(ItemData resource, Color glowColor, BiomeType biome, bool active, float regenRemaining)
    {
        this.resource = resource;
        this.glowColor = glowColor;
        this.biome = biome;
        BuildVisualIfNeeded();
        if (active)
        {
            ApplyState(true);
        }
        else
        {
            // Time.time - regenStartTime should equal (regenSeconds - regenRemaining)
            regenStartTime = Time.time - Mathf.Max(0f, regenSeconds - regenRemaining);
            ApplyState(false);
        }
    }

    public bool TryExtract(int amount, out int actuallyAdded)
    {
        actuallyAdded = 0;
        if (!CanExtract) return false;
        if (InventorySystem.Instance == null) return false;
        if (amount < 1) amount = 1;

        for (int i = 0; i < amount; i++)
        {
            if (!InventorySystem.Instance.TryAddItem(new InventoryItem(resource)))
            {
                if (PickupPromptUI.Instance != null) PickupPromptUI.ShowMessage("Inventory full.");
                break;
            }
            actuallyAdded++;
        }

        if (actuallyAdded == 0) return false;

        regenStartTime = Time.time;
        ApplyState(false);
        OnExtracted?.Invoke(this);
        return true;
    }

    private void Update()
    {
        if (isActive) return;
        if (Time.time - regenStartTime >= regenSeconds)
        {
            ApplyState(true);
            OnRegenerated?.Invoke(this);
        }
    }

    private void ApplyState(bool active)
    {
        isActive = active;
        if (runtimeMaterial == null) return;
        float intensity = active ? activeIntensity : dimIntensity;
        Color tinted = new Color(glowColor.r * intensity, glowColor.g * intensity, glowColor.b * intensity, 1f);
        CrackVisuals.ApplyColor(runtimeMaterial, tinted);
    }

    private void BuildVisualIfNeeded()
    {
        if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();

        if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();
        if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();

        if (meshFilter.sharedMesh == null)
            meshFilter.sharedMesh = BuildQuadMesh(crackSize);

        if (overrideMaterial != null)
        {
            runtimeMaterial = new Material(overrideMaterial);
        }
        else
        {
            runtimeTexture = CrackVisuals.GenerateCrackTexture(256, 7, UnityEngine.Random.Range(1, int.MaxValue));
            runtimeMaterial = CrackVisuals.CreateGlowMaterial(glowColor, runtimeTexture);
        }
        meshRenderer.sharedMaterial = runtimeMaterial;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        if (hitCollider == null) hitCollider = GetComponent<BoxCollider>();
        if (hitCollider == null) hitCollider = gameObject.AddComponent<BoxCollider>();
        hitCollider.isTrigger = true;
        hitCollider.size = new Vector3(crackSize, 0.1f, crackSize);
        hitCollider.center = new Vector3(0f, 0.05f, 0f);
    }

    private static Mesh BuildQuadMesh(float size)
    {
        float h = size * 0.5f;
        var m = new Mesh { name = "CrackQuad" };
        m.vertices = new[]
        {
            new Vector3(-h, 0f, -h),
            new Vector3( h, 0f, -h),
            new Vector3( h, 0f,  h),
            new Vector3(-h, 0f,  h),
        };
        m.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
        };
        m.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
        m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        m.RecalculateBounds();
        return m;
    }

    public float SurfaceLift => surfaceLift;
    public float CrackSize => crackSize;

    private void OnDestroy()
    {
        if (runtimeMaterial != null) Destroy(runtimeMaterial);
        if (runtimeTexture != null)  Destroy(runtimeTexture);
    }
}
