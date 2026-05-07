using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD for the Resource Extractor. Shows a progress bar while the player
/// holds the extract input on a crack. Fades out when extraction stops.
/// </summary>
public class ExtractorProgressUI : MonoBehaviour
{
    [Tooltip("ResourceExtractor whose progress this UI displays.")]
    [SerializeField] private ResourceExtractor extractor;

    [Tooltip("CanvasGroup used to fade the progress UI in and out.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Tooltip("Image used as the fill bar. Set its Image Type to Filled, Horizontal.")]
    [SerializeField] private Image fillBar;

    [Tooltip("Optional label shown above the bar (e.g., the resource name).")]
    [SerializeField] private TextMeshProUGUI label;

    [SerializeField] private float fadeDuration = 0.15f;

    private float targetAlpha;

    private void OnEnable()
    {
        if (extractor == null) return;
        extractor.OnExtractStarted    += HandleStarted;
        extractor.OnExtractCanceled   += HandleCanceled;
        extractor.OnExtractCompleted  += HandleCompleted;
        extractor.OnProgressChanged   += HandleProgress;
    }

    private void OnDisable()
    {
        if (extractor == null) return;
        extractor.OnExtractStarted    -= HandleStarted;
        extractor.OnExtractCanceled   -= HandleCanceled;
        extractor.OnExtractCompleted  -= HandleCompleted;
        extractor.OnProgressChanged   -= HandleProgress;
    }

    private void Start()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        targetAlpha = 0f;
        if (fillBar != null) fillBar.fillAmount = 0f;
    }

    private void HandleStarted(ResourceExtractor _)
    {
        targetAlpha = 1f;
        if (label != null && extractor.CurrentTarget != null && extractor.CurrentTarget.Resource != null)
            label.text = $"Extracting {extractor.CurrentTarget.Resource.itemName}";
    }

    private void HandleCanceled(ResourceExtractor _)
    {
        targetAlpha = 0f;
        if (fillBar != null) fillBar.fillAmount = 0f;
    }

    private void HandleCompleted(ResourceExtractor _, ResourceCrack crack, int amount)
    {
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
