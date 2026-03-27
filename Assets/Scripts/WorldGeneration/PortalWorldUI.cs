using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class PortalWorldUI : MonoBehaviour
{
    private const int PanelWidth = 520;
    private const int PanelHeight = 300;

    private Action<WorldType, string> confirmHandler;

    private Canvas canvas;
    private GameObject rootPanel;
    private InputField seedInput;
    private Text worldTypeLabel;
    private Text statusText;
    private Button confirmButton;
    private Button cancelButton;
    private bool isOpen;
    private WorldType selectedWorldType = WorldType.Stone;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        BuildUI();
        Close();
    }

    private void Update()
    {
        if (!isOpen)
            return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Close();
            return;
        }

        if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
        {
            Confirm();
        }
    }

    public void Open(string seedText, WorldType worldType, Action<WorldType, string> onConfirm)
    {
        confirmHandler = onConfirm;
        seedInput.text = string.IsNullOrWhiteSpace(seedText) ? "default" : seedText.Trim();
        selectedWorldType = worldType;
        RefreshWorldTypeLabel();
        canvas.gameObject.SetActive(true);
        rootPanel.SetActive(true);
        isOpen = true;
        WorldInputGate.SetUIOpen(true);
        statusText.text = "Right-click the portal to open this panel. Enter confirms, Escape cancels.";
        seedInput.ActivateInputField();
        seedInput.Select();
    }

    public void Close()
    {
        isOpen = false;
        rootPanel.SetActive(false);
        if (canvas != null)
            canvas.gameObject.SetActive(false);
        WorldInputGate.SetUIOpen(false);
    }

    private void Confirm()
    {
        if (confirmHandler == null)
            return;

        string seed = seedInput.text.Trim();
        if (string.IsNullOrEmpty(seed))
            seed = "default";

        confirmHandler.Invoke(selectedWorldType, seed);
    }

    private void BuildUI()
    {
        EnsureEventSystem();

        GameObject canvasObject = new GameObject("PortalWorldUI");
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();

        rootPanel = CreatePanel(canvas.transform);

        Text title = CreateText(rootPanel.transform, "World Portal", 26, TextAnchor.MiddleLeft, new Vector2(20f, 250f), new Vector2(480f, 36f));
        title.color = new Color(0.92f, 0.96f, 1f);

        CreateText(rootPanel.transform, "Seed", 18, TextAnchor.MiddleLeft, new Vector2(20f, 200f), new Vector2(200f, 24f));
        seedInput = CreateInputField(rootPanel.transform, new Vector2(20f, 168f), new Vector2(480f, 34f));

        CreateText(rootPanel.transform, "World Type", 18, TextAnchor.MiddleLeft, new Vector2(20f, 154f), new Vector2(200f, 24f));
        worldTypeLabel = CreateText(rootPanel.transform, WorldTypeUtility.ToDisplayName(selectedWorldType), 16, TextAnchor.MiddleLeft, new Vector2(20f, 126f), new Vector2(180f, 24f));
        RefreshWorldTypeLabel();
        CreateWorldTypeButton(rootPanel.transform, "Stone", WorldType.Stone, new Vector2(20f, 92f), new Vector2(100f, 34f));
        CreateWorldTypeButton(rootPanel.transform, "Wood", WorldType.Wood, new Vector2(128f, 92f), new Vector2(100f, 34f));
        CreateWorldTypeButton(rootPanel.transform, "Iron", WorldType.Iron, new Vector2(236f, 92f), new Vector2(100f, 34f));

        confirmButton = CreateButton(rootPanel.transform, "Enter", new Vector2(344f, 92f), new Vector2(156f, 34f));
        cancelButton = CreateButton(rootPanel.transform, "Cancel", new Vector2(344f, 48f), new Vector2(156f, 34f));

        statusText = CreateText(rootPanel.transform, string.Empty, 14, TextAnchor.LowerLeft, new Vector2(20f, 12f), new Vector2(480f, 30f));
        statusText.color = new Color(0.85f, 0.9f, 0.95f);

        confirmButton.onClick.AddListener(Confirm);
        cancelButton.onClick.AddListener(Close);
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }

    private static GameObject CreatePanel(Transform parent)
    {
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.08f, 0.1f, 0.14f, 0.92f);

        return panel;
    }

    private static Text CreateText(Transform parent, string value, int fontSize, TextAnchor alignment, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.text = value;
        text.color = Color.white;
        return text;
    }

    private static InputField CreateInputField(Transform parent, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject fieldObject = new GameObject("SeedInput");
        fieldObject.transform.SetParent(parent, false);
        RectTransform rect = fieldObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image background = fieldObject.AddComponent<Image>();
        background.color = new Color(0.15f, 0.18f, 0.24f, 1f);

        InputField inputField = fieldObject.AddComponent<InputField>();

        GameObject placeholderObject = new GameObject("Placeholder");
        placeholderObject.transform.SetParent(fieldObject.transform, false);
        RectTransform placeholderRect = placeholderObject.AddComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = new Vector2(10f, 4f);
        placeholderRect.offsetMax = new Vector2(-10f, -4f);

        Text placeholder = placeholderObject.AddComponent<Text>();
        placeholder.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        placeholder.text = "Type a world seed...";
        placeholder.color = new Color(0.7f, 0.75f, 0.8f, 0.8f);
        placeholder.alignment = TextAnchor.MiddleLeft;
        placeholder.fontSize = 16;

        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(fieldObject.transform, false);
        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10f, 4f);
        textRect.offsetMax = new Vector2(-10f, -4f);

        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        text.fontSize = 16;

        inputField.placeholder = placeholder;
        inputField.textComponent = text;
        inputField.lineType = InputField.LineType.SingleLine;

        return inputField;
    }

    private void RefreshWorldTypeLabel()
    {
        if (worldTypeLabel != null)
            worldTypeLabel.text = "Selected: " + WorldTypeUtility.ToDisplayName(selectedWorldType);
    }

    private Button CreateWorldTypeButton(Transform parent, string label, WorldType worldType, Vector2 anchoredPosition, Vector2 size)
    {
        Button button = CreateButton(parent, label, anchoredPosition, size);
        button.onClick.AddListener(delegate
        {
            selectedWorldType = worldType;
            RefreshWorldTypeLabel();
        });
        return button;
    }

    private static Button CreateButton(Transform parent, string label, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject buttonObject = new GameObject(label + "Button");
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.22f, 0.28f, 0.36f, 1f);

        Button button = buttonObject.AddComponent<Button>();

        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(buttonObject.transform, false);
        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.text = label;
        text.fontSize = 16;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        return button;
    }
}
