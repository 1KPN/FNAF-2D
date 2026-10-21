using UnityEngine;
using UnityEngine.InputSystem;

public class fuseInteract : MonoBehaviour
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
            Coletar();
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

    void Coletar()
    {
        if (_scriptDoPlayer != null)
        {
            _scriptDoPlayer.fusiveisColetados++;
        }
        Destroy(gameObject);
    }
}