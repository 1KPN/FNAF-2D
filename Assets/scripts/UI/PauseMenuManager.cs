
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PauseMenuManager : MonoBehaviour
{
    [Header("Paineis")]
    public GameObject pausePanel;
    public GameObject pauseContainer;
    public GameObject pauseOptionsPanel;

    [Header("Configuracoes")]
    public Slider volumeSlider;
    public Toggle fullScreenToggle;

    [Header("Cena do Menu Principal")]
    public string menuSceneName = "Menu";

    private bool isPaused = false;
    private float previousTimeScale = 1f;

    private bool previousCursorVisible;
    private CursorLockMode previousCursorLock;

    void Start()
    {
        pausePanel.SetActive(false);
        pauseContainer.SetActive(true);
        pauseOptionsPanel.SetActive(false);

        volumeSlider.SetValueWithoutNotify(
            AudioListener.volume
        );

        fullScreenToggle.SetIsOnWithoutNotify(
            Screen.fullScreen
        );
    }

    void Update()
    {
        bool escapePressed = false;

#if ENABLE_INPUT_SYSTEM
        escapePressed = Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        escapePressed = Input.GetKeyDown(KeyCode.Escape);
#endif

        if (escapePressed)
        {
            if (!isPaused)
            {
                Pausar();
            }
            else if (pauseOptionsPanel.activeSelf)
            {
                VoltarDasOpcoes();
            }
            else
            {
                Continuar();
            }
        }
    }

    public void Pausar()
    {
        if (isPaused || Time.timeScale == 0f)
            return;

        previousTimeScale = Time.timeScale;
        previousCursorVisible = Cursor.visible;
        previousCursorLock = Cursor.lockState;

        isPaused = true;

        pausePanel.SetActive(true);
        pauseContainer.SetActive(true);
        pauseOptionsPanel.SetActive(false);

        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Continuar()
    {
        if (!isPaused)
            return;

        isPaused = false;

        pausePanel.SetActive(false);
        pauseContainer.SetActive(true);
        pauseOptionsPanel.SetActive(false);

        Time.timeScale = previousTimeScale;

        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
    }

    public void Opcoes()
    {
        if (!isPaused)
            return;

        pauseContainer.SetActive(false);
        pauseOptionsPanel.SetActive(true);

        volumeSlider.SetValueWithoutNotify(
            AudioListener.volume
        );

        fullScreenToggle.SetIsOnWithoutNotify(
            Screen.fullScreen
        );
    }

    public void VoltarDasOpcoes()
    {
        pauseOptionsPanel.SetActive(false);
        pauseContainer.SetActive(true);
    }

    public void AlterarVolume(float volume)
    {
        AudioListener.volume = Mathf.Clamp01(volume);
    }

    public void AlterarTelaCheia(bool telaCheia)
    {
        Screen.fullScreen = telaCheia;
    }

    public void Reiniciar()
    {
        Time.timeScale = 1f;
        isPaused = false;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name
        );
    }

    public void MenuPrincipal()
    {
        if (!Application.CanStreamedLevelBeLoaded(menuSceneName))
        {
            Debug.LogError(
                "Cena do menu nao encontrada: " + menuSceneName
            );

            return;
        }

        Time.timeScale = 1f;
        isPaused = false;

        SceneManager.LoadScene(menuSceneName);
    }

    public void Sair()
    {
        Debug.Log("Saindo do jogo...");
        Application.Quit();
    }
}
