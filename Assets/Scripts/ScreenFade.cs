using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Provides a full-screen fade utility usable by other game systems.
/// </summary>
public class ScreenFade : MonoBehaviour
{
    public static ScreenFade Instance { get; private set; }

    [SerializeField]
    [Tooltip("Default fade duration in seconds.")]
    private float defaultFadeDuration = 1f;

    private Canvas fadeCanvas;
    private Image fadeImage;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CreateFadeCanvas();
    }

    private void CreateFadeCanvas()
    {
        GameObject canvasObject = new GameObject("ScreenFadeCanvas");
        canvasObject.transform.SetParent(transform);

        fadeCanvas = canvasObject.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 999;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject panelObject = new GameObject("FadePanel");
        panelObject.transform.SetParent(canvasObject.transform);

        fadeImage = panelObject.AddComponent<Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        fadeImage.raycastTarget = false;

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
    }

    public void FadeOut(float duration, Action onComplete)
    {
        if (fadeImage == null)
            return;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeRoutine(fadeImage.color.a, 1f, duration, onComplete));
    }

    public void FadeIn(float duration)
    {
        if (fadeImage == null)
            return;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeRoutine(fadeImage.color.a, 0f, duration, null));
    }

    private IEnumerator FadeRoutine(float fromAlpha, float toAlpha, float duration, Action onComplete)
    {
        float elapsed = 0f;
        Color color = fadeImage.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            color.a = Mathf.Lerp(fromAlpha, toAlpha, t);
            fadeImage.color = color;
            yield return null;
        }

        color.a = toAlpha;
        fadeImage.color = color;
        fadeCoroutine = null;

        onComplete?.Invoke();
    }
}
