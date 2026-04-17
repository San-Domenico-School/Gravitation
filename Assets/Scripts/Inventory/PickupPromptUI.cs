using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PickupPromptUI : MonoBehaviour
{
    public static PickupPromptUI Instance { get; private set; }

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private float fadeDuration = 0.2f;

    private readonly List<WorldItem> trackedItems = new List<WorldItem>();
    private static string pendingMessage;
    private float messageTimer;
    private float targetAlpha;
    private bool showingMessage;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    private void OnEnable()
    {
        WorldItem.OnItemInRange += OnItemInRange;
        WorldItem.OnItemOutOfRange += OnItemOutOfRange;
    }

    private void OnDisable()
    {
        WorldItem.OnItemInRange -= OnItemInRange;
        WorldItem.OnItemOutOfRange -= OnItemOutOfRange;
    }

    private void OnItemInRange(WorldItem item)
    {
        if (!trackedItems.Contains(item)) trackedItems.Add(item);
        Refresh();
    }

    private void OnItemOutOfRange(WorldItem item)
    {
        trackedItems.Remove(item);
        Refresh();
    }

    private void Refresh()
    {
        if (showingMessage) return;

        WorldItem closest = GetClosest();
        if (closest != null)
        {
            promptText.text = $"Press E to pick up {closest.itemData.itemName}";
            targetAlpha = 1f;
        }
        else
        {
            targetAlpha = 0f;
        }
    }

    private WorldItem GetClosest()
    {
        WorldItem best = null;
        float minDist = float.MaxValue;
        for (int i = trackedItems.Count - 1; i >= 0; i--)
        {
            if (trackedItems[i] == null) { trackedItems.RemoveAt(i); continue; }
            float d = trackedItems[i].DistanceToPlayer();
            if (d < minDist) { minDist = d; best = trackedItems[i]; }
        }
        return best;
    }

    private void Update()
    {
        if (showingMessage)
        {
            messageTimer -= Time.unscaledDeltaTime;
            if (messageTimer <= 0f)
            {
                showingMessage = false;
                Refresh();
            }
        }

        if (canvasGroup != null)
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.unscaledDeltaTime / fadeDuration);
    }

    public static void ShowMessage(string message, float duration = 2f)
    {
        if (Instance == null) return;
        Instance.promptText.text = message;
        Instance.targetAlpha = 1f;
        Instance.showingMessage = true;
        Instance.messageTimer = duration;
        if (Instance.canvasGroup != null) Instance.canvasGroup.alpha = 1f;
    }
}
