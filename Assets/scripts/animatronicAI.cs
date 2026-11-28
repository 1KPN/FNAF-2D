using UnityEngine;
using UnityEngine.AI;

public class AnimatronicoIA : MonoBehaviour
{
    public enum EstadoIA { Patrulha, Perseguicao, Retornando }
    public EstadoIA estadoAtual = EstadoIA.Patrulha;

    [Header("Configurações de Movimento")]
    public Transform alvoPlayer;
    public float velocidadePatrulha = 1.5f;
    public float velocidadePerseguicao = 3.5f;
    public float distanciaDeVisao = 12f;
    public LayerMask layerParedes;

    private NavMeshAgent _agente;
    private Vector3 _posicaoInicial;
    private SpriteRenderer _spriteRenderer;
    private AudioSource _audioPassos;

    void Start()
    {
        _agente = GetComponent<NavMeshAgent>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _audioPassos = GetComponent<AudioSource>();
        if (_audioPassos == null)
            _audioPassos = gameObject.AddComponent<AudioSource>();
        _audioPassos.playOnAwake = false;
        _audioPassos.loop = true;
        _audioPassos.spatialBlend = 0f;

        if (_agente == null)
        {
            Debug.LogError("O script de IA legado precisa de um NavMeshAgent.", this);
            enabled = false;
            return;
        }

        _agente.updateRotation = false;
        _agente.updateUpAxis = false;
        
        _posicaoInicial = transform.position;
    }

    void Update()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, 0f);

        if (_agente == null || !_agente.enabled || !_agente.isOnNavMesh)
        {
            AtualizarAudio(false, false);
            return;
        }

        if (_agente.velocity.x != 0)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.flipX = _agente.velocity.x < 0;
            }
        }

        switch (estadoAtual)
        {
            case EstadoIA.Patrulha:
                _agente.speed = velocidadePatrulha * MultiplicadorDificuldade();
                Patrulhar();
                VerificarVisao();
                break;

            case EstadoIA.Perseguicao:
                _agente.speed = velocidadePerseguicao * MultiplicadorDificuldade();
                if (alvoPlayer != null) 
                {
                    _agente.SetDestination(alvoPlayer.position);
                }
                break;

            case EstadoIA.Retornando:
                _agente.speed = velocidadePerseguicao * MultiplicadorDificuldade();
                _agente.SetDestination(_posicaoInicial);

                if (!_agente.pathPending && _agente.remainingDistance < 0.3f)
                {
                    estadoAtual = EstadoIA.Patrulha;
                }
                break;
        }

        AtualizarAudio(
            estadoAtual == EstadoIA.Perseguicao,
            !_agente.isStopped && _agente.velocity.sqrMagnitude > 0.04f);
    }

    void Patrulhar()
    {
        if (!_agente.pathPending && (_agente.remainingDistance < 0.5f || !_agente.hasPath))
        {
            Vector3 pontoAleatorio = ObterPontoAleatorioNaMalha(transform.position, 10f);
            _agente.SetDestination(pontoAleatorio);
        }
    }

    Vector3 ObterPontoAleatorioNaMalha(Vector3 centro, float raio)
    {
        Vector2 direcaoAleatoria = Random.insideUnitCircle * raio;
        Vector3 posicaoDesejada = centro + new Vector3(direcaoAleatoria.x, direcaoAleatoria.y, 0f);
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(posicaoDesejada, out hit, raio, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return centro;
    }

    void VerificarVisao()
    {
        if (alvoPlayer == null) return;

        Vector2 direcaoAtePlayer = (Vector2)(alvoPlayer.position - transform.position);
        float distancia = direcaoAtePlayer.magnitude;

        if (distancia <= distanciaDeVisao)
        {
            Vector2 pontoOrigem = (Vector2)transform.position + (direcaoAtePlayer.normalized * 0.5f);
            RaycastHit2D hit = Physics2D.Raycast(pontoOrigem, direcaoAtePlayer.normalized, distancia);

            if (hit.collider != null)
            {
                if (hit.collider.CompareTag("Player") || hit.collider.name == "Player")
                {
                    if (estadoAtual != EstadoIA.Perseguicao && GameManager.Instance != null)
                        GameManager.Instance.TocarAlertaAnimatronico();
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

    void AtualizarAudio(bool perseguindo, bool movendo)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.AtualizarAudioAnimatronico(_audioPassos, perseguindo, movendo);
        else if (!movendo && _audioPassos != null && _audioPassos.isPlaying)
            _audioPassos.Stop();
    }

    public void PararAudio()
    {
        if (_audioPassos == null)
            return;

        _audioPassos.Stop();
        _audioPassos.clip = null;
    }

    float MultiplicadorDificuldade()
    {
        return GameManager.Instance != null ? GameManager.Instance.MultiplicadorDificuldade : 1f;
    }
}