using UnityEngine;
using TMPro; // Use "using UnityEngine.UI;" se estiver a usar Text normal em vez de TextMeshPro

public class gerenciadorUI : MonoBehaviour
{
    public static gerenciadorUI Instance;

    [Header("Elementos Visuais")]
    public TextMeshProUGUI txtContadorFusiveis; // Arraste o texto de fusíveis aqui
    public GameObject painelGameOver;         // Arraste o painel de Game Over aqui
    public GameObject painelVitoria;          // Arraste o painel de Vitória aqui

    private int fusiveisColetados = 0;
    private int totalFusiveisNecessarios = 3; // Altere para o número total de fusíveis do jogo

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (painelGameOver) painelGameOver.SetActive(false);
        if (painelVitoria) painelVitoria.SetActive(false);
        AtualizarTextoFusiveis();
    }

    // Chamado quando o jogador coleta um fusível
    public void ColetarFusivel()
    {
        fusiveisColetados++;
        AtualizarTextoFusiveis();
        Debug.Log("Fusível coletado! Total: " + fusiveisColetados);
    }

    // Chamado quando o jogador deposita os fusíveis no gerador (gerador2)
    public void DepositarFusiveisNoGerador()
    {
        if (fusiveisColetados >= totalFusiveisNecessarios)
        {
            AtivarVitoria();
        }
        else
        {
            Debug.Log("Faltam fusíveis para vencer!");
        }
    }

    void AtualizarTextoFusiveis()
    {
        if (txtContadorFusiveis != null)
        {
            txtContadorFusiveis.text = "Fusíveis: " + fusiveisColetados + " / " + totalFusiveisNecessarios;
        }
    }

    public void AtivarGameOver()
    {
        if (painelGameOver != null)
        {
            painelGameOver.SetActive(true);
            Time.timeScale = 0f; // Congela o jogo
        }
    }

    public void AtivarVitoria()
    {
        if (painelVitoria != null)
        {
            painelVitoria.SetActive(true);
            Time.timeScale = 0f; // Congela o jogo
        }
    }
}