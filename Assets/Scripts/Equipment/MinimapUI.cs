using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Circular radar minimap shown in the top-right corner when the Scout Drone is equipped.
///
/// ── UI hierarchy expected ──────────────────────────────────────────────────
/// MinimapRoot (this component + Image for radar background + Mask)
///   └── DotContainer (RectTransform, child of MinimapRoot)
///         └── (dot GameObjects are spawned here at runtime)
///
/// The radar background should be a circular sprite so the Mask clips dots
/// that would overshoot the edge. Set the background Image's Sprite to a
/// circle/disc sprite (or leave as default — dots are clamped anyway).
///
/// ── Dot colours ───────────────────────────────────────────────────────────
///   Enemy    → Red
///   Resource → Yellow
///   POI      → Cyan
///   Player   → White (always at center)
///
/// ── How positions work ────────────────────────────────────────────────────
/// Each scan from ScoutDrone calls UpdateTargets(). Dots are rebuilt at scan time.
/// Every frame, dot positions are recalculated so they rotate with the player.
/// The radar is player-forward-facing: the top of the circle = where you're looking.
/// </summary>
public class MinimapUI : MonoBehaviour
{
    public static MinimapUI Instance { get; private set; }

    [Header("Radar geometry")]
    [Tooltip("Pixel radius of the radar circle (half the width of the radar image).")]
    [SerializeField] private float minimapRadius = 70f;

    [Tooltip("World-unit radius that maps to minimapRadius. Match ScoutDrone.scanRadius.")]
    [SerializeField] private float worldRadius = 75f;

    [Header("References")]
    [Tooltip("Parent transform for the blip GameObjects (child of this).")]
    [SerializeField] private Transform dotContainer;

    [Header("Dot colours")]
    [SerializeField] private Color enemyColor    = Color.red;
    [SerializeField] private Color resourceColor = Color.yellow;
    [SerializeField] private Color poiColor      = Color.cyan;
    [SerializeField] private Color playerColor   = Color.white;
    [Tooltip("Size of each dot in pixels.")]
    [SerializeField] private float dotSize = 8f;

    // -------------------------------------------------------------------------
    // Runtime state
    // -------------------------------------------------------------------------

    private Transform playerTransform;
    private List<DroneTarget> currentTargets = new List<DroneTarget>();

    // Active blip images (parallel to currentTargets).
    private readonly List<Image> activeBlips = new List<Image>();

    // Player dot at the center (always visible).
    private Image playerBlip;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // The minimap lives under the HUD which is DDOL'd.
        // Hide by default — ScoutDroneEquipSystem shows it.
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        var player = GameObject.FindWithTag("Player");
        playerTransform = player != null ? player.transform : null;

        EnsurePlayerBlip();
    }

    private void Update()
    {
        if (playerTransform == null || currentTargets.Count == 0) return;
        UpdateBlipPositions();
    }

    // -------------------------------------------------------------------------
    // Public API — called by ScoutDrone
    // -------------------------------------------------------------------------

    public void UpdateTargets(List<DroneTarget> targets)
    {
        currentTargets = new List<DroneTarget>(targets);
        RebuildBlips();
    }

    // -------------------------------------------------------------------------
    // Blip management
    // -------------------------------------------------------------------------

    private void RebuildBlips()
    {
        // Destroy old blips.
        foreach (var blip in activeBlips)
            if (blip != null) Destroy(blip.gameObject);
        activeBlips.Clear();

        if (dotContainer == null) return;

        foreach (var target in currentTargets)
        {
            Color col = target.type == DroneTargetType.Enemy    ? enemyColor    :
                        target.type == DroneTargetType.Resource ? resourceColor :
                        poiColor;
            activeBlips.Add(CreateDot(col));
        }

        UpdateBlipPositions();
    }

    private void UpdateBlipPositions()
    {
        if (playerTransform == null) return;

        // Radar is player-forward-facing: rotate world offset by negative player yaw.
        float playerYaw = playerTransform.eulerAngles.y * Mathf.Deg2Rad;
        float sinY = Mathf.Sin(-playerYaw);
        float cosY = Mathf.Cos(-playerYaw);

        for (int i = 0; i < activeBlips.Count; i++)
        {
            if (activeBlips[i] == null || i >= currentTargets.Count) continue;

            Vector3 worldOffset = currentTargets[i].worldPosition - playerTransform.position;
            // Project onto the XZ plane (ignore height difference).
            Vector2 flat = new Vector2(worldOffset.x, worldOffset.z);
            float dist = flat.magnitude;

            // Normalise, scale to minimap space, clamp to edge.
            float scale = Mathf.Min(dist / worldRadius, 1f) * minimapRadius;
            Vector2 dir = dist > 0.01f ? flat.normalized : Vector2.up;

            // Rotate by player yaw so forward is always "up" on the radar.
            Vector2 rotated = new Vector2(
                dir.x * cosY - dir.y * sinY,
                dir.x * sinY + dir.y * cosY) * scale;

            activeBlips[i].rectTransform.anchoredPosition = rotated;
        }
    }

    private void EnsurePlayerBlip()
    {
        if (playerBlip != null) return;
        if (dotContainer == null) return;
        playerBlip = CreateDot(playerColor);
        playerBlip.rectTransform.anchoredPosition = Vector2.zero; // always at center
        // Make player dot slightly larger to distinguish it.
        playerBlip.rectTransform.sizeDelta = Vector2.one * (dotSize * 1.5f);
    }

    private Image CreateDot(Color color)
    {
        var go = new GameObject("Blip", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(dotContainer, false);

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = Vector2.one * dotSize;
        rt.anchoredPosition = Vector2.zero;

        var img = go.GetComponent<Image>();
        img.color = color;
        // Use a circle sprite if one is available; otherwise the default Unity square works.
        img.raycastTarget = false;

        return img;
    }
}
