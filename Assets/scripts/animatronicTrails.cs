using UnityEngine;
using System.Collections.Generic;

public class animatronicTrails : MonoBehaviour
{
    public enum EstadoIA { Patrulha, Perseguicao, Retornando }
    public EstadoIA estadoAtual = EstadoIA.Patrulha;

    [Header("Trilhos de Patrulha")]
    public List<Transform> pontosDeTrilho;
    private int indiceAtual = 0;

    [Header("Configurações")]
    public Transform alvoPlayer;
    public float velocidadePatrulha = 2f;
    public float velocidadePerseguicao = 4f;
    public float distanciaDeVisao = 12f;
    public LayerMask layerParedes;

    private Vector3 _posicaoInicial;
    private SpriteRenderer _spriteRenderer;

    void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _posicaoInicial = transform.position;

        if (pontosDeTrilho == null || pontosDeTrilho.Count == 0)
        {
            Debug.LogWarning("Adiciona pontos de trilho no Inspector do Freddy!");
        }
    }

    void Update()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, 0f);

        switch (estadoAtual)
        {
            case EstadoIA.Patrulha:
                PatrulharNosTrilhos();
                VerificarVisao();
                break;

            case EstadoIA.Perseguicao:
                PerseguirLinhaReta();
                break;

            case EstadoIA.Retornando:
                RetornarAoPalco();
                break;
        }
    }

    void PatrulharNosTrilhos()
    {
        if (pontosDeTrilho == null || pontosDeTrilho.Count == 0) return;

        Transform alvo = pontosDeTrilho[indiceAtual];
        MoverEmDirecao(alvo.position, velocidadePatrulha * MultiplicadorDificuldade());

        if (Vector3.Distance(transform.position, alvo.position) < 0.2f)
        {
            indiceAtual = (indiceAtual + 1) % pontosDeTrilho.Count;
        }
    }

    void PerseguirLinhaReta()
    {
        if (alvoPlayer == null) return;
        MoverEmDirecao(alvoPlayer.position, velocidadePerseguicao * MultiplicadorDificuldade());
    }

    void RetornarAoPalco()
    {
        MoverEmDirecao(_posicaoInicial, velocidadePerseguicao * MultiplicadorDificuldade());

        if (Vector3.Distance(transform.position, _posicaoInicial) < 0.2f)
        {
            estadoAtual = EstadoIA.Patrulha;
        }
    }

    void MoverEmDirecao(Vector3 destino, float velocidade)
    {
        Vector3 direcao = (destino - transform.position).normalized;
        transform.position += direcao * velocidade * Time.deltaTime;

        if (_spriteRenderer != null && direcao.x != 0)
        {
            _spriteRenderer.flipX = direcao.x < 0;
        }
    }

    void VerificarVisao()
    {
        if (alvoPlayer == null) return;

        Vector2 direcaoAtePlayer = (Vector2)(alvoPlayer.position - transform.position);
        float distancia = direcaoAtePlayer.magnitude;

        if (distancia <= distanciaDeVisao)
        {
            Vector2 pontoOrigem = (Vector2)transform.position + (direcaoAtePlayer.normalized * 0.5f);
            RaycastHit2D hit = Physics2D.Raycast(pontoOrigem, direcaoAtePlayer.normalized, distancia, ~layerParedes);

            if (hit.collider != null)
            {
                if (hit.collider.CompareTag("Player") || hit.collider.name == "Player")
                {
                    estadoAtual = EstadoIA.Perseguicao;
                }
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        VerificarPorta(collision.gameObject);
        VerificarMortePlayer(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        VerificarPorta(collision.gameObject);
        VerificarMortePlayer(collision.gameObject);
    }

    void VerificarPorta(GameObject obj)
    {
        if (estadoAtual == EstadoIA.Perseguicao)
        {
            if (obj.name == "portaEsquerda" || obj.name == "portaDireita")
            {
                estadoAtual = EstadoIA.Retornando;
            }
        }
    }

    void VerificarMortePlayer(GameObject obj)
    {
        playerController player = obj.GetComponentInParent<playerController>();
        if (player == null && !obj.CompareTag("Player") && obj.name != "Player")
            return;

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager não encontrado; não foi possível iniciar o jumpscare.", this);
            return;
        }

        if (GameManager.Instance.IniciarJumpscare())
            enabled = false;
    }

    float MultiplicadorDificuldade()
    {
        return GameManager.Instance != null ? GameManager.Instance.MultiplicadorDificuldade : 1f;
    }
}