
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    // =====================================
    // PAINEIS DO MENU
    // =====================================

    [Header("Paineis")]
    public GameObject mainPanel;
    public GameObject optionsPanel;
    public GameObject creditsPanel;

    // =====================================
    // CONFIGURACOES
    // =====================================

    [Header("Configuracoes")]
    public Slider volumeSlider;
    public Toggle fullscreenToggle;

    // =====================================
    // INICIALIZACAO DO MENU
    // =====================================

    void Start()
    {
        // Garante que o jogo nao esteja pausado
        Time.timeScale = 1f;

        // Mostra apenas o menu principal
        mainPanel.SetActive(true);
        optionsPanel.SetActive(false);
        creditsPanel.SetActive(false);

        // Recupera o volume salvo
        float volumeSalvo =
            PlayerPrefs.GetFloat("Volume", 1f);

        AudioListener.volume = volumeSalvo;

        volumeSlider.SetValueWithoutNotify(volumeSalvo);

        // Recupera a configuracao de tela cheia
        bool telaCheiaSalva =
            PlayerPrefs.GetInt(
                "Fullscreen",
                Screen.fullScreen ? 1 : 0
            ) == 1;

        Screen.fullScreen = telaCheiaSalva;

        fullscreenToggle.SetIsOnWithoutNotify(
            telaCheiaSalva
        );
    }

    // =====================================
    // BOTAO JOGAR
    // =====================================

    public void Jogar()
    {
        Debug.Log("Botao JOGAR clicado!");

        SceneManager.LoadScene("Gameplay");
    }

    // =====================================
    // BOTAO OPCOES
    // =====================================

    public void Opcoes()
    {
        mainPanel.SetActive(false);
        optionsPanel.SetActive(true);
        creditsPanel.SetActive(false);
    }

    // =====================================
    // BOTAO CREDITOS
    // =====================================

    public void Creditos()
    {
        mainPanel.SetActive(false);
        optionsPanel.SetActive(false);
        creditsPanel.SetActive(true);
    }

    // =====================================
    // BOTAO VOLTAR
    // =====================================

    public void Voltar()
    {
        optionsPanel.SetActive(false);
        creditsPanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    // =====================================
    // CONTROLE DE VOLUME
    // =====================================

    public void AlterarVolume(float valor)
    {
        // Altera o volume geral do jogo
        AudioListener.volume = valor;

        // Salva o volume escolhido
        PlayerPrefs.SetFloat("Volume", valor);
        PlayerPrefs.Save();

        Debug.Log("Volume alterado para: " + valor);
    }

    // =====================================
    // CONTROLE DE TELA CHEIA
    // =====================================

    public void AlterarTelaCheia(bool ativado)
    {
        // Ativa ou desativa a tela cheia
        Screen.fullScreen = ativado;

        // Salva a configuracao
        PlayerPrefs.SetInt(
            "Fullscreen",
            ativado ? 1 : 0
        );

        PlayerPrefs.Save();

        Debug.Log("Tela cheia: " + ativado);
    }

    // =====================================
    // BOTAO SAIR
    // =====================================

    public void Sair()
    {
        Debug.Log("Botao SAIR clicado!");

        Application.Quit();
    }
}
