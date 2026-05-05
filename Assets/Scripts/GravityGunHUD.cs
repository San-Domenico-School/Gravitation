using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Self-building HUD that shows the gun's current mode, installed cell, and any
/// active T4 context (lock status, placement strength).
///
/// SETUP: Add this component to the same GameObject as GravityGun. Nothing else needed.
/// </summary>
[RequireComponent(typeof(GravityGun))]
public class GravityGunHUD : MonoBehaviour
{
    // ── Mode colours ──────────────────────────────────────────────────────────
    private static readonly Color C_SELECTION   = new Color(0.85f, 0.85f, 0.85f, 1f);
    private static readonly Color C_PLACEMENT   = new Color(0.35f, 0.85f, 1.00f, 1f);
    private static readonly Color C_PULSE       = new Color(1.00f, 0.65f, 0.10f, 1f);
    private static readonly Color C_LOCK        = new Color(0.40f, 1.00f, 0.55f, 1f);
    private static readonly Color C_LOCK_ACTIVE = new Color(1.00f, 0.35f, 0.35f, 1f);
    private static readonly Color C_FLIP        = new Color(0.90f, 0.35f, 1.00f, 1f);
    private static readonly Color C_DIM         = new Color(1.00f, 1.00f, 1.00f, 0.55f);

    // ── Position settings (editable in Inspector) ────────────────────────────
    public enum ScreenCorner { BottomLeft, BottomRight, TopLeft, TopRight }

    [Header("Position")]
    [Tooltip("Which corner of the screen to anchor the HUD to.")]
    [SerializeField] private ScreenCorner corner = ScreenCorner.BottomRight;

    [Tooltip("Pixel offset from the chosen corner (scaled with screen resolution).")]
    [SerializeField] private Vector2 offset = new Vector2(-32, 72);

    // ── Runtime refs ──────────────────────────────────────────────────────────
    private GravityGun gun;
    private GunBatterySystem battery;

    // ── UI nodes ──────────────────────────────────────────────────────────────
    private GameObject canvasRoot;
    private RectTransform panelRT;
    private TextMeshProUGUI modeLabel;
    private TextMeshProUGUI cellLabel;
    private TextMeshProUGUI lockBadge;
    private TextMeshProUGUI strengthLabel;
    private Image accentBar;

    private void OnValidate()
    {
        // Allows live tweaking of corner/offset while the game is running.
        if (panelRT != null) ApplyPosition();
    }

    private void Awake()
    {
        gun     = GetComponent<GravityGun>();
        battery = GetComponent<GunBatterySystem>();
        BuildUI();
    }

    // Named handlers so -= unsubscription works correctly.
    private void OnModeChangedHandler(GravityGun.Mode _) => Refresh();
    private void OnTierChangedHandler(int _)             => Refresh();
    private void OnChargeChangedHandler(float _, float __) => RefreshCell();

    private void Start()
    {
        gun.OnModeChanged          += OnModeChangedHandler;
        gun.OnEffectiveTierChanged += OnTierChangedHandler;
        gun.OnEquippedChanged      += OnEquippedChanged;
        gun.OnStrengthChanged      += OnStrengthChangedHandler;
        if (battery != null) battery.OnChargeChanged += OnChargeChangedHandler;

        // Start hidden; becomes visible the first time SetEquipped(true) fires.
        SetVisible(false);
    }

    private void OnStrengthChangedHandler(float _) => RefreshStrength();

    private void OnDestroy()
    {
        if (gun != null)
        {
            gun.OnModeChanged          -= OnModeChangedHandler;
            gun.OnEffectiveTierChanged -= OnTierChangedHandler;
            gun.OnEquippedChanged      -= OnEquippedChanged;
            gun.OnStrengthChanged      -= OnStrengthChangedHandler;
        }
        if (battery != null) battery.OnChargeChanged -= OnChargeChangedHandler;
        if (canvasRoot != null) Destroy(canvasRoot);
    }

    // ── Visibility ────────────────────────────────────────────────────────────

    private void OnEquippedChanged(bool equipped)
    {
        SetVisible(equipped);
        if (equipped) Refresh();
    }

    private void SetVisible(bool show) { if (canvasRoot != null) canvasRoot.SetActive(show); }

    // ── Refresh ───────────────────────────────────────────────────────────────

    private void Refresh()
    {
        RefreshMode();
        RefreshCell();
        RefreshStrength();
    }

    private void RefreshMode()
    {
        bool lockActive = gun.IsLockActive;
        GravityGun.Mode mode = gun.CurrentMode;

        // Mode label text and colour.
        modeLabel.text  = ModeText(mode, lockActive);
        modeLabel.color = ModeColour(mode, lockActive);

        // Left accent bar colour mirrors the mode colour.
        if (accentBar != null)
            accentBar.color = ModeColour(mode, lockActive);

        // Lock badge — shown only in LockMode while engaged.
        lockBadge.gameObject.SetActive(mode == GravityGun.Mode.LockMode && lockActive);
    }

    private void RefreshCell()
    {
        if (battery != null && battery.CurrentCell != null)
        {
            var c = battery.CurrentCell;
            cellLabel.text = $"T{c.Tier}  ·  {c.CellName.ToUpper()}";
        }
        else
        {
            cellLabel.text = "NO CELL";
        }
    }

    private void RefreshStrength()
    {
        bool show = gun.CurrentMode == GravityGun.Mode.GravityPlacement && gun.EffectiveTier >= 4;
        strengthLabel.gameObject.SetActive(show);
        if (show) strengthLabel.text = $"STRENGTH  {gun.PlacementStrength:F1}";
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string ModeText(GravityGun.Mode mode, bool lockActive) => mode switch
    {
        GravityGun.Mode.Selection        => "SELECTION",
        GravityGun.Mode.GravityPlacement => "PLACEMENT",
        GravityGun.Mode.PulseMode        => "PULSE",
        GravityGun.Mode.LockMode         => lockActive ? "LOCK  ●" : "LOCK",
        GravityGun.Mode.FlipMode         => "FLIP",
        _                                => mode.ToString().ToUpper()
    };

    private static Color ModeColour(GravityGun.Mode mode, bool lockActive) => mode switch
    {
        GravityGun.Mode.Selection        => C_SELECTION,
        GravityGun.Mode.GravityPlacement => C_PLACEMENT,
        GravityGun.Mode.PulseMode        => C_PULSE,
        GravityGun.Mode.LockMode         => lockActive ? C_LOCK_ACTIVE : C_LOCK,
        GravityGun.Mode.FlipMode         => C_FLIP,
        _                                => Color.white
    };

    // ── Canvas builder ────────────────────────────────────────────────────────

    private void ApplyPosition()
    {
        Vector2 anchor = corner switch
        {
            ScreenCorner.BottomLeft  => new Vector2(0, 0),
            ScreenCorner.BottomRight => new Vector2(1, 0),
            ScreenCorner.TopLeft     => new Vector2(0, 1),
            ScreenCorner.TopRight    => new Vector2(1, 1),
            _                        => new Vector2(1, 0)
        };

        panelRT.anchorMin        = anchor;
        panelRT.anchorMax        = anchor;
        panelRT.pivot            = anchor;
        panelRT.anchoredPosition = offset;
    }

    private void BuildUI()
    {
        // ── Root canvas (screen-space overlay, non-interactive) ───────────────
        canvasRoot = new GameObject("GravityGunHUD");

        var cv = canvasRoot.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 20;

        var scaler = canvasRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        // Non-interactive: don't block gameplay clicks.
        var cg = canvasRoot.AddComponent<CanvasGroup>();
        cg.interactable   = false;
        cg.blocksRaycasts = false;

        // ── Outer panel (auto-height via ContentSizeFitter) ───────────────────
        var panel = NewGO("HUD_Panel", canvasRoot.transform);
        panelRT = panel.GetComponent<RectTransform>();
        ApplyPosition();
        panelRT.sizeDelta        = new Vector2(300, 0);   // width fixed; height driven by content

        var panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0.04f, 0.04f, 0.09f, 0.82f);

        // Vertical layout — children stack top to bottom, panel shrinks to fit.
        var vl = panel.AddComponent<VerticalLayoutGroup>();
        vl.padding                  = new RectOffset(20, 16, 14, 14);
        vl.spacing                  = 3;
        vl.childForceExpandWidth    = true;
        vl.childForceExpandHeight   = false;
        vl.childControlWidth        = true;
        vl.childControlHeight       = true;

        var csf = panel.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ── Coloured left accent bar ───────────────────────────────────────────
        var bar = NewGO("AccentBar", panel.transform);
        var barRT = bar.GetComponent<RectTransform>();
        // Position it as a sibling overlay: absolute left edge, full panel height.
        // We achieve this by making it a child of the panel but outside the layout
        // via LayoutElement.ignoreLayout.
        barRT.anchorMin  = new Vector2(0, 0);
        barRT.anchorMax  = new Vector2(0, 1);
        barRT.pivot      = new Vector2(0, 0);
        barRT.offsetMin  = Vector2.zero;
        barRT.offsetMax  = new Vector2(4, 0);

        accentBar = bar.AddComponent<Image>();
        accentBar.color = C_SELECTION;

        var barLE = bar.AddComponent<LayoutElement>();
        barLE.ignoreLayout = true;   // don't participate in vertical stacking

        // ── Labels ────────────────────────────────────────────────────────────
        modeLabel     = MakeLabel(panel.transform, "SELECTION",    24, FontStyles.Bold,   C_SELECTION);
        cellLabel     = MakeLabel(panel.transform, "T1  ·  CELL",  13, FontStyles.Normal, C_DIM);
        lockBadge     = MakeLabel(panel.transform, "●  LOCK ACTIVE", 13, FontStyles.Bold, C_LOCK_ACTIVE);
        strengthLabel = MakeLabel(panel.transform, "STRENGTH  9.8", 13, FontStyles.Normal, C_PLACEMENT);

        lockBadge.gameObject.SetActive(false);
        strengthLabel.gameObject.SetActive(false);
    }

    // Creates a child GameObject with a RectTransform.
    private static GameObject NewGO(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    // Creates a label that participates in the VerticalLayoutGroup.
    private static TextMeshProUGUI MakeLabel(Transform parent, string defaultText, float size, FontStyles style, Color colour)
    {
        var go = NewGO("Label", parent);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, size + 8);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text               = defaultText;
        tmp.fontSize           = size;
        tmp.fontStyle          = style;
        tmp.color              = colour;
        tmp.overflowMode       = TextOverflowModes.Ellipsis;
        tmp.enableWordWrapping = false;

        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = size + 8;
        le.flexibleWidth   = 1;

        return tmp;
    }
}
