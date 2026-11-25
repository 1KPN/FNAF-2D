using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public class animatronicHibrid : MonoBehaviour
{
    private static readonly RaycastHit2D[] impactosRaycast = new RaycastHit2D[32];
    private static readonly ContactFilter2D filtroPortas = ContactFilter2D.noFilter;
    private static readonly ContactFilter2D filtroVisao = CriarFiltroVisao();

    public enum EstadoIA { Patrulha, Perseguicao, Retornando }
    public EstadoIA estadoAtual = EstadoIA.Patrulha;

    public List<Transform> todosOsNosDoMapa; 
    private Transform noAlvoAtual;

    public Transform alvoPlayer;
    public ControleBotoes botaoPortaEsquerda;
    public ControleBotoes botaoPortaDireita;
    public float velocidadePatrulha = 2f;
    public float velocidadePerseguicao = 4f;
    public float distanciaDeVisao = 7f;
    public float distanciaAtaque = 1.1f;
    public float raioAgenteSeguro = 0.5f;
    public LayerMask layerParedes;

    private NavMeshAgent _agente;
    private Vector3 _posicaoInicial;
    private SpriteRenderer _spriteRenderer;
    private AudioSource _audioPassos;
    private Collider2D[] _collidersPlayer;
    private bool jogoAcabou = false;
    private bool _rotaBloqueadaPorPorta;
    private float _multiplicadorDificuldade = 1f;

    public bool EstaBloqueadoPorPorta => _rotaBloqueadaPorPorta;

    void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _agente = GetComponent<NavMeshAgent>();
        _audioPassos = GetComponent<AudioSource>();
        if (_audioPassos == null)
            _audioPassos = gameObject.AddComponent<AudioSource>();
        _audioPassos.playOnAwake = false;
        _audioPassos.loop = true;
        _audioPassos.spatialBlend = 0f;
        _posicaoInicial = transform.position;
        _collidersPlayer = alvoPlayer != null
            ? alvoPlayer.GetComponentsInChildren<Collider2D>()
            : new Collider2D[0];

        foreach (BoxCollider2D collider in GetComponents<BoxCollider2D>())
        {
            collider.size = new Vector2(
                Mathf.Min(collider.size.x, 0.9f),
                Mathf.Min(collider.size.y, 1.3f));
            collider.offset = new Vector2(collider.offset.x, -0.9f);
        }

        if (GameManager.Instance != null)
            _multiplicadorDificuldade = GameManager.Instance.MultiplicadorDificuldade;

        if (_agente == null)
        {
            Debug.LogError("O animatrônico precisa de um NavMeshAgent.", this);
            enabled = false;
            return;
        }

        _agente.updateRotation = false;
        _agente.updateUpAxis = false;
        _agente.radius = Mathf.Max(_agente.radius, raioAgenteSeguro);
        _agente.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(
            transform.position,
            out hit,
            Mathf.Max(raioAgenteSeguro * 2f, 1f),
            _agente.areaMask))
        {
            transform.position = hit.position;
            _agente.enabled = true;
            if (!_agente.Warp(hit.position))
            {
                Debug.LogWarning("Não foi possível posicionar o agente do animatrônico na NavMesh.", this);
            }
            else
            {
                _posicaoInicial = hit.position;
            }
        }
        else
        {
            Debug.LogWarning("Não há NavMesh próxima para posicionar este animatrônico.", this);
        }

        if (todosOsNosDoMapa != null && todosOsNosDoMapa.Count > 0)
        {
            EscolherNovoNoAleatorio();
        }
    }

    void Update()
    {
        if (jogoAcabou) return;

        transform.position = new Vector3(transform.position.x, transform.position.y, 0f);

        if (_agente == null || !_agente.isOnNavMesh)
        {
            AtualizarAudio(false, false);
            return;
        }

        if (_spriteRenderer != null && _agente.velocity.x != 0)
        {
            _spriteRenderer.flipX = _agente.velocity.x < 0;
        }

        if (estadoAtual == EstadoIA.Perseguicao && !JogadorVisivel())
        {
            estadoAtual = EstadoIA.Retornando;
            _agente.ResetPath();
        }

        switch (estadoAtual)
        {
            case EstadoIA.Patrulha:
                _agente.speed = velocidadePatrulha * _multiplicadorDificuldade;
                PatrulharComNavMesh();
                VerificarVisao();
                break;

            case EstadoIA.Perseguicao:
                _agente.speed = velocidadePerseguicao * _multiplicadorDificuldade;
                if (alvoPlayer != null)
                    TentarDefinirDestino(alvoPlayer.position);
                break;

            case EstadoIA.Retornando:
                _agente.speed = velocidadePerseguicao * _multiplicadorDificuldade;
                if (!TentarDefinirDestino(_posicaoInicial)) break;

                if (!_agente.pathPending && _agente.remainingDistance < 0.5f)
                {
                    estadoAtual = EstadoIA.Patrulha;
                    EscolherNovoNoAleatorio();
                }
                VerificarVisao();
                break;
        }

        AtualizarAudio(
            estadoAtual == EstadoIA.Perseguicao,
            !_agente.isStopped && _agente.velocity.sqrMagnitude > 0.04f);
    }

    bool PatrulharComNavMesh()
    {
        if (todosOsNosDoMapa == null || todosOsNosDoMapa.Count == 0) return false;
        if (noAlvoAtual == null) EscolherNovoNoAleatorio();
        if (noAlvoAtual == null || !TentarDefinirDestino(noAlvoAtual.position)) return false;

        if (!_agente.pathPending && _agente.remainingDistance < 0.5f)
        {
            EscolherNovoNoAleatorio();
        }
        return true;
    }

    bool TentarDefinirDestino(Vector3 destino)
    {
        if (PortaFechadaNoCaminho(destino))
        {
            if (!_rotaBloqueadaPorPorta)
                _agente.ResetPath();

            _rotaBloqueadaPorPorta = true;
            _agente.isStopped = true;
            return false;
        }

        if (!NavMesh.SamplePosition(
            destino,
            out NavMeshHit pontoSeguro,
            Mathf.Max(raioAgenteSeguro * 2f, 1f),
            _agente.areaMask))
        {
            _agente.ResetPath();
            _agente.isStopped = true;
            return false;
        }

        _rotaBloqueadaPorPorta = false;
        _agente.isStopped = false;
        return _agente.SetDestination(pontoSeguro.position);
    }

    bool PortaFechadaNoCaminho(Vector3 destino)
    {
        return PortaBloqueiaDestino(botaoPortaEsquerda, destino)
            || PortaBloqueiaDestino(botaoPortaDireita, destino);
    }

    bool PortaBloqueiaDestino(ControleBotoes botao, Vector3 destino)
    {
        if (botao == null || !botao.PortaFechada || botao.porta == null)
            return false;

        Collider2D colliderPorta = botao.porta.GetComponent<Collider2D>();
        if (colliderPorta == null || !colliderPorta.enabled)
            return false;

        int quantidadeImpactos = Physics2D.Linecast(
            transform.position,
            destino,
            filtroPortas,
            impactosRaycast);
        for (int i = 0; i < quantidadeImpactos; i++)
        {
            Collider2D collider = impactosRaycast[i].collider;
            if (collider != null && (collider == colliderPorta
                || collider.transform.IsChildOf(botao.porta.transform)))
                return true;
        }

        return false;
    }

    void EscolherNovoNoAleatorio()
    {
        if (todosOsNosDoMapa == null || todosOsNosDoMapa.Count == 0)
        {
            noAlvoAtual = null;
            return;
        }

        int inicio = Random.Range(0, todosOsNosDoMapa.Count);
        for (int i = 0; i < todosOsNosDoMapa.Count; i++)
        {
            Transform candidato = todosOsNosDoMapa[(inicio + i) % todosOsNosDoMapa.Count];
            if (candidato != null)
            {
                noAlvoAtual = candidato;
                return;
            }
        }

        noAlvoAtual = null;
    }

    void VerificarVisao()
    {
        if (estadoAtual != EstadoIA.Perseguicao && JogadorVisivel())
        {
            estadoAtual = EstadoIA.Perseguicao;
            if (GameManager.Instance != null)
                GameManager.Instance.TocarAlertaAnimatronico(gameObject);
        }
    }

    bool JogadorVisivel()
    {
        if (alvoPlayer == null || PortaFechadaNoCaminho(alvoPlayer.position))
            return false;

        if (_collidersPlayer == null || _collidersPlayer.Length == 0)
            _collidersPlayer = alvoPlayer.GetComponentsInChildren<Collider2D>();

        Vector2 origem = transform.position + Vector3.up * 0.25f;
        foreach (Collider2D colliderPlayer in _collidersPlayer)
        {
            if (colliderPlayer == null || !colliderPlayer.enabled || !colliderPlayer.gameObject.activeInHierarchy)
                continue;

            Vector2 pontoAlvo = colliderPlayer.ClosestPoint(origem);
            Vector2 direcao = pontoAlvo - origem;
            float distancia = direcao.magnitude;
            if (distancia > distanciaDeVisao)
                continue;
            if (distancia <= 0.001f)
                return true;

            int quantidadeImpactos = Physics2D.Raycast(
                origem,
                direcao / distancia,
                filtroVisao,
                impactosRaycast,
                distancia + 0.02f);
            bool jogadorAtingido = false;
            bool bloqueado = false;

            for (int i = 0; i < quantidadeImpactos; i++)
            {
                Collider2D collider = impactosRaycast[i].collider;
                if (collider == null || FazParteDoAnimatronico(collider))
                    continue;

                if (FazParteDoJogador(collider))
                    jogadorAtingido = true;
                else if (impactosRaycast[i].distance < distancia - 0.03f)
                    bloqueado = true;
            }

            if (jogadorAtingido && !bloqueado)
                return true;
        }

        return false;
    }

    bool FazParteDoAnimatronico(Collider2D collider)
    {
        return collider.transform == transform || collider.transform.IsChildOf(transform);
    }

    bool FazParteDoJogador(Collider2D collider)
    {
        return collider.transform == alvoPlayer || collider.transform.IsChildOf(alvoPlayer);
    }

    static ContactFilter2D CriarFiltroVisao()
    {
        ContactFilter2D filtro = new ContactFilter2D();
        filtro.useTriggers = false;
        filtro.useLayerMask = false;
        filtro.useDepth = false;
        return filtro;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        VerificarMortePlayer(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        VerificarMortePlayer(collision.gameObject);
    }

    void VerificarMortePlayer(GameObject obj)
    {
        playerController player = obj.GetComponentInParent<playerController>();
        if (player == null && !obj.CompareTag("Player") && obj.name != "Player")
            return;

        Transform alvo = player != null ? player.transform : alvoPlayer;
        if (alvo == null || Vector2.Distance(transform.position, alvo.position) > distanciaAtaque)
            return;

        if (_rotaBloqueadaPorPorta || PortaFechadaNoCaminho(alvo.position))
            return;

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager não encontrado; não foi possível iniciar o jumpscare.", this);
            return;
        }

        if (GameManager.Instance.IniciarJumpscare(this))
            PausarParaJumpscare();
    }

    public void PausarParaJumpscare()
    {
        jogoAcabou = true;
        PararAudio();
        if (_agente != null && _agente.isActiveAndEnabled && _agente.isOnNavMesh)
        {
            _agente.isStopped = true;
            _agente.velocity = Vector3.zero;
        }
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
}