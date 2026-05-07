using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD for the Resource Extractor. Shows a progress bar while the player
/// holds the extract input on a crack. Fades out when extraction stops.
/// Listens to every ResourceExtractor in the scene by default so it follows
/// the player from T1 to T2 (or any future tier) without rewiring.
/// </summary>
public class ExtractorProgressUI : MonoBehaviour
{
    [Tooltip("Optional: pin this UI to a single extractor. Leave empty to auto-discover every ResourceExtractor in the scene and follow whichever is active.")]
    [SerializeField] private ResourceExtractor extractor;

    [Tooltip("CanvasGroup used to fade the progress UI in and out.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Tooltip("Image used as the fill bar. Set its Image Type to Filled, Horizontal.")]
    [SerializeField] private Image fillBar;

    [Tooltip("Optional label shown above the bar (e.g., the resource name).")]
    [SerializeField] private TextMeshProUGUI label;

    [SerializeField] private float fadeDuration = 0.15f;

    private float targetAlpha;
    private ResourceExtractor activeExtractor;
    private readonly List<ResourceExtractor> bound = new List<ResourceExtractor>(4);

    private void OnEnable()
    {
        Bind();
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void Start()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        targetAlpha = 0f;
        if (fillBar != null) fillBar.fillAmount = 0f;
    }

    /// <summary>
    /// Re-discovers and re-subscribes. Call this if extractors are
    /// instantiated after scene start (e.g., spawned with the player rig).
    /// </summary>
    public void Rebind()
    {
        Unbind();
        Bind();
    }

    private void Bind()
    {
        if (extractor != null)
        {
            Subscribe(extractor);
            return;
        }

#if UNITY_2022_2_OR_NEWER
        ResourceExtractor[] all = Object.FindObjectsByType<ResourceExtractor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        ResourceExtractor[] all = Object.FindObjectsOfType<ResourceExtractor>(true);
#endif
        for (int i = 0; i < all.Length; i++) Subscribe(all[i]);
    }

    private void Subscribe(ResourceExtractor ex)
    {
        if (ex == null || bound.Contains(ex)) return;
        ex.OnExtractStarted   += HandleStarted;
        ex.OnExtractCanceled  += HandleCanceled;
        ex.OnExtractCompleted += HandleCompleted;
        ex.OnProgressChanged  += HandleProgress;
        bound.Add(ex);
    }

    private void Unbind()
    {
        for (int i = 0; i < bound.Count; i++)
        {
            ResourceExtractor ex = bound[i];
            if (ex == null) continue;
            ex.OnExtractStarted   -= HandleStarted;
            ex.OnExtractCanceled  -= HandleCanceled;
            ex.OnExtractCompleted -= HandleCompleted;
            ex.OnProgressChanged  -= HandleProgress;
        }
        bound.Clear();
        activeExtractor = null;
    }

    private void HandleStarted(ResourceExtractor source)
    {
        activeExtractor = source;
        targetAlpha = 1f;
        if (label != null && source != null && source.CurrentTarget != null && source.CurrentTarget.Resource != null)
            label.text = $"Extracting {source.CurrentTarget.Resource.itemName}";
    }

    private void HandleCanceled(ResourceExtractor source)
    {
        if (activeExtractor != null && source != activeExtractor) return;
        activeExtractor = null;
        targetAlpha = 0f;
        if (fillBar != null) fillBar.fillAmount = 0f;
    }

    private void HandleCompleted(ResourceExtractor source, ResourceCrack crack, int amount)
    {
        if (activeExtractor != null && source != activeExtractor) return;
        activeExtractor = null;
        targetAlpha = 0f;
        if (fillBar != null) fillBar.fillAmount = 1f;
        if (crack != null && crack.Resource != null)
            PickupPromptUI.ShowMessage($"+{amount} {crack.Resource.itemName}", 1.5f);
    }

    private void HandleProgress(float progress01)
    {
        if (fillBar != null) fillBar.fillAmount = progress01;
    }

    private void Update()
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.unscaledDeltaTime / Mathf.Max(0.001f, fadeDuration));
    }
}
