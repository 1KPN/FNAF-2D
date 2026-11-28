using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

[DefaultExecutionOrder(-1000)]
public class mainMenuUI : MonoBehaviour
{
    private const float StaticFrameDuration = 0.055f;

    [Header("Menu sprites")]
    [SerializeField] private Sprite[] freddyTwitchingFrames = new Sprite[4];
    [SerializeField] private Sprite[] staticFrames = new Sprite[7];
    [SerializeField] private Sprite titleSprite;
    [SerializeField] private Sprite newspaperSprite;
    [SerializeField] private AudioClip clipMusicaMenu;

    [Header("Menu font")]
    [SerializeField] private TMP_FontAsset menuFont;
    [SerializeField] private Material menuFontMaterial;

    [Header("UI da cena Menu")]
    [SerializeField] private Image imagemFreddy;
    [SerializeField] private Image imagemStatic;
    [SerializeField] private GameObject painelNewspaper;
    [SerializeField] private Image imagemFade;
    [SerializeField] private Button botaoNewGame;
    [SerializeField] private Button botaoContinue;
    [SerializeField] private TextMeshProUGUI textoContinue;
    [SerializeField] private TextMeshProUGUI textoNoiteContinue;
    private bool _startingGame;

    public static bool IsMenuOpen { get; private set; }

    void Awake()
    {
        if (GameManager.ConsumirIgnorarMenuNaProximaCarga())
        {
            IsMenuOpen = false;
            Time.timeScale = 1f;
            enabled = false;
            return;
        }

        IsMenuOpen = true;
        Time.timeScale = 0f;
        EnsureSingleEventSystem();

        if (!ValidateAssets())
        {
            enabled = false;
            return;
        }

    }

    void Start()
    {
        AudioSource audioMusicaMenu = GetComponent<AudioSource>();
        if (audioMusicaMenu == null)
            audioMusicaMenu = gameObject.AddComponent<AudioSource>();

        if (clipMusicaMenu == null)
        {
            Debug.LogError("Configure darkness music.wav no Main Menu Controller.", this);
        }
        else
        {
            audioMusicaMenu.clip = clipMusicaMenu;
            audioMusicaMenu.loop = true;
            audioMusicaMenu.playOnAwake = false;
            audioMusicaMenu.ignoreListenerPause = true;
            audioMusicaMenu.volume = 0.3f;
            audioMusicaMenu.Play();
        }

        textoContinue.text = "Continue";
        textoNoiteContinue.text = $"Noite {GameManager.GetSavedNight()}";
        textoContinue.font = menuFont;
        textoContinue.fontSharedMaterial = menuFontMaterial;
        textoNoiteContinue.font = menuFont;
        textoNoiteContinue.fontSharedMaterial = menuFontMaterial;
        textoNoiteContinue.gameObject.SetActive(false);
        painelNewspaper.SetActive(false);
        EventSystem.current?.SetSelectedGameObject(botaoNewGame.gameObject);
        StartCoroutine(AnimateFreddy());
        imagemStatic.enabled = true;
        StartCoroutine(AnimateStatic());
    }

    void Update()
    {
        if (_startingGame || textoNoiteContinue == null || botaoContinue == null)
            return;

        bool mouseSobreContinue = false;
        if (Mouse.current != null)
        {
            Canvas canvas = botaoContinue.GetComponentInParent<Canvas>();
            Camera cameraUI = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            mouseSobreContinue = RectTransformUtility.RectangleContainsScreenPoint(
                (RectTransform)botaoContinue.transform,
                Mouse.current.position.ReadValue(),
                cameraUI);
        }

        if (textoNoiteContinue.gameObject.activeSelf != mouseSobreContinue)
            textoNoiteContinue.gameObject.SetActive(mouseSobreContinue);
    }

    bool ValidateAssets()
    {
        if (freddyTwitchingFrames == null || freddyTwitchingFrames.Length != 4)
        {
            Debug.LogError("O menu precisa de exatamente quatro sprites de Freddy Twitching.", this);
            return false;
        }

        for (int i = 0; i < freddyTwitchingFrames.Length; i++)
        {
            if (freddyTwitchingFrames[i] == null)
            {
                Debug.LogError($"Configure o sprite Freddy Twitching {i + 1} no Main Menu Controller.", this);
                return false;
            }
        }

        if (staticFrames == null || staticFrames.Length != 7)
        {
            Debug.LogError("O menu precisa de exatamente sete sprites de Static.", this);
            return false;
        }

        for (int i = 0; i < staticFrames.Length; i++)
        {
            if (staticFrames[i] == null)
            {
                Debug.LogError($"Configure o sprite Static {i + 1} no Main Menu Controller.", this);
                return false;
            }
        }

        if (titleSprite == null)
        {
            Debug.LogError("Configure o sprite title.png no Main Menu Controller.", this);
            return false;
        }

        if (newspaperSprite == null)
        {
            Debug.LogError("Configure o sprite newspaper.png no Main Menu Controller.", this);
            return false;
        }

        if (menuFont == null || menuFontMaterial == null)
        {
            Debug.LogError("Configure a fonte e o material FNAF-Regular SDF Material no Main Menu Controller.", this);
            return false;
        }

        if (imagemFreddy == null || imagemStatic == null || painelNewspaper == null || imagemFade == null
            || botaoNewGame == null || botaoContinue == null || textoContinue == null
            || textoNoiteContinue == null)
        {
            Debug.LogError("Vincule os objetos do menu e do jornal na Hierarchy da cena Menu.", this);
            return false;
        }

        textoContinue.font = menuFont;
        textoContinue.fontSharedMaterial = menuFontMaterial;
        return true;
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

    IEnumerator AnimateFreddy()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(Random.Range(2.4f, 5.2f));

            float eventRoll = Random.value;
            int twitchFrame = eventRoll < 0.025f ? 3 : Random.Range(1, 3);
            float twitchDuration = twitchFrame == 3
                ? Random.Range(0.055f, 0.12f)
                : Random.Range(0.08f, 0.19f);

            imagemFreddy.sprite = freddyTwitchingFrames[twitchFrame];
            imagemFreddy.color = new Color(1f, 1f, 1f, Random.Range(0.68f, 1f));
            yield return new WaitForSecondsRealtime(twitchDuration);

            imagemFreddy.sprite = freddyTwitchingFrames[0];
            imagemFreddy.color = new Color(1f, 1f, 1f, Random.Range(0.82f, 1f));
        }
    }

    IEnumerator AnimateStatic()
    {
        int frame = 0;
        while (true)
        {
            imagemStatic.sprite = staticFrames[frame];
            imagemStatic.color = new Color(1f, 1f, 1f, Random.Range(0.09f, 0.2f));
            frame = (frame + 1) % staticFrames.Length;
            yield return new WaitForSecondsRealtime(StaticFrameDuration);
        }
    }

    public void StartNewGame()
    {
        if (_startingGame)
            return;

        if (!CanLoadGameplay())
            return;

        _startingGame = true;
        StartCoroutine(ShowNewspaper());
    }

    public void ContinueGame()
    {
        StartGameWithFade(GameManager.IniciarPartidaSalva);
    }

    bool CanLoadGameplay()
    {
        if (!Application.CanStreamedLevelBeLoaded("Gameplay"))
        {
            Debug.LogError("A cena 'Gameplay' não está habilitada nas Build Settings.", this);
            return false;
        }

        return true;
    }

    void StartGameWithFade(System.Action startGame)
    {
        if (_startingGame)
            return;

        if (!CanLoadGameplay())
            return;

        _startingGame = true;
        StartCoroutine(FadeAndLoad(startGame));
    }

    IEnumerator ShowNewspaper()
    {
        yield return FadeTo(1f, 0.45f);
        painelNewspaper.SetActive(true);
        yield return FadeTo(0f, 0.45f);
        yield return new WaitForSecondsRealtime(7f);
        yield return FadeTo(1f, 0.45f);
        IsMenuOpen = false;
        Time.timeScale = 1f;
        GameManager.IniciarNovaPartida();
    }

    IEnumerator FadeAndLoad(System.Action startGame)
    {
        yield return FadeTo(1f, 0.45f);
        IsMenuOpen = false;
        Time.timeScale = 1f;
        startGame();
    }

    IEnumerator FadeTo(float targetAlpha, float duration)
    {
        float startAlpha = imagemFade.color.a;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration));
            imagemFade.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }

        imagemFade.color = new Color(0f, 0f, 0f, targetAlpha);
    }
}
