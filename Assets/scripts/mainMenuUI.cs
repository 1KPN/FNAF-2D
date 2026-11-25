using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class mainMenuUI : MonoBehaviour
{
    private const string GameplayScene = "Gameplay";
    private const string CanvasName = "MenuCanvas";

    void Awake()
    {
        Time.timeScale = 1f;
        EnsureSingleEventSystem();
        if (GameObject.Find(CanvasName) == null)
            CreateMenuCanvas();
    }

    void EnsureSingleEventSystem()
    {
        EventSystem[] eventSystems = FindObjectsByType<EventSystem>();
        EventSystem primary = eventSystems.Length > 0 ? eventSystems[0] : null;

        if (primary == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            primary = eventSystemObject.AddComponent<EventSystem>();
        }

        for (int i = 1; i < eventSystems.Length; i++)
        {
            if (eventSystems[i] != null)
                Destroy(eventSystems[i].gameObject);
        }

        if (primary.GetComponent<BaseInputModule>() == null)
        {
            InputSystemUIInputModule inputModule = primary.gameObject.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
        }
    }

    void CreateMenuCanvas()
    {
        GameObject canvasObject = new GameObject(
            CanvasName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject background = new GameObject(
            "Background",
            typeof(RectTransform),
            typeof(Image));
        background.transform.SetParent(canvas.transform, false);
        StretchToParent(background.GetComponent<RectTransform>());
        Image backgroundImage = background.GetComponent<Image>();
        backgroundImage.color = new Color(0.035f, 0.035f, 0.05f, 1f);
        backgroundImage.raycastTarget = false;

        CreateLabel(
            canvas.transform,
            "Title",
            "FNAF 2D",
            new Vector2(0f, 110f),
            new Vector2(800f, 140f),
            72f,
            FontStyles.Bold);
        CreateLabel(
            canvas.transform,
            "Subtitle",
            "Five nights. Stay alert.",
            new Vector2(0f, 25f),
            new Vector2(700f, 60f),
            30f,
            FontStyles.Normal);

        Button playButton = CreateButton(canvas.transform, "Jogar", new Vector2(0f, -90f));
        playButton.onClick.AddListener(StartGame);

        EventSystem.current?.SetSelectedGameObject(playButton.gameObject);
    }

    static void CreateLabel(
        Transform parent,
        string objectName,
        string value,
        Vector2 position,
        Vector2 size,
        float fontSize,
        FontStyles style)
    {
        GameObject labelObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);

        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = value;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
    }

    Button CreateButton(Transform parent, string label, Vector2 position)
    {
        GameObject buttonObject = new GameObject(
            "BotaoJogar",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = position;
        buttonRect.sizeDelta = new Vector2(300f, 76f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.65f, 0.08f, 0.08f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.8f, 0.12f, 0.12f, 1f);
        colors.pressedColor = new Color(0.45f, 0.04f, 0.04f, 1f);
        button.colors = colors;

        CreateLabel(
            buttonObject.transform,
            "Label",
            label,
            Vector2.zero,
            buttonRect.sizeDelta,
            32f,
            FontStyles.Bold);
        return button;
    }

    static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    void StartGame()
    {
        Time.timeScale = 1f;
        if (Application.CanStreamedLevelBeLoaded(GameplayScene))
        {
            GameManager.IniciarNovaPartida();
            return;
        }

        Debug.LogError($"A cena '{GameplayScene}' não está habilitada nas Build Settings.", this);
    }
}
