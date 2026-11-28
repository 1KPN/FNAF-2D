using UnityEngine;
using UnityEngine.InputSystem;

public class generatorInteract : MonoBehaviour
{
    private bool playerPerto = false;
    private playerController scriptDoPlayer;
    public GameObject textoAviso;

    void Start()
    {
        if (textoAviso != null) textoAviso.SetActive(false);
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.JogoEncerrado)
            return;

        if (playerPerto && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            DepositarFusiveis();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerPerto = true;
            scriptDoPlayer = collision.GetComponent<playerController>();
            if (textoAviso != null) textoAviso.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerPerto = false;
            scriptDoPlayer = null;
            if (textoAviso != null) textoAviso.SetActive(false);
        }
    }

    void DepositarFusiveis()
    {
        if (scriptDoPlayer != null && GameManager.Instance != null)
        {
            GameManager.Instance.TocarSomGerador();
            if (scriptDoPlayer.fusiveisColetados >= GameManager.Instance.fusiveisNecessarios)
            {
                GameManager.Instance.ConcluirNoite(scriptDoPlayer);
            }
        }
    }
}