using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using UnityEngine.Rendering.Universal;

public class ControleBotoes : MonoBehaviour
{
    public enum TipoBotao { Porta, Luz }
    
    [Header("Qual a função deste botão?")]
    public TipoBotao tipo;

    [Header("Configurações Visuais do Botão")]
    public SpriteRenderer spriteRendererBotao;
    public Sprite spriteDesligado;
    public Sprite spriteLigado;
    public TextMeshPro textoAviso;

    [Header("Se for um botão de PORTA:")]
    public GameObject porta;
    public Transform pontoAberta;
    public float velocidadePorta = 8f;

    [Header("Se for um botão de LUZ:")]
    public Light2D luzExterna;
    public float tempoLuz = 4f; 

    private bool jogadorNaArea = false;
    private bool ativado = false;
    
    private Vector3 posicaoFechada;
    private Vector3 posicaoAberta;
    private Coroutine fadeCoroutine;
    private Coroutine luzCoroutine;

    void Start()
    {
        if (tipo == TipoBotao.Porta && porta != null && pontoAberta != null)
        {
            posicaoFechada = porta.transform.position;
            posicaoAberta = pontoAberta.position; 
        }

        if (spriteRendererBotao != null) spriteRendererBotao.sprite = spriteDesligado;
        if (textoAviso != null) textoAviso.alpha = 0f;
    }

    void Update()
    {
        if (jogadorNaArea && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (tipo == TipoBotao.Porta)
            {
                ativado = !ativado; 
                if (spriteRendererBotao != null) spriteRendererBotao.sprite = ativado ? spriteLigado : spriteDesligado;
            }
            else if (tipo == TipoBotao.Luz && !ativado) 
            {
                if (luzCoroutine != null) StopCoroutine(luzCoroutine);
                luzCoroutine = StartCoroutine(TemporizadorLuz());
            }
        }

        if (tipo == TipoBotao.Porta && porta != null)
        {
            Vector3 destino = ativado ? posicaoAberta : posicaoFechada;
            porta.transform.position = Vector3.MoveTowards(porta.transform.position, destino, velocidadePorta * Time.deltaTime);
        }
    }

    IEnumerator TemporizadorLuz()
    {
        ativado = true;
        if (spriteRendererBotao != null) spriteRendererBotao.sprite = spriteLigado;
        if (luzExterna != null) luzExterna.enabled = true;

        yield return new WaitForSeconds(tempoLuz);

        ativado = false;
        if (spriteRendererBotao != null) spriteRendererBotao.sprite = spriteDesligado;
        if (luzExterna != null) luzExterna.enabled = false;
    }

    void OnTriggerEnter2D(Collider2D outro)
    {
        if (outro.CompareTag("Player"))
        {
            jogadorNaArea = true;
            if (textoAviso != null)
            {
                if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
                fadeCoroutine = StartCoroutine(EfeitoFade(1f));
            }
        }
    }

    void OnTriggerExit2D(Collider2D outro)
    {
        if (outro.CompareTag("Player"))
        {
            jogadorNaArea = false;
            if (textoAviso != null)
            {
                if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
                fadeCoroutine = StartCoroutine(EfeitoFade(0f));
            }
        }
    }

    IEnumerator EfeitoFade(float alphaDestino)
    {
        while (Mathf.Abs(textoAviso.alpha - alphaDestino) > 0.01f)
        {
            textoAviso.alpha = Mathf.MoveTowards(textoAviso.alpha, alphaDestino, Time.deltaTime * 5f);
            yield return null;
        }
        textoAviso.alpha = alphaDestino;
    }
}