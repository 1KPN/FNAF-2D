using UnityEngine;
using UnityEngine.InputSystem;

public class generatorInteract : MonoBehaviour
{
    private bool _playerPerto = false;
    private playerController _scriptDoPlayer;

    [Header("Interface")]
    public GameObject textoAviso;

    void Start()
    {
        if (textoAviso != null) textoAviso.SetActive(false);
    }

    void Update()
    {
        if (_playerPerto && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            DepositarFusivel();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            _playerPerto = true;
            _scriptDoPlayer = collision.GetComponent<playerController>();
            
            if (textoAviso != null) textoAviso.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            _playerPerto = false;
            _scriptDoPlayer = null;
            
            if (textoAviso != null) textoAviso.SetActive(false);
        }
    }

    void DepositarFusivel()
    {
        if (_scriptDoPlayer != null)
        {
            int quantidadeNoBolso = _scriptDoPlayer.fusiveisColetados;

            if (quantidadeNoBolso > 0)
            {
                // Zera o bolso do jogador
                _scriptDoPlayer.fusiveisColetados = 0; 
                
                // Manda a quantidade exata para o GameManager
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.RegistrarDeposito(quantidadeNoBolso);
                }
                
                Debug.Log($"BZZZ! {quantidadeNoBolso} fusível(eis) depositado(s) de uma vez!");
            }
            else
            {
                Debug.Log("O gerador precisa de fusíveis! Você não tem nenhum no bolso.");
            }
        }
    }
}