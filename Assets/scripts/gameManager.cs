using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Configurações da Noite")]
    public int noiteAtual = 1;
    public int fusiveisNecessarios = 5; 
    
    private int fusiveisDepositados = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void RegistrarDeposito(int quantidade)
    {
        fusiveisDepositados += quantidade;
        Debug.Log($"Progresso da Noite {noiteAtual}: {fusiveisDepositados}/{fusiveisNecessarios} fusíveis no gerador.");

        if (fusiveisDepositados >= fusiveisNecessarios)
        {
            ConcluirNoite();
        }
    }

    void ConcluirNoite()
    {
        Debug.Log("NOITE CONCLUÍDA! Todos os fusíveis foram depositados com sucesso.");
    }
}